using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using NeoLauncher.Services.WebHost;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.UI;

namespace NeoLauncher.Views;

public sealed class NativeFriendContextMenuView : UserControl
{
	private readonly Grid _root;

	private readonly Border _surface;

	private readonly StackPanel _stack;

	private JsonNode? _friend;

	private string _mode = "actions";

	private TextBox? _nicknameBox;

	private NeoEdgeOutlineStyle _edgeOutlineStyle;

	private const double ContentWidth = 224.0;

	private const double SurfacePaddingY = 32.0;

	private Dictionary<string, string> _labels = new Dictionary<string, string>();

	private static readonly Color NeoAccent = Color.FromArgb(byte.MaxValue, 54, 34, 34);

	private static readonly Color NeoAccentHover = Color.FromArgb(byte.MaxValue, 76, 48, 48);

	private static readonly Color EdgeColor = Color.FromArgb(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);

	private static readonly Color GlassFieldRest = Color.FromArgb(12, byte.MaxValue, byte.MaxValue, byte.MaxValue);

	private static readonly Color GlassFieldHover = Color.FromArgb(20, byte.MaxValue, byte.MaxValue, byte.MaxValue);

	private static readonly Color GlassFieldFocus = Color.FromArgb(26, byte.MaxValue, byte.MaxValue, byte.MaxValue);

	private static readonly Color GlassFieldEdge = Color.FromArgb(0, byte.MaxValue, byte.MaxValue, byte.MaxValue);

	private static readonly Color GlassFieldEdgeFocus = NeoAccent;

	private const string DefaultAvatarUrl = "https://fortnite-api.com/images/cosmetics/br/CID_001_Athena_Commando_F_Default/smallicon.png";

	private static readonly Dictionary<string, BitmapImage> AvatarCache = new Dictionary<string, BitmapImage>(StringComparer.OrdinalIgnoreCase);

	private const int AvatarCacheLimit = 64;

	public double LastRequestedHeight { get; private set; } = 328.0;

	private bool IsOnlineLike
	{
		get
		{
			if (IsBlocked)
			{
				return false;
			}
			if (GetBool("online", "isOnline", "available"))
			{
				return true;
			}
			string value = GetValue("gameStatus", "launcherActivity", "presence", "status");
			if (string.IsNullOrWhiteSpace(value))
			{
				return false;
			}
			if (!value.Contains("offline", StringComparison.OrdinalIgnoreCase))
			{
				return !value.Contains("blocked", StringComparison.OrdinalIgnoreCase);
			}
			return false;
		}
	}

	private Color ProfileStatusDotColor
	{
		get
		{
			if (IsBlocked)
			{
				return Color.FromArgb(byte.MaxValue, byte.MaxValue, 82, 96);
			}
			if (IsOnlineLike)
			{
				return Color.FromArgb(byte.MaxValue, 74, 229, 106);
			}
			return Color.FromArgb(byte.MaxValue, 138, 145, 160);
		}
	}

	private string AccountId => GetValue("accountId", "id");

	private string Nickname => GetValue("nickname");

	private string Username => GetValue("username", "displayName", "name");

	private string DisplayName
	{
		get
		{
			if (string.IsNullOrWhiteSpace(Nickname))
			{
				if (string.IsNullOrWhiteSpace(Username))
				{
					return L("unknown", "Unknown");
				}
				return Username;
			}
			return Nickname;
		}
	}

	private bool ForcedBlocked => GetBool("__nativeBlocked", "blocked", "isBlocked");

	private bool HasNickname
	{
		get
		{
			if (!string.IsNullOrWhiteSpace(Nickname) && !string.IsNullOrWhiteSpace(Username))
			{
				return !string.Equals(Nickname, Username, StringComparison.OrdinalIgnoreCase);
			}
			return false;
		}
	}

	private bool IsBlocked
	{
		get
		{
			if (ForcedBlocked)
			{
				return true;
			}
			if (GetValue("status", "relationship", "bucket", "relationshipBucket", "section", "group", "list").Contains("blocked", StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
			return false;
		}
	}

	private bool IsPartyInvite
	{
		get
		{
			if (!IsBlocked)
			{
				return GetBool("__nativePartyInvite", "__partyInvite");
			}
			return false;
		}
	}

	private string PartyInviteBuildName => GetValue("__partyInviteBuildName");

	private string PartyInviteBuildId => GetValue("__partyInviteBuildId");

	private string PartyInvitePartyId => GetValue("__partyInvitePartyId");

	private bool IsIncomingRequest
	{
		get
		{
			if (IsBlocked)
			{
				return false;
			}
			if (GetBool("__nativeIncomingRequest", "__incomingRequestPanel", "incoming"))
			{
				return true;
			}
			string value = GetValue("relationship", "status", "bucket", "relationshipBucket", "section", "group", "list", "gameStatus");
			if (!value.Contains("incoming", StringComparison.OrdinalIgnoreCase))
			{
				return value.Contains("request", StringComparison.OrdinalIgnoreCase);
			}
			return true;
		}
	}

	private char Initial
	{
		get
		{
			string text = DisplayName.Trim();
			if (text.Length <= 0)
			{
				return '?';
			}
			return char.ToUpperInvariant(text[0]);
		}
	}

	private string AvatarUrl => GetValue("avatarUrl", "avatar_url", "avatar", "picture");

	public event Action<double>? HeightRequested;

	public event Action? CloseRequested;

	public NativeFriendContextMenuView()
	{
		HorizontalAlignment = HorizontalAlignment.Stretch;
		VerticalAlignment = VerticalAlignment.Stretch;
		_root = new Grid
		{
			Background = CreateAcrylicBrush(),
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Stretch
		};
		_surface = new Border
		{
			CornerRadius = new CornerRadius(0.0),
			BorderThickness = new Thickness(0.0),
			BorderBrush = new SolidColorBrush(Color.FromArgb(0, byte.MaxValue, byte.MaxValue, byte.MaxValue)),
			Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)),
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Stretch,
			Margin = new Thickness(0.0),
			Padding = new Thickness(17.0, 16.0, 17.0, 16.0)
		};
		_stack = new StackPanel
		{
			Spacing = 0.0,
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Stretch
		};
		_surface.Child = _stack;
		_root.Children.Add(_surface);
		Content = _root;
		Loaded += (object _, RoutedEventArgs _) =>
		{
			Render();
		};
	}

	public void SetEdgeOutlineStyle(string? style)
	{
		_edgeOutlineStyle = (string.Equals(style, "fade", StringComparison.OrdinalIgnoreCase) ? NeoEdgeOutlineStyle.Fade : NeoEdgeOutlineStyle.Grow);
	}

	public void SetLabels(string? labelsJson)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		try
		{
			if (!string.IsNullOrWhiteSpace(labelsJson) && JsonNode.Parse(labelsJson) is JsonObject jsonObject)
			{
				foreach (KeyValuePair<string, JsonNode> item in jsonObject)
				{
					string text = item.Value?.GetValue<string>();
					if (text != null && !string.IsNullOrWhiteSpace(text))
					{
						dictionary[item.Key] = text;
					}
				}
			}
		}
		catch
		{
		}
		_labels = dictionary;
	}

	private string L(string key, string fallback)
	{
		if (!_labels.TryGetValue(key, out string value))
		{
			return fallback;
		}
		return value;
	}

	private string L(string key, string fallback, string slot, string value)
	{
		return L(key, fallback).Replace("{" + slot + "}", value, StringComparison.Ordinal);
	}

	public void SetFriend(JsonNode? friend)
	{
		_friend = friend?.DeepClone();
		_mode = "actions";
		Render();
	}

	public void ResetToActions()
	{
		_mode = "actions";
		Render();
	}

	private static Brush CreateAcrylicBrush()
	{
		return new SolidColorBrush(Color.FromArgb(24, byte.MaxValue, byte.MaxValue, byte.MaxValue));
	}

	private static Brush TextBrush(byte alpha = byte.MaxValue)
	{
		return new SolidColorBrush(Color.FromArgb(alpha, byte.MaxValue, byte.MaxValue, byte.MaxValue));
	}

	private static Brush MutedTextBrush(byte alpha = 92)
	{
		return new SolidColorBrush(Color.FromArgb(alpha, byte.MaxValue, byte.MaxValue, byte.MaxValue));
	}

	private static Brush DangerBrush()
	{
		return new SolidColorBrush(Color.FromArgb(byte.MaxValue, byte.MaxValue, 116, 128));
	}

	private static Brush GoodBrush()
	{
		return new SolidColorBrush(Color.FromArgb(byte.MaxValue, 116, byte.MaxValue, 152));
	}

	private static Brush InviteBrush()
	{
		return new SolidColorBrush(Color.FromArgb(byte.MaxValue, byte.MaxValue, 191, 0));
	}

	private static void ApplyGlassFieldTheme(TextBox box)
	{
		Set("TextControlBackground", new SolidColorBrush(GlassFieldRest));
		Set("TextControlBackgroundPointerOver", new SolidColorBrush(GlassFieldHover));
		Set("TextControlBackgroundFocused", new SolidColorBrush(GlassFieldFocus));
		Set("TextControlBackgroundDisabled", new SolidColorBrush(GlassFieldRest));
		Set("TextControlBorderBrush", new SolidColorBrush(GlassFieldEdge));
		Set("TextControlBorderBrushPointerOver", new SolidColorBrush(GlassFieldEdge));
		Set("TextControlBorderBrushFocused", new SolidColorBrush(GlassFieldEdgeFocus));
		Set("TextControlBorderBrushDisabled", new SolidColorBrush(GlassFieldEdge));
		Set("TextControlBorderThemeThickness", new Thickness(1.0));
		Set("TextControlBorderThemeThicknessFocused", new Thickness(1.0));
		Set("TextControlForeground", TextBrush());
		Set("TextControlForegroundPointerOver", TextBrush());
		Set("TextControlForegroundFocused", TextBrush());
		Set("TextControlPlaceholderForeground", MutedTextBrush(96));
		Set("TextControlPlaceholderForegroundPointerOver", MutedTextBrush(112));
		Set("TextControlPlaceholderForegroundFocused", MutedTextBrush(120));
		Set("TextControlSelectionHighlightColor", new SolidColorBrush(Color.FromArgb(byte.MaxValue, 159, 99, byte.MaxValue)));
		void Set(string key, object value)
		{
			box.Resources[key] = value;
		}
	}

	private string GetValue(params string[] keys)
	{
		if (!(_friend is JsonObject jsonObject))
		{
			return string.Empty;
		}
		foreach (string propertyName in keys)
		{
			if (!jsonObject.TryGetPropertyValue(propertyName, out JsonNode jsonNode) || jsonNode == null)
			{
				continue;
			}
			if (jsonNode is JsonValue jsonValue)
			{
				if (jsonValue.TryGetValue<string>(out string value) && !string.IsNullOrWhiteSpace(value))
				{
					return value;
				}
				if (jsonValue.TryGetValue<Guid>(out var value2))
				{
					return value2.ToString();
				}
				if (jsonValue.TryGetValue<int>(out var value3))
				{
					return value3.ToString();
				}
				if (jsonValue.TryGetValue<long>(out var value4))
				{
					return value4.ToString();
				}
			}
			string text = jsonNode.ToJsonString().Trim('"');
			if (!string.IsNullOrWhiteSpace(text))
			{
				return text;
			}
		}
		return string.Empty;
	}

	private bool GetBool(params string[] keys)
	{
		if (!(_friend is JsonObject jsonObject))
		{
			return false;
		}
		foreach (string propertyName in keys)
		{
			if (!jsonObject.TryGetPropertyValue(propertyName, out JsonNode jsonNode) || jsonNode == null || !(jsonNode is JsonValue jsonValue))
			{
				continue;
			}
			if (jsonValue.TryGetValue<bool>(out var value))
			{
				return value;
			}
			if (jsonValue.TryGetValue<string>(out string value2))
			{
				if (bool.TryParse(value2, out var result))
				{
					return result;
				}
				if (value2.Contains("online", StringComparison.OrdinalIgnoreCase) || value2.Contains("launcher", StringComparison.OrdinalIgnoreCase) || value2.Contains("game", StringComparison.OrdinalIgnoreCase) || value2.Contains("lobby", StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
			}
		}
		return false;
	}

	private Border CreateAvatar(double size, double fontSize, Brush background, Brush borderBrush, double borderThickness)
	{
		TextBlock textBlock = new TextBlock
		{
			Text = Initial.ToString(),
			Foreground = TextBrush(),
			FontSize = fontSize,
			FontWeight = FontWeights.Black,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		};
		Border border = new Border
		{
			CornerRadius = new CornerRadius(size / 2.0),
			Opacity = 0.0,
			IsHitTestVisible = false
		};
		Grid grid = new Grid();
		grid.Children.Add(textBlock);
		grid.Children.Add(border);
		Border result = new Border
		{
			Width = size,
			Height = size,
			CornerRadius = new CornerRadius(size / 2.0),
			BorderThickness = new Thickness(borderThickness),
			BorderBrush = borderBrush,
			Background = background,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			Child = grid
		};
		string url = (string.IsNullOrWhiteSpace(AvatarUrl) ? "https://fortnite-api.com/images/cosmetics/br/CID_001_Athena_Commando_F_Default/smallicon.png" : AvatarUrl);
		LoadAvatarInto(border, textBlock, url, size, allowFallback: true, allowRetry: true);
		return result;
	}

	private void LoadAvatarInto(Border picture, TextBlock initial, string url, double size, bool allowFallback, bool allowRetry)
	{
		if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out Uri result))
		{
			return;
		}
		if (AvatarCache.TryGetValue(url, out BitmapImage value))
		{
			Reveal(value);
			return;
		}
		BitmapImage bitmap = new BitmapImage
		{
			DecodePixelWidth = (int)Math.Ceiling(size * 3.0)
		};
		bitmap.ImageOpened += (object _, RoutedEventArgs _) =>
		{
			if (AvatarCache.Count >= 64)
			{
				AvatarCache.Clear();
			}
			AvatarCache[url] = bitmap;
			Reveal(bitmap);
		};
		bitmap.ImageFailed += (object _, ExceptionRoutedEventArgs _) =>
		{
			picture.Opacity = 0.0;
			picture.Background = null;
			initial.Visibility = Visibility.Visible;
			if (allowRetry)
			{
				RunAfter(450, () =>
				{
					LoadAvatarInto(picture, initial, url, size, allowFallback, allowRetry: false);
				});
			}
			else if (allowFallback && !string.Equals(url, "https://fortnite-api.com/images/cosmetics/br/CID_001_Athena_Commando_F_Default/smallicon.png", StringComparison.OrdinalIgnoreCase))
			{
				LoadAvatarInto(picture, initial, "https://fortnite-api.com/images/cosmetics/br/CID_001_Athena_Commando_F_Default/smallicon.png", size, allowFallback: false, allowRetry: true);
			}
		};
		picture.Background = new ImageBrush
		{
			ImageSource = bitmap,
			Stretch = Stretch.UniformToFill
		};
		bitmap.UriSource = result;
		void Reveal(BitmapImage source)
		{
			picture.Background = new ImageBrush
			{
				ImageSource = source,
				Stretch = Stretch.UniformToFill
			};
			picture.Opacity = 1.0;
			initial.Visibility = Visibility.Collapsed;
		}
	}

	private void RunAfter(int milliseconds, Action action)
	{
		try
		{
			DispatcherQueueTimer dispatcherQueueTimer = DispatcherQueue.CreateTimer();
			dispatcherQueueTimer.Interval = TimeSpan.FromMilliseconds(milliseconds);
			dispatcherQueueTimer.IsRepeating = false;
			dispatcherQueueTimer.Tick += (DispatcherQueueTimer sender, object _) =>
			{
				sender.Stop();
				action();
			};
			dispatcherQueueTimer.Start();
		}
		catch
		{
		}
	}

	private void Render()
	{
		if ((object)_stack == null)
		{
			return;
		}
		_stack.Children.Clear();
		if (_friend == null)
		{
			RequestHeight(92.0);
			AnimateMenu();
			return;
		}
		if (_mode == "profile")
		{
			RenderProfile();
			RequestHeight(318.0);
			AnimateMenu();
			return;
		}
		if (IsPartyInvite && _mode == "actions")
		{
			AddHeader(string.IsNullOrWhiteSpace(PartyInviteBuildName) ? L("invitedYouToPlay", "Invited you to play") : L("invitedYouBuild", "Invited you · {build}", "build", PartyInviteBuildName));
			RenderInviteActions();
			RequestMeasuredHeight(250.0);
			AnimateMenu();
			return;
		}
		AddHeader();
		if (IsBlocked && _mode == "actions")
		{
			AddMenuButton(L("viewProfile", "View Profile"), () =>
			{
				_mode = "profile";
				Render();
			});
			AddDivider();
			AddMenuButton(L("unblock", "Unblock"), async () =>
			{
				await RunActionAsync("unblock");
			}, "good");
			RequestMeasuredHeight(196.0);
			AnimateMenu();
			return;
		}
		switch (_mode)
		{
		case "nickname":
			RenderNickname();
			RequestMeasuredHeight(266.0);
			break;
		case "remove":
			RenderConfirm(L("removeTitle", "Remove {name} as a friend?", "name", DisplayName), L("remove", "Remove"), async () =>
			{
				await RunActionAsync("remove");
			});
			RequestMeasuredHeight(224.0);
			break;
		case "block":
			RenderConfirm(L("blockTitle", "Block {name}?", "name", DisplayName), L("block", "Block"), async () =>
			{
				await RunActionAsync("block");
			}, L("blockCaption", "They will be removed from your friends list."));
			RequestMeasuredHeight(232.0);
			break;
		case "report":
			RenderReport();
			RequestMeasuredHeight(232.0);
			break;
		case "reportThanks":
			AddTitle(L("reportSentTitle", "Report sent"));
			AddCaption(L("reportSentBody", "Thanks. We will review this report if moderation tools are connected."));
			AddSpacer(14.0);
			AddMenuButton(L("done", "Done"), () =>
			{
				CloseRequested?.Invoke();
			});
			RequestMeasuredHeight(176.0);
			break;
		default:
			RenderActions();
			RequestMeasuredHeight(IsIncomingRequest ? 214 : 328);
			break;
		}
		AnimateMenu();
	}

	private Border AttachHoverEdge(Border host, double radius)
	{
		UIElement child = host.Child;
		Grid grid = new Grid();
		if ((object)child != null)
		{
			host.Child = null;
			grid.Children.Add(child);
		}
		NeoEdgeGrowOutline outline = new NeoEdgeGrowOutline(EdgeColor, radius, 1.5, _edgeOutlineStyle);
		grid.Children.Add(outline);
		host.Child = grid;
		host.PointerEntered += (object _, PointerRoutedEventArgs _) =>
		{
			outline.SetHovered(hovered: true);
		};
		host.PointerExited += (object _, PointerRoutedEventArgs _) =>
		{
			outline.SetHovered(hovered: false);
		};
		host.PointerPressed += (object _, PointerRoutedEventArgs _) =>
		{
			outline.SetPressed(pressed: true);
		};
		host.PointerReleased += (object _, PointerRoutedEventArgs _) =>
		{
			outline.SetPressed(pressed: false);
		};
		host.PointerCaptureLost += (object _, PointerRoutedEventArgs _) =>
		{
			outline.SetPressed(pressed: false);
		};
		return host;
	}

	private void RequestHeight(double height)
	{
		LastRequestedHeight = height;
		HeightRequested?.Invoke(height);
	}

	private void RequestMeasuredHeight(double fallback)
	{
		double height = fallback;
		try
		{
			_stack.Measure(new Size(224.0, double.PositiveInfinity));
			double height2 = _stack.DesiredSize.Height;
			if (height2 > 1.0)
			{
				height = Math.Ceiling(height2) + 32.0;
			}
		}
		catch
		{
			height = fallback;
		}
		RequestHeight(height);
	}

	private void AddHeader(string? subtitle = null)
	{
		Grid grid = new Grid
		{
			Margin = new Thickness(0.0, 0.0, 0.0, 12.0),
			ColumnSpacing = 9.0
		};
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(34.0)
		});
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Star)
		});
		Border border = CreateAvatar(34.0, 15.0, new SolidColorBrush(Color.FromArgb(58, 80, 86, 108)), new SolidColorBrush(Color.FromArgb(46, byte.MaxValue, byte.MaxValue, byte.MaxValue)), 1.0);
		StackPanel stackPanel = new StackPanel
		{
			VerticalAlignment = VerticalAlignment.Center,
			Spacing = 2.0
		};
		StackPanel stackPanel2 = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 6.0,
			VerticalAlignment = VerticalAlignment.Center
		};
		stackPanel2.Children.Add(new TextBlock
		{
			Text = DisplayName,
			Foreground = TextBrush(),
			FontSize = 13.0,
			FontWeight = FontWeights.Bold,
			TextTrimming = TextTrimming.CharacterEllipsis,
			MaxLines = 1,
			MaxWidth = (HasNickname ? 108 : 150)
		});
		if (HasNickname)
		{
			stackPanel2.Children.Add(new TextBlock
			{
				Text = Username,
				Foreground = MutedTextBrush(92),
				FontSize = 9.0,
				FontWeight = FontWeights.SemiBold,
				TextTrimming = TextTrimming.CharacterEllipsis,
				MaxLines = 1,
				MaxWidth = 70.0,
				VerticalAlignment = VerticalAlignment.Bottom,
				Margin = new Thickness(0.0, 0.0, 0.0, 1.0)
			});
		}
		stackPanel.Children.Add(stackPanel2);
		if (!string.IsNullOrWhiteSpace(subtitle))
		{
			stackPanel.Children.Add(new TextBlock
			{
				Text = subtitle,
				Foreground = InviteBrush(),
				FontSize = 10.0,
				FontWeight = FontWeights.Bold,
				TextTrimming = TextTrimming.CharacterEllipsis,
				MaxLines = 1
			});
		}
		else if (!string.IsNullOrWhiteSpace(AccountId))
		{
			stackPanel.Children.Add(CreateAccountIdButton(AccountId, TextAlignment.Left, HorizontalAlignment.Left, 9.0, 76, new Thickness(-4.0, 0.0, 0.0, -2.0)));
		}
		Grid.SetColumn(border, 0);
		Grid.SetColumn(stackPanel, 1);
		grid.Children.Add(border);
		grid.Children.Add(stackPanel);
		_stack.Children.Add(grid);
	}

	private void RenderActions()
	{
		AddMenuButton(L("viewProfile", "View Profile"), () =>
		{
			_mode = "profile";
			Render();
		});
		if (IsIncomingRequest)
		{
			AddDivider();
			AddMenuButton(L("block", "Block"), () =>
			{
				_mode = "block";
				Render();
			}, "danger");
			AddMenuButton(L("report", "Report"), () =>
			{
				_mode = "report";
				Render();
			}, "danger");
			return;
		}
		AddMenuButton(L("sendMessage", "Send Message"), OpenMessageThread);
		AddMenuButton(string.IsNullOrWhiteSpace(Nickname) ? L("addNickname", "Add Nickname") : L("changeNickname", "Change Nickname"), () =>
		{
			_mode = "nickname";
			Render();
		});
		AddDivider();
		AddMenuButton(L("removeFriend", "Remove Friend"), () =>
		{
			_mode = "remove";
			Render();
		}, "danger");
		AddMenuButton(L("block", "Block"), () =>
		{
			_mode = "block";
			Render();
		}, "danger");
		AddMenuButton(L("report", "Report"), () =>
		{
			_mode = "report";
			Render();
		}, "danger");
	}

	private void RenderInviteActions()
	{
		AddMenuButton(L("acceptInvite", "Accept Invite"), async () =>
		{
			await RespondToInviteAsync("accept");
		}, "invite");
		AddMenuButton(L("rejectInvite", "Reject Invite"), async () =>
		{
			await RespondToInviteAsync("reject");
		});
		AddDivider();
		AddMenuButton(L("viewProfile", "View Profile"), () =>
		{
			_mode = "profile";
			Render();
		});
		AddMenuButton(L("sendMessage", "Send Message"), OpenMessageThread);
	}

	private void RenderNickname()
	{
		AddBackButton();
		AddTitle(string.IsNullOrWhiteSpace(Nickname) ? L("addNickname", "Add Nickname") : L("changeNickname", "Change Nickname"));
		_nicknameBox = new TextBox
		{
			Text = Nickname,
			PlaceholderText = L("nicknamePlaceholder", "Nickname"),
			MaxLength = 24,
			Height = 42.0,
			MinHeight = 0.0,
			Margin = new Thickness(0.0, 4.0, 0.0, 14.0),
			FontSize = 13.0,
			VerticalAlignment = VerticalAlignment.Center,
			VerticalContentAlignment = VerticalAlignment.Center,
			FontWeight = FontWeights.SemiBold,
			Foreground = TextBrush(),
			Background = new SolidColorBrush(GlassFieldRest),
			BorderBrush = new SolidColorBrush(GlassFieldEdge),
			BorderThickness = new Thickness(1.0),
			CornerRadius = new CornerRadius(10.0),
			Padding = new Thickness(10.0, 9.0, 10.0, 0.0)
		};
		ApplyGlassFieldTheme(_nicknameBox);
		_stack.Children.Add(_nicknameBox);
		Grid grid = new Grid
		{
			ColumnSpacing = 8.0,
			Margin = new Thickness(0.0)
		};
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Star)
		});
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Star)
		});
		Border border = CreateSmallButton(L("cancel", "Cancel"), () =>
		{
			_mode = "actions";
			Render();
		});
		Border border2 = CreateSmallButton(L("save", "Save"), (Action)SaveNickname, "accent");
		Grid.SetColumn(border, 0);
		Grid.SetColumn(border2, 1);
		grid.Children.Add(border);
		grid.Children.Add(border2);
		_stack.Children.Add(grid);
		_nicknameBox.Focus(FocusState.Programmatic);
		_nicknameBox.Select(_nicknameBox.Text.Length, 0);
	}

	private void RenderConfirm(string title, string confirmLabel, Func<Task> confirmAction, string? caption = null)
	{
		AddBackButton();
		AddTitle(title, 16.0);
		if (!string.IsNullOrWhiteSpace(caption))
		{
			AddCaption(caption);
		}
		AddSpacer(10.0);
		Grid grid = new Grid
		{
			ColumnSpacing = 8.0,
			Margin = new Thickness(0.0)
		};
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Star)
		});
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Star)
		});
		Border border = CreateSmallButton(L("no", "No"), () =>
		{
			_mode = "actions";
			Render();
		});
		Border border2 = CreateSmallButton(confirmLabel, async () =>
		{
			await confirmAction();
		}, "danger");
		Grid.SetColumn(border, 0);
		Grid.SetColumn(border2, 1);
		grid.Children.Add(border);
		grid.Children.Add(border2);
		_stack.Children.Add(grid);
	}

	private void RenderReport()
	{
		AddBackButton();
		AddTitle(L("reportTitle", "Report {name}?", "name", DisplayName));
		AddCaption(L("reportShell", "This only submits the launcher-side report flow for now."));
		AddSpacer(10.0);
		Grid grid = new Grid
		{
			ColumnSpacing = 8.0,
			Margin = new Thickness(0.0)
		};
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Star)
		});
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Star)
		});
		Border border = CreateSmallButton(L("cancel", "Cancel"), () =>
		{
			_mode = "actions";
			Render();
		});
		Border border2 = CreateSmallButton(L("report", "Report"), () =>
		{
			_mode = "reportThanks";
			Render();
		}, "danger");
		Grid.SetColumn(border, 0);
		Grid.SetColumn(border2, 1);
		grid.Children.Add(border);
		grid.Children.Add(border2);
		_stack.Children.Add(grid);
	}

	private void RenderProfile()
	{
		Grid grid = new Grid
		{
			Height = 30.0,
			Margin = new Thickness(0.0, 0.0, 0.0, 6.0)
		};
		Border border = CreateMenuRow("‹  " + L("back", "Back"), () =>
		{
			_mode = "actions";
			Render();
		}, "normal", disabled: false, 28.0);
		border.Width = 88.0;
		border.HorizontalAlignment = HorizontalAlignment.Left;
		Border border2 = CreateIconButton("×", () =>
		{
			CloseRequested?.Invoke();
		});
		border2.HorizontalAlignment = HorizontalAlignment.Right;
		grid.Children.Add(border);
		grid.Children.Add(border2);
		_stack.Children.Add(grid);
		StackPanel stackPanel = new StackPanel
		{
			HorizontalAlignment = HorizontalAlignment.Center,
			Spacing = 8.0,
			Margin = new Thickness(0.0, 6.0, 0.0, 0.0)
		};
		Grid grid2 = new Grid
		{
			Width = 72.0,
			Height = 72.0,
			HorizontalAlignment = HorizontalAlignment.Center
		};
		Border item = CreateAvatar(64.0, 24.0, new SolidColorBrush(Color.FromArgb(byte.MaxValue, 78, 84, 104)), new SolidColorBrush(Color.FromArgb(54, byte.MaxValue, byte.MaxValue, byte.MaxValue)), 1.0);
		Border item2 = new Border
		{
			Width = 15.0,
			Height = 15.0,
			CornerRadius = new CornerRadius(8.0),
			Background = new SolidColorBrush(ProfileStatusDotColor),
			BorderBrush = new SolidColorBrush(Color.FromArgb(byte.MaxValue, 66, 70, 76)),
			BorderThickness = new Thickness(2.0),
			HorizontalAlignment = HorizontalAlignment.Right,
			VerticalAlignment = VerticalAlignment.Bottom,
			Margin = new Thickness(0.0, 0.0, 7.0, 7.0)
		};
		grid2.Children.Add(item);
		grid2.Children.Add(item2);
		stackPanel.Children.Add(grid2);
		stackPanel.Children.Add(new TextBlock
		{
			Text = DisplayName,
			Foreground = TextBrush(),
			FontSize = 22.0,
			FontWeight = FontWeights.Black,
			TextAlignment = TextAlignment.Center,
			HorizontalAlignment = HorizontalAlignment.Center,
			TextTrimming = TextTrimming.CharacterEllipsis,
			MaxLines = 1
		});
		if (HasNickname)
		{
			stackPanel.Children.Add(new TextBlock
			{
				Text = Username,
				Foreground = MutedTextBrush(105),
				FontSize = 11.0,
				FontWeight = FontWeights.SemiBold,
				TextAlignment = TextAlignment.Center,
				HorizontalAlignment = HorizontalAlignment.Center,
				TextTrimming = TextTrimming.CharacterEllipsis,
				MaxLines = 1,
				MaxWidth = 210.0,
				Margin = new Thickness(0.0, -4.0, 0.0, 0.0)
			});
		}
		string text = (IsBlocked ? L("statusBlocked", "Blocked") : GetValue("gameStatus", "launcherActivity", "status"));
		if (string.IsNullOrWhiteSpace(text) || !IsOnlineLike)
		{
			text = (IsBlocked ? L("statusBlocked", "Blocked") : L("statusOffline", "Offline"));
		}
		stackPanel.Children.Add(new TextBlock
		{
			Text = text,
			Foreground = MutedTextBrush(170),
			FontSize = 12.0,
			FontWeight = FontWeights.SemiBold,
			TextAlignment = TextAlignment.Center,
			HorizontalAlignment = HorizontalAlignment.Center,
			TextWrapping = TextWrapping.Wrap,
			MaxWidth = 210.0
		});
		if (!string.IsNullOrWhiteSpace(AccountId))
		{
			stackPanel.Children.Add(CreateAccountIdButton(AccountId, TextAlignment.Center, HorizontalAlignment.Center, 9.0, 82, new Thickness(0.0, 0.0, 0.0, 0.0), 210.0));
		}
		_stack.Children.Add(stackPanel);
	}

	private void AnimateMenu()
	{
		try
		{
			_stack.Opacity = 0.0;
			DoubleAnimation doubleAnimation = new DoubleAnimation
			{
				From = 0.0,
				To = 1.0,
				Duration = new Duration(TimeSpan.FromMilliseconds(115L)),
				EasingFunction = new CubicEase
				{
					EasingMode = EasingMode.EaseOut
				}
			};
			Storyboard.SetTarget(doubleAnimation, _stack);
			Storyboard.SetTargetProperty(doubleAnimation, "Opacity");
			Storyboard storyboard = new Storyboard();
			storyboard.Children.Add(doubleAnimation);
			storyboard.Begin();
		}
		catch
		{
			_stack.Opacity = 1.0;
		}
	}

	private void AddBackButton()
	{
		Border border = CreateMenuRow("‹  " + L("back", "Back"), () =>
		{
			_mode = "actions";
			Render();
		}, "normal", disabled: false, 26.0);
		border.Margin = new Thickness(0.0, 0.0, 0.0, 10.0);
		_stack.Children.Add(border);
	}

	private void AddTitle(string text, double size = 15.0)
	{
		_stack.Children.Add(new TextBlock
		{
			Text = text,
			Foreground = TextBrush(),
			FontSize = size,
			FontWeight = FontWeights.Bold,
			TextWrapping = TextWrapping.Wrap,
			Margin = new Thickness(0.0, 0.0, 0.0, 8.0)
		});
	}

	private void AddCaption(string text)
	{
		_stack.Children.Add(new TextBlock
		{
			Text = text,
			Foreground = MutedTextBrush(124),
			FontSize = 11.0,
			FontWeight = FontWeights.SemiBold,
			TextWrapping = TextWrapping.Wrap,
			Margin = new Thickness(0.0, 0.0, 0.0, 0.0)
		});
	}

	private void AddSpacer(double height)
	{
		_stack.Children.Add(new Border
		{
			Height = height,
			Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0))
		});
	}

	private void AddDivider()
	{
		_stack.Children.Add(new Border
		{
			Height = 1.0,
			Margin = new Thickness(0.0, 8.0, 0.0, 8.0),
			Background = new SolidColorBrush(Color.FromArgb(20, byte.MaxValue, byte.MaxValue, byte.MaxValue))
		});
	}

	private void AddMenuButton(string label, Action? action, string tone = "normal", bool disabled = false)
	{
		_stack.Children.Add(CreateMenuRow(label, action, tone, disabled));
	}

	private Border CreateMenuRow(string label, Action? action, string tone = "normal", bool disabled = false, double height = 36.0)
	{
		Brush foreground;
		if (disabled)
		{
			foreground = MutedTextBrush(58);
		}
		else if (tone == "danger")
		{
			foreground = DangerBrush();
		}
		else if (tone == "good")
		{
			foreground = GoodBrush();
		}
		else
		{
			foreground = ((tone == "invite") ? InviteBrush() : TextBrush(220));
		}
		Border row = new Border
		{
			Height = height,
			CornerRadius = new CornerRadius(8.0),
			Background = new SolidColorBrush(Color.FromArgb(0, byte.MaxValue, byte.MaxValue, byte.MaxValue)),
			Child = new TextBlock
			{
				Text = label,
				Foreground = foreground,
				FontSize = 13.0,
				FontWeight = FontWeights.SemiBold,
				VerticalAlignment = VerticalAlignment.Center,
				Margin = new Thickness(10.0, 0.0, 10.0, 0.0)
			}
		};
		if (!disabled && action != null)
		{
			row.PointerEntered += (object _, PointerRoutedEventArgs _) =>
			{
				Border border = row;
				Color color = ((tone == "danger") ? Color.FromArgb(28, byte.MaxValue, 90, 105) : ((tone == "invite") ? Color.FromArgb(28, byte.MaxValue, 191, 0) : Color.FromArgb(24, byte.MaxValue, byte.MaxValue, byte.MaxValue)));
				border.Background = new SolidColorBrush(color);
			};
			row.PointerExited += (object _, PointerRoutedEventArgs _) =>
			{
				row.Background = new SolidColorBrush(Color.FromArgb(0, byte.MaxValue, byte.MaxValue, byte.MaxValue));
			};
			row.PointerPressed += (object _, PointerRoutedEventArgs e) =>
			{
				e.Handled = true;
				action();
			};
			AttachHoverEdge(row, 8.0);
		}
		return row;
	}

	private Border CreateIconButton(string label, Action action)
	{
		Border button = new Border
		{
			Width = 30.0,
			Height = 30.0,
			CornerRadius = new CornerRadius(10.0),
			Background = new SolidColorBrush(Color.FromArgb(28, byte.MaxValue, byte.MaxValue, byte.MaxValue)),
			BorderThickness = new Thickness(1.0),
			BorderBrush = new SolidColorBrush(Color.FromArgb(24, byte.MaxValue, byte.MaxValue, byte.MaxValue)),
			Child = new TextBlock
			{
				Text = label,
				Foreground = TextBrush(190),
				FontSize = 18.0,
				FontWeight = FontWeights.SemiBold,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
				Margin = new Thickness(0.0, -2.0, 0.0, 0.0)
			}
		};
		button.PointerEntered += (object _, PointerRoutedEventArgs _) =>
		{
			button.Background = new SolidColorBrush(Color.FromArgb(42, byte.MaxValue, byte.MaxValue, byte.MaxValue));
		};
		button.PointerExited += (object _, PointerRoutedEventArgs _) =>
		{
			button.Background = new SolidColorBrush(Color.FromArgb(28, byte.MaxValue, byte.MaxValue, byte.MaxValue));
		};
		button.PointerPressed += (object _, PointerRoutedEventArgs e) =>
		{
			e.Handled = true;
			action();
		};
		return AttachHoverEdge(button, 9.0);
	}

	private Border CreateAccountIdButton(string accountId, TextAlignment align, HorizontalAlignment hAlign, double fontSize, byte alpha, Thickness margin, double? maxWidth = null)
	{
		TextBlock textBlock = new TextBlock
		{
			Text = accountId,
			Foreground = MutedTextBrush(alpha),
			FontSize = fontSize,
			FontWeight = FontWeights.SemiBold,
			TextAlignment = align,
			TextTrimming = TextTrimming.CharacterEllipsis,
			MaxLines = 1
		};
		if (maxWidth.HasValue)
		{
			textBlock.MaxWidth = maxWidth.Value;
		}
		Border button = new Border
		{
			Background = new SolidColorBrush(Color.FromArgb(0, byte.MaxValue, byte.MaxValue, byte.MaxValue)),
			CornerRadius = new CornerRadius(6.0),
			Padding = new Thickness(4.0, 2.0, 4.0, 2.0),
			Margin = margin,
			HorizontalAlignment = hAlign,
			Child = textBlock
		};
		button.PointerEntered += (object _, PointerRoutedEventArgs _) =>
		{
			button.Background = new SolidColorBrush(Color.FromArgb(16, byte.MaxValue, byte.MaxValue, byte.MaxValue));
			textBlock.Foreground = MutedTextBrush((byte)Math.Min(255, alpha + 90));
		};
		button.PointerExited += (object _, PointerRoutedEventArgs _) =>
		{
			button.Background = new SolidColorBrush(Color.FromArgb(0, byte.MaxValue, byte.MaxValue, byte.MaxValue));
			textBlock.Foreground = MutedTextBrush(alpha);
		};
		button.PointerPressed += (object _, PointerRoutedEventArgs e) =>
		{
			e.Handled = true;
			try
			{
				DataPackage dataPackage = new DataPackage();
				dataPackage.SetText(accountId);
				Clipboard.SetContent(dataPackage);
			}
			catch
			{
			}
			textBlock.Text = L("copied", "Copied");
			DispatcherQueueTimer dispatcherQueueTimer = DispatcherQueue.CreateTimer();
			dispatcherQueueTimer.Interval = TimeSpan.FromMilliseconds(1100L);
			dispatcherQueueTimer.IsRepeating = false;
			dispatcherQueueTimer.Tick += (DispatcherQueueTimer t, object obj2) =>
			{
				textBlock.Text = accountId;
				t.Stop();
			};
			dispatcherQueueTimer.Start();
		};
		return button;
	}

	private Border CreateSmallButton(string label, Action action, string tone = "normal")
	{
		return CreateSmallButton(label, () =>
		{
			action();
			return Task.CompletedTask;
		}, tone);
	}

	private Border CreateSmallButton(string label, Func<Task> action, string tone = "normal")
	{
		Border button = new Border
		{
			Height = 38.0,
			CornerRadius = new CornerRadius(12.0),
			Background = new SolidColorBrush((tone == "danger") ? Color.FromArgb(34, byte.MaxValue, 90, 105) : ((tone == "accent") ? NeoAccent : ((tone == "good") ? Color.FromArgb(34, 160, 112, byte.MaxValue) : Color.FromArgb(22, byte.MaxValue, byte.MaxValue, byte.MaxValue)))),
			BorderThickness = new Thickness(1.0),
			BorderBrush = new SolidColorBrush(Color.FromArgb(22, byte.MaxValue, byte.MaxValue, byte.MaxValue)),
			Child = new TextBlock
			{
				Text = label,
				Foreground = ((tone == "danger") ? DangerBrush() : ((tone == "accent") ? TextBrush() : ((tone == "good") ? TextBrush() : TextBrush(166)))),
				FontSize = 11.0,
				FontWeight = FontWeights.Black,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center
			}
		};
		button.PointerEntered += (object _, PointerRoutedEventArgs _) =>
		{
			Border border = button;
			Color color = ((tone == "danger") ? Color.FromArgb(50, byte.MaxValue, 90, 105) : ((tone == "accent") ? NeoAccentHover : ((tone == "good") ? Color.FromArgb(52, 160, 112, byte.MaxValue) : Color.FromArgb(34, byte.MaxValue, byte.MaxValue, byte.MaxValue))));
			border.Background = new SolidColorBrush(color);
		};
		button.PointerExited += (object _, PointerRoutedEventArgs _) =>
		{
			Border border = button;
			Color color = ((tone == "danger") ? Color.FromArgb(34, byte.MaxValue, 90, 105) : ((tone == "accent") ? NeoAccent : ((tone == "good") ? Color.FromArgb(34, 160, 112, byte.MaxValue) : Color.FromArgb(22, byte.MaxValue, byte.MaxValue, byte.MaxValue))));
			border.Background = new SolidColorBrush(color);
		};
		button.PointerPressed += async (object _, PointerRoutedEventArgs e) =>
		{
			e.Handled = true;
			await action();
		};
		return AttachHoverEdge(button, 11.0);
	}

	private async Task RunActionAsync(string action)
	{
		if (!string.IsNullOrWhiteSpace(AccountId))
		{
			try
			{
				await NeoWebBridge.ExecuteNativeFriendActionAsync(action, AccountId);
			}
			catch
			{
			}
			CloseRequested?.Invoke();
		}
	}

	private Task RespondToInviteAsync(string answer)
	{
		if (string.IsNullOrWhiteSpace(AccountId))
		{
			return Task.CompletedTask;
		}
		try
		{
			NeoWebBridge.RespondToNativePartyInvite(AccountId, answer, PartyInviteBuildId, PartyInviteBuildName, PartyInvitePartyId);
		}
		catch
		{
		}
		CloseRequested?.Invoke();
		return Task.CompletedTask;
	}

	private void OpenMessageThread()
	{
		if (!string.IsNullOrWhiteSpace(AccountId))
		{
			try
			{
				NeoWebBridge.OpenMessageThread(AccountId);
			}
			catch
			{
			}
			CloseRequested?.Invoke();
		}
	}

	private void SaveNickname()
	{
		if (!string.IsNullOrWhiteSpace(AccountId))
		{
			string nickname = _nicknameBox?.Text?.Trim() ?? "";
			NeoWebBridge.SetNativeFriendNickname(AccountId, nickname);
			CloseRequested?.Invoke();
		}
	}
}
