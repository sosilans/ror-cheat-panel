using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HG;
using RoR2;
using UnityEngine;

namespace RoRCheatPanel.UI.Tabs;

internal static class StagesTab
{
	private struct Row
	{
		public SceneDef Def;

		public string Title;

		public string Tip;

		public Texture Preview;
	}

	private static string _search = "";

	private static Vector2 _scroll;

	private static bool _stages = true;

	private static bool _intermission = true;

	private static bool _other;

	private static readonly List<Row> Rows = new List<Row>(128);

	private static string _cacheSearch;

	private static int _cacheKey = int.MinValue;

	public static void Draw()
	{
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		GUILayout.Label(Loc.T("stages_help"), Array.Empty<GUILayoutOption>());
		if (Object.op_Implicit((Object)(object)Run.instance) && Object.op_Implicit((Object)(object)Run.instance.nextStageScene))
		{
			GUILayout.Label(Loc.F("assigned", GameUtil.DisplayName(Run.instance.nextStageScene)), Array.Empty<GUILayoutOption>());
		}
		_search = UiHelpers.Search(_search, Loc.T("search_stage"));
		UiHelpers.LiveCatalog(CatalogHub.Scenes);
		GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
		_stages = UiHelpers.ToggleButton(Loc.T("stages"), _stages, 100f);
		_intermission = UiHelpers.ToggleButton(Loc.T("intermission"), _intermission, 140f);
		_other = UiHelpers.ToggleButton(Loc.T("other_scenes"), _other, 100f);
		GUILayout.EndHorizontal();
		RebuildIfNeeded();
		_scroll = GUILayout.BeginScrollView(_scroll, Array.Empty<GUILayoutOption>());
		UiHelpers.DrawVirtual(Rows, _scroll, 50f, DrawRow);
		GUILayout.EndScrollView();
	}

	private static void RebuildIfNeeded()
	{
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		int num = Loc.Generation * 8 + CatalogHub.Stamp * 1000 + (_stages ? 1 : 0) + (_intermission ? 2 : 0) + (_other ? 4 : 0);
		if (_cacheKey == num && _cacheSearch == _search)
		{
			return;
		}
		_cacheKey = num;
		_cacheSearch = _search;
		Rows.Clear();
		Enumerator<SceneDef> enumerator = SceneCatalog.allSceneDefs.GetEnumerator();
		try
		{
			while (enumerator.MoveNext())
			{
				SceneDef current = enumerator.Current;
				if (Object.op_Implicit((Object)(object)current) && TypeOk(current.sceneType) && GameUtil.Matches(_search, GameUtil.DisplayName(current), current.cachedName, current.baseSceneName, current.nameToken))
				{
					Rows.Add(new Row
					{
						Def = current,
						Title = GameUtil.DisplayName(current) + "\n" + current.cachedName + "  [" + ((object)Unsafe.As<SceneType, SceneType>(ref current.sceneType)/*cast due to .constrained prefix*/).ToString() + "]",
						Tip = GameUtil.Description(current),
						Preview = current.previewTexture
					});
				}
			}
		}
		finally
		{
			((IDisposable)enumerator/*cast due to .constrained prefix*/).Dispose();
		}
	}

	private static bool TypeOk(SceneType type)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Expected I4, but got Unknown
		switch ((int)type)
		{
		case 1:
		case 5:
			return _stages;
		case 2:
		case 4:
			return _intermission;
		default:
			return _other;
		}
	}

	private static void DrawRow(Row row)
	{
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Expected O, but got Unknown
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Expected O, but got Unknown
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Expected O, but got Unknown
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_012a: Expected O, but got Unknown
		GUILayout.BeginHorizontal(GUIStyle.op_Implicit("box"), (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Height(46f) });
		if (Object.op_Implicit((Object)(object)row.Preview))
		{
			GUILayout.Label(new GUIContent(row.Preview, row.Tip), (GUILayoutOption[])(object)new GUILayoutOption[2]
			{
				GUILayout.Width(72f),
				GUILayout.Height(40f)
			});
		}
		GUILayout.Label(new GUIContent(row.Title, row.Tip), (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(360f) });
		if (GUILayout.Button(new GUIContent(Loc.T("next"), row.Tip), (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(100f) }))
		{
			GameUtil.SetNextStage(row.Def);
		}
		if (GUILayout.Button(new GUIContent(Loc.T("now"), row.Tip), (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(80f) }))
		{
			UiHelpers.Confirm(Loc.F("confirm_travel", GameUtil.DisplayName(row.Def)), delegate
			{
				GameUtil.TravelNow(row.Def);
			});
		}
		GUILayout.EndHorizontal();
	}
}
