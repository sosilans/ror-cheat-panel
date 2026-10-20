using System;
using System.Collections.Generic;
using RoR2;
using UnityEngine;

namespace RoRCheatPanel.UI.Tabs;

internal static class ItemsTab
{
	private struct Row
	{
		public ItemDef Def;

		public string Title;

		public string Name;

		public string Tip;

		public GuiIcon Icon;

		public Color Color;
	}

	private static string _search = "";

	private static int _qty;

	private static bool _inited;

	private static Vector2 _scroll;

	private static bool _t1 = true;

	private static bool _t2 = true;

	private static bool _t3 = true;

	private static bool _lunar = true;

	private static bool _boss = true;

	private static bool _void = true;

	private static bool _food = true;

	private static bool _other = true;

	private static bool _hidden;

	private static readonly List<Row> Rows = new List<Row>(256);

	private static string _cacheSearch;

	private static int _cacheKey = int.MinValue;

	public static void Draw()
	{
		//IL_02d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f1: Unknown result type (might be due to invalid IL or missing references)
		if (!_inited)
		{
			_qty = Mathf.Max(1, CheatConfig.DefaultItemQuantity.Value);
			_hidden = CheatConfig.ShowHiddenItems.Value;
			_inited = true;
		}
		UiHelpers.DrawPlayerPicker();
		_search = UiHelpers.Search(_search, Loc.T("search_item"));
		UiHelpers.LiveCatalog(CatalogHub.Items);
		GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
		_t1 = UiHelpers.ToggleButton(Loc.T("tier_white"), _t1, 90f);
		_t2 = UiHelpers.ToggleButton(Loc.T("tier_green"), _t2, 100f);
		_t3 = UiHelpers.ToggleButton(Loc.T("tier_red"), _t3, 100f);
		_lunar = UiHelpers.ToggleButton(Loc.T("tier_lunar"), _lunar, 100f);
		_boss = UiHelpers.ToggleButton(Loc.T("tier_boss"), _boss, 80f);
		_void = UiHelpers.ToggleButton(Loc.T("tier_void"), _void, 80f);
		_food = UiHelpers.ToggleButton(Loc.T("tier_food"), _food, 70f);
		_other = UiHelpers.ToggleButton(Loc.T("tier_other"), _other, 90f);
		_hidden = UiHelpers.ToggleButton(Loc.T("hidden"), _hidden, 100f);
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
		_qty = UiHelpers.IntField(Loc.T("qty"), _qty, 1, 999, 1);
		if (GUILayout.Button("1", (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(32f) }))
		{
			_qty = 1;
		}
		if (GUILayout.Button("10", (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(36f) }))
		{
			_qty = 10;
		}
		if (GUILayout.Button("50", (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(36f) }))
		{
			_qty = 50;
		}
		if (GUILayout.Button(Loc.T("give_visible"), (GUILayoutOption[])(object)new GUILayoutOption[2]
		{
			GUILayout.Width(180f),
			GUILayout.Height(26f)
		}))
		{
			int qty = _qty;
			UiHelpers.Confirm(Loc.F("confirm_give_all", qty), delegate
			{
				GameUtil.GiveAllVisibleItems(Visible, qty);
			});
		}
		if (GUILayout.Button(Loc.T("clear_inv"), (GUILayoutOption[])(object)new GUILayoutOption[2]
		{
			GUILayout.Width(180f),
			GUILayout.Height(26f)
		}))
		{
			UiHelpers.Confirm(Loc.T("confirm_clear"), GameUtil.ClearInventory);
		}
		GUILayout.EndHorizontal();
		RebuildIfNeeded();
		_scroll = GUILayout.BeginScrollView(_scroll, Array.Empty<GUILayoutOption>());
		UiHelpers.DrawVirtual(Rows, _scroll, 46f, DrawRow);
		GUILayout.EndScrollView();
	}

	private static void RebuildIfNeeded()
	{
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		//IL_013b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		int num = Loc.Generation + CatalogHub.Stamp * 1000;
		num = num * 31 + (_t1 ? 1 : 0) + (_t2 ? 2 : 0) + (_t3 ? 4 : 0) + (_lunar ? 8 : 0) + (_boss ? 16 : 0) + (_void ? 32 : 0) + (_food ? 64 : 0) + (_other ? 128 : 0) + (_hidden ? 256 : 0);
		if (_cacheKey == num && _cacheSearch == _search)
		{
			return;
		}
		_cacheKey = num;
		_cacheSearch = _search;
		Rows.Clear();
		for (int i = 0; i < ItemCatalog.itemCount; i++)
		{
			ItemDef itemDef = ItemCatalog.GetItemDef((ItemIndex)i);
			if (Visible(itemDef))
			{
				Rows.Add(new Row
				{
					Def = itemDef,
					Title = GameUtil.DisplayName(itemDef) + "\n" + GameUtil.TierLabel(itemDef.tier),
					Name = ((Object)itemDef).name,
					Tip = GameUtil.Description(itemDef),
					Icon = UiHelpers.Icon(itemDef),
					Color = GameUtil.TierColor(itemDef.tier)
				});
			}
		}
	}

	private static bool Visible(ItemDef def)
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		if (!Object.op_Implicit((Object)(object)def))
		{
			return false;
		}
		if (def.hidden && !_hidden)
		{
			return false;
		}
		if (!TierOk(def.tier))
		{
			return false;
		}
		return GameUtil.Matches(_search, GameUtil.DisplayName(def), ((Object)def).name, def.nameToken);
	}

	private static bool TierOk(ItemTier tier)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Expected I4, but got Unknown
		switch ((int)tier)
		{
		case 0:
			return _t1;
		case 1:
			return _t2;
		case 2:
			return _t3;
		case 3:
			return _lunar;
		case 4:
			return _boss;
		case 6:
		case 7:
		case 8:
		case 9:
			return _void;
		case 10:
			return _food;
		default:
			return _other;
		}
	}

	private static void DrawRow(Row row)
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Expected O, but got Unknown
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Expected O, but got Unknown
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Expected O, but got Unknown
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Expected O, but got Unknown
		GUILayout.BeginHorizontal(GUIStyle.op_Implicit("box"), (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Height(42f) });
		UiHelpers.DrawIcon(row.Icon);
		Color color = GUI.color;
		GUI.color = row.Color;
		GUILayout.Label(new GUIContent(row.Title, row.Tip), (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(260f) });
		GUI.color = color;
		GUILayout.Label(new GUIContent(row.Name, row.Tip), (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(180f) });
		if (GUILayout.Button(new GUIContent("+" + _qty, row.Tip), (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(50f) }))
		{
			GameUtil.GiveItem(row.Def, _qty);
		}
		if (GUILayout.Button(new GUIContent("-" + _qty, row.Tip), (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(50f) }))
		{
			GameUtil.GiveItem(row.Def, -_qty);
		}
		if (GUILayout.Button(Loc.T("remove_all"), (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(100f) }))
		{
			foreach (NetworkUser item in GameUtil.Targets())
			{
				Inventory val = (Object.op_Implicit((Object)(object)item.master) ? item.master.inventory : null);
				if (Object.op_Implicit((Object)(object)val))
				{
					int itemCountPermanent = val.GetItemCountPermanent(row.Def);
					if (itemCountPermanent > 0)
					{
						val.RemoveItemPermanent(row.Def, itemCountPermanent);
					}
				}
			}
		}
		GUILayout.EndHorizontal();
	}
}
