using System;
using System.Collections.Generic;
using RoR2;
using RoR2.Skills;
using UnityEngine;

namespace RoRCheatPanel.UI.Tabs;

internal static class SkillsTab
{
	private struct Row
	{
		public SkillDef Def;

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
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		UiHelpers.DrawPlayerPicker();
		Color color = GUI.color;
		GUI.color = new Color(1f, 0.7f, 0.35f);
		GUILayout.Label(Loc.T("skills_warn"), Array.Empty<GUILayoutOption>());
		GUI.color = color;
		GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
		if (GUILayout.Button(Loc.T("reset_skills"), (GUILayoutOption[])(object)new GUILayoutOption[2]
		{
			GUILayout.Width(140f),
			GUILayout.Height(28f)
		}))
		{
			GameUtil.ResetSkills();
		}
		if (GUILayout.Button(Loc.T("reset_cd"), (GUILayoutOption[])(object)new GUILayoutOption[2]
		{
			GUILayout.Width(180f),
			GUILayout.Height(28f)
		}))
		{
			GameUtil.ResetSkillCooldowns();
		}
		CheatRuntime.InfiniteSkills = UiHelpers.ToggleButton(Loc.T("inf_skills"), CheatRuntime.InfiniteSkills, 200f);
		GUILayout.EndHorizontal();
		_search = UiHelpers.Search(_search, Loc.T("search_skill"));
		UiHelpers.LiveCatalog(CatalogHub.Skills);
		RebuildIfNeeded();
		_scroll = GUILayout.BeginScrollView(_scroll, Array.Empty<GUILayoutOption>());
		UiHelpers.DrawVirtual(Rows, _scroll, 46f, DrawRow);
		GUILayout.EndScrollView();
	}

	private static void RebuildIfNeeded()
	{
		if (_cacheKey == Loc.Generation + CatalogHub.Stamp * 1000 && _cacheSearch == _search)
		{
			return;
		}
		_cacheKey = Loc.Generation + CatalogHub.Stamp * 1000;
		_cacheSearch = _search;
		Rows.Clear();
		if (SkillCatalog.allSkillDefs == null)
		{
			return;
		}
		foreach (SkillDef allSkillDef in SkillCatalog.allSkillDefs)
		{
			if (Object.op_Implicit((Object)(object)allSkillDef) && GameUtil.Matches(_search, GameUtil.DisplayName(allSkillDef), allSkillDef.skillName, allSkillDef.name, allSkillDef.skillNameToken))
			{
				Rows.Add(new Row
				{
					Def = allSkillDef,
					Title = GameUtil.DisplayName(allSkillDef),
					Tip = GameUtil.Description(allSkillDef),
					Icon = UiHelpers.Icon(allSkillDef)
				});
			}
		}
	}

	private static void DrawRow(Row row)
	{
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Expected O, but got Unknown
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Expected O, but got Unknown
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Expected O, but got Unknown
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Expected O, but got Unknown
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_0125: Expected O, but got Unknown
		GUILayout.BeginHorizontal(GUIStyle.op_Implicit("box"), (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Height(42f) });
		UiHelpers.DrawIcon(row.Icon);
		GUILayout.Label(new GUIContent(row.Title, row.Tip), (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(260f) });
		if (GUILayout.Button(new GUIContent("M1", row.Tip), (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(44f) }))
		{
			GameUtil.SetSkill(row.Def, (SkillSlot)0);
		}
		if (GUILayout.Button(new GUIContent("M2", row.Tip), (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(44f) }))
		{
			GameUtil.SetSkill(row.Def, (SkillSlot)1);
		}
		if (GUILayout.Button(new GUIContent("Shift", row.Tip), (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(54f) }))
		{
			GameUtil.SetSkill(row.Def, (SkillSlot)2);
		}
		if (GUILayout.Button(new GUIContent("R", row.Tip), (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(36f) }))
		{
			GameUtil.SetSkill(row.Def, (SkillSlot)3);
		}
		GUILayout.EndHorizontal();
	}
}
