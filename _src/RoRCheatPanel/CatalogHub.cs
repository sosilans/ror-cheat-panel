using System;
using System.Collections.Generic;
using System.Reflection;
using RoR2;
using RoR2.Skills;
using UnityEngine;

namespace RoRCheatPanel;

internal static class CatalogHub
{
	private struct Snapshot
	{
		public int Items;

		public int Equipment;

		public int Scenes;

		public int Bodies;

		public int Skills;

		public int Artifacts;

		public int Survivors;

		public int Achievements;

		public int Tiers;

		public int Difficulties;
	}

	private static bool _hooked;

	private static float _nextPoll;

	private static FieldInfo _difficultyDefs;

	public static int Stamp { get; private set; }

	public static bool Ready { get; private set; }

	public static int Items { get; private set; }

	public static int Equipment { get; private set; }

	public static int Scenes { get; private set; }

	public static int Bodies { get; private set; }

	public static int Skills { get; private set; }

	public static int Artifacts { get; private set; }

	public static int Survivors { get; private set; }

	public static int Achievements { get; private set; }

	public static int Tiers { get; private set; }

	public static int Difficulties { get; private set; }

	public static void Init()
	{
		if (!_hooked)
		{
			_hooked = true;
			RoR2Application.onLoad = (Action)Delegate.Combine(RoR2Application.onLoad, new Action(Refresh));
			RoR2Application.onLoadFinished = (Action)Delegate.Combine(RoR2Application.onLoadFinished, new Action(Refresh));
			ItemCatalog.availability.CallWhenAvailable((Action)Refresh);
			EquipmentCatalog.availability.CallWhenAvailable((Action)Refresh);
			ArtifactCatalog.availability.CallWhenAvailable((Action)Refresh);
			SceneCatalog.availability.CallWhenAvailable((Action)Refresh);
			BodyCatalog.availability.CallWhenAvailable((Action)Refresh);
			ItemTierCatalog.availability.CallWhenAvailable((Action)Refresh);
			UnlockableCatalog.availability.CallWhenAvailable((Action)Refresh);
			PickupCatalog.availability.CallWhenAvailable((Action)Refresh);
			AchievementManager.availability.CallWhenAvailable((Action)Refresh);
			SkillCatalog.skillsDefined.CallWhenAvailable((Action)Refresh);
			Refresh();
		}
	}

	public static void Tick()
	{
		if (!(Time.unscaledTime < _nextPoll))
		{
			_nextPoll = Time.unscaledTime + 2f;
			Refresh();
		}
	}

	public static void ForceRefresh()
	{
		Apply(ReadSnapshot(), force: true);
	}

	public static int DifficultySlotCount()
	{
		try
		{
			if (_difficultyDefs == null)
			{
				_difficultyDefs = typeof(DifficultyCatalog).GetField("difficultyDefs", BindingFlags.Static | BindingFlags.NonPublic);
			}
			DifficultyDef[] array = ((_difficultyDefs != null) ? (_difficultyDefs.GetValue(null) as DifficultyDef[]) : null);
			if (array != null && array.Length != 0)
			{
				return array.Length;
			}
		}
		catch
		{
		}
		return 11;
	}

	private static void Refresh()
	{
		Apply(ReadSnapshot(), force: false);
	}

	private static Snapshot ReadSnapshot()
	{
		return new Snapshot
		{
			Items = Safe(() => ItemCatalog.itemCount),
			Equipment = Safe(() => EquipmentCatalog.equipmentCount),
			Scenes = Safe(() => SceneCatalog.sceneDefCount),
			Bodies = Safe(() => BodyCatalog.bodyCount),
			Skills = CountSkills(),
			Artifacts = Safe(() => ArtifactCatalog.artifactCount),
			Survivors = Safe(() => SurvivorCatalog.survivorCount),
			Achievements = Safe(() => AchievementManager.achievementCount),
			Tiers = Safe(() => ItemTierCatalog.itemCount),
			Difficulties = DifficultySlotCount()
		};
	}

	private static void Apply(Snapshot snap, bool force)
	{
		if (!force && snap.Items == Items && snap.Equipment == Equipment && snap.Scenes == Scenes && snap.Bodies == Bodies && snap.Skills == Skills && snap.Artifacts == Artifacts && snap.Survivors == Survivors && snap.Achievements == Achievements && snap.Tiers == Tiers && snap.Difficulties == Difficulties)
		{
			Ready = snap.Items > 0 && snap.Scenes > 0 && snap.Bodies > 0;
			return;
		}
		Items = snap.Items;
		Equipment = snap.Equipment;
		Scenes = snap.Scenes;
		Bodies = snap.Bodies;
		Skills = snap.Skills;
		Artifacts = snap.Artifacts;
		Survivors = snap.Survivors;
		Achievements = snap.Achievements;
		Tiers = snap.Tiers;
		Difficulties = snap.Difficulties;
		Ready = Items > 0 && Scenes > 0 && Bodies > 0;
		Stamp++;
		if (Plugin.Log != null)
		{
			Plugin.Log.LogInfo((object)("Catalogs live: items=" + Items + " equip=" + Equipment + " scenes=" + Scenes + " bodies=" + Bodies + " skills=" + Skills + " artifacts=" + Artifacts + " survivors=" + Survivors + " ach=" + Achievements));
		}
	}

	private static int CountSkills()
	{
		try
		{
			IEnumerable<SkillDef> allSkillDefs = SkillCatalog.allSkillDefs;
			if (allSkillDefs == null)
			{
				return 0;
			}
			int num = 0;
			foreach (SkillDef item in allSkillDefs)
			{
				if (Object.op_Implicit((Object)(object)item))
				{
					num++;
				}
			}
			return num;
		}
		catch
		{
			return 0;
		}
	}

	private static int Safe(Func<int> read)
	{
		try
		{
			return read();
		}
		catch
		{
			return 0;
		}
	}
}
