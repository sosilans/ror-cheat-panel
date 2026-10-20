using System;
using System.Collections.Generic;
using RoR2;
using UnityEngine;

namespace RoRCheatPanel.UI.Tabs;

internal static class AchievementsTab
{
	private struct Row
	{
		public AchievementDef Def;

		public string Title;

		public string Tip;

		public GuiIcon Icon;
	}

	private static string _search = "";

	private static Vector2 _scroll;

	private static readonly List<Row> Rows = new List<Row>(256);

	private static string _cacheSearch;

	private static int _cacheKey = int.MinValue;

	public static void Draw()
	{
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		GUILayout.Label(Loc.T("ach_local"), Array.Empty<GUILayoutOption>());
		GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
		if (GUILayout.Button(Loc.T("grant_all"), (GUILayoutOption[])(object)new GUILayoutOption[2]
		{
			GUILayout.Width(200f),
			GUILayout.Height(28f)
		}))
		{
			UiHelpers.Confirm(Loc.T("confirm_ach_all"), GameUtil.GrantAllAchievements);
		}
		if (GUILayout.Button(Loc.T("admin_ach_sel_all"), (GUILayoutOption[])(object)new GUILayoutOption[2]
		{
			GUILayout.Width(240f),
			GUILayout.Height(28f)
		}))
		{
			UiHelpers.Confirm(Loc.T("confirm_ach_all"), GameUtil.GrantAllAchievementsToTargets);
		}
		GUILayout.EndHorizontal();
		GUILayout.Label(Loc.T("admin_ach_hint"), Array.Empty<GUILayoutOption>());
		_search = UiHelpers.Search(_search, Loc.T("search_ach"));
		RebuildIfNeeded();
		_scroll = GUILayout.BeginScrollView(_scroll, Array.Empty<GUILayoutOption>());
		UiHelpers.DrawVirtual(Rows, _scroll, 46f, DrawRow);
		GUILayout.EndScrollView();
	}

	private unsafe static void RebuildIfNeeded()
	{
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		if (_cacheKey == Loc.Generation + CatalogHub.Stamp * 1000 && _cacheSearch == _search)
		{
			return;
		}
		_cacheKey = Loc.Generation + CatalogHub.Stamp * 1000;
		_cacheSearch = _search;
		Rows.Clear();
		GenericStaticEnumerable<AchievementDef, Enumerator> allAchievementDefs = AchievementManager.allAchievementDefs;
		Enumerator enumerator = allAchievementDefs.GetEnumerator();
		try
		{
			while (enumerator.MoveNext())
			{
				AchievementDef current = enumerator.Current;
				if (current != null && GameUtil.Matches(_search, GameUtil.DisplayName(current), current.identifier, current.nameToken))
				{
					Rows.Add(new Row
					{
						Def = current,
						Title = GameUtil.DisplayName(current),
						Tip = GameUtil.Description(current),
						Icon = UiHelpers.Icon(current.GetAchievedIcon())
					});
				}
			}
		}
		finally
		{
			((IDisposable)(*(Enumerator*)(&enumerator))/*cast due to .constrained prefix*/).Dispose();
		}
	}

	private static void DrawRow(Row row)
	{
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Expected O, but got Unknown
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Expected O, but got Unknown
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Expected O, but got Unknown
		LocalUser localUser = GameUtil.LocalUser;
		bool flag = localUser != null && localUser.userProfile != null && localUser.userProfile.HasAchievement(row.Def.identifier);
		GUILayout.BeginHorizontal(GUIStyle.op_Implicit("box"), (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Height(42f) });
		UiHelpers.DrawIcon(row.Icon);
		GUILayout.Label(new GUIContent(row.Title + (flag ? Loc.T("has_ach") : ""), row.Tip), (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(360f) });
		if (GUILayout.Button(new GUIContent(flag ? Loc.T("again") : Loc.T("give"), row.Tip), (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(90f) }))
		{
			GameUtil.GrantAchievement(row.Def);
		}
		if (GUILayout.Button(new GUIContent(Loc.T("admin_ach_sel"), row.Tip), (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(150f) }))
		{
			GameUtil.GrantAchievementToTargets(row.Def);
		}
		GUILayout.EndHorizontal();
	}
}
