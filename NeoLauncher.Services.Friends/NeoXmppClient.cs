using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace NeoLauncher.Services.Friends;

internal sealed class NeoXmppClient : IAsyncDisposable
{
	private static readonly TimeSpan HandshakeTimeout = TimeSpan.FromSeconds(15L);

	private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

	private static readonly XmlReaderSettings FragmentSettings = new XmlReaderSettings
	{
		Async = false,
		ConformanceLevel = ConformanceLevel.Fragment
	};

	private readonly ClientWebSocket _webSocket = new ClientWebSocket();

	private readonly Uri _serverUri;

	private readonly string _domain;

	private readonly string _jid;

	private readonly string _accountId;

	private readonly string _resource;

	private readonly SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1);

	private readonly CancellationTokenSource _cts = new CancellationTokenSource();

	private Task? _receiveLoop;

	private int _iqCounter;

	public string BoundJid { get; private set; } = string.Empty;

	public string LastInboundXml { get; private set; } = string.Empty;

	public string LastOutboundXml { get; private set; } = string.Empty;

	public string LastError { get; private set; } = string.Empty;

	public bool IsConnected => _webSocket.State == WebSocketState.Open;

	private event Func<string, Task>? RawStanzaReceived;

	public event Func<NeoXmppPresenceStanza, Task>? PresenceReceived;

	public event Func<NeoXmppMessageStanza, Task>? MessageReceived;

	public event EventHandler? Disconnected;

	public NeoXmppClient(Uri serverUri, string domain, string accountId, string resource = "launcher")
	{
		_serverUri = serverUri;
		_domain = domain;
		_accountId = accountId;
		_resource = (string.IsNullOrWhiteSpace(resource) ? "launcher" : resource);
		_jid = accountId + "@" + domain;
		_webSocket.Options.AddSubProtocol("xmpp");
	}

	public async Task ConnectAsync(string token, CancellationToken cancellationToken = default(CancellationToken))
	{
		await _webSocket.ConnectAsync(_serverUri, cancellationToken);
		_receiveLoop = ReceiveLoopAsync(_cts.Token);
		await OpenStreamAsync(cancellationToken);
		await AuthenticateAsync(token, cancellationToken);
		await OpenStreamAsync(cancellationToken);
		BoundJid = await BindAsync(_resource, cancellationToken);
		await EstablishSessionAsync(cancellationToken);
	}

	public Task SendAvailablePresenceAsync(string statusJson, int priority = 0, CancellationToken cancellationToken = default(CancellationToken))
	{
		return SendAsync((XmlWriter writer) =>
		{
			writer.WriteStartElement("presence");
			writer.WriteElementString("status", statusJson ?? string.Empty);
			writer.WriteElementString("priority", priority.ToString(CultureInfo.InvariantCulture));
			writer.WriteEndElement();
		}, cancellationToken);
	}

	public Task SendUnavailablePresenceAsync(CancellationToken cancellationToken = default(CancellationToken))
	{
		return SendAsync((XmlWriter writer) =>
		{
			writer.WriteStartElement("presence");
			writer.WriteAttributeString("type", "unavailable");
			writer.WriteEndElement();
		}, cancellationToken);
	}

	public Task SendChatMessageAsync(string toAccountId, string body, string? id = null, CancellationToken cancellationToken = default(CancellationToken))
	{
		return SendAsync((XmlWriter writer) =>
		{
			writer.WriteStartElement("message");
			writer.WriteAttributeString("to", toAccountId + "@" + _domain);
			writer.WriteAttributeString("type", "chat");
			if (!string.IsNullOrWhiteSpace(id))
			{
				writer.WriteAttributeString("id", id);
			}
			writer.WriteElementString("body", body ?? string.Empty);
			writer.WriteEndElement();
		}, cancellationToken);
	}

	public Task RequestRosterAsync(CancellationToken cancellationToken = default(CancellationToken))
	{
		string id = $"neo_launcher_roster_{Interlocked.Increment(ref _iqCounter)}";
		return SendAsync((XmlWriter writer) =>
		{
			writer.WriteStartElement("iq");
			writer.WriteAttributeString("type", "get");
			writer.WriteAttributeString("id", id);
			writer.WriteStartElement(null, "query", "jabber:iq:roster");
			writer.WriteEndElement();
			writer.WriteEndElement();
		}, cancellationToken);
	}

	private async Task OpenStreamAsync(CancellationToken cancellationToken)
	{
		TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		RawStanzaReceived += Handler;
		try
		{
			await SendAsync((XmlWriter writer) =>
			{
				writer.WriteStartElement(null, "open", "urn:ietf:params:xml:ns:xmpp-framing");
				writer.WriteAttributeString("to", _domain);
				writer.WriteAttributeString("version", "1.0");
				writer.WriteEndElement();
			}, cancellationToken);
			await WaitAsync(tcs.Task, "XMPP stream open", cancellationToken);
		}
		finally
		{
			RawStanzaReceived -= Handler;
		}
		Task Handler(string xml)
		{
			try
			{
				using XmlReader xmlReader = XmlReader.Create(new StringReader(xml), FragmentSettings);
				while (xmlReader.Read())
				{
					if (xmlReader.NodeType == XmlNodeType.Element)
					{
						if (xmlReader.LocalName == "open" && xmlReader.NamespaceURI == "urn:ietf:params:xml:ns:xmpp-framing")
						{
							tcs.TrySetResult(result: true);
							break;
						}
						if (xmlReader.LocalName == "close" && xmlReader.NamespaceURI == "urn:ietf:params:xml:ns:xmpp-framing")
						{
							tcs.TrySetException(new InvalidOperationException("XMPP server closed the stream during open."));
							break;
						}
						if (xmlReader.LocalName == "error" && xmlReader.NamespaceURI == "http://etherx.jabber.org/streams")
						{
							tcs.TrySetException(new InvalidOperationException("XMPP stream open failed."));
							break;
						}
					}
				}
			}
			catch (Exception exception)
			{
				tcs.TrySetException(exception);
			}
			return Task.CompletedTask;
		}
	}

	private async Task AuthenticateAsync(string token, CancellationToken cancellationToken)
	{
		TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		string username = _accountId;
		RawStanzaReceived += Handler;
		try
		{
			await SendAsync((XmlWriter writer) =>
			{
				string text = Convert.ToBase64String(Utf8NoBom.GetBytes("\0" + username + "\0" + token));
				writer.WriteStartElement(null, "auth", "urn:ietf:params:xml:ns:xmpp-sasl");
				writer.WriteAttributeString("mechanism", "PLAIN");
				writer.WriteString(text);
				writer.WriteEndElement();
			}, cancellationToken);
			await WaitAsync(tcs.Task, "XMPP SASL authentication", cancellationToken);
		}
		finally
		{
			RawStanzaReceived -= Handler;
		}
		Task Handler(string xml)
		{
			try
			{
				using XmlReader xmlReader = XmlReader.Create(new StringReader(xml), FragmentSettings);
				while (xmlReader.Read())
				{
					if (xmlReader.NodeType == XmlNodeType.Element)
					{
						if (xmlReader.LocalName == "close" && xmlReader.NamespaceURI == "urn:ietf:params:xml:ns:xmpp-framing")
						{
							tcs.TrySetException(new InvalidOperationException("XMPP server closed the stream during SASL authentication."));
							break;
						}
						if (!(xmlReader.NamespaceURI != "urn:ietf:params:xml:ns:xmpp-sasl"))
						{
							if (xmlReader.LocalName == "success")
							{
								tcs.TrySetResult(result: true);
								break;
							}
							if (xmlReader.LocalName == "failure")
							{
								tcs.TrySetException(new InvalidOperationException("XMPP SASL authentication failed."));
								break;
							}
						}
					}
				}
			}
			catch (Exception exception)
			{
				tcs.TrySetException(exception);
			}
			return Task.CompletedTask;
		}
	}

	private async Task<string> BindAsync(string resource, CancellationToken cancellationToken)
	{
		string id = $"neo_launcher_bind_{Interlocked.Increment(ref _iqCounter)}";
		TaskCompletionSource<string> tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
		RawStanzaReceived += Handler;
		try
		{
			await SendAsync((XmlWriter writer) =>
			{
				writer.WriteStartElement("iq");
				writer.WriteAttributeString("type", "set");
				writer.WriteAttributeString("id", id);
				writer.WriteStartElement(null, "bind", "urn:ietf:params:xml:ns:xmpp-bind");
				writer.WriteElementString("resource", resource);
				writer.WriteEndElement();
				writer.WriteEndElement();
			}, cancellationToken);
			return await WaitAsync(tcs.Task, "XMPP bind", cancellationToken);
		}
		finally
		{
			RawStanzaReceived -= Handler;
		}
		Task Handler(string xml)
		{
			try
			{
				using XmlReader xmlReader = XmlReader.Create(new StringReader(xml), FragmentSettings);
				xmlReader.MoveToContent();
				if (xmlReader.LocalName != "iq" || xmlReader.GetAttribute("id") != id)
				{
					return Task.CompletedTask;
				}
				if (xmlReader.GetAttribute("type") == "error")
				{
					tcs.TrySetException(new InvalidOperationException("XMPP bind failed."));
					return Task.CompletedTask;
				}
				while (xmlReader.Read())
				{
					if (xmlReader.NodeType == XmlNodeType.Element && xmlReader.LocalName == "jid")
					{
						tcs.TrySetResult(xmlReader.ReadElementContentAsString());
						break;
					}
				}
			}
			catch (Exception exception)
			{
				tcs.TrySetException(exception);
			}
			return Task.CompletedTask;
		}
	}

	private async Task EstablishSessionAsync(CancellationToken cancellationToken)
	{
		string id = $"neo_launcher_session_{Interlocked.Increment(ref _iqCounter)}";
		TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		RawStanzaReceived += Handler;
		try
		{
			await SendAsync((XmlWriter writer) =>
			{
				writer.WriteStartElement("iq");
				writer.WriteAttributeString("type", "set");
				writer.WriteAttributeString("id", id);
				writer.WriteStartElement(null, "session", "urn:ietf:params:xml:ns:xmpp-session");
				writer.WriteEndElement();
				writer.WriteEndElement();
			}, cancellationToken);
			await WaitAsync(tcs.Task, "XMPP session", cancellationToken);
		}
		finally
		{
			RawStanzaReceived -= Handler;
		}
		Task Handler(string xml)
		{
			try
			{
				using XmlReader xmlReader = XmlReader.Create(new StringReader(xml), FragmentSettings);
				xmlReader.MoveToContent();
				if (xmlReader.LocalName == "iq" && xmlReader.GetAttribute("id") == id)
				{
					string attribute = xmlReader.GetAttribute("type");
					if (attribute == "result")
					{
						tcs.TrySetResult(result: true);
					}
					else if (attribute == "error")
					{
						tcs.TrySetException(new InvalidOperationException("XMPP session establishment failed."));
					}
				}
			}
			catch (Exception exception)
			{
				tcs.TrySetException(exception);
			}
			return Task.CompletedTask;
		}
	}

	private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
	{
		ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[8192]);
		using MemoryStream ms = new MemoryStream();
		_ = 3;
		try
		{
			while (_webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
			{
				ms.SetLength(0L);
				WebSocketReceiveResult webSocketReceiveResult;
				do
				{
					webSocketReceiveResult = await _webSocket.ReceiveAsync(buffer, cancellationToken);
					if (webSocketReceiveResult.MessageType == WebSocketMessageType.Close)
					{
						return;
					}
					ms.Write(buffer.Array, 0, webSocketReceiveResult.Count);
				}
				while (!webSocketReceiveResult.EndOfMessage);
				string xml = Encoding.UTF8.GetString(ms.ToArray());
				if (string.IsNullOrWhiteSpace(xml))
				{
					continue;
				}
				LastInboundXml = xml;
				Func<string, Task> func = RawStanzaReceived;
				if (func != null)
				{
					foreach (Func<string, Task> item in func.GetInvocationList().Cast<Func<string, Task>>())
					{
						try
						{
							await item(xml);
						}
						catch
						{
						}
					}
				}
				await TryDispatchPresenceAsync(xml);
				await TryDispatchMessageAsync(xml);
			}
		}
		catch (OperationCanceledException)
		{
		}
		catch (WebSocketException ex2)
		{
			LastError = ex2.Message;
		}
		catch (Exception ex3)
		{
			LastError = ex3.Message;
		}
		finally
		{
			if (!cancellationToken.IsCancellationRequested)
			{
				Disconnected?.Invoke(this, EventArgs.Empty);
			}
		}
	}

	private async Task TryDispatchPresenceAsync(string xml)
	{
		try
		{
			using XmlReader reader = XmlReader.Create(new StringReader(xml), FragmentSettings);
			while (reader.Read())
			{
				if (reader.NodeType != XmlNodeType.Element || reader.LocalName != "presence")
				{
					continue;
				}
				string text = reader.GetAttribute("from") ?? string.Empty;
				if (string.IsNullOrWhiteSpace(text))
				{
					continue;
				}
				int num = text.IndexOf('/');
				string text2 = ((num >= 0) ? text.Substring(0, num) : text);
				int num2 = text2.IndexOf('@');
				string accountId = ((num2 >= 0) ? text2.Substring(0, num2) : text2);
				string text3;
				if (num < 0)
				{
					text3 = string.Empty;
				}
				else
				{
					string text4 = text;
					int num3 = num + 1;
					text3 = text4.Substring(num3, text4.Length - num3);
				}
				string resource = text3;
				string type = reader.GetAttribute("type") ?? "available";
				string statusJson = string.Empty;
				int result = 0;
				if (!reader.IsEmptyElement)
				{
					int depth = reader.Depth;
					while (reader.Read() && (reader.NodeType != XmlNodeType.EndElement || reader.Depth != depth || !(reader.LocalName == "presence")))
					{
						if (reader.NodeType == XmlNodeType.Element)
						{
							if (reader.LocalName == "status")
							{
								statusJson = reader.ReadElementContentAsString();
							}
							else if (reader.LocalName == "priority")
							{
								int.TryParse(reader.ReadElementContentAsString(), out result);
							}
						}
					}
				}
				Func<NeoXmppPresenceStanza, Task> func = PresenceReceived;
				if (func == null)
				{
					continue;
				}
				NeoXmppPresenceStanza stanza = new NeoXmppPresenceStanza(accountId, resource, type, statusJson, result, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
				foreach (Func<NeoXmppPresenceStanza, Task> item in func.GetInvocationList().Cast<Func<NeoXmppPresenceStanza, Task>>())
				{
					try
					{
						await item(stanza);
					}
					catch
					{
					}
				}
			}
		}
		catch (Exception ex)
		{
			LastError = ex.Message;
		}
	}

	private async Task TryDispatchMessageAsync(string xml)
	{
		try
		{
			using XmlReader reader = XmlReader.Create(new StringReader(xml), FragmentSettings);
			while (reader.Read())
			{
				if (reader.NodeType != XmlNodeType.Element || reader.LocalName != "message")
				{
					continue;
				}
				string text = reader.GetAttribute("from") ?? string.Empty;
				string type = reader.GetAttribute("type") ?? "normal";
				string id = reader.GetAttribute("id") ?? string.Empty;
				string text2 = string.Empty;
				if (!reader.IsEmptyElement)
				{
					int depth = reader.Depth;
					while (reader.Read() && (reader.NodeType != XmlNodeType.EndElement || reader.Depth != depth || !(reader.LocalName == "message")))
					{
						if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "body")
						{
							text2 = reader.ReadElementContentAsString();
						}
					}
				}
				if (string.IsNullOrWhiteSpace(text2))
				{
					continue;
				}
				Func<NeoXmppMessageStanza, Task> func = MessageReceived;
				if (func == null)
				{
					continue;
				}
				NeoXmppMessageStanza stanza = new NeoXmppMessageStanza(text, type, text2, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), id);
				foreach (Func<NeoXmppMessageStanza, Task> item in func.GetInvocationList().Cast<Func<NeoXmppMessageStanza, Task>>())
				{
					try
					{
						await item(stanza);
					}
					catch
					{
					}
				}
			}
		}
		catch (Exception ex)
		{
			LastError = ex.Message;
		}
	}

	private async Task SendAsync(Action<XmlWriter> writeStanza, CancellationToken cancellationToken = default(CancellationToken))
	{
		byte[] payload;
		using (MemoryStream memoryStream = new MemoryStream())
		{
			using (XmlWriter obj = XmlWriter.Create(memoryStream, new XmlWriterSettings
			{
				Encoding = Utf8NoBom,
				OmitXmlDeclaration = true,
				CloseOutput = false
			}))
			{
				writeStanza(obj);
			}
			payload = memoryStream.ToArray();
		}
		LastOutboundXml = Utf8NoBom.GetString(payload);
		await _sendLock.WaitAsync(cancellationToken);
		try
		{
			await _webSocket.SendAsync(payload, WebSocketMessageType.Text, endOfMessage: true, cancellationToken);
		}
		finally
		{
			_sendLock.Release();
		}
	}

	private static async Task<T> WaitAsync<T>(Task<T> task, string phase, CancellationToken cancellationToken)
	{
		using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		timeout.CancelAfter(HandshakeTimeout);
		if (await Task.WhenAny(task, Task.Delay(Timeout.InfiniteTimeSpan, timeout.Token)) != task)
		{
			cancellationToken.ThrowIfCancellationRequested();
			throw new TimeoutException(phase + " timed out.");
		}
		return await task;
	}

	private static async Task WaitAsync(Task task, string phase, CancellationToken cancellationToken)
	{
		using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		timeout.CancelAfter(HandshakeTimeout);
		if (await Task.WhenAny(task, Task.Delay(Timeout.InfiniteTimeSpan, timeout.Token)) != task)
		{
			cancellationToken.ThrowIfCancellationRequested();
			throw new TimeoutException(phase + " timed out.");
		}
		await task;
	}

	public async ValueTask DisposeAsync()
	{
		if (!_cts.IsCancellationRequested)
		{
			await _cts.CancelAsync();
		}
		if (_receiveLoop != null)
		{
			await _receiveLoop.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
		}
		if (_webSocket.State == WebSocketState.Open)
		{
			try
			{
				await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "closing", CancellationToken.None);
			}
			catch
			{
			}
		}
		_webSocket.Dispose();
		_sendLock.Dispose();
		_cts.Dispose();
	}
}
