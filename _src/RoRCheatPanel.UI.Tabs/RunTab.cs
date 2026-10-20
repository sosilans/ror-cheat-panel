using System;
using RoR2;
using UnityEngine;

namespace RoRCheatPanel.UI.Tabs;

internal static class RunTab
{
	private static Vector2 _scroll;

	public static void Draw()
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		_scroll = GUILayout.BeginScrollView(_scroll, Array.Empty<GUILayoutOption>());
		GUILayout.Label(Loc.T("run_help"), Array.Empty<GUILayoutOption>());
		DrawDifficulty();
		GUILayout.Space(8f);
		DrawTime();
		GUILayout.Space(8f);
		DrawArtifacts();
		GUILayout.Space(8f);
		DrawSurvivors();
		GUILayout.EndScrollView();
	}

	private unsafe static void DrawDifficulty()
	{
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Invalid comparison between Unknown and I4
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Invalid comparison between Unknown and I4
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		GUILayout.Label(Loc.T("difficulty"), Array.Empty<GUILayoutOption>());
		GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
		int num = Mathf.Max(11, CatalogHub.Difficulties);
		for (int i = 0; i < num; i++)
		{
			DifficultyIndex val = (DifficultyIndex)i;
			if ((int)val == -1 || (int)val == 11)
			{
				continue;
			}
			DifficultyDef difficultyDef = DifficultyCatalog.GetDifficultyDef(val);
			if (difficultyDef != null || i < 11)
			{
				string text = ((difficultyDef != null) ? GameUtil.Lang(difficultyDef.nameToken) : ((object)(*(DifficultyIndex*)(&val))/*cast due to .constrained prefix*/).ToString());
				if (string.IsNullOrEmpty(text) || text == difficultyDef?.nameToken)
				{
					text = ((object)(*(DifficultyIndex*)(&val))/*cast due to .constrained prefix*/).ToString();
				}
				bool num2 = Object.op_Implicit((Object)(object)Run.instance) && Run.instance.selectedDifficulty == val;
				Color backgroundColor = GUI.backgroundColor;
				if (num2)
				{
					GUI.backgroundColor = new Color(0.4f, 0.7f, 0.4f);
				}
				if (GUILayout.Button(text, (GUILayoutOption[])(object)new GUILayoutOption[2]
				{
					GUILayout.Height(28f),
					GUILayout.Width(110f)
				}))
				{
					GameUtil.SetDifficulty(val);
				}
				GUI.backgroundColor = backgroundColor;
				if ((i + 1) % 5 == 0)
				{
					GUILayout.EndHorizontal();
					GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
				}
			}
		}
		GUILayout.EndHorizontal();
	}

	private static void DrawTime()
	{
		GUILayout.Label(Loc.T("time"), Array.Empty<GUILayoutOption>());
		GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
		GUILayout.Label(Loc.F("timescale", CheatRuntime.TimeScale.ToString("0.00")), (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(160f) });
		CheatRuntime.TimeScale = GUILayout.HorizontalSlider(CheatRuntime.TimeScale, 0.1f, 3f, Array.Empty<GUILayoutOption>());
		if (GUILayout.Button("1x", (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(40f) }))
		{
			CheatRuntime.ResetTimeScale();
		}
		GUILayout.EndHorizontal();
		CheatRuntime.FreezeStopwatch = UiHelpers.ToggleButton(Loc.T("freeze"), CheatRuntime.FreezeStopwatch, 240f);
		if (CheatRuntime.FreezeStopwatch && Object.op_Implicit((Object)(object)Run.instance) && GameUtil.IsHost)
		{
			Run.instance.NetworkisRunStopwatchForcePaused = true;
		}
		else if (!CheatRuntime.FreezeStopwatch && Object.op_Implicit((Object)(object)Run.instance) && GameUtil.IsHost)
		{
			Run.instance.NetworkisRunStopwatchForcePaused = false;
		}
	}

	private static void DrawArtifacts()
	{
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Expected O, but got Unknown
		//IL_0113: Unknown result type (might be due to invalid IL or missing references)
		GUILayout.Label(Loc.T("artifacts"), Array.Empty<GUILayoutOption>());
		if ((Object)(object)RunArtifactManager.instance == (Object)null)
		{
			GUILayout.Label(Loc.T("no_run"), Array.Empty<GUILayoutOption>());
			return;
		}
		GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
		int num = 0;
		for (int i = 0; i < ArtifactCatalog.artifactCount; i++)
		{
			ArtifactDef artifactDef = ArtifactCatalog.GetArtifactDef((ArtifactIndex)i);
			if (Object.op_Implicit((Object)(object)artifactDef))
			{
				bool flag = RunArtifactManager.instance.IsArtifactEnabled(artifactDef);
				Color backgroundColor = GUI.backgroundColor;
				GUI.backgroundColor = (flag ? new Color(0.45f, 0.75f, 0.4f) : new Color(0.3f, 0.3f, 0.3f));
				string text = GameUtil.Description(artifactDef);
				GUILayout.BeginHorizontal((GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(210f) });
				UiHelpers.DrawIcon(UiHelpers.Icon(artifactDef), 28f);
				if (GUILayout.Button(new GUIContent(GameUtil.DisplayName(artifactDef), text), (GUILayoutOption[])(object)new GUILayoutOption[2]
				{
					GUILayout.Height(32f),
					GUILayout.MinWidth(150f)
				}))
				{
					GameUtil.SetArtifact(artifactDef, !flag);
				}
				GUILayout.EndHorizontal();
				GUI.backgroundColor = backgroundColor;
				num++;
				if (num % 4 == 0)
				{
					GUILayout.EndHorizontal();
					GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
				}
			}
		}
		GUILayout.EndHorizontal();
	}

	private static void DrawSurvivors()
	{
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Expected O, but got Unknown
		GUILayout.Label(Loc.T("change_survivor") + "  →  " + Loc.T("tab_chars"), Array.Empty<GUILayoutOption>());
		UiHelpers.DrawPlayerPicker();
		GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
		int num = 0;
		foreach (SurvivorDef allSurvivorDef in SurvivorCatalog.allSurvivorDefs)
		{
			if (Object.op_Implicit((Object)(object)allSurvivorDef) && !allSurvivorDef.hidden)
			{
				string text = GameUtil.Description(allSurvivorDef);
				if (GUILayout.Button(new GUIContent(GameUtil.DisplayName(allSurvivorDef), text), (GUILayoutOption[])(object)new GUILayoutOption[2]
				{
					GUILayout.Width(140f),
					GUILayout.Height(28f)
				}))
				{
					GameUtil.TransformSurvivor(allSurvivorDef);
				}
				num++;
				if (num % 6 == 0)
				{
					GUILayout.EndHorizontal();
					GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
				}
			}
		}
		GUILayout.EndHorizontal();
	}
}
