using System.Text.Json;

namespace NeoLauncher.Services.WebHost;

public sealed class NeoBridgeContext(NeoWebBridge bridge, JsonElement payload)
{
	public NeoWebBridge Bridge { get; } = bridge;

	public JsonElement Payload { get; } = payload;

	private bool HasObject => Payload.ValueKind == JsonValueKind.Object;

	public string? GetString(string name)
	{
		if (!HasObject || !Payload.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.String)
		{
			return null;
		}
		return value.GetString();
	}

	public double? GetNumber(string name)
	{
		if (!HasObject || !Payload.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.Number)
		{
			return null;
		}
		return value.GetDouble();
	}

	public bool GetBool(string name, bool fallback = false)
	{
		if (!HasObject || !Payload.TryGetProperty(name, out var value) || (value.ValueKind != JsonValueKind.True && value.ValueKind != JsonValueKind.False))
		{
			return fallback;
		}
		return value.GetBoolean();
	}

	public JsonElement? GetElement(string name)
	{
		if (!HasObject || !Payload.TryGetProperty(name, out var value))
		{
			return null;
		}
		return value;
	}

	public T? Deserialize<T>()
	{
		JsonValueKind valueKind = Payload.ValueKind;
		if ((valueKind != JsonValueKind.Undefined && valueKind != JsonValueKind.Null) || 1 == 0)
		{
			return Payload.Deserialize<T>(NeoWebBridge.JsonOptions);
		}
		return default;
	}
}
