using System;
using System.Collections.Generic;
using RoR2;
using UnityEngine;

namespace RoRCheatPanel.UI.Tabs;

internal static class PlayersTab
{
	private static Vector2 _scroll;

	private static string _say = "";

	public static void Draw()
	{
		//IL_0224: Unknown result type (might be due to invalid IL or missing references)
		//IL_022e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0233: Unknown result type (might be due to invalid IL or missing references)
		GUILayout.Label(Loc.T("admin_help"), Array.Empty<GUILayoutOption>());
		UiHelpers.DrawPlayerPicker();
		GUILayout.BeginVertical(GUIStyle.op_Implicit("box"), Array.Empty<GUILayoutOption>());
		UiHelpers.Section(Loc.T("tab_players"));
		GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
		if (GUILayout.Button(Loc.T("admin_kick"), (GUILayoutOption[])(object)new GUILayoutOption[2]
		{
			GUILayout.Height(28f),
			GUILayout.MinWidth(110f)
		}))
		{
			UiHelpers.Confirm(Loc.T("admin_confirm_kick_sel"), PlayerAdmin.KickSelected);
		}
		if (GUILayout.Button(Loc.T("admin_ban"), (GUILayoutOption[])(object)new GUILayoutOption[2]
		{
			GUILayout.Height(28f),
			GUILayout.MinWidth(90f)
		}))
		{
			UiHelpers.Confirm(Loc.T("admin_confirm_ban_sel"), PlayerAdmin.BanSelected);
		}
		if (GUILayout.Button(Loc.T("heal"), (GUILayoutOption[])(object)new GUILayoutOption[2]
		{
			GUILayout.Height(28f),
			GUILayout.MinWidth(80f)
		}))
		{
			GameUtil.HealFull();
		}
		if (GUILayout.Button(Loc.T("admin_ach_sel_all"), (GUILayoutOption[])(object)new GUILayoutOption[2]
		{
			GUILayout.Height(28f),
			GUILayout.MinWidth(200f)
		}))
		{
			UiHelpers.Confirm(Loc.T("confirm_ach_all"), GameUtil.GrantAllAchievementsToTargets);
		}
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
		GUILayout.Label(Loc.T("admin_say"), (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(80f) });
		_say = GUILayout.TextField(_say ?? "", (GUILayoutOption[])(object)new GUILayoutOption[2]
		{
			GUILayout.MinWidth(280f),
			GUILayout.Height(24f)
		});
		if (GUILayout.Button(Loc.T("admin_say_btn"), (GUILayoutOption[])(object)new GUILayoutOption[2]
		{
			GUILayout.Width(120f),
			GUILayout.Height(26f)
		}) && !string.IsNullOrWhiteSpace(_say))
		{
			PlayerAdmin.Announce(_say);
			_say = "";
		}
		GUILayout.EndHorizontal();
		GUILayout.EndVertical();
		_scroll = GUILayout.BeginScrollView(_scroll, Array.Empty<GUILayoutOption>());
		int num = 0;
		foreach (NetworkUser item in GameUtil.AllUsers())
		{
			DrawUser(item, num);
			num++;
		}
		if (num == 0)
		{
			GUILayout.Label(Loc.T("no_players"), Array.Empty<GUILayoutOption>());
		}
		GUILayout.BeginVertical(GUIStyle.op_Implicit("box"), Array.Empty<GUILayoutOption>());
		UiHelpers.Section(Loc.T("admin_bans"));
		int num2 = 0;
		foreach (KeyValuePair<string, string> item2 in PlayerAdmin.BanEntries())
		{
			num2++;
			GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
			GUILayout.Label(item2.Value + "  [" + item2.Key + "]", (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.MinWidth(280f) });
			if (GUILayout.Button(Loc.T("admin_unban"), (GUILayoutOption[])(object)new GUILayoutOption[2]
			{
				GUILayout.Width(90f),
				GUILayout.Height(24f)
			}))
			{
				PlayerAdmin.Unban(item2.Key);
			}
			GUILayout.EndHorizontal();
		}
		if (num2 == 0)
		{
			GUILayout.Label(Loc.T("admin_no_bans"), Array.Empty<GUILayoutOption>());
		}
		GUILayout.EndVertical();
		GUILayout.EndScrollView();
	}

	private static void DrawUser(NetworkUser user, int index)
	{
		bool flag = (Object)(object)user == (Object)(object)GameUtil.LocalNetworkUser;
		GUILayout.BeginVertical(GUIStyle.op_Implicit("box"), Array.Empty<GUILayoutOption>());
		GUILayout.Label(GameUtil.UserLabel(user, index) + (flag ? Loc.T("you") : ""), Array.Empty<GUILayoutOption>());
		GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
		if (!flag)
		{
			if (GUILayout.Button(Loc.T("admin_kick"), (GUILayoutOption[])(object)new GUILayoutOption[2]
			{
				GUILayout.Width(90f),
				GUILayout.Height(26f)
			}))
			{
				UiHelpers.Confirm(Loc.F("admin_confirm_kick", user.userName), delegate
				{
					PlayerAdmin.KickUser(user);
				});
			}
			if (GUILayout.Button(Loc.T("admin_ban"), (GUILayoutOption[])(object)new GUILayoutOption[2]
			{
				GUILayout.Width(70f),
				GUILayout.Height(26f)
			}))
			{
				UiHelpers.Confirm(Loc.F("admin_confirm_ban", user.userName), delegate
				{
					PlayerAdmin.BanUser(user);
				});
			}
			bool flag2 = UiHelpers.ToggleButton(Loc.T("admin_mute"), PlayerAdmin.IsMuted(user), 80f);
			if (flag2 != PlayerAdmin.IsMuted(user))
			{
				PlayerAdmin.SetMuted(user, flag2);
			}
		}
		bool flag3 = UiHelpers.ToggleButton(Loc.T("admin_noclip"), PlayerAdmin.IsNoclip(user), 90f);
		if (flag3 != PlayerAdmin.IsNoclip(user))
		{
			PlayerAdmin.SetNoclip(user, flag3);
		}
		bool flag4 = UiHelpers.ToggleButton(Loc.T("admin_god"), PlayerAdmin.IsGod(user), 70f);
		if (flag4 != PlayerAdmin.IsGod(user))
		{
			PlayerAdmin.SetGod(user, flag4);
		}
		bool flag5 = UiHelpers.ToggleButton(Loc.T("admin_freeze"), PlayerAdmin.IsFrozen(user), 110f);
		if (flag5 != PlayerAdmin.IsFrozen(user))
		{
			PlayerAdmin.SetFrozen(user, flag5);
		}
		if (GUILayout.Button(Loc.T("heal"), (GUILayoutOption[])(object)new GUILayoutOption[2]
		{
			GUILayout.Width(70f),
			GUILayout.Height(26f)
		}))
		{
			HealUser(user);
		}
		if (!flag && GUILayout.Button(Loc.T("tp_to_me"), (GUILayoutOption[])(object)new GUILayoutOption[2]
		{
			GUILayout.Width(80f),
			GUILayout.Height(26f)
		}))
		{
			GameUtil.TeleportUserToLocal(user);
		}
		GUILayout.EndHorizontal();
		GUILayout.EndVertical();
	}

	private static void HealUser(NetworkUser user)
	{
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		if (GameUtil.RequireHost(Loc.T("act_stats")))
		{
			CharacterBody val = (Object.op_Implicit((Object)(object)user) ? user.GetCurrentBody() : null);
			HealthComponent val2 = (Object.op_Implicit((Object)(object)val) ? val.healthComponent : null);
			if (Object.op_Implicit((Object)(object)val2))
			{
				val2.Networkhealth = val2.fullHealth;
				val2.Heal(val2.fullCombinedHealth, default(ProcChainMask), true);
			}
		}
	}
}
