using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using RoR2;
using RoR2.Skills;
using UnityEngine;

namespace RoRCheatPanel.UI;

internal static class UiHelpers
{
	[Serializable]
	[CompilerGenerated]
	private sealed class _003C_003Ec
	{
		public static readonly _003C_003Ec _003C_003E9 = new _003C_003Ec();

		public static GUI.WindowFunction _003C_003E9__15_0;

		internal void _003CDrawToast_003Eb__15_0(int _)
		{
			ApplySkin();
			GUILayout.Label(_toast, Array.Empty<GUILayoutOption>());
		}
	}

	private static string _toast;

	private static float _toastUntil;

	private static string _confirmText;

	private static Action _confirmAction;

	private static string _intBuffer = "";

	private static int _intBufferId = -1;

	private static Font _font;

	private static GUIStyle _tipStyle;

	private static bool _stylesReady;

	public static bool HasModal => !string.IsNullOrEmpty(_confirmText);

	public static void ApplySkin()
	{
		if (!Object.op_Implicit((Object)(object)_font))
		{
			_font = Font.CreateDynamicFontFromOSFont(new string[4] { "Segoe UI", "Arial", "Tahoma", "Microsoft Sans Serif" }, 14);
		}
		if (Object.op_Implicit((Object)(object)_font) && (Object)(object)GUI.skin.font != (Object)(object)_font)
		{
			GUI.skin.font = _font;
			GUI.skin.window.font = _font;
			GUI.skin.label.font = _font;
			GUI.skin.button.font = _font;
			GUI.skin.toggle.font = _font;
			GUI.skin.textField.font = _font;
			GUI.skin.box.font = _font;
		}
		if (!_stylesReady)
		{
			GUI.skin.label.wordWrap = false;
			GUI.skin.button.alignment = (TextAnchor)4;
			GUI.skin.toggle.fixedHeight = 22f;
			_stylesReady = true;
		}
	}

	public static void DrawVirtual<T>(IList<T> rows, Vector2 scroll, float rowHeight, Action<T> drawRow)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		if (rows != null && rows.Count != 0)
		{
			int num = Mathf.Max(0, Mathf.FloorToInt(scroll.y / rowHeight) - 1);
			int num2 = Mathf.CeilToInt(560f / rowHeight) + 4;
			int num3 = Mathf.Min(rows.Count, num + num2);
			if (num > 0)
			{
				GUILayout.Space((float)num * rowHeight);
			}
			for (int i = num; i < num3; i++)
			{
				drawRow(rows[i]);
			}
			int num4 = rows.Count - num3;
			if (num4 > 0)
			{
				GUILayout.Space((float)num4 * rowHeight);
			}
		}
	}

	public static void Toast(string message)
	{
		_toast = message;
		_toastUntil = Time.realtimeSinceStartup + 3.2f;
		if (Plugin.Log != null)
		{
			Plugin.Log.LogInfo((object)message);
		}
	}

	public static void Confirm(string text, Action action)
	{
		_confirmText = text;
		_confirmAction = action;
	}

	public static void DrawToast()
	{
		if (string.IsNullOrEmpty(_toast) || Time.realtimeSinceStartup > _toastUntil)
		{
			return;
		}
		ApplySkin();
		string title = _toast;
		string subtitle = null;
		int split = _toast.IndexOf(" — ", StringComparison.Ordinal);
		if (split < 0)
		{
			split = _toast.IndexOf(" - ", StringComparison.Ordinal);
		}
		if (split > 0)
		{
			title = _toast.Substring(0, split).Trim();
			subtitle = _toast.Substring(split + 3).Trim();
		}
		float left = _toastUntil - Time.realtimeSinceStartup;
		float alpha = 1f;
		if (left < 0.4f)
		{
			alpha = Mathf.Clamp01(left / 0.4f);
		}
		Color accent = UiTheme.Accent;
		Color window = UiTheme.Window;
		accent.a = alpha;
		window.a = 0.92f * alpha;
		GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
		{
			alignment = (TextAnchor)4,
			fontStyle = (FontStyle)1,
			fontSize = 16,
			wordWrap = false
		};
		titleStyle.normal.textColor = new Color(Mathf.Lerp(1f, accent.r, 0.35f), Mathf.Lerp(1f, accent.g, 0.35f), Mathf.Lerp(1f, accent.b, 0.35f), alpha);
		GUIStyle subStyle = new GUIStyle(titleStyle)
		{
			fontStyle = (FontStyle)0,
			fontSize = 13
		};
		subStyle.normal.textColor = new Color(0.92f, 0.93f, 0.96f, 0.88f * alpha);
		Vector2 titleSize = titleStyle.CalcSize(new GUIContent(title));
		Vector2 subSize = string.IsNullOrEmpty(subtitle) ? Vector2.zero : subStyle.CalcSize(new GUIContent(subtitle));
		float width = Mathf.Clamp(Mathf.Max(titleSize.x, subSize.x) + 44f, 260f, (float)Screen.width - 48f);
		float height = 18f + titleSize.y + (string.IsNullOrEmpty(subtitle) ? 0f : subSize.y + 2f);
		float x = ((float)Screen.width - width) * 0.5f;
		float y = 14f;
		Color old = GUI.color;
		GUI.color = window;
		GUI.Box(new Rect(x, y, width, height), GUIContent.none);
		GUI.color = accent;
		GUI.Box(new Rect(x, y + height - 3f, width, 3f), GUIContent.none);
		GUI.color = old;
		GUI.Label(new Rect(x + 14f, y + 8f, width - 28f, titleSize.y), title, titleStyle);
		if (!string.IsNullOrEmpty(subtitle))
		{
			GUI.Label(new Rect(x + 14f, y + 8f + titleSize.y + 1f, width - 28f, subSize.y), subtitle, subStyle);
		}
	}

	public static void DrawModal()
	{
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Expected O, but got Unknown
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		if (HasModal)
		{
			float num = 500f;
			float num2 = 190f;
			Rect val = default(Rect);
			val._002Ector(((float)Screen.width - num) * 0.5f, ((float)Screen.height - num2) * 0.5f, num, num2);
			GUI.ModalWindow(5423327, val, new WindowFunction(DrawModalWindow), Loc.T("confirm"));
		}
	}

	private static void DrawModalWindow(int id)
	{
		ApplySkin();
		GUILayout.Space(4f);
		GUILayout.Label(_confirmText, Array.Empty<GUILayoutOption>());
		GUILayout.FlexibleSpace();
		GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
		if (GUILayout.Button(Loc.T("yes_btn"), (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Height(36f) }))
		{
			Action confirmAction = _confirmAction;
			_confirmText = null;
			_confirmAction = null;
			confirmAction?.Invoke();
		}
		if (GUILayout.Button(Loc.T("no_btn"), (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Height(36f) }))
		{
			_confirmText = null;
			_confirmAction = null;
		}
		GUILayout.EndHorizontal();
	}

	public static void DrawTooltip()
	{
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Expected O, but got Unknown
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Expected O, but got Unknown
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Expected O, but got Unknown
		string tooltip = GUI.tooltip;
		if (!string.IsNullOrEmpty(tooltip))
		{
			if (_tipStyle == null)
			{
				_tipStyle = new GUIStyle(GUI.skin.box);
				_tipStyle.wordWrap = true;
				_tipStyle.alignment = (TextAnchor)0;
				_tipStyle.padding = new RectOffset(8, 8, 6, 6);
			}
			float num = 360f;
			float num2 = _tipStyle.CalcHeight(new GUIContent(tooltip), num) + 8f;
			Vector2 mousePosition = Event.current.mousePosition;
			GUI.Box(new Rect(mousePosition.x + 18f, mousePosition.y + 18f, num, num2), tooltip, _tipStyle);
		}
	}

	public static string Search(string value, string hint)
	{
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
		GUILayout.Label(Loc.T("search"), (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(70f) });
		string text = GUILayout.TextField(value ?? "", (GUILayoutOption[])(object)new GUILayoutOption[2]
		{
			GUILayout.MinWidth(220f),
			GUILayout.Height(24f)
		});
		if (string.IsNullOrEmpty(text))
		{
			GUI.Label(GUILayoutUtility.GetLastRect(), "  " + hint);
		}
		if (GUILayout.Button("X", (GUILayoutOption[])(object)new GUILayoutOption[2]
		{
			GUILayout.Width(28f),
			GUILayout.Height(22f)
		}))
		{
			text = "";
		}
		GUILayout.EndHorizontal();
		return text;
	}

	public static int IntField(string label, int value, int min, int max, int id)
	{
		GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
		GUILayout.Label(label, (GUILayoutOption[])(object)new GUILayoutOption[2]
		{
			GUILayout.Width(90f),
			GUILayout.Height(24f)
		});
		if (_intBufferId != id)
		{
			_intBuffer = value.ToString();
			_intBufferId = id;
		}
		if (int.TryParse(_intBuffer = GUILayout.TextField(_intBuffer, (GUILayoutOption[])(object)new GUILayoutOption[2]
		{
			GUILayout.Width(88f),
			GUILayout.Height(24f)
		}), out var result))
		{
			value = Mathf.Clamp(result, min, max);
		}
		GUIStyle mini = new GUIStyle(GUI.skin.button)
		{
			alignment = (TextAnchor)4,
			fixedWidth = 28f,
			fixedHeight = 24f,
			margin = new RectOffset(2, 2, 1, 1),
			padding = new RectOffset(0, 0, 0, 0)
		};
		if (GUILayout.Button("-", mini, (GUILayoutOption[])(object)new GUILayoutOption[2]
		{
			GUILayout.Width(28f),
			GUILayout.Height(24f)
		}))
		{
			value = Mathf.Max(min, value - 1);
			_intBuffer = value.ToString();
		}
		if (GUILayout.Button("+", mini, (GUILayoutOption[])(object)new GUILayoutOption[2]
		{
			GUILayout.Width(28f),
			GUILayout.Height(24f)
		}))
		{
			value = Mathf.Min(max, value + 1);
			_intBuffer = value.ToString();
		}
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();
		return value;
	}

	public static bool ToggleButton(string label, bool value, float width = 120f)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		Color backgroundColor = GUI.backgroundColor;
		GUI.backgroundColor = (value ? UiTheme.Accent : UiTheme.AccentOff);
		if ((width > 0f) ? GUILayout.Button(label, (GUILayoutOption[])(object)new GUILayoutOption[2]
		{
			GUILayout.Width(width),
			GUILayout.Height(28f)
		}) : GUILayout.Button(label, (GUILayoutOption[])(object)new GUILayoutOption[2]
		{
			GUILayout.Height(28f),
			GUILayout.MinWidth(80f)
		}))
		{
			value = !value;
		}
		GUI.backgroundColor = backgroundColor;
		return value;
	}

	public static bool TabButton(string label, bool selected, params GUILayoutOption[] extra)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		Color backgroundColor = GUI.backgroundColor;
		GUI.backgroundColor = (selected ? UiTheme.Accent : UiTheme.AccentOff);
		GUILayoutOption[] array = (GUILayoutOption[])((extra != null && extra.Length != 0) ? ((Array)extra) : ((Array)new GUILayoutOption[1] { GUILayout.Height(30f) }));
		bool result = GUILayout.Button(label, array);
		GUI.backgroundColor = backgroundColor;
		return result;
	}

	public static int DrawPills(string[] names, int selected, float height = 26f)
	{
		GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
		for (int i = 0; i < names.Length; i++)
		{
			if (TabButton(names[i], selected == i, GUILayout.Height(height), GUILayout.MinWidth(90f)))
			{
				selected = i;
			}
		}
		GUILayout.EndHorizontal();
		return selected;
	}

	public static void LiveCatalog(int count)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		Color color = GUI.color;
		GUI.color = (CatalogHub.Ready ? new Color(0.7f, 0.9f, 0.72f) : new Color(1f, 0.8f, 0.4f));
		GUILayout.Label(CatalogHub.Ready ? Loc.F("catalog_count", count) : Loc.T("catalog_wait"), Array.Empty<GUILayoutOption>());
		GUI.color = color;
	}

	public static void Section(string title)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		GUILayout.Space(2f);
		Color color = GUI.color;
		GUI.color = UiTheme.Accent;
		GUILayout.Label(title, Array.Empty<GUILayoutOption>());
		GUI.color = color;
	}

	public static bool Toggle(string label, bool value, float width = 0f)
	{
		return ToggleButton(label, value, (width > 0f) ? width : 120f);
	}

	public static void LabelTip(string text, string tip, params GUILayoutOption[] options)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Expected O, but got Unknown
		GUILayout.Label(new GUIContent(text, tip ?? ""), options);
	}

	public static bool ButtonTip(string text, string tip, params GUILayoutOption[] options)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Expected O, but got Unknown
		return GUILayout.Button(new GUIContent(text, tip ?? ""), options);
	}

	public static void DrawPlayerPicker()
	{
		GameUtil.CleanupSelected();
		GUILayout.BeginVertical(GUIStyle.op_Implicit("box"), Array.Empty<GUILayoutOption>());
		Section(Loc.T("players"));
		GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
		int num = 0;
		foreach (NetworkUser item in GameUtil.AllUsers())
		{
			bool flag = GameUtil.SelectedUsers.Contains(item);
			string text = (string.IsNullOrEmpty(item.userName) ? ("Player " + num) : item.userName);
			if ((Object)(object)item == (Object)(object)GameUtil.LocalNetworkUser)
			{
				text += Loc.T("you");
			}
			bool num2 = ToggleButton(text, flag, 170f);
			if (num2 && !flag)
			{
				GameUtil.SelectedUsers.Add(item);
			}
			if (!num2 && flag)
			{
				GameUtil.SelectedUsers.Remove(item);
			}
			num++;
		}
		if (num == 0)
		{
			GUILayout.Label(Loc.T("no_players"), Array.Empty<GUILayoutOption>());
		}
		GUILayout.EndHorizontal();
		GUILayout.EndVertical();
	}

	public static void DrawPlayerTeleport()
	{
		GUILayout.BeginVertical(GUIStyle.op_Implicit("box"), Array.Empty<GUILayoutOption>());
		Section(Loc.T("player_tp"));
		GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
		if (GUILayout.Button(Loc.T("tp_selected_to_me"), (GUILayoutOption[])(object)new GUILayoutOption[2]
		{
			GUILayout.Height(28f),
			GUILayout.MinWidth(160f)
		}))
		{
			GameUtil.TeleportSelectedToLocal();
		}
		if (GUILayout.Button(Loc.T("tp_all_to_me"), (GUILayoutOption[])(object)new GUILayoutOption[2]
		{
			GUILayout.Height(28f),
			GUILayout.MinWidth(140f)
		}))
		{
			GameUtil.TeleportAllToLocal();
		}
		GUILayout.EndHorizontal();
		int num = 0;
		int num2 = 0;
		foreach (NetworkUser item in GameUtil.AllUsers())
		{
			bool flag = (Object)(object)item == (Object)(object)GameUtil.LocalNetworkUser;
			GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
			GUILayout.Label(GameUtil.UserLabel(item, num), (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(220f) });
			if (!flag)
			{
				num2++;
				if (GUILayout.Button(Loc.T("tp_to_me"), (GUILayoutOption[])(object)new GUILayoutOption[2]
				{
					GUILayout.Width(110f),
					GUILayout.Height(26f)
				}))
				{
					GameUtil.TeleportUserToLocal(item);
				}
				if (GUILayout.Button(Loc.T("tp_me_to"), (GUILayoutOption[])(object)new GUILayoutOption[2]
				{
					GUILayout.Width(110f),
					GUILayout.Height(26f)
				}))
				{
					GameUtil.TeleportLocalToUser(item);
				}
				if (GUILayout.Button(Loc.T("fun_kick"), (GUILayoutOption[])(object)new GUILayoutOption[2]
				{
					GUILayout.Width(80f),
					GUILayout.Height(26f)
				}))
				{
					GameUtil.FunnyKickUser(item, takeHealth: false);
				}
				if (GUILayout.Button(Loc.T("fun_kick_hp"), (GUILayoutOption[])(object)new GUILayoutOption[2]
				{
					GUILayout.Width(100f),
					GUILayout.Height(26f)
				}))
				{
					GameUtil.FunnyKickUser(item, takeHealth: true);
				}
			}
			GUILayout.EndHorizontal();
			num++;
		}
		if (num == 0)
		{
			GUILayout.Label(Loc.T("no_players"), Array.Empty<GUILayoutOption>());
		}
		else if (num2 == 0)
		{
			GUILayout.Label(Loc.T("tp_no_others"), Array.Empty<GUILayoutOption>());
		}
		GUILayout.EndVertical();
	}

	public static void HostBanner()
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		if (GameUtil.IsHost)
		{
			GUILayout.Label(Loc.T("host_ok"), Array.Empty<GUILayoutOption>());
			return;
		}
		Color color = GUI.color;
		GUI.color = new Color(1f, 0.75f, 0.3f);
		GUILayout.Label(Loc.T("host_client"), Array.Empty<GUILayoutOption>());
		GUI.color = color;
	}

	public static GuiIcon Icon(ItemDef def)
	{
		if (!Object.op_Implicit((Object)(object)def))
		{
			return default(GuiIcon);
		}
		if (Object.op_Implicit((Object)(object)def.pickupIconSprite))
		{
			return GuiIcon.FromSprite(def.pickupIconSprite);
		}
		return GuiIcon.FromTexture(def.pickupIconTexture);
	}

	public static GuiIcon Icon(EquipmentDef def)
	{
		if (!Object.op_Implicit((Object)(object)def))
		{
			return default(GuiIcon);
		}
		if (Object.op_Implicit((Object)(object)def.pickupIconSprite))
		{
			return GuiIcon.FromSprite(def.pickupIconSprite);
		}
		return GuiIcon.FromTexture(def.pickupIconTexture);
	}

	public static GuiIcon Icon(SkillDef def)
	{
		if (!Object.op_Implicit((Object)(object)def))
		{
			return default(GuiIcon);
		}
		return GuiIcon.FromSprite(def.icon);
	}

	public static GuiIcon Icon(ArtifactDef def)
	{
		if (!Object.op_Implicit((Object)(object)def))
		{
			return default(GuiIcon);
		}
		if (Object.op_Implicit((Object)(object)def.smallIconSelectedSprite))
		{
			return GuiIcon.FromSprite(def.smallIconSelectedSprite);
		}
		if (Object.op_Implicit((Object)(object)def.smallIconDeselectedSprite))
		{
			return GuiIcon.FromSprite(def.smallIconDeselectedSprite);
		}
		return default(GuiIcon);
	}

	public static GuiIcon Icon(Sprite sprite)
	{
		return GuiIcon.FromSprite(sprite);
	}

	public static void DrawIcon(GuiIcon icon, float size = 36f)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Invalid comparison between Unknown and I4
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		Rect rect = GUILayoutUtility.GetRect(size, size, (GUILayoutOption[])(object)new GUILayoutOption[2]
		{
			GUILayout.Width(size),
			GUILayout.Height(size)
		});
		if (Object.op_Implicit((Object)(object)icon.Tex) && (int)Event.current.type == 7)
		{
			GUI.DrawTextureWithTexCoords(rect, icon.Tex, icon.Uv, true);
		}
		else if (!Object.op_Implicit((Object)(object)icon.Tex))
		{
			GUI.Box(rect, "");
		}
	}

	public static void DrawIcon(Texture tex, float size = 36f)
	{
		DrawIcon(GuiIcon.FromTexture(tex), size);
	}

	public static void DrawIcon(Sprite sprite, float size = 36f)
	{
		DrawIcon(GuiIcon.FromSprite(sprite), size);
	}

	public static IEnumerable<T> Filter<T>(IEnumerable<T> source, Func<T, bool> pred)
	{
		foreach (T item in source)
		{
			if (pred(item))
			{
				yield return item;
			}
		}
	}
}
