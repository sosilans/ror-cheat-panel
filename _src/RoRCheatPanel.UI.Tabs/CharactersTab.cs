using System;
using System.Collections.Generic;
using RoR2;
using UnityEngine;

namespace RoRCheatPanel.UI.Tabs;

internal static class CharactersTab
{
	private struct Row
	{
		public GameObject Prefab;

		public string Display;

		public string BodyName;

		public string Kind;

		public string Tip;

		public Texture Icon;
	}

	private static string _search = "";

	private static Vector2 _scroll;

	private static bool _survivors = true;

	private static bool _monsters = true;

	private static bool _bosses = true;

	private static bool _drones = true;

	private static bool _other;

	private static readonly List<Row> Rows = new List<Row>(256);

	private static string _cacheSearch;

	private static int _cacheKey = int.MinValue;

	public static void Draw()
	{
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		UiHelpers.DrawPlayerPicker();
		GUILayout.Label(Loc.T("chars_help"), Array.Empty<GUILayoutOption>());
		_search = UiHelpers.Search(_search, Loc.T("search_body"));
		UiHelpers.LiveCatalog(CatalogHub.Bodies);
		GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
		_survivors = UiHelpers.ToggleButton(Loc.T("chars_survivors"), _survivors, 130f);
		_monsters = UiHelpers.ToggleButton(Loc.T("chars_monsters"), _monsters);
		_bosses = UiHelpers.ToggleButton(Loc.T("chars_bosses"), _bosses, 100f);
		_drones = UiHelpers.ToggleButton(Loc.T("chars_drones"), _drones, 100f);
		_other = UiHelpers.ToggleButton(Loc.T("chars_other"), _other, 100f);
		GUILayout.EndHorizontal();
		RebuildIfNeeded();
		_scroll = GUILayout.BeginScrollView(_scroll, Array.Empty<GUILayoutOption>());
		UiHelpers.DrawVirtual(Rows, _scroll, 46f, DrawRow);
		GUILayout.EndScrollView();
	}

	private static void RebuildIfNeeded()
	{
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		int num = Loc.Generation * 32 + CatalogHub.Stamp * 1000 + (_survivors ? 1 : 0) + (_monsters ? 2 : 0) + (_bosses ? 4 : 0) + (_drones ? 8 : 0) + (_other ? 16 : 0);
		if (_cacheKey == num && _cacheSearch == _search)
		{
			return;
		}
		_cacheKey = num;
		_cacheSearch = _search;
		Rows.Clear();
		int bodyCount = BodyCatalog.bodyCount;
		for (int i = 0; i < bodyCount; i++)
		{
			BodyIndex val = (BodyIndex)i;
			GameObject bodyPrefab = BodyCatalog.GetBodyPrefab(val);
			CharacterBody bodyPrefabBodyComponent = BodyCatalog.GetBodyPrefabBodyComponent(val);
			if (Object.op_Implicit((Object)(object)bodyPrefab) && Object.op_Implicit((Object)(object)bodyPrefabBodyComponent) && KindOk(GameUtil.ClassifyBody(bodyPrefab, bodyPrefabBodyComponent)))
			{
				string text = GameUtil.DisplayName(bodyPrefabBodyComponent);
				string text2 = BodyCatalog.GetBodyName(val);
				if (string.IsNullOrEmpty(text2))
				{
					text2 = ((Object)bodyPrefab).name;
				}
				if (GameUtil.Matches(_search, text, text2, ((Object)bodyPrefab).name, bodyPrefabBodyComponent.baseNameToken))
				{
					Rows.Add(new Row
					{
						Prefab = bodyPrefab,
						Display = text,
						BodyName = text2,
						Kind = GameUtil.BodyKindLabel(bodyPrefab, bodyPrefabBodyComponent),
						Tip = GameUtil.Description(bodyPrefabBodyComponent),
						Icon = bodyPrefabBodyComponent.portraitIcon
					});
				}
			}
		}
	}

	private static bool KindOk(BodyKind kind)
	{
		return kind switch
		{
			BodyKind.Survivor => _survivors, 
			BodyKind.Monster => _monsters, 
			BodyKind.Boss => _bosses, 
			BodyKind.Drone => _drones, 
			_ => _other, 
		};
	}

	private static void DrawRow(Row row)
	{
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Expected O, but got Unknown
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Expected O, but got Unknown
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Expected O, but got Unknown
		GUILayout.BeginHorizontal(GUIStyle.op_Implicit("box"), (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Height(42f) });
		UiHelpers.DrawIcon(row.Icon);
		GUILayout.Label(new GUIContent(row.Display + "\n" + row.Kind, row.Tip), (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(260f) });
		GUILayout.Label(new GUIContent(row.BodyName, row.Tip), (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(240f) });
		if (GUILayout.Button(new GUIContent(Loc.T("become_body"), row.Tip), (GUILayoutOption[])(object)new GUILayoutOption[2]
		{
			GUILayout.Width(90f),
			GUILayout.Height(28f)
		}))
		{
			GameUtil.TransformBodyPrefab(row.Prefab, row.Display);
		}
		GUILayout.EndHorizontal();
	}
}
