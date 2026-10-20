using RoR2;
using UnityEngine;

namespace RoRCheatPanel;

internal static class UiTheme
{
	private struct Colors
	{
		public Color window;

		public Color accent;

		public Color off;
	}

	public static PanelTheme Current
	{
		get
		{
			if (CheatConfig.UiTheme == null)
			{
				return PanelTheme.Night;
			}
			return CheatConfig.UiTheme.Value;
		}
	}

	public static Color Window => Palette(Current).window;

	public static Color Accent => Palette(Current).accent;

	public static Color AccentOff => Palette(Current).off;

	public static void Set(PanelTheme theme)
	{
		if (CheatConfig.UiTheme != null)
		{
			CheatConfig.UiTheme.Value = theme;
		}
	}

	public static Color AccentOf(PanelTheme theme)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		return Palette(theme).accent;
	}

	public static Color OffOf(PanelTheme theme)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		return Palette(theme).off;
	}

	public static string LocKey(PanelTheme theme)
	{
		return theme switch
		{
			PanelTheme.Orange => "theme_orange", 
			PanelTheme.Red => "theme_red", 
			PanelTheme.Blue => "theme_blue", 
			PanelTheme.Green => "theme_green", 
			PanelTheme.Teal => "theme_teal", 
			PanelTheme.Purple => "theme_purple", 
			PanelTheme.Lavender => "theme_lavender", 
			PanelTheme.Gold => "theme_gold", 
			PanelTheme.Pink => "theme_pink", 
			_ => "theme_night", 
		};
	}

	public static string BodyName(PanelTheme theme)
	{
		return theme switch
		{
			PanelTheme.Orange => "CommandoBody", 
			PanelTheme.Red => "HuntressBody", 
			PanelTheme.Blue => "MageBody", 
			PanelTheme.Green => "CrocoBody", 
			PanelTheme.Teal => "TreebotBody", 
			PanelTheme.Purple => "RailgunnerBody", 
			PanelTheme.Lavender => "VoidSurvivorBody", 
			PanelTheme.Gold => "LoaderBody", 
			PanelTheme.Pink => "Bandit2Body", 
			_ => "CaptainBody", 
		};
	}

	public static Texture Icon(PanelTheme theme)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Invalid comparison between Unknown and I4
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		BodyIndex val = BodyCatalog.FindBodyIndex(BodyName(theme));
		if ((int)val == -1)
		{
			return null;
		}
		CharacterBody bodyPrefabBodyComponent = BodyCatalog.GetBodyPrefabBodyComponent(val);
		if (Object.op_Implicit((Object)(object)bodyPrefabBodyComponent) && Object.op_Implicit((Object)(object)bodyPrefabBodyComponent.portraitIcon))
		{
			return bodyPrefabBodyComponent.portraitIcon;
		}
		return null;
	}

	private static Colors Palette(PanelTheme theme)
	{
		return theme switch
		{
			PanelTheme.Orange => C(0.3f, 0.16f, 0.08f, 0.96f, 0.2f, 0.92f, 0.28f), 
			PanelTheme.Red => C(0.28f, 0.1f, 0.12f, 0.92f, 0.28f, 0.32f, 0.22f), 
			PanelTheme.Blue => C(0.1f, 0.16f, 0.3f, 0.35f, 0.58f, 0.96f, 0.22f), 
			PanelTheme.Green => C(0.1f, 0.22f, 0.13f, 0.32f, 0.78f, 0.4f, 0.2f), 
			PanelTheme.Teal => C(0.08f, 0.2f, 0.22f, 0.22f, 0.78f, 0.74f, 0.2f), 
			PanelTheme.Purple => C(0.18f, 0.1f, 0.28f, 0.68f, 0.4f, 0.95f, 0.22f), 
			PanelTheme.Lavender => C(0.22f, 0.16f, 0.28f, 0.78f, 0.62f, 0.95f, 0.24f), 
			PanelTheme.Gold => C(0.26f, 0.2f, 0.08f, 0.95f, 0.78f, 0.22f, 0.22f), 
			PanelTheme.Pink => C(0.26f, 0.1f, 0.16f, 0.95f, 0.4f, 0.62f, 0.22f), 
			_ => C(0.16f, 0.18f, 0.24f, 0.38f, 0.58f, 0.88f, 0.22f), 
		};
	}

	private static Colors C(float wr, float wg, float wb, float ar, float ag, float ab, float off)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		return new Colors
		{
			window = new Color(wr, wg, wb, 0.97f),
			accent = new Color(ar, ag, ab, 1f),
			off = new Color(off, off, off + 0.04f, 1f)
		};
	}
}
