using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RoRCheatPanel;

internal static class HudClickBlocker
{
	private static readonly List<GraphicRaycaster> Raycasters = new List<GraphicRaycaster>();

	private static readonly List<bool> RaycasterWasEnabled = new List<bool>();

	private static readonly List<EventSystem> EventSystems = new List<EventSystem>();

	private static bool _blocked;

	private static float _nextScan;

	public static void Tick(bool blockHud)
	{
		if (blockHud)
		{
			Block();
		}
		else
		{
			Restore();
		}
	}

	private static void Block()
	{
		if (_blocked && Time.unscaledTime < _nextScan)
		{
			KeepDisabled();
			return;
		}
		Restore();
		GraphicRaycaster[] array = Object.FindObjectsOfType<GraphicRaycaster>();
		foreach (GraphicRaycaster val in array)
		{
			if (Object.op_Implicit((Object)(object)val))
			{
				Raycasters.Add(val);
				RaycasterWasEnabled.Add(((Behaviour)val).enabled);
				if (((Behaviour)val).enabled)
				{
					((Behaviour)val).enabled = false;
				}
			}
		}
		EventSystem[] array2 = Object.FindObjectsOfType<EventSystem>();
		foreach (EventSystem val2 in array2)
		{
			if (Object.op_Implicit((Object)(object)val2))
			{
				EventSystems.Add(val2);
				val2.sendNavigationEvents = false;
			}
		}
		_blocked = true;
		_nextScan = Time.unscaledTime + 2f;
	}

	private static void KeepDisabled()
	{
		for (int i = 0; i < Raycasters.Count; i++)
		{
			if (Object.op_Implicit((Object)(object)Raycasters[i]) && ((Behaviour)Raycasters[i]).enabled)
			{
				((Behaviour)Raycasters[i]).enabled = false;
			}
		}
	}

	private static void Restore()
	{
		if (!_blocked && Raycasters.Count == 0 && EventSystems.Count == 0)
		{
			return;
		}
		for (int i = 0; i < Raycasters.Count; i++)
		{
			if (Object.op_Implicit((Object)(object)Raycasters[i]) && RaycasterWasEnabled[i])
			{
				((Behaviour)Raycasters[i]).enabled = true;
			}
		}
		for (int j = 0; j < EventSystems.Count; j++)
		{
			if (Object.op_Implicit((Object)(object)EventSystems[j]))
			{
				EventSystems[j].sendNavigationEvents = true;
			}
		}
		Raycasters.Clear();
		RaycasterWasEnabled.Clear();
		EventSystems.Clear();
		_blocked = false;
		_nextScan = 0f;
	}
}
