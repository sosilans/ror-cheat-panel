using System;
using BepInEx.Configuration;
using UnityEngine;

namespace RoRCheatPanel;

internal static class CheatConfig
{
	public static ConfigEntry<KeyboardShortcut> OpenKey;

	public static ConfigEntry<KeyboardShortcut> OpenKeyAlt;

	public static ConfigEntry<KeyboardShortcut> ScanKey;

	public static ConfigEntry<KeyboardShortcut> GummyKey;

	public static ConfigEntry<KeyboardShortcut> DronesKey;

	public static ConfigEntry<KeyboardShortcut> ArmyKey;

	public static ConfigEntry<KeyboardShortcut> GodModeKey;

	public static ConfigEntry<KeyboardShortcut> ChargeTeleporterKey;

	public static ConfigEntry<KeyboardShortcut> NoclipKey;

	public static ConfigEntry<float> NoclipSpeed;

	public static ConfigEntry<float> UiScale;

	public static ConfigEntry<float> UiOpacity;

	public static ConfigEntry<bool> UnlockCursor;

	public static ConfigEntry<bool> PauseStopwatchOnOpen;

	public static ConfigEntry<bool> ShowHiddenItems;

	public static ConfigEntry<int> DefaultItemQuantity;

	public static ConfigEntry<PanelLanguage> UiLanguage;

	public static ConfigEntry<PanelTheme> UiTheme;

	public static void Bind(ConfigFile config)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_0132: Unknown result type (might be due to invalid IL or missing references)
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_0190: Unknown result type (might be due to invalid IL or missing references)
		OpenKey = config.Bind<KeyboardShortcut>("Hotkeys", "OpenPanel", new KeyboardShortcut((KeyCode)291, (KeyCode[])(object)new KeyCode[1] { (KeyCode)306 }), "Open / close the panel.");
		OpenKeyAlt = config.Bind<KeyboardShortcut>("Hotkeys", "OpenPanelAlt", new KeyboardShortcut((KeyCode)277, Array.Empty<KeyCode>()), "Second open / close bind.");
		ScanKey = config.Bind<KeyboardShortcut>("Hotkeys", "ScanArea", new KeyboardShortcut((KeyCode)118, (KeyCode[])(object)new KeyCode[1] { (KeyCode)306 }), "Scan interactables.");
		GummyKey = config.Bind<KeyboardShortcut>("Hotkeys", "GummyClone", new KeyboardShortcut((KeyCode)98, (KeyCode[])(object)new KeyCode[1] { (KeyCode)306 }), "Spawn a gummy clone.");
		DronesKey = config.Bind<KeyboardShortcut>("Hotkeys", "DroneBackup", new KeyboardShortcut((KeyCode)110, (KeyCode[])(object)new KeyCode[1] { (KeyCode)306 }), "Spawn gunner drones.");
		ArmyKey = config.Bind<KeyboardShortcut>("Hotkeys", "SpawnArmy", new KeyboardShortcut((KeyCode)109, (KeyCode[])(object)new KeyCode[1] { (KeyCode)306 }), "Spawn army. Changed from Ctrl+C to avoid copy conflict.");
		GodModeKey = config.Bind<KeyboardShortcut>("Hotkeys", "GodMode", new KeyboardShortcut((KeyCode)103, (KeyCode[])(object)new KeyCode[1] { (KeyCode)306 }), "Toggle godmode.");
		ChargeTeleporterKey = config.Bind<KeyboardShortcut>("Hotkeys", "ChargeTeleporter", new KeyboardShortcut((KeyCode)116, (KeyCode[])(object)new KeyCode[1] { (KeyCode)306 }), "Charge the teleporter.");
		NoclipKey = config.Bind<KeyboardShortcut>("Hotkeys", "Noclip", new KeyboardShortcut((KeyCode)102, (KeyCode[])(object)new KeyCode[1] { (KeyCode)306 }), "Toggle noclip / fly.");
		NoclipSpeed = config.Bind<float>("Movement", "NoclipSpeed", 22f, "Noclip fly speed. Hold Shift to triple it.");
		UiScale = config.Bind<float>("UI", "Scale", 1f, "IMGUI scale. 0.75 - 1.5 is typical.");
		UiOpacity = config.Bind<float>("UI", "PanelOpacity", 1f, "Panel opacity. 1 = solid, 0.25 = more transparent.");
		UnlockCursor = config.Bind<bool>("UI", "UnlockCursor", true, "Show and unlock the cursor while the panel is open.");
		PauseStopwatchOnOpen = config.Bind<bool>("UI", "PauseRunStopwatch", false, "Pause the run timer while the panel is open (host only).");
		ShowHiddenItems = config.Bind<bool>("Items", "ShowHiddenByDefault", false, "Show hidden / unused items in catalogs.");
		DefaultItemQuantity = config.Bind<int>("Items", "DefaultQuantity", 1, "Default stack size in the Items tab.");
		UiLanguage = config.Bind<PanelLanguage>("UI", "Language", PanelLanguage.Ru, "Panel language: Ru, En, Es. Also used for item/artifact names and descriptions.");
		UiTheme = config.Bind<PanelTheme>("UI", "Theme", PanelTheme.Night, "Panel color theme.");
	}
}
