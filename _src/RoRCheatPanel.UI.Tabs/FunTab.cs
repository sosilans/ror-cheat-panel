using System;
using UnityEngine;

namespace RoRCheatPanel.UI.Tabs;

internal static class FunTab
{
	private static string _nick = "БОСС";

	public static void Draw()
	{
		UiHelpers.DrawPlayerPicker();
		GUILayout.Label(Loc.T("fun_help"), Array.Empty<GUILayoutOption>());
		GUILayout.BeginVertical(GUIStyle.op_Implicit("box"), Array.Empty<GUILayoutOption>());
		GUILayout.Label(Loc.T("fun_chaos"), Array.Empty<GUILayoutOption>());
		GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
		if (GUILayout.Button(Loc.T("fun_rand_item"), (GUILayoutOption[])(object)new GUILayoutOption[2]
		{
			GUILayout.Height(28f),
			GUILayout.MinWidth(150f)
		}))
		{
			GameUtil.GiveRandomItem(1);
		}
		if (GUILayout.Button(Loc.T("fun_rand_item10"), (GUILayoutOption[])(object)new GUILayoutOption[2]
		{
			GUILayout.Height(28f),
			GUILayout.MinWidth(160f)
		}))
		{
			GameUtil.GiveRandomItem(10);
		}
		if (GUILayout.Button(Loc.T("fun_rand_equip"), (GUILayoutOption[])(object)new GUILayoutOption[2]
		{
			GUILayout.Height(28f),
			GUILayout.MinWidth(170f)
		}))
		{
			GameUtil.GiveRandomEquipment();
		}
		if (GUILayout.Button(Loc.T("fun_rand_survivor"), (GUILayoutOption[])(object)new GUILayoutOption[2]
		{
			GUILayout.Height(28f),
			GUILayout.MinWidth(170f)
		}))
		{
			GameUtil.RandomSurvivor();
		}
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
		if (GUILayout.Button(Loc.T("fun_rain"), (GUILayoutOption[])(object)new GUILayoutOption[2]
		{
			GUILayout.Height(28f),
			GUILayout.MinWidth(180f)
		}))
		{
			GameUtil.RainPickups(24);
		}
		if (GUILayout.Button(Loc.T("fun_rain_big"), (GUILayoutOption[])(object)new GUILayoutOption[2]
		{
			GUILayout.Height(28f),
			GUILayout.MinWidth(180f)
		}))
		{
			GameUtil.RainPickups(80);
		}
		if (GUILayout.Button(Loc.T("fun_yeet_btn"), (GUILayoutOption[])(object)new GUILayoutOption[2]
		{
			GUILayout.Height(28f),
			GUILayout.MinWidth(150f)
		}))
		{
			GameUtil.LaunchUp(55f);
		}
		if (GUILayout.Button(Loc.T("fun_joke"), (GUILayoutOption[])(object)new GUILayoutOption[2]
		{
			GUILayout.Height(28f),
			GUILayout.MinWidth(140f)
		}))
		{
			GameUtil.ToastJoke();
		}
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
		if (GUILayout.Button(Loc.T("fun_chests"), (GUILayoutOption[])(object)new GUILayoutOption[2]
		{
			GUILayout.Height(28f),
			GUILayout.MinWidth(160f)
		}))
		{
			GameUtil.RainChests(10);
		}
		if (GUILayout.Button(Loc.T("fun_chests_big"), (GUILayoutOption[])(object)new GUILayoutOption[2]
		{
			GUILayout.Height(28f),
			GUILayout.MinWidth(160f)
		}))
		{
			GameUtil.RainChests(22);
		}
		if (GUILayout.Button(Loc.T("fun_artifact"), (GUILayoutOption[])(object)new GUILayoutOption[2]
		{
			GUILayout.Height(28f),
			GUILayout.MinWidth(170f)
		}))
		{
			GameUtil.ToggleRandomArtifact();
		}
		if (GUILayout.Button(Loc.T("fun_gold"), (GUILayoutOption[])(object)new GUILayoutOption[2]
		{
			GUILayout.Height(28f),
			GUILayout.MinWidth(150f)
		}))
		{
			GameUtil.GoldExplosion();
		}
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
		if (GUILayout.Button(Loc.T("fun_kick"), (GUILayoutOption[])(object)new GUILayoutOption[2]
		{
			GUILayout.Height(30f),
			GUILayout.MinWidth(180f)
		}))
		{
			GameUtil.FunnyKick(takeHealth: false);
		}
		if (GUILayout.Button(Loc.T("fun_kick_hp"), (GUILayoutOption[])(object)new GUILayoutOption[2]
		{
			GUILayout.Height(30f),
			GUILayout.MinWidth(200f)
		}))
		{
			GameUtil.FunnyKick(takeHealth: true);
		}
		if (GUILayout.Button(Loc.T("fun_boss"), (GUILayoutOption[])(object)new GUILayoutOption[2]
		{
			GUILayout.Height(30f),
			GUILayout.MinWidth(200f)
		}))
		{
			GameUtil.BecomeBoss();
		}
		GUILayout.EndHorizontal();
		GUILayout.EndVertical();
		GUILayout.BeginVertical(GUIStyle.op_Implicit("box"), Array.Empty<GUILayoutOption>());
		GUILayout.Label(Loc.T("fun_body"), Array.Empty<GUILayoutOption>());
		GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
		if (GUILayout.Button(Loc.T("fun_tiny"), (GUILayoutOption[])(object)new GUILayoutOption[2]
		{
			GUILayout.Width(110f),
			GUILayout.Height(28f)
		}))
		{
			CheatRuntime.BodyScale = 0.35f;
		}
		if (GUILayout.Button(Loc.T("fun_normal"), (GUILayoutOption[])(object)new GUILayoutOption[2]
		{
			GUILayout.Width(110f),
			GUILayout.Height(28f)
		}))
		{
			CheatRuntime.BodyScale = 1f;
		}
		if (GUILayout.Button(Loc.T("fun_giant"), (GUILayoutOption[])(object)new GUILayoutOption[2]
		{
			GUILayout.Width(110f),
			GUILayout.Height(28f)
		}))
		{
			CheatRuntime.BodyScale = 3.2f;
		}
		if (GUILayout.Button(Loc.T("fun_mega"), (GUILayoutOption[])(object)new GUILayoutOption[2]
		{
			GUILayout.Width(110f),
			GUILayout.Height(28f)
		}))
		{
			CheatRuntime.BodyScale = 6f;
		}
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
		GUILayout.Label(Loc.F("fun_scale", CheatRuntime.BodyScale.ToString("0.00")), (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(160f) });
		CheatRuntime.BodyScale = GUILayout.HorizontalSlider(CheatRuntime.BodyScale, 0.2f, 8f, Array.Empty<GUILayoutOption>());
		GUILayout.EndHorizontal();
		CheatRuntime.LowGravity = UiHelpers.ToggleButton(Loc.T("fun_lowgrav"), CheatRuntime.LowGravity, 200f);
		GUILayout.EndVertical();
		GUILayout.BeginVertical(GUIStyle.op_Implicit("box"), Array.Empty<GUILayoutOption>());
		GUILayout.Label(Loc.T("fun_cam"), Array.Empty<GUILayoutOption>());
		GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
		CheatRuntime.Disco = UiHelpers.ToggleButton(Loc.T("fun_disco"), CheatRuntime.Disco, 150f);
		CheatRuntime.ThirdPerson = UiHelpers.ToggleButton(Loc.T("fun_tpv"), CheatRuntime.ThirdPerson, 160f);
		CheatRuntime.DrunkCamera = UiHelpers.ToggleButton(Loc.T("fun_drunk"), CheatRuntime.DrunkCamera, 160f);
		if (GUILayout.Button(Loc.T("fun_cam_reset"), (GUILayoutOption[])(object)new GUILayoutOption[2]
		{
			GUILayout.Width(190f),
			GUILayout.Height(28f)
		}))
		{
			CheatRuntime.RestoreCamera();
		}
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
		GUILayout.Label(Loc.F("fun_fov", CheatRuntime.Fov.ToString("0")), (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(120f) });
		CheatRuntime.Fov = GUILayout.HorizontalSlider(CheatRuntime.Fov, 30f, 140f, Array.Empty<GUILayoutOption>());
		if (GUILayout.Button("60", (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(40f) }))
		{
			CheatRuntime.Fov = 60f;
		}
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
		GUILayout.Label(Loc.F("fun_tpv_dist", CheatRuntime.ThirdPersonDistance.ToString("0.0")), (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(170f) });
		float dist = GUILayout.HorizontalSlider(CheatRuntime.ThirdPersonDistance, 3f, 18f, Array.Empty<GUILayoutOption>());
		if (Mathf.Abs(dist - CheatRuntime.ThirdPersonDistance) > 0.01f)
		{
			CheatRuntime.ThirdPerson = true;
		}
		CheatRuntime.ThirdPersonDistance = dist;
		if (GUILayout.Button("8", (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(40f) }))
		{
			CheatRuntime.ThirdPersonDistance = 8f;
		}
		GUILayout.EndHorizontal();
		GUILayout.EndVertical();
		GUILayout.BeginVertical(GUIStyle.op_Implicit("box"), Array.Empty<GUILayoutOption>());
		GUILayout.Label(Loc.T("fun_nick"), Array.Empty<GUILayoutOption>());
		GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
		_nick = GUILayout.TextField(_nick ?? "", (GUILayoutOption[])(object)new GUILayoutOption[2]
		{
			GUILayout.MinWidth(180f),
			GUILayout.Height(24f)
		});
		if (GUILayout.Button(Loc.T("fun_nick_set"), (GUILayoutOption[])(object)new GUILayoutOption[2]
		{
			GUILayout.Width(140f),
			GUILayout.Height(28f)
		}))
		{
			GameUtil.SetPlayerNick(_nick);
		}
		if (GUILayout.Button("БОСС", (GUILayoutOption[])(object)new GUILayoutOption[2]
		{
			GUILayout.Width(70f),
			GUILayout.Height(28f)
		}))
		{
			_nick = Loc.T("fun_nick_boss");
			GameUtil.SetPlayerNick(_nick);
		}
		if (GUILayout.Button(Loc.T("fun_nick_moon"), (GUILayoutOption[])(object)new GUILayoutOption[2]
		{
			GUILayout.Width(120f),
			GUILayout.Height(28f)
		}))
		{
			_nick = Loc.T("fun_nick_moon");
			GameUtil.SetPlayerNick(_nick);
		}
		GUILayout.EndHorizontal();
		GUILayout.EndVertical();
	}
}
