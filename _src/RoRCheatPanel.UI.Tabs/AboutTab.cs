using System;
using System.IO;
using UnityEngine;
using IoPath = System.IO.Path;

namespace RoRCheatPanel.UI.Tabs;

internal static class AboutTab
{
	private static Vector2 _scroll;
	private static Texture2D _channelTex;
	private static bool _channelTried;
	private const string ChannelUrl = "https://www.youtube.com/@Silans";

	public static void Draw()
	{
		_scroll = GUILayout.BeginScrollView(_scroll);
		GUILayout.BeginVertical("box");
		UiHelpers.Section(Plugin.PluginName + "  " + Plugin.PluginVersion);
		GUILayout.Label(Loc.T("about_desc"));
		GUILayout.Label(Loc.T("hover_hint"));
		GUILayout.EndVertical();

		GUILayout.BeginVertical("box");
		UiHelpers.Section(Loc.T("catalog_title"));
		GUILayout.Label(Loc.T("catalog_help"));
		GUILayout.Label(Loc.F("catalog_stats",
			CatalogHub.Items, CatalogHub.Equipment, CatalogHub.Scenes, CatalogHub.Bodies,
			CatalogHub.Skills, CatalogHub.Artifacts, CatalogHub.Survivors, CatalogHub.Achievements));
		if (GUILayout.Button(Loc.T("catalog_refresh"), GUILayout.Width(200), GUILayout.Height(26)))
			CatalogHub.ForceRefresh();
		GUILayout.EndVertical();

		GUILayout.BeginVertical("box");
		UiHelpers.Section(Loc.T("language"));
		GUILayout.BeginHorizontal();
		LangButton(PanelLanguage.Ru, "Русский");
		LangButton(PanelLanguage.En, "English");
		LangButton(PanelLanguage.Es, "Español");
		GUILayout.EndHorizontal();
		GUILayout.EndVertical();

		GUILayout.BeginVertical("box");
		UiHelpers.Section(Loc.T("theme_title"));
		GUILayout.Label(Loc.T("theme_help"));
		GUILayout.BeginHorizontal();
		var i = 0;
		foreach (PanelTheme theme in Enum.GetValues(typeof(PanelTheme)))
		{
			ThemeButton(theme);
			i++;
			if (i % 5 == 0)
			{
				GUILayout.EndHorizontal();
				GUILayout.BeginHorizontal();
			}
		}
		GUILayout.EndHorizontal();
		GUILayout.EndVertical();

		GUILayout.BeginVertical("box");
		UiHelpers.Section(Loc.F("hotkeys", Plugin.PluginGuid));
		GUILayout.Label("  " + CheatConfig.OpenKey.Value + "  /  " + CheatConfig.OpenKeyAlt.Value + " — " + Loc.T("hk_open"));
		GUILayout.Label("  " + CheatConfig.ScanKey.Value + " — " + Loc.T("hk_scan"));
		GUILayout.Label("  " + CheatConfig.GummyKey.Value + " — " + Loc.T("hk_gummy"));
		GUILayout.Label("  " + CheatConfig.DronesKey.Value + " — " + Loc.T("hk_drones"));
		GUILayout.Label("  " + CheatConfig.ArmyKey.Value + " — " + Loc.T("hk_army"));
		GUILayout.Label("  " + CheatConfig.GodModeKey.Value + " — " + Loc.T("hk_god"));
		GUILayout.Label("  " + CheatConfig.NoclipKey.Value + " — " + Loc.T("hk_noclip"));
		GUILayout.Label("  " + CheatConfig.ChargeTeleporterKey.Value + " — " + Loc.T("hk_charge"));
		GUILayout.EndVertical();

		DrawOpacity();
		DrawChannelLink();
		GUILayout.EndScrollView();
	}

	private static void DrawOpacity()
	{
		if (CheatConfig.UiOpacity == null)
			return;
		float opacity = Mathf.Clamp(CheatConfig.UiOpacity.Value, 0.25f, 1f);
		GUILayout.BeginVertical("box");
		UiHelpers.Section(Loc.T("ui_opacity"));
		GUILayout.BeginHorizontal();
		GUILayout.Label(Loc.F("ui_opacity_val", Mathf.RoundToInt(opacity * 100f)), GUILayout.Width(170f));
		opacity = GUILayout.HorizontalSlider(opacity, 0.25f, 1f);
		if (GUILayout.Button("100", GUILayout.Width(40f)))
			opacity = 1f;
		GUILayout.EndHorizontal();
		GUILayout.EndVertical();
		CheatConfig.UiOpacity.Value = opacity;
	}

	private static void DrawChannelLink()
	{
		var tex = ChannelTexture();
		var height = 118f;
		var width = tex
			? Mathf.Clamp(tex.width * (height / Mathf.Max(1, tex.height)), 220f, 420f)
			: 260f;

		GUILayout.Space(28);
		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		GUILayout.BeginVertical(GUILayout.Width(width));

		var caption = new GUIStyle(GUI.skin.label)
		{
			alignment = TextAnchor.MiddleCenter,
			fontStyle = FontStyle.Bold
		};
		var prev = GUI.color;
		GUI.color = new Color(0.82f, 0.8f, 1f);
		GUILayout.Label(Loc.T("yt_title"), caption);
		GUI.color = prev;

		var rect = GUILayoutUtility.GetRect(width, height, GUILayout.Width(width), GUILayout.Height(height));
		if (tex)
			GUI.DrawTexture(rect, tex, ScaleMode.ScaleToFit, true);
		if (GUI.Button(rect, new GUIContent(string.Empty, ChannelUrl), GUIStyle.none))
			OpenChannel();

		GUILayout.EndVertical();
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();
		GUILayout.Space(12);
	}

	private static void OpenChannel()
	{
		Application.OpenURL(ChannelUrl);
	}

	private static Texture2D ChannelTexture()
	{
		if (_channelTried)
			return _channelTex;
		_channelTried = true;
		try
		{
			var dir = IoPath.GetDirectoryName(typeof(Plugin).Assembly.Location);
			var path = IoPath.Combine(string.IsNullOrEmpty(dir) ? "." : dir, "voicenextdoor.png");
			if (!File.Exists(path))
				return null;
			var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
			tex.wrapMode = TextureWrapMode.Clamp;
			tex.filterMode = FilterMode.Bilinear;
			if (!ImageConversion.LoadImage(tex, File.ReadAllBytes(path), true))
				return null;
			_channelTex = tex;
		}
		catch (Exception ex)
		{
			if (Plugin.Log != null)
				Plugin.Log.LogWarning("voicenextdoor.png: " + ex.Message);
		}

		return _channelTex;
	}

	private static void LangButton(PanelLanguage lang, string label)
	{
		if (UiHelpers.TabButton(label, Loc.Current == lang, GUILayout.Width(120), GUILayout.Height(28)))
			Loc.Set(lang);
	}

	private static void ThemeButton(PanelTheme theme)
	{
		var icon = UiTheme.Icon(theme);
		var name = Loc.T(UiTheme.LocKey(theme));
		var selected = UiTheme.Current == theme;
		var prev = GUI.backgroundColor;
		GUI.backgroundColor = selected ? UiTheme.AccentOf(theme) : UiTheme.OffOf(theme);
		GUILayout.BeginHorizontal(GUILayout.Width(208), GUILayout.Height(30));
		UiHelpers.DrawIcon(icon, 28f);
		if (GUILayout.Button(new GUIContent(name, name), GUILayout.Height(28), GUILayout.MinWidth(168)))
			UiTheme.Set(theme);
		GUILayout.EndHorizontal();
		GUI.backgroundColor = prev;
	}
}
