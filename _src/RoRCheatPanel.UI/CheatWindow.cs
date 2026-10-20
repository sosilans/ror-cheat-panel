using System;
using RoR2;
using RoRCheatPanel.UI.Tabs;
using UnityEngine;

namespace RoRCheatPanel.UI;

internal static class CheatWindow
{
	private static Rect _rect = new Rect(48f, 48f, 1140f, 760f);

	private static int _cat;

	private static readonly int[] Sub = new int[6];

	private static string[] _catNames;

	private static int _catLang = -1;

	private static string _status;

	private static float _statusAt;

	public static bool Opened { get; private set; }

	public static void Toggle()
	{
		Opened = !Opened;
		if (!Opened)
		{
			CheatRuntime.OnWindowClosed();
		}
	}

	public static void OnGUI()
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Expected O, but got Unknown
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		if (!Opened)
		{
			UiHelpers.DrawToast();
			return;
		}
		UiHelpers.ApplySkin();
		float num = Mathf.Clamp(CheatConfig.UiScale.Value, 0.6f, 2f);
		Matrix4x4 matrix = GUI.matrix;
		GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(num, num, 1f));
		Color backgroundColor = GUI.backgroundColor;
		Color windowColor = (CheatRuntime.Disco ? Color.HSVToRGB(Time.unscaledTime * 0.12f % 1f, 0.55f, 0.55f) : UiTheme.Window);
		windowColor.a *= Mathf.Clamp(CheatConfig.UiOpacity != null ? CheatConfig.UiOpacity.Value : 1f, 0.25f, 1f);
		GUI.backgroundColor = windowColor;
		string text = (CheatRuntime.Disco ? Loc.F("window_disco", "3.6.3") : Loc.F("window_title", "3.6.3"));
		_rect = GUI.Window(5423326, _rect, new WindowFunction(Draw), text);
		GUI.backgroundColor = backgroundColor;
		GUI.matrix = matrix;
		UiHelpers.DrawModal();
		UiHelpers.DrawToast();
	}

	private static void Draw(int id)
	{
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		UiHelpers.ApplySkin();
		GUILayout.BeginVertical(Array.Empty<GUILayoutOption>());
		UiHelpers.HostBanner();
		DrawStatus();
		DrawNav();
		GUILayout.Space(4f);
		try
		{
			DrawCategory();
		}
		catch (Exception ex)
		{
			GUILayout.Label(Loc.F("tab_error", ex.Message), Array.Empty<GUILayoutOption>());
		}
		GUILayout.EndVertical();
		UiHelpers.DrawTooltip();
		GUI.DragWindow(new Rect(0f, 0f, _rect.width, 22f));
	}

	private static void DrawNav()
	{
		if (_catNames == null || _catLang != Loc.Generation)
		{
			_catLang = Loc.Generation;
			_catNames = new string[6]
			{
				Loc.T("cat_loot"),
				Loc.T("cat_char"),
				Loc.T("cat_world"),
				Loc.T("cat_tools"),
				Loc.T("cat_fun"),
				Loc.T("cat_panel")
			};
		}
		_cat = UiHelpers.DrawPills(_catNames, _cat, 32f);
		string[] array = SubNames(_cat);
		if (array != null && array.Length > 1)
		{
			GUILayout.Space(2f);
			Sub[_cat] = UiHelpers.DrawPills(array, Sub[_cat]);
		}
	}

	private static string[] SubNames(int cat)
	{
		return cat switch
		{
			0 => new string[3]
			{
				Loc.T("tab_items"),
				Loc.T("tab_equip"),
				Loc.T("tab_presets")
			}, 
			1 => new string[3]
			{
				Loc.T("tab_skills"),
				Loc.T("tab_chars"),
				Loc.T("tab_ach")
			}, 
			2 => new string[4]
			{
				Loc.T("tab_run"),
				Loc.T("tab_stages"),
				Loc.T("tab_teams"),
				Loc.T("tab_players")
			}, 
			_ => null, 
		};
	}

	private static void DrawCategory()
	{
		switch (_cat)
		{
		case 0:
			if (Sub[0] == 1)
			{
				EquipmentTab.Draw();
			}
			else if (Sub[0] == 2)
			{
				PresetsTab.Draw();
			}
			else
			{
				ItemsTab.Draw();
			}
			break;
		case 1:
			if (Sub[1] == 1)
			{
				CharactersTab.Draw();
			}
			else if (Sub[1] == 2)
			{
				AchievementsTab.Draw();
			}
			else
			{
				SkillsTab.Draw();
			}
			break;
		case 2:
			if (Sub[2] == 1)
			{
				StagesTab.Draw();
			}
			else if (Sub[2] == 2)
			{
				TeamsTab.Draw();
			}
			else if (Sub[2] == 3)
			{
				PlayersTab.Draw();
			}
			else
			{
				RunTab.Draw();
			}
			break;
		case 3:
			ToolsTab.Draw();
			break;
		case 4:
			FunTab.Draw();
			break;
		default:
			AboutTab.Draw();
			break;
		}
	}

	private static void DrawStatus()
	{
		if (_status == null || Time.unscaledTime - _statusAt > 0.25f)
		{
			CharacterBody localBody = GameUtil.LocalBody;
			CharacterMaster localMaster = GameUtil.LocalMaster;
			string text = Loc.T("run") + ": " + (GameUtil.InRun ? Loc.T("yes") : Loc.T("no"));
			text = text + "  ·  " + Loc.T("host") + ": " + (GameUtil.IsHost ? Loc.T("yes") : Loc.T("no"));
			if (Object.op_Implicit((Object)(object)localMaster))
			{
				text = text + "  ·  $" + localMaster.money;
			}
			NetworkUser localNetworkUser = GameUtil.LocalNetworkUser;
			if (Object.op_Implicit((Object)(object)localNetworkUser))
			{
				text = text + "  ·  " + Loc.T("moon") + " " + localNetworkUser.lunarCoins;
			}
			if (Object.op_Implicit((Object)(object)localBody))
			{
				text = text + "  ·  HP " + Mathf.RoundToInt(Object.op_Implicit((Object)(object)localBody.healthComponent) ? localBody.healthComponent.health : 0f) + "/" + Mathf.RoundToInt(localBody.maxHealth);
			}
			text = text + "  ·  God " + (CheatRuntime.GodMode ? "ON" : "off");
			text = text + "  ·  Noclip " + (CheatRuntime.Noclip ? "ON" : "off");
			text = text + "  ·  x" + CheatRuntime.TimeScale.ToString("0.00");
			_status = text;
			_statusAt = Time.unscaledTime;
		}
		GUILayout.Label(_status, Array.Empty<GUILayoutOption>());
	}
}
