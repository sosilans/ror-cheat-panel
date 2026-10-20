using System;
using RoR2;
using UnityEngine;

namespace RoRCheatPanel.UI.Tabs;

internal static class ToolsTab
{
	private static Vector2 _scroll;

	private static int _money = 100;

	private static int _lunar = 10;

	private static int _level = 1;

	private static bool _gold = true;

	private static bool _shop = true;

	private static bool _ms;

	private static bool _hidden;

	private static float _hp = 110f;

	private static float _shield;

	private static float _damage = 12f;

	private static float _attack = 1f;

	private static float _move = 7f;

	private static float _regen;

	private static float _crit;

	private static float _jump = 15f;

	private static int _jumps = 1;

	public static void Draw()
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		_scroll = GUILayout.BeginScrollView(_scroll, Array.Empty<GUILayoutOption>());
		UiHelpers.DrawPlayerPicker();
		UiHelpers.DrawPlayerTeleport();
		DrawToggles();
		DrawTeleporter();
		DrawMoney();
		DrawStats();
		DrawSpawns();
		GUILayout.EndScrollView();
	}

	private static void DrawToggles()
	{
		GUILayout.BeginVertical(GUIStyle.op_Implicit("box"), Array.Empty<GUILayoutOption>());
		GUILayout.Label(Loc.T("effects"), Array.Empty<GUILayoutOption>());
		GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
		bool flag = UiHelpers.ToggleButton("Godmode", CheatRuntime.GodMode);
		if (flag != CheatRuntime.GodMode)
		{
			CheatRuntime.GodMode = flag;
			if (!flag)
			{
				foreach (NetworkUser item in GameUtil.Targets())
				{
					CharacterBody currentBody = item.GetCurrentBody();
					if (Object.op_Implicit((Object)(object)currentBody) && Object.op_Implicit((Object)(object)currentBody.healthComponent))
					{
						currentBody.healthComponent.godMode = false;
					}
				}
			}
		}
		if (UiHelpers.ToggleButton("Noclip", CheatRuntime.Noclip, 90f) != CheatRuntime.Noclip)
		{
			CheatRuntime.ToggleNoclip();
		}
		CheatRuntime.InfiniteSkills = UiHelpers.ToggleButton(Loc.T("inf_skills"), CheatRuntime.InfiniteSkills, 180f);
		CheatRuntime.InfiniteEquipment = UiHelpers.ToggleButton(Loc.T("inf_equip"), CheatRuntime.InfiniteEquipment, 170f);
		if (GUILayout.Button(Loc.T("heal"), (GUILayoutOption[])(object)new GUILayoutOption[2]
		{
			GUILayout.Width(120f),
			GUILayout.Height(28f)
		}))
		{
			GameUtil.HealFull();
		}
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
		GUILayout.Label(Loc.F("noclip_speed", CheatRuntime.NoclipSpeed.ToString("0")), (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(180f) });
		CheatRuntime.NoclipSpeed = GUILayout.HorizontalSlider(CheatRuntime.NoclipSpeed, 4f, 80f, Array.Empty<GUILayoutOption>());
		if (CheatConfig.NoclipSpeed != null)
		{
			CheatConfig.NoclipSpeed.Value = CheatRuntime.NoclipSpeed;
		}
		GUILayout.EndHorizontal();
		GUILayout.Label(Loc.T("noclip_help"), Array.Empty<GUILayoutOption>());
		GUILayout.EndVertical();
	}

	private static void DrawTeleporter()
	{
		GUILayout.BeginVertical(GUIStyle.op_Implicit("box"), Array.Empty<GUILayoutOption>());
		GUILayout.Label(Loc.T("teleporter"), Array.Empty<GUILayoutOption>());
		GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
		_gold = UiHelpers.ToggleButton("Gold", _gold, 80f);
		_shop = UiHelpers.ToggleButton("Bazaar", _shop, 90f);
		_ms = UiHelpers.ToggleButton("Celestial", _ms, 100f);
		_hidden = UiHelpers.ToggleButton("Hidden Realm", _hidden, 140f);
		if (GUILayout.Button(Loc.T("apply_portals"), (GUILayoutOption[])(object)new GUILayoutOption[2]
		{
			GUILayout.Width(170f),
			GUILayout.Height(28f)
		}))
		{
			GameUtil.SetPortalFlags(_gold, _shop, _ms, _hidden);
		}
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
		if (GUILayout.Button(Loc.T("activate"), (GUILayoutOption[])(object)new GUILayoutOption[2]
		{
			GUILayout.Width(130f),
			GUILayout.Height(28f)
		}))
		{
			GameUtil.ActivateTeleporter();
		}
		if (GUILayout.Button(Loc.T("charge"), (GUILayoutOption[])(object)new GUILayoutOption[2]
		{
			GUILayout.Width(110f),
			GUILayout.Height(28f)
		}))
		{
			GameUtil.ChargeTeleporter();
		}
		if (GUILayout.Button(Loc.T("ping"), (GUILayoutOption[])(object)new GUILayoutOption[2]
		{
			GUILayout.Width(80f),
			GUILayout.Height(28f)
		}))
		{
			GameUtil.PingTeleporter();
		}
		GUILayout.EndHorizontal();
		TeleporterInteraction instance = TeleporterInteraction.instance;
		if (Object.op_Implicit((Object)(object)instance))
		{
			GUILayout.Label(Loc.F("charge_line", Mathf.RoundToInt(instance.chargeFraction * 100f), instance.NetworkisActivated), Array.Empty<GUILayoutOption>());
		}
		else
		{
			GUILayout.Label(Loc.T("no_tp"), Array.Empty<GUILayoutOption>());
		}
		GUILayout.EndVertical();
	}

	private static void DrawMoney()
	{
		GUILayout.BeginVertical(GUIStyle.op_Implicit("box"), Array.Empty<GUILayoutOption>());
		GUILayout.Label(Loc.T("money_level"), Array.Empty<GUILayoutOption>());
		_money = UiHelpers.IntField(Loc.T("gold"), _money, 1, 999999, 20);
		_lunar = UiHelpers.IntField(Loc.T("lunar"), _lunar, 1, 9999, 21);
		_level = UiHelpers.IntField(Loc.T("level"), _level, 1, 999, 22);
		GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
		if (GUILayout.Button(Loc.T("give_gold"), (GUILayoutOption[])(object)new GUILayoutOption[2]
		{
			GUILayout.Width(140f),
			GUILayout.Height(28f)
		}))
		{
			GameUtil.GiveMoney(_money);
		}
		if (GUILayout.Button(Loc.T("give_lunar"), (GUILayoutOption[])(object)new GUILayoutOption[2]
		{
			GUILayout.Width(130f),
			GUILayout.Height(28f)
		}))
		{
			GameUtil.GiveLunar((uint)_lunar);
		}
		if (GUILayout.Button(Loc.T("set_level"), (GUILayoutOption[])(object)new GUILayoutOption[2]
		{
			GUILayout.Width(160f),
			GUILayout.Height(28f)
		}))
		{
			GameUtil.SetTeamLevel((uint)_level);
		}
		GUILayout.EndHorizontal();
		GUILayout.EndVertical();
	}

	private static void DrawStats()
	{
		GUILayout.BeginVertical(GUIStyle.op_Implicit("box"), Array.Empty<GUILayoutOption>());
		GUILayout.Label(Loc.T("stats_help"), Array.Empty<GUILayoutOption>());
		GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
		if (GUILayout.Button(Loc.T("read_stats"), (GUILayoutOption[])(object)new GUILayoutOption[2]
		{
			GUILayout.Width(170f),
			GUILayout.Height(28f)
		}))
		{
			ReadStats();
		}
		if (GUILayout.Button(Loc.T("apply_changes"), (GUILayoutOption[])(object)new GUILayoutOption[2]
		{
			GUILayout.Width(150f),
			GUILayout.Height(28f)
		}))
		{
			GameUtil.ApplyStats(GameUtil.LocalBody, _hp, _shield, _damage, _attack, _move, _regen, _crit, _jump, _jumps);
		}
		GUILayout.EndHorizontal();
		_hp = Slider("HP", _hp, 1f, 20000f);
		_shield = Slider("Shield", _shield, 0f, 20000f);
		_damage = Slider("Damage", _damage, 0f, 5000f);
		_attack = Slider("Atk spd", _attack, 0.1f, 20f);
		_move = Slider("Move", _move, 0f, 40f);
		_regen = Slider("Regen", _regen, 0f, 500f);
		_crit = Slider("Crit %", _crit, 0f, 100f);
		_jump = Slider("Jump", _jump, 0f, 80f);
		_jumps = Mathf.RoundToInt(Slider("Jumps", _jumps, 0f, 20f));
		GUILayout.EndVertical();
	}

	private static void ReadStats()
	{
		CharacterBody localBody = GameUtil.LocalBody;
		if (!Object.op_Implicit((Object)(object)localBody))
		{
			UiHelpers.Toast(Loc.T("no_body"));
			return;
		}
		_hp = localBody.baseMaxHealth;
		_shield = localBody.baseMaxShield;
		_damage = localBody.baseDamage;
		_attack = localBody.baseAttackSpeed;
		_move = localBody.baseMoveSpeed;
		_regen = localBody.baseRegen;
		_crit = localBody.baseCrit;
		_jump = localBody.baseJumpPower;
		_jumps = localBody.baseJumpCount;
		UiHelpers.Toast(Loc.T("stats_read"));
	}

	private static float Slider(string label, float value, float min, float max)
	{
		GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
		GUILayout.Label(label + " " + value.ToString("0.##"), (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(140f) });
		value = GUILayout.HorizontalSlider(value, min, max, Array.Empty<GUILayoutOption>());
		GUILayout.EndHorizontal();
		return value;
	}

	private static void DrawSpawns()
	{
		GUILayout.BeginVertical(GUIStyle.op_Implicit("box"), Array.Empty<GUILayoutOption>());
		GUILayout.Label(Loc.T("spawn_help"), Array.Empty<GUILayoutOption>());
		GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
		if (GUILayout.Button("Scan area", (GUILayoutOption[])(object)new GUILayoutOption[2]
		{
			GUILayout.Width(110f),
			GUILayout.Height(28f)
		}))
		{
			GameUtil.ScanArea();
		}
		if (GUILayout.Button("Gummy Clone", (GUILayoutOption[])(object)new GUILayoutOption[2]
		{
			GUILayout.Width(120f),
			GUILayout.Height(28f)
		}))
		{
			GameUtil.SpawnGummyClone();
		}
		if (GUILayout.Button("Drone Backup", (GUILayoutOption[])(object)new GUILayoutOption[2]
		{
			GUILayout.Width(130f),
			GUILayout.Height(28f)
		}))
		{
			GameUtil.SpawnDroneBackup();
		}
		if (GUILayout.Button(Loc.T("army"), (GUILayoutOption[])(object)new GUILayoutOption[2]
		{
			GUILayout.Width(90f),
			GUILayout.Height(28f)
		}))
		{
			GameUtil.SpawnArmy();
		}
		if (GUILayout.Button(Loc.T("kill_enemies"), (GUILayoutOption[])(object)new GUILayoutOption[2]
		{
			GUILayout.Width(140f),
			GUILayout.Height(28f)
		}))
		{
			UiHelpers.Confirm(Loc.T("confirm_kill_enemies"), GameUtil.KillEnemies);
		}
		GUILayout.EndHorizontal();
		GUILayout.EndVertical();
	}
}
