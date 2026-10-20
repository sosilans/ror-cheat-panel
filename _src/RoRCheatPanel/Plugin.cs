using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using RoRCheatPanel.UI;

namespace RoRCheatPanel;

[BepInPlugin("com.rorcheatpanel.plugin", "RoR Cheat Panel", "3.6.4")]
public sealed class Plugin : BaseUnityPlugin
{
	public const string PluginGuid = "com.rorcheatpanel.plugin";

	public const string PluginName = "RoR Cheat Panel";

	public const string PluginVersion = "3.6.4";

	public static Plugin Instance { get; private set; }

	public static ManualLogSource Log { get; private set; }

	private void Awake()
	{
		Instance = this;
		Log = ((BaseUnityPlugin)this).Logger;
		CheatConfig.Bind(((BaseUnityPlugin)this).Config);
		Loc.Current = CheatConfig.UiLanguage.Value;
		CheatRuntime.NoclipSpeed = CheatConfig.NoclipSpeed.Value;
		CatalogHub.Init();
		PlayerAdmin.Init();
		AdminNet.Init();
		NoclipBounds.Init();
		Log.LogInfo((object)"RoR Cheat Panel 3.6.4 loaded. Open: Ctrl+F10 or Insert.");
	}

	private void Update()
	{
		if (WasPressed(CheatConfig.OpenKey) || WasPressed(CheatConfig.OpenKeyAlt))
		{
			CheatWindow.Toggle();
		}
		CheatRuntime.Tick();
		CatalogHub.Tick();
		HudClickBlocker.Tick(CheatWindow.Opened);
		if (WasPressed(CheatConfig.NoclipKey))
		{
			CheatRuntime.ToggleNoclip();
		}
		if (WasPressed(CheatConfig.GodModeKey))
		{
			CheatRuntime.ToggleGodMode();
		}
		if (!CheatWindow.Opened)
		{
			if (WasPressed(CheatConfig.ScanKey))
			{
				GameUtil.ScanArea();
			}
			if (WasPressed(CheatConfig.GummyKey))
			{
				GameUtil.SpawnGummyClone();
			}
			if (WasPressed(CheatConfig.DronesKey))
			{
				GameUtil.SpawnDroneBackup();
			}
			if (WasPressed(CheatConfig.ArmyKey))
			{
				GameUtil.SpawnArmy();
			}
			if (WasPressed(CheatConfig.ChargeTeleporterKey))
			{
				GameUtil.ChargeTeleporter();
			}
		}
	}

	private void LateUpdate()
	{
		CheatRuntime.LateTick();
	}

	private void OnGUI()
	{
		CheatWindow.OnGUI();
	}

	private static bool WasPressed(ConfigEntry<KeyboardShortcut> entry)
	{
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		if (entry != null)
		{
			KeyboardShortcut value = entry.Value;
			if ((int)value.MainKey != 0)
			{
				value = entry.Value;
				return value.IsDown();
			}
		}
		return false;
	}
}
