using System;
using System.Collections.ObjectModel;
using RoR2;
using UnityEngine;

namespace RoRCheatPanel.UI.Tabs;

internal static class TeamsTab
{
	private static Vector2 _scroll;

	public static void Draw()
	{
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		UiHelpers.DrawPlayerPicker();
		UiHelpers.DrawPlayerTeleport();
		GUILayout.Label(Loc.T("teams_help"), Array.Empty<GUILayoutOption>());
		GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
		TeamButton((TeamIndex)1, Loc.T("team_player"));
		TeamButton((TeamIndex)2, Loc.T("team_monster"));
		TeamButton((TeamIndex)0, Loc.T("team_neutral"));
		TeamButton((TeamIndex)3, "Lunar");
		TeamButton((TeamIndex)4, "Void");
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
		if (GUILayout.Button(Loc.T("kill_enemies"), (GUILayoutOption[])(object)new GUILayoutOption[2]
		{
			GUILayout.Width(180f),
			GUILayout.Height(28f)
		}))
		{
			UiHelpers.Confirm(Loc.T("confirm_kill_enemies"), GameUtil.KillEnemies);
		}
		GUILayout.EndHorizontal();
		_scroll = GUILayout.BeginScrollView(_scroll, Array.Empty<GUILayoutOption>());
		DrawTeam((TeamIndex)1, Loc.T("team_player"));
		DrawTeam((TeamIndex)2, Loc.T("team_monster"));
		DrawTeam((TeamIndex)0, Loc.T("team_neutral"));
		DrawTeam((TeamIndex)3, "Lunar");
		DrawTeam((TeamIndex)4, "Void");
		GUILayout.EndScrollView();
	}

	private static void TeamButton(TeamIndex team, string label)
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		if (GUILayout.Button(Loc.F("become", label), (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Height(28f) }))
		{
			GameUtil.SetTeam(team);
		}
	}

	private static void DrawTeam(TeamIndex team, string title)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		GUILayout.BeginVertical(GUIStyle.op_Implicit("box"), Array.Empty<GUILayoutOption>());
		GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
		GUILayout.Label(title, (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(120f) });
		if (GUILayout.Button(Loc.T("kill_team"), (GUILayoutOption[])(object)new GUILayoutOption[2]
		{
			GUILayout.Width(150f),
			GUILayout.Height(26f)
		}))
		{
			UiHelpers.Confirm(Loc.F("confirm_kill_team", title), delegate
			{
				//IL_0001: Unknown result type (might be due to invalid IL or missing references)
				GameUtil.KillTeam(team);
			});
		}
		GUILayout.EndHorizontal();
		ReadOnlyCollection<TeamComponent> teamMembers = TeamComponent.GetTeamMembers(team);
		if (teamMembers == null || teamMembers.Count == 0)
		{
			GUILayout.Label(Loc.T("empty"), Array.Empty<GUILayoutOption>());
		}
		else
		{
			int num = 0;
			foreach (TeamComponent item in teamMembers)
			{
				if (Object.op_Implicit((Object)(object)item) && Object.op_Implicit((Object)(object)item.body))
				{
					num++;
					if (num > 24)
					{
						GUILayout.Label(Loc.F("more", teamMembers.Count - 24), Array.Empty<GUILayoutOption>());
						break;
					}
					GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
					GUILayout.Label("  " + item.body.GetDisplayName(), (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(260f) });
					if (GUILayout.Button("Kill", (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(60f) }) && Object.op_Implicit((Object)(object)item.body.master))
					{
						GameUtil.KillMaster(item.body.master);
					}
					GUILayout.EndHorizontal();
				}
			}
		}
		GUILayout.EndVertical();
	}
}
