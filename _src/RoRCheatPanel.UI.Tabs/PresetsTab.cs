using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoRCheatPanel.UI.Tabs;

internal static class PresetsTab
{
	private static string _name = "My build";

	private static Vector2 _scroll;

	private static List<CheatPreset> _presets;

	private static float _nextReload;

	public static void Draw()
	{
		//IL_0157: Unknown result type (might be due to invalid IL or missing references)
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_0166: Unknown result type (might be due to invalid IL or missing references)
		if (_presets == null || Time.realtimeSinceStartup > _nextReload)
		{
			Reload();
		}
		UiHelpers.DrawPlayerPicker();
		GUILayout.Label(Loc.T("presets_help"), Array.Empty<GUILayoutOption>());
		GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
		GUILayout.Label(Loc.T("name"), (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(40f) });
		_name = GUILayout.TextField(_name ?? "", (GUILayoutOption[])(object)new GUILayoutOption[2]
		{
			GUILayout.MinWidth(180f),
			GUILayout.Height(22f)
		});
		if (GUILayout.Button(Loc.T("create_preset"), (GUILayoutOption[])(object)new GUILayoutOption[2]
		{
			GUILayout.Width(200f),
			GUILayout.Height(28f)
		}))
		{
			CheatPreset cheatPreset = PresetStore.CaptureCurrent(_name);
			PresetStore.Save(cheatPreset);
			Reload();
			UiHelpers.Toast(Loc.F("saved", cheatPreset.Name));
		}
		if (GUILayout.Button(Loc.T("refresh"), (GUILayoutOption[])(object)new GUILayoutOption[2]
		{
			GUILayout.Width(160f),
			GUILayout.Height(28f)
		}))
		{
			Reload();
		}
		GUILayout.EndHorizontal();
		GUILayout.Label(Loc.F("folder", PresetStore.Folder), Array.Empty<GUILayoutOption>());
		_scroll = GUILayout.BeginScrollView(_scroll, Array.Empty<GUILayoutOption>());
		if (_presets != null)
		{
			foreach (CheatPreset preset in _presets)
			{
				DrawPreset(preset);
			}
		}
		GUILayout.EndScrollView();
	}

	private static void DrawPreset(CheatPreset preset)
	{
		GUILayout.BeginVertical(GUIStyle.op_Implicit("box"), Array.Empty<GUILayoutOption>());
		GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
		GUILayout.Label(Loc.F("items_count", preset.Name, preset.Items.Count), (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(320f) });
		if (GUILayout.Button(Loc.T("apply"), (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(90f) }))
		{
			PresetStore.Apply(preset, replace: false);
		}
		if (GUILayout.Button(Loc.T("replace_inv"), (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(160f) }))
		{
			UiHelpers.Confirm(Loc.F("confirm_replace", preset.Name), delegate
			{
				PresetStore.Apply(preset, replace: true);
			});
		}
		if (GUILayout.Button(Loc.T("delete"), (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(80f) }))
		{
			UiHelpers.Confirm(Loc.F("confirm_del_preset", preset.Name), delegate
			{
				PresetStore.Delete(preset);
				Reload();
			});
		}
		GUILayout.EndHorizontal();
		string text = "";
		for (int num = 0; num < preset.Items.Count && num < 8; num++)
		{
			if (num > 0)
			{
				text += ", ";
			}
			text = text + preset.Items[num].Id + " x" + preset.Items[num].Count;
		}
		if (preset.Items.Count > 8)
		{
			text += "...";
		}
		if (!string.IsNullOrEmpty(preset.EquipmentId))
		{
			text = text + "  |  EQ " + preset.EquipmentId;
		}
		GUILayout.Label(text, Array.Empty<GUILayoutOption>());
		GUILayout.EndVertical();
	}

	private static void Reload()
	{
		_presets = PresetStore.LoadAll();
		_nextReload = Time.realtimeSinceStartup + 8f;
	}
}
