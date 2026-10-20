using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RoR2;
using UnityEngine;

namespace RoRCheatPanel;

internal static class NoclipBounds
{
	private static readonly List<Behaviour> Behaviours = new List<Behaviour>();

	private static readonly List<bool> BehaviourWasEnabled = new List<bool>();

	private static readonly List<Collider> Colliders = new List<Collider>();

	private static readonly List<bool> ColliderWasEnabled = new List<bool>();

	private static readonly List<Type> ExtraTypes = new List<Type>();

	private static bool _patched;

	private static bool _disabled;

	private static float _nextScan;

	public static void Init()
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Expected O, but got Unknown
		if (_patched)
		{
			return;
		}
		_patched = true;
		CacheExtraTypes();
		Harmony harmony = new Harmony("com.rorcheatpanel.plugin.noclip");
		TryPatch(harmony, typeof(MapZone), "TryZoneStart", "PrefixCollider");
		TryPatch(harmony, typeof(MapZone), "TeleportBody", "PrefixBody");
		foreach (Type extraType in ExtraTypes)
		{
			TryPatch(harmony, extraType, "TryZoneStart", "PrefixCollider");
			TryPatch(harmony, extraType, "TeleportBody", "PrefixBody");
		}
	}

	public static void Tick(bool noclipOn)
	{
		if (noclipOn)
		{
			DisableZones();
		}
		else
		{
			RestoreZones();
		}
	}

	private static void TryPatch(Harmony harmony, Type type, string methodName, string prefix)
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Expected O, but got Unknown
		if (type == null)
		{
			return;
		}
		MethodInfo methodInfo = AccessTools.Method(type, methodName, (Type[])null, (Type[])null);
		if (methodInfo == null)
		{
			return;
		}
		try
		{
			harmony.Patch((MethodBase)methodInfo, new HarmonyMethod(typeof(NoclipBounds), prefix, (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
		}
		catch (Exception ex)
		{
			if (Plugin.Log != null)
			{
				Plugin.Log.LogDebug((object)("Noclip patch skip " + type.Name + "." + methodName + ": " + ex.Message));
			}
		}
	}

	private static void CacheExtraTypes()
	{
		ExtraTypes.Clear();
		Type[] types;
		try
		{
			types = typeof(MapZone).Assembly.GetTypes();
		}
		catch (ReflectionTypeLoadException ex)
		{
			types = ex.Types;
		}
		if (types == null)
		{
			return;
		}
		foreach (Type type in types)
		{
			if (!(type == null) && !(type == typeof(MapZone)) && typeof(MonoBehaviour).IsAssignableFrom(type))
			{
				string name = type.Name;
				if (name == "TeleportWhenOob" || name == "KillZone" || name.IndexOf("OutOfBounds", StringComparison.Ordinal) >= 0)
				{
					ExtraTypes.Add(type);
				}
			}
		}
	}

	private static void DisableZones()
	{
		if (!_disabled || !(Time.unscaledTime < _nextScan))
		{
			RestoreZones();
			DisableType(typeof(MapZone));
			for (int i = 0; i < ExtraTypes.Count; i++)
			{
				DisableType(ExtraTypes[i]);
			}
			_disabled = true;
			_nextScan = Time.unscaledTime + 2f;
		}
	}

	private static void DisableType(Type type)
	{
		Object[] array = Object.FindObjectsOfType(type);
		foreach (Object obj in array)
		{
			Behaviour val = (Behaviour)(object)((obj is Behaviour) ? obj : null);
			if (!Object.op_Implicit((Object)(object)val))
			{
				continue;
			}
			Behaviours.Add(val);
			BehaviourWasEnabled.Add(val.enabled);
			if (val.enabled)
			{
				val.enabled = false;
			}
			Collider[] components = ((Component)val).GetComponents<Collider>();
			for (int j = 0; j < components.Length; j++)
			{
				if (Object.op_Implicit((Object)(object)components[j]))
				{
					Colliders.Add(components[j]);
					ColliderWasEnabled.Add(components[j].enabled);
					if (components[j].enabled)
					{
						components[j].enabled = false;
					}
				}
			}
		}
	}

	private static void RestoreZones()
	{
		if (!_disabled && Behaviours.Count == 0)
		{
			return;
		}
		for (int i = 0; i < Behaviours.Count; i++)
		{
			if (Object.op_Implicit((Object)(object)Behaviours[i]) && BehaviourWasEnabled[i])
			{
				Behaviours[i].enabled = true;
			}
		}
		for (int j = 0; j < Colliders.Count; j++)
		{
			if (Object.op_Implicit((Object)(object)Colliders[j]) && ColliderWasEnabled[j])
			{
				Colliders[j].enabled = true;
			}
		}
		Behaviours.Clear();
		BehaviourWasEnabled.Clear();
		Colliders.Clear();
		ColliderWasEnabled.Clear();
		_disabled = false;
		_nextScan = 0f;
	}

	private static bool ShouldSkip(CharacterBody body)
	{
		if (CheatRuntime.Noclip && Object.op_Implicit((Object)(object)body) && Object.op_Implicit((Object)(object)GameUtil.LocalBody) && (Object)(object)body == (Object)(object)GameUtil.LocalBody)
		{
			return true;
		}
		return PlayerAdmin.IsNoclipBody(body);
	}

	private static CharacterBody BodyFromCollider(Collider other)
	{
		if (!Object.op_Implicit((Object)(object)other))
		{
			return null;
		}
		CharacterBody component = ((Component)other).GetComponent<CharacterBody>();
		if (Object.op_Implicit((Object)(object)component))
		{
			return component;
		}
		HurtBox component2 = ((Component)other).GetComponent<HurtBox>();
		if (Object.op_Implicit((Object)(object)component2) && Object.op_Implicit((Object)(object)component2.healthComponent))
		{
			return component2.healthComponent.body;
		}
		return ((Component)other).GetComponentInParent<CharacterBody>();
	}

	private static bool PrefixCollider(Collider other)
	{
		return !ShouldSkip(BodyFromCollider(other));
	}

	private static bool PrefixBody(CharacterBody characterBody)
	{
		return !ShouldSkip(characterBody);
	}
}
