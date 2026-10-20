using System;
using System.Collections.Generic;
using RoR2;
using UnityEngine;

namespace RoRCheatPanel.UI.Tabs;

internal static class EquipmentTab
{
	private struct Row
	{
		public EquipmentDef Def;

		public string Title;

		public string Name;

		public string Tip;

		public GuiIcon Icon;
	}

	private static string _search = "";

	private static Vector2 _scroll;

	private static bool _lunar = true;

	private static bool _normal = true;

	private static readonly List<Row> Rows = new List<Row>(128);

	private static string _cacheSearch;

	private static int _cacheKey = int.MinValue;

	public static void Draw()
	{
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		UiHelpers.DrawPlayerPicker();
		_search = UiHelpers.Search(_search, Loc.T("search_equip"));
		UiHelpers.LiveCatalog(CatalogHub.Equipment);
		GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
		_normal = UiHelpers.ToggleButton(Loc.T("eq_normal"), _normal, 130f);
		_lunar = UiHelpers.ToggleButton(Loc.T("eq_lunar"), _lunar, 130f);
		if (GUILayout.Button(Loc.T("unequip"), (GUILayoutOption[])(object)new GUILayoutOption[2]
		{
			GUILayout.Width(180f),
			GUILayout.Height(28f)
		}))
		{
			GameUtil.SetEquipment(null);
		}
		if (GUILayout.Button(Loc.T("recharge"), (GUILayoutOption[])(object)new GUILayoutOption[2]
		{
			GUILayout.Width(220f),
			GUILayout.Height(28f)
		}))
		{
			GameUtil.RechargeEquipment();
		}
		GUILayout.EndHorizontal();
		RebuildIfNeeded();
		_scroll = GUILayout.BeginScrollView(_scroll, Array.Empty<GUILayoutOption>());
		UiHelpers.DrawVirtual(Rows, _scroll, 46f, DrawRow);
		GUILayout.EndScrollView();
	}

	private static void RebuildIfNeeded()
	{
		int num = Loc.Generation * 8 + CatalogHub.Stamp * 1000 + (_lunar ? 1 : 0) + (_normal ? 2 : 0);
		if (_cacheKey == num && _cacheSearch == _search)
		{
			return;
		}
		_cacheKey = num;
		_cacheSearch = _search;
		Rows.Clear();
		for (int i = 0; i < EquipmentCatalog.equipmentCount; i++)
		{
			EquipmentDef equipmentDef = EquipmentCatalog.GetEquipmentDef((EquipmentIndex)i);
			if (Object.op_Implicit((Object)(object)equipmentDef) && (!equipmentDef.isLunar || _lunar) && (equipmentDef.isLunar || _normal) && GameUtil.Matches(_search, GameUtil.DisplayName(equipmentDef), ((Object)equipmentDef).name, equipmentDef.nameToken))
			{
				Rows.Add(new Row
				{
					Def = equipmentDef,
					Title = GameUtil.DisplayName(equipmentDef) + (equipmentDef.isLunar ? Loc.T("lunar_tag") : ""),
					Name = ((Object)equipmentDef).name,
					Tip = GameUtil.Description(equipmentDef),
					Icon = UiHelpers.Icon(equipmentDef)
				});
			}
		}
	}

	private static void DrawRow(Row row)
	{
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Expected O, but got Unknown
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Expected O, but got Unknown
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Expected O, but got Unknown
		GUILayout.BeginHorizontal(GUIStyle.op_Implicit("box"), (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Height(42f) });
		UiHelpers.DrawIcon(row.Icon);
		GUILayout.Label(new GUIContent(row.Title, row.Tip), (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(280f) });
		GUILayout.Label(new GUIContent(row.Name, row.Tip), (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(200f) });
		if (GUILayout.Button(new GUIContent(Loc.T("give"), row.Tip), (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(80f) }))
		{
			GameUtil.SetEquipment(row.Def);
		}
		GUILayout.EndHorizontal();
	}
}
