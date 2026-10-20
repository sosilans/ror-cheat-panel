using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Text.RegularExpressions;
using KinematicCharacterController;
using RoR2;
using RoR2.Skills;
using RoRCheatPanel.UI;
using UnityEngine;
using UnityEngine.Networking;

namespace RoRCheatPanel;

internal static class GameUtil
{
	public static readonly List<NetworkUser> SelectedUsers = new List<NetworkUser>();

	private static MethodInfo _fireScanner;

	private static MethodInfo _fireGummy;

	private static MethodInfo _fireDrones;

	private static bool _methodsResolved;

	private static readonly Regex RichTag = new Regex("<[^>]+>", RegexOptions.Compiled);

	public static bool IsHost => NetworkServer.active;

	public static bool InRun => (Object)(object)Run.instance != (Object)null;

	public static LocalUser LocalUser => LocalUserManager.GetFirstLocalUser();

	public static NetworkUser LocalNetworkUser
	{
		get
		{
			LocalUser localUser = LocalUser;
			if (localUser != null && Object.op_Implicit((Object)(object)localUser.currentNetworkUser))
			{
				return localUser.currentNetworkUser;
			}
			CharacterBody val = ((localUser != null) ? localUser.cachedBody : null);
			if (Object.op_Implicit((Object)(object)val) && Object.op_Implicit((Object)(object)val.master))
			{
				foreach (NetworkUser readOnlyInstances in NetworkUser.readOnlyInstancesList)
				{
					if (Object.op_Implicit((Object)(object)readOnlyInstances) && (Object)(object)readOnlyInstances.master == (Object)(object)val.master)
					{
						return readOnlyInstances;
					}
				}
			}
			if (NetworkUser.readOnlyLocalPlayersList != null && NetworkUser.readOnlyLocalPlayersList.Count > 0)
			{
				return NetworkUser.readOnlyLocalPlayersList[0];
			}
			return null;
		}
	}

	public static CharacterBody LocalBody
	{
		get
		{
			LocalUser localUser = LocalUser;
			if (localUser != null && Object.op_Implicit((Object)(object)localUser.cachedBody))
			{
				return localUser.cachedBody;
			}
			NetworkUser localNetworkUser = LocalNetworkUser;
			if (!Object.op_Implicit((Object)(object)localNetworkUser))
			{
				return null;
			}
			return localNetworkUser.GetCurrentBody();
		}
	}

	public static CharacterMaster LocalMaster
	{
		get
		{
			LocalUser localUser = LocalUser;
			if (localUser != null && Object.op_Implicit((Object)(object)localUser.cachedMaster))
			{
				return localUser.cachedMaster;
			}
			NetworkUser localNetworkUser = LocalNetworkUser;
			if (!Object.op_Implicit((Object)(object)localNetworkUser))
			{
				return null;
			}
			return localNetworkUser.master;
		}
	}

	public static string Lang(string token)
	{
		if (string.IsNullOrEmpty(token))
		{
			return "";
		}
		try
		{
			string text = Language.GetString(token, Loc.GameCode);
			if (string.IsNullOrEmpty(text) || text == token)
			{
				text = Language.GetString(token);
			}
			if (string.IsNullOrEmpty(text) || text == token)
			{
				return token;
			}
			return StripRich(text);
		}
		catch
		{
			return token;
		}
	}

	public static string StripRich(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return "";
		}
		return RichTag.Replace(text, "").Trim();
	}

	public static string Description(ItemDef def)
	{
		if (!Object.op_Implicit((Object)(object)def))
		{
			return "";
		}
		return JoinTip(DisplayName(def), Lang(def.pickupToken), Lang(def.descriptionToken));
	}

	public static string Description(EquipmentDef def)
	{
		if (!Object.op_Implicit((Object)(object)def))
		{
			return "";
		}
		return JoinTip(DisplayName(def), Lang(def.pickupToken), Lang(def.descriptionToken));
	}

	public static string Description(ArtifactDef def)
	{
		if (!Object.op_Implicit((Object)(object)def))
		{
			return "";
		}
		return JoinTip(DisplayName(def), Lang(def.descriptionToken));
	}

	public static string Description(SkillDef def)
	{
		if (!Object.op_Implicit((Object)(object)def))
		{
			return "";
		}
		return JoinTip(DisplayName(def), Lang(def.skillDescriptionToken));
	}

	public static string Description(AchievementDef def)
	{
		if (def == null)
		{
			return "";
		}
		return JoinTip(DisplayName(def), Lang(def.descriptionToken));
	}

	public static string Description(SurvivorDef def)
	{
		if (!Object.op_Implicit((Object)(object)def))
		{
			return "";
		}
		return JoinTip(DisplayName(def), Lang(def.descriptionToken));
	}

	public static string Description(SceneDef def)
	{
		if (!Object.op_Implicit((Object)(object)def))
		{
			return "";
		}
		return JoinTip(DisplayName(def), Lang(def.loreToken));
	}

	private static string JoinTip(params string[] parts)
	{
		string text = "";
		foreach (string text2 in parts)
		{
			if (!string.IsNullOrWhiteSpace(text2) && text.IndexOf(text2, StringComparison.Ordinal) < 0)
			{
				if (text.Length > 0)
				{
					text += "\n";
				}
				text += text2;
			}
		}
		return text;
	}

	public static string DisplayName(ItemDef def)
	{
		if (!Object.op_Implicit((Object)(object)def))
		{
			return "?";
		}
		string text = Lang(def.nameToken);
		if (!string.IsNullOrEmpty(text))
		{
			return text;
		}
		return ((Object)def).name;
	}

	public static string DisplayName(EquipmentDef def)
	{
		if (!Object.op_Implicit((Object)(object)def))
		{
			return "?";
		}
		string text = Lang(def.nameToken);
		if (!string.IsNullOrEmpty(text))
		{
			return text;
		}
		return ((Object)def).name;
	}

	public static string DisplayName(SkillDef def)
	{
		if (!Object.op_Implicit((Object)(object)def))
		{
			return "?";
		}
		string text = Lang(def.skillNameToken);
		if (!string.IsNullOrEmpty(text) && text != def.skillNameToken)
		{
			return text;
		}
		if (!string.IsNullOrEmpty(def.skillName))
		{
			return def.skillName;
		}
		return def.name;
	}

	public static string DisplayName(AchievementDef def)
	{
		if (def == null)
		{
			return "?";
		}
		string text = Lang(def.nameToken);
		if (!string.IsNullOrEmpty(text))
		{
			return text;
		}
		return def.identifier;
	}

	public static string DisplayName(SceneDef def)
	{
		if (!Object.op_Implicit((Object)(object)def))
		{
			return "?";
		}
		string text = Lang(def.nameToken);
		if (!string.IsNullOrEmpty(text))
		{
			return text;
		}
		return def.cachedName;
	}

	public static string DisplayName(ArtifactDef def)
	{
		if (!Object.op_Implicit((Object)(object)def))
		{
			return "?";
		}
		string text = Lang(def.nameToken);
		if (!string.IsNullOrEmpty(text))
		{
			return text;
		}
		return def.cachedName;
	}

	public static string DisplayName(SurvivorDef def)
	{
		if (!Object.op_Implicit((Object)(object)def))
		{
			return "?";
		}
		string text = Lang(def.displayNameToken);
		if (!string.IsNullOrEmpty(text))
		{
			return text;
		}
		return def.cachedName;
	}

	public static string DisplayName(CharacterBody body)
	{
		if (!Object.op_Implicit((Object)(object)body))
		{
			return "?";
		}
		string text = Lang(body.baseNameToken);
		if (!string.IsNullOrEmpty(text) && text != body.baseNameToken)
		{
			return text;
		}
		string displayName = body.GetDisplayName();
		if (!string.IsNullOrEmpty(displayName))
		{
			return displayName;
		}
		return ((Object)body).name;
	}

	public static string Description(CharacterBody body)
	{
		if (!Object.op_Implicit((Object)(object)body))
		{
			return "";
		}
		return JoinTip(DisplayName(body), Lang(body.subtitleNameToken), ((Object)body).name);
	}

	public static string BodyKindLabel(GameObject prefab, CharacterBody body)
	{
		return ClassifyBody(prefab, body) switch
		{
			BodyKind.Survivor => Loc.T("chars_survivors"), 
			BodyKind.Boss => Loc.T("chars_bosses"), 
			BodyKind.Drone => Loc.T("chars_drones"), 
			BodyKind.Monster => Loc.T("chars_monsters"), 
			_ => Loc.T("chars_other"), 
		};
	}

	public static BodyKind ClassifyBody(GameObject prefab, CharacterBody body)
	{
		if (Object.op_Implicit((Object)(object)prefab) && Object.op_Implicit((Object)(object)SurvivorCatalog.FindSurvivorDefFromBody(prefab)))
		{
			return BodyKind.Survivor;
		}
		string n = (Object.op_Implicit((Object)(object)prefab) ? ((Object)prefab).name : (Object.op_Implicit((Object)(object)body) ? ((Object)body).name : ""));
		if (NameLooksLikeDrone(n))
		{
			return BodyKind.Drone;
		}
		if (Object.op_Implicit((Object)(object)body) && body.isChampion)
		{
			return BodyKind.Boss;
		}
		if (NameLooksLikeOther(n))
		{
			return BodyKind.Other;
		}
		return BodyKind.Monster;
	}

	private static bool NameLooksLikeDrone(string n)
	{
		return ContainsAny(n, "Drone", "Turret", "MegaDrone", "EquipmentDrone", "EmergencyDrone", "FlameDrone");
	}

	private static bool NameLooksLikeOther(string n)
	{
		return ContainsAny(n, "Display", "Pod", "Corpse", "Dead", "Fake", "Temp", "Projectile", "Joint", "WeakPoint", "HurtBox", "Spectator", "Attachment", "Dummy");
	}

	private static bool ContainsAny(string text, params string[] parts)
	{
		if (string.IsNullOrEmpty(text))
		{
			return false;
		}
		for (int i = 0; i < parts.Length; i++)
		{
			if (text.IndexOf(parts[i], StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return true;
			}
		}
		return false;
	}

	public static IEnumerable<NetworkUser> AllUsers()
	{
		if (NetworkUser.readOnlyInstancesList == null)
		{
			yield break;
		}
		foreach (NetworkUser readOnlyInstances in NetworkUser.readOnlyInstancesList)
		{
			if (Object.op_Implicit((Object)(object)readOnlyInstances))
			{
				yield return readOnlyInstances;
			}
		}
	}

	public static IEnumerable<NetworkUser> Targets()
	{
		CleanupSelected();
		if (SelectedUsers.Count > 0)
		{
			foreach (NetworkUser selectedUser in SelectedUsers)
			{
				yield return selectedUser;
			}
			yield break;
		}
		NetworkUser localNetworkUser = LocalNetworkUser;
		if (Object.op_Implicit((Object)(object)localNetworkUser))
		{
			yield return localNetworkUser;
		}
	}

	public static void CleanupSelected()
	{
		for (int num = SelectedUsers.Count - 1; num >= 0; num--)
		{
			if (!Object.op_Implicit((Object)(object)SelectedUsers[num]))
			{
				SelectedUsers.RemoveAt(num);
			}
		}
	}

	public static bool RequireHost(string action)
	{
		if (IsHost)
		{
			return true;
		}
		UiHelpers.Toast(Loc.F("need_host", action));
		return false;
	}

	public static void GiveItem(ItemDef def, int count)
	{
		if (!Object.op_Implicit((Object)(object)def) || count == 0 || !RequireHost(Loc.T("act_items")))
		{
			return;
		}
		foreach (NetworkUser item in Targets())
		{
			Inventory val = (Object.op_Implicit((Object)(object)item.master) ? item.master.inventory : null);
			if (Object.op_Implicit((Object)(object)val))
			{
				if (count > 0)
				{
					val.GiveItemPermanent(def, count);
				}
				else
				{
					val.RemoveItemPermanent(def, -count);
				}
			}
		}
		UiHelpers.Toast(((count > 0) ? "+" : "") + count + " " + DisplayName(def));
	}

	public static void ClearInventory()
	{
		if (!RequireHost(Loc.T("act_clear")))
		{
			return;
		}
		foreach (NetworkUser item in Targets())
		{
			Inventory val = (Object.op_Implicit((Object)(object)item.master) ? item.master.inventory : null);
			if (!Object.op_Implicit((Object)(object)val))
			{
				continue;
			}
			for (int i = 0; i < ItemCatalog.itemCount; i++)
			{
				ItemDef itemDef = ItemCatalog.GetItemDef((ItemIndex)i);
				if (Object.op_Implicit((Object)(object)itemDef))
				{
					int itemCountPermanent = val.GetItemCountPermanent(itemDef);
					if (itemCountPermanent > 0)
					{
						val.RemoveItemPermanent(itemDef, itemCountPermanent);
					}
				}
			}
		}
		UiHelpers.Toast(Loc.T("inv_cleared"));
	}

	public static void GiveAllVisibleItems(Predicate<ItemDef> filter, int count)
	{
		if (!RequireHost(Loc.T("act_all_items")))
		{
			return;
		}
		int num = 0;
		for (int i = 0; i < ItemCatalog.itemCount; i++)
		{
			ItemDef itemDef = ItemCatalog.GetItemDef((ItemIndex)i);
			if (!Object.op_Implicit((Object)(object)itemDef) || (filter != null && !filter(itemDef)))
			{
				continue;
			}
			foreach (NetworkUser item in Targets())
			{
				Inventory val = (Object.op_Implicit((Object)(object)item.master) ? item.master.inventory : null);
				if (Object.op_Implicit((Object)(object)val))
				{
					val.GiveItemPermanent(itemDef, count);
				}
			}
			num++;
		}
		UiHelpers.Toast(Loc.F("given_items", num));
	}

	public static void SetEquipment(EquipmentDef def)
	{
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		if (!RequireHost(Loc.T("act_equip")))
		{
			return;
		}
		foreach (NetworkUser item in Targets())
		{
			Inventory val = (Object.op_Implicit((Object)(object)item.master) ? item.master.inventory : null);
			if (Object.op_Implicit((Object)(object)val))
			{
				val.SetEquipmentIndex((EquipmentIndex)((!Object.op_Implicit((Object)(object)def)) ? (-1) : ((int)def.equipmentIndex)), (Object)(object)def == (Object)null);
			}
		}
		UiHelpers.Toast(Object.op_Implicit((Object)(object)def) ? DisplayName(def) : Loc.T("equip_off"));
	}

	public static void RechargeEquipment()
	{
		if (!RequireHost(Loc.T("act_recharge")))
		{
			return;
		}
		foreach (NetworkUser item in Targets())
		{
			Inventory val = (Object.op_Implicit((Object)(object)item.master) ? item.master.inventory : null);
			if (Object.op_Implicit((Object)(object)val))
			{
				val.DeductActiveEquipmentCooldown(9999f);
				val.RestockActiveEquipmentCharges(99);
			}
		}
		UiHelpers.Toast(Loc.T("equip_recharged"));
	}

	public static void SetSkill(SkillDef def, SkillSlot slot)
	{
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		if (!Object.op_Implicit((Object)(object)def) || !RequireHost(Loc.T("act_skill")))
		{
			return;
		}
		foreach (NetworkUser item in Targets())
		{
			CharacterBody currentBody = item.GetCurrentBody();
			SkillLocator val = (Object.op_Implicit((Object)(object)currentBody) ? currentBody.skillLocator : null);
			GenericSkill val2 = (Object.op_Implicit((Object)(object)val) ? val.GetSkill(slot) : null);
			if (Object.op_Implicit((Object)(object)val2))
			{
				val2.SetBaseSkill(def);
			}
		}
		UiHelpers.Toast(SlotLabel(slot) + " = " + DisplayName(def));
	}

	public static void ResetSkills()
	{
		if (!RequireHost(Loc.T("act_reset_skills")))
		{
			return;
		}
		foreach (NetworkUser item in Targets())
		{
			CharacterBody currentBody = item.GetCurrentBody();
			if (Object.op_Implicit((Object)(object)currentBody) && Object.op_Implicit((Object)(object)currentBody.skillLocator))
			{
				currentBody.skillLocator.ResetSkills();
			}
		}
		UiHelpers.Toast(Loc.T("skills_reset"));
	}

	public static void ResetSkillCooldowns()
	{
		if (!RequireHost(Loc.T("act_reset_cd")))
		{
			return;
		}
		foreach (NetworkUser item in Targets())
		{
			CharacterBody currentBody = item.GetCurrentBody();
			SkillLocator val = (Object.op_Implicit((Object)(object)currentBody) ? currentBody.skillLocator : null);
			if (!Object.op_Implicit((Object)(object)val))
			{
				continue;
			}
			GenericSkill[] allSkills = val.AllSkills;
			foreach (GenericSkill val2 in allSkills)
			{
				if (Object.op_Implicit((Object)(object)val2))
				{
					val2.Reset();
					val2.stock = val2.maxStock;
				}
			}
		}
		UiHelpers.Toast(Loc.T("cd_reset"));
	}

	public unsafe static string SlotLabel(SkillSlot slot)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Expected I4, but got Unknown
		return (int)slot switch
		{
			0 => "M1", 
			1 => "M2", 
			2 => "Shift", 
			3 => "R", 
			_ => ((object)(*(SkillSlot*)(&slot))/*cast due to .constrained prefix*/).ToString(), 
		};
	}

	public static void GrantAchievement(AchievementDef def, bool toast = true)
	{
		if (def == null)
		{
			return;
		}
		LocalUser localUser = LocalUser;
		if (localUser == null)
		{
			if (toast)
			{
				UiHelpers.Toast(Loc.T("no_profile"));
			}
			return;
		}
		UserAchievementManager userAchievementManager = AchievementManager.GetUserAchievementManager(localUser);
		if (userAchievementManager != null)
		{
			userAchievementManager.GrantAchievement(def);
		}
		if (localUser.userProfile != null && !string.IsNullOrEmpty(def.unlockableRewardIdentifier))
		{
			UnlockableDef unlockableDef = UnlockableCatalog.GetUnlockableDef(def.unlockableRewardIdentifier);
			if (Object.op_Implicit((Object)(object)unlockableDef))
			{
				localUser.userProfile.GrantUnlockable(unlockableDef);
			}
		}
		if (toast)
		{
			UiHelpers.Toast(Loc.F("ach_one", DisplayName(def)));
		}
	}

	public unsafe static void GrantAllAchievements()
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		int num = 0;
		GenericStaticEnumerable<AchievementDef, Enumerator> allAchievementDefs = AchievementManager.allAchievementDefs;
		Enumerator enumerator = allAchievementDefs.GetEnumerator();
		try
		{
			while (enumerator.MoveNext())
			{
				AchievementDef current = enumerator.Current;
				if (current != null)
				{
					GrantAchievement(current, toast: false);
					num++;
				}
			}
		}
		finally
		{
			((IDisposable)(*(Enumerator*)(&enumerator))/*cast due to .constrained prefix*/).Dispose();
		}
		UiHelpers.Toast(Loc.F("ach_many", num));
	}

	public static void GrantAchievementToTargets(AchievementDef def)
	{
		if (def == null)
		{
			return;
		}
		CleanupSelected();
		int num = 0;
		int num2 = 0;
		foreach (NetworkUser item in Targets())
		{
			if ((Object)(object)item == (Object)(object)LocalNetworkUser)
			{
				GrantAchievement(def, toast: false);
				num++;
			}
			else if (IsHost && AdminNet.SendGrant(item, def.identifier))
			{
				num2++;
			}
		}
		if (num2 > 0)
		{
			UiHelpers.Toast(Loc.F("admin_ach_sent", DisplayName(def)));
		}
		else if (num > 0)
		{
			UiHelpers.Toast(Loc.F("ach_one", DisplayName(def)));
		}
		else
		{
			UiHelpers.Toast(Loc.T("admin_need_sel"));
		}
	}

	public static void GrantAllAchievementsToTargets()
	{
		CleanupSelected();
		int num = 0;
		bool flag = false;
		foreach (NetworkUser item in Targets())
		{
			if ((Object)(object)item == (Object)(object)LocalNetworkUser)
			{
				GrantAllAchievements();
				flag = true;
			}
			else if (IsHost && AdminNet.SendGrantAll(item))
			{
				num++;
			}
		}
		if (num > 0)
		{
			UiHelpers.Toast(Loc.F("admin_ach_sent", num));
		}
		else if (!flag)
		{
			UiHelpers.Toast(Loc.T("admin_need_sel"));
		}
	}

	public static void SetTeam(TeamIndex team)
	{
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		if (!RequireHost(Loc.T("act_team")))
		{
			return;
		}
		foreach (NetworkUser item in Targets())
		{
			if (Object.op_Implicit((Object)(object)item.master))
			{
				item.master.teamIndex = team;
			}
			CharacterBody currentBody = item.GetCurrentBody();
			if (Object.op_Implicit((Object)(object)currentBody) && Object.op_Implicit((Object)(object)currentBody.teamComponent))
			{
				currentBody.teamComponent.teamIndex = team;
			}
		}
		UiHelpers.Toast(Loc.F("team_set", team));
	}

	public static void KillMaster(CharacterMaster master)
	{
		if (Object.op_Implicit((Object)(object)master) && RequireHost(Loc.T("act_kill")))
		{
			master.TrueKill();
		}
	}

	public static void KillTeam(TeamIndex team)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		if (!RequireHost(Loc.T("act_kill_team")))
		{
			return;
		}
		ReadOnlyCollection<TeamComponent> teamMembers = TeamComponent.GetTeamMembers(team);
		int num = 0;
		if (teamMembers != null)
		{
			foreach (TeamComponent item in new List<TeamComponent>(teamMembers))
			{
				if (Object.op_Implicit((Object)(object)item) && Object.op_Implicit((Object)(object)item.body) && Object.op_Implicit((Object)(object)item.body.master))
				{
					item.body.master.TrueKill();
					num++;
				}
			}
		}
		UiHelpers.Toast(Loc.F("killed", team, num));
	}

	public static void KillEnemies()
	{
		KillTeam((TeamIndex)2);
		KillTeam((TeamIndex)4);
		KillTeam((TeamIndex)3);
	}

	public static void SetNextStage(SceneDef scene)
	{
		if (Object.op_Implicit((Object)(object)scene) && InRun && RequireHost(Loc.T("act_stage")))
		{
			Run.instance.nextStageScene = scene;
			UiHelpers.Toast(Loc.F("next_stage", DisplayName(scene)));
		}
	}

	public static void TravelNow(SceneDef scene)
	{
		if (Object.op_Implicit((Object)(object)scene) && InRun && RequireHost(Loc.T("act_travel")))
		{
			Run.instance.nextStageScene = scene;
			if (Object.op_Implicit((Object)(object)Stage.instance))
			{
				Stage.instance.BeginAdvanceStage(scene);
			}
			else
			{
				Run.instance.AdvanceStage(scene);
			}
			UiHelpers.Toast(Loc.F("travel", DisplayName(scene)));
		}
	}

	public static void SetDifficulty(DifficultyIndex index)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		if (InRun && RequireHost(Loc.T("act_diff")))
		{
			Run.instance.selectedDifficulty = index;
			Run.instance.RecalculateDifficultyCoefficent();
			UiHelpers.Toast(Loc.F("diff_set", index));
		}
	}

	public static void SetArtifact(ArtifactDef def, bool enabled)
	{
		if (Object.op_Implicit((Object)(object)def) && !((Object)(object)RunArtifactManager.instance == (Object)null) && RequireHost(Loc.T("act_art")))
		{
			RunArtifactManager.instance.SetArtifactEnabledServer(def, enabled);
		}
	}

	public static void GiveMoney(int amount)
	{
		if (!RequireHost(Loc.T("act_money")))
		{
			return;
		}
		foreach (NetworkUser item in Targets())
		{
			if (Object.op_Implicit((Object)(object)item.master))
			{
				item.master.GiveMoney((uint)Mathf.Max(0, amount));
			}
		}
		if (Object.op_Implicit((Object)(object)TeamManager.instance))
		{
			TeamManager.instance.GiveTeamMoney((TeamIndex)1, amount);
		}
		UiHelpers.Toast(Loc.F("gold_plus", amount));
	}

	public static void GiveLunar(uint amount)
	{
		foreach (NetworkUser item in Targets())
		{
			item.AwardLunarCoins(amount);
		}
		UiHelpers.Toast(Loc.F("lunar_plus", amount));
	}

	public static void SetTeamLevel(uint level)
	{
		if (Object.op_Implicit((Object)(object)TeamManager.instance) && RequireHost(Loc.T("act_level")))
		{
			TeamManager.instance.SetTeamLevel((TeamIndex)1, level);
			UiHelpers.Toast(Loc.F("level_set", level));
		}
	}

	public static void HealFull()
	{
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		if (!RequireHost(Loc.T("act_heal")))
		{
			return;
		}
		foreach (NetworkUser item in Targets())
		{
			CharacterBody currentBody = item.GetCurrentBody();
			HealthComponent val = (Object.op_Implicit((Object)(object)currentBody) ? currentBody.healthComponent : null);
			if (Object.op_Implicit((Object)(object)val))
			{
				val.Networkhealth = val.fullHealth;
				val.Networkshield = currentBody.maxShield;
				val.Heal(val.fullCombinedHealth, default(ProcChainMask), true);
			}
		}
		UiHelpers.Toast(Loc.T("healed"));
	}

	public static void ApplyStats(CharacterBody body, float health, float shield, float damage, float attack, float move, float regen, float crit, float jump, int jumps)
	{
		if (Object.op_Implicit((Object)(object)body) && RequireHost(Loc.T("act_stats")))
		{
			body.baseMaxHealth = Mathf.Max(1f, health);
			body.baseMaxShield = Mathf.Max(0f, shield);
			body.baseDamage = Mathf.Max(0f, damage);
			body.baseAttackSpeed = Mathf.Max(0.01f, attack);
			body.baseMoveSpeed = Mathf.Max(0f, move);
			body.baseRegen = regen;
			body.baseCrit = crit;
			body.baseJumpPower = jump;
			body.baseJumpCount = Mathf.Max(0, jumps);
			body.RecalculateStats();
			if (Object.op_Implicit((Object)(object)body.healthComponent))
			{
				body.healthComponent.Networkhealth = body.maxHealth;
				body.healthComponent.Networkshield = body.maxShield;
			}
			UiHelpers.Toast(Loc.T("stats_ok"));
		}
	}

	public static void TransformSurvivor(SurvivorDef survivor)
	{
		if (Object.op_Implicit((Object)(object)survivor) && Object.op_Implicit((Object)(object)survivor.bodyPrefab) && RequireHost(Loc.T("act_survivor")))
		{
			TransformBodyPrefab(survivor.bodyPrefab, DisplayName(survivor));
		}
	}

	public static void TransformBodyPrefab(GameObject prefab, string displayName = null)
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Invalid comparison between Unknown and I4
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		if (!Object.op_Implicit((Object)(object)prefab) || !RequireHost(Loc.T("act_survivor")))
		{
			return;
		}
		string text = ((Object)prefab).name;
		BodyIndex val = BodyCatalog.FindBodyIndex(prefab);
		if ((int)val != -1)
		{
			string bodyName = BodyCatalog.GetBodyName(val);
			if (!string.IsNullOrEmpty(bodyName))
			{
				text = bodyName;
			}
		}
		CharacterBody component = prefab.GetComponent<CharacterBody>();
		foreach (NetworkUser item in Targets())
		{
			if (Object.op_Implicit((Object)(object)item.master))
			{
				item.master.bodyPrefab = prefab;
				item.master.TransformBody(text);
				CopyPrefabSkills(item.master.GetBody(), component);
			}
		}
		if (string.IsNullOrEmpty(displayName))
		{
			displayName = DisplayName(component);
		}
		UiHelpers.Toast(Loc.F("body_set", displayName));
	}

	private static void CopyPrefabSkills(CharacterBody dest, CharacterBody src)
	{
		if (Object.op_Implicit((Object)(object)dest) && Object.op_Implicit((Object)(object)src) && Object.op_Implicit((Object)(object)dest.skillLocator) && Object.op_Implicit((Object)(object)src.skillLocator))
		{
			TryCopySkill(dest.skillLocator.primary, src.skillLocator.primary);
			TryCopySkill(dest.skillLocator.secondary, src.skillLocator.secondary);
			TryCopySkill(dest.skillLocator.utility, src.skillLocator.utility);
			TryCopySkill(dest.skillLocator.special, src.skillLocator.special);
		}
	}

	private static void TryCopySkill(GenericSkill dest, GenericSkill src)
	{
		if (Object.op_Implicit((Object)(object)dest) && Object.op_Implicit((Object)(object)src) && Object.op_Implicit((Object)(object)src.skillDef))
		{
			dest.SetBaseSkill(src.skillDef);
		}
	}

	public static void SetPortalFlags(bool gold, bool shop, bool ms, bool hidden)
	{
		TeleporterInteraction instance = TeleporterInteraction.instance;
		if (Object.op_Implicit((Object)(object)instance) && RequireHost(Loc.T("act_portals")))
		{
			instance.shouldAttemptToSpawnGoldshoresPortal = gold;
			instance.shouldAttemptToSpawnShopPortal = shop;
			instance.shouldAttemptToSpawnMSPortal = ms;
			instance.shouldAttemptToSpawnHiddenRealmsPortal = hidden;
			UiHelpers.Toast(Loc.T("portals_ok"));
		}
	}

	public static void ActivateTeleporter()
	{
		TeleporterInteraction instance = TeleporterInteraction.instance;
		CharacterBody localBody = LocalBody;
		if (Object.op_Implicit((Object)(object)instance) && Object.op_Implicit((Object)(object)localBody) && RequireHost(Loc.T("act_tp_on")))
		{
			Interactor component = ((Component)localBody).GetComponent<Interactor>();
			if (Object.op_Implicit((Object)(object)component))
			{
				instance.OnInteractionBegin(component);
			}
			instance.NetworkisActivated = true;
			UiHelpers.Toast(Loc.T("tp_on"));
		}
	}

	public static void ChargeTeleporter()
	{
		TeleporterInteraction instance = TeleporterInteraction.instance;
		if (!Object.op_Implicit((Object)(object)instance) || !RequireHost(Loc.T("act_tp_charge")))
		{
			return;
		}
		HoldoutZoneController holdoutZoneController = instance.holdoutZoneController;
		if (Object.op_Implicit((Object)(object)holdoutZoneController))
		{
			if (Object.op_Implicit((Object)(object)instance.bossGroup) && Object.op_Implicit((Object)(object)instance.bossGroup.combatSquad) && instance.bossGroup.combatSquad.memberCount > 0)
			{
				holdoutZoneController.Network_charge = 0.99f;
			}
			else
			{
				holdoutZoneController.FullyChargeHoldoutZone();
			}
		}
		UiHelpers.Toast(Loc.T("tp_charged"));
	}

	public static void PingTeleporter()
	{
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		TeleporterInteraction instance = TeleporterInteraction.instance;
		CharacterMaster localMaster = LocalMaster;
		if (Object.op_Implicit((Object)(object)instance) && Object.op_Implicit((Object)(object)localMaster))
		{
			PingerController val = ((Component)localMaster).GetComponent<PingerController>() ?? (Object.op_Implicit((Object)(object)LocalBody) ? ((Component)LocalBody).GetComponent<PingerController>() : null);
			if (!Object.op_Implicit((Object)(object)val))
			{
				UiHelpers.Toast(Loc.T("no_pinger"));
				return;
			}
			PingInfo val2 = new PingInfo
			{
				active = true,
				origin = ((Component)instance).transform.position,
				normal = Vector3.up,
				targetNetworkIdentity = ((Component)instance).GetComponent<NetworkIdentity>()
			};
			val.CallCmdPing(val2);
			UiHelpers.Toast(Loc.T("tp_ping"));
		}
	}

	public static void ScanArea()
	{
		InvokeEquipmentFire("FireScanner", ref _fireScanner, "Скан");
	}

	public static void SpawnGummyClone()
	{
		InvokeEquipmentFire("FireGummyClone", ref _fireGummy, "Gummy Clone");
	}

	public static void SpawnDroneBackup()
	{
		InvokeEquipmentFire("FireDroneBackup", ref _fireDrones, "Дроны");
	}

	public static void SpawnArmy()
	{
		if (RequireHost(Loc.T("act_army")))
		{
			for (int i = 0; i < 3; i++)
			{
				SpawnGummyClone();
			}
			for (int j = 0; j < 4; j++)
			{
				SpawnDroneBackup();
			}
			UiHelpers.Toast(Loc.T("army_ok"));
		}
	}

	private static void InvokeEquipmentFire(string methodName, ref MethodInfo cached, string label)
	{
		if (!RequireHost(label))
		{
			return;
		}
		ResolveFireMethods();
		CharacterBody localBody = LocalBody;
		EquipmentSlot val = (Object.op_Implicit((Object)(object)localBody) ? localBody.equipmentSlot : null);
		if (!Object.op_Implicit((Object)(object)val) || cached == null)
		{
			UiHelpers.Toast(Loc.F("call_fail", label));
			return;
		}
		try
		{
			cached.Invoke(val, null);
			UiHelpers.Toast(label);
		}
		catch (Exception ex)
		{
			Plugin.Log.LogWarning((object)(methodName + " failed: " + ex.Message));
			UiHelpers.Toast(Loc.F("call_err", label));
		}
	}

	private static void ResolveFireMethods()
	{
		if (!_methodsResolved)
		{
			_methodsResolved = true;
			Type? typeFromHandle = typeof(EquipmentSlot);
			_fireScanner = typeFromHandle.GetMethod("FireScanner", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			_fireGummy = typeFromHandle.GetMethod("FireGummyClone", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			_fireDrones = typeFromHandle.GetMethod("FireDroneBackup", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		}
	}

	public static string UserLabel(NetworkUser user, int index)
	{
		if (!Object.op_Implicit((Object)(object)user))
		{
			return "Player " + index;
		}
		string text = (string.IsNullOrEmpty(user.userName) ? ("Player " + index) : user.userName);
		if ((Object)(object)user == (Object)(object)LocalNetworkUser)
		{
			text += Loc.T("you");
		}
		return text;
	}

	public static void TeleportSelectedToLocal()
	{
		CleanupSelected();
		if (SelectedUsers.Count == 0)
		{
			UiHelpers.Toast(Loc.T("tp_need_player"));
		}
		else
		{
			TeleportUsersToLocal(SelectedUsers);
		}
	}

	public static void TeleportAllToLocal()
	{
		TeleportUsersToLocal(AllUsers());
	}

	public static void TeleportUserToLocal(NetworkUser user)
	{
		if (Object.op_Implicit((Object)(object)user))
		{
			TeleportUsersToLocal((IEnumerable<NetworkUser>)(object)new NetworkUser[1] { user });
		}
	}

	public static void TeleportLocalToUser(NetworkUser user)
	{
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		if (RequireHost(Loc.T("act_player_tp")))
		{
			CharacterBody localBody = LocalBody;
			CharacterBody val = (Object.op_Implicit((Object)(object)user) ? user.GetCurrentBody() : null);
			if (!Object.op_Implicit((Object)(object)localBody) || !Object.op_Implicit((Object)(object)val))
			{
				UiHelpers.Toast(Loc.T("tp_need_body"));
				return;
			}
			Vector3 position = val.footPosition + ((Component)val).transform.forward * -2.2f + Vector3.up * 0.4f;
			MoveBody(localBody, position);
			UiHelpers.Toast(Loc.F("tp_me_ok", UserLabel(user, 0)));
		}
	}

	private static void TeleportUsersToLocal(IEnumerable<NetworkUser> users)
	{
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		if (!RequireHost(Loc.T("act_player_tp")))
		{
			return;
		}
		CharacterBody localBody = LocalBody;
		if (!Object.op_Implicit((Object)(object)localBody))
		{
			UiHelpers.Toast(Loc.T("tp_need_body"));
			return;
		}
		int num = 0;
		foreach (NetworkUser user in users)
		{
			if (Object.op_Implicit((Object)(object)user) && !((Object)(object)user == (Object)(object)LocalNetworkUser))
			{
				CharacterBody currentBody = user.GetCurrentBody();
				if (Object.op_Implicit((Object)(object)currentBody))
				{
					Vector3 val = Quaternion.Euler(0f, (float)num * 45f, 0f) * Vector3.forward * 2.4f;
					MoveBody(currentBody, localBody.footPosition + val + Vector3.up * 0.4f);
					num++;
				}
			}
		}
		UiHelpers.Toast((num > 0) ? Loc.F("tp_players_ok", num) : Loc.T("tp_no_others"));
	}

	private static void MoveBody(CharacterBody body, Vector3 position)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		if (Object.op_Implicit((Object)(object)body))
		{
			TeleportHelper.TeleportBody(body, position);
			if (Object.op_Implicit((Object)(object)body.characterMotor) && Object.op_Implicit((Object)(object)((BaseCharacterController)body.characterMotor).Motor))
			{
				((BaseCharacterController)body.characterMotor).Motor.SetPosition(position, true);
			}
		}
	}

	public static void GiveRandomItem(int stacks)
	{
		if (RequireHost(Loc.T("act_items")))
		{
			ItemDef val = RandomVisibleItem();
			if (!Object.op_Implicit((Object)(object)val))
			{
				UiHelpers.Toast(Loc.T("fun_empty"));
			}
			else
			{
				GiveItem(val, Mathf.Max(1, stacks));
			}
		}
	}

	public static void GiveRandomEquipment()
	{
		if (!RequireHost(Loc.T("act_equip")))
		{
			return;
		}
		List<EquipmentDef> list = new List<EquipmentDef>();
		for (int i = 0; i < EquipmentCatalog.equipmentCount; i++)
		{
			EquipmentDef equipmentDef = EquipmentCatalog.GetEquipmentDef((EquipmentIndex)i);
			if (Object.op_Implicit((Object)(object)equipmentDef))
			{
				list.Add(equipmentDef);
			}
		}
		if (list.Count == 0)
		{
			UiHelpers.Toast(Loc.T("fun_empty"));
		}
		else
		{
			SetEquipment(list[Random.Range(0, list.Count)]);
		}
	}

	public static void RainPickups(int count)
	{
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		if (!RequireHost(Loc.T("act_items")))
		{
			return;
		}
		CharacterBody localBody = LocalBody;
		if (!Object.op_Implicit((Object)(object)localBody))
		{
			UiHelpers.Toast(Loc.T("no_body"));
			return;
		}
		int num = 0;
		Vector3 val4 = default(Vector3);
		for (int i = 0; i < count; i++)
		{
			ItemDef val = RandomVisibleItem();
			if (Object.op_Implicit((Object)(object)val))
			{
				PickupIndex val2 = PickupCatalog.FindPickupIndex(val.itemIndex);
				Vector3 val3 = localBody.footPosition + Vector3.up * 10f + new Vector3(Random.Range(-7f, 7f), Random.Range(0f, 4f), Random.Range(-7f, 7f));
				val4._002Ector(Random.Range(-6f, 6f), Random.Range(-2f, 2f), Random.Range(-6f, 6f));
				PickupDropletController.CreatePickupDroplet(val2, val3, val4);
				num++;
			}
		}
		UiHelpers.Toast(Loc.F("fun_rain_ok", num));
	}

	public static void RandomSurvivor()
	{
		if (!RequireHost(Loc.T("act_survivor")))
		{
			return;
		}
		List<SurvivorDef> list = new List<SurvivorDef>();
		foreach (SurvivorDef allSurvivorDef in SurvivorCatalog.allSurvivorDefs)
		{
			if (Object.op_Implicit((Object)(object)allSurvivorDef) && !allSurvivorDef.hidden && Object.op_Implicit((Object)(object)allSurvivorDef.bodyPrefab))
			{
				list.Add(allSurvivorDef);
			}
		}
		if (list.Count == 0)
		{
			UiHelpers.Toast(Loc.T("fun_empty"));
		}
		else
		{
			TransformSurvivor(list[Random.Range(0, list.Count)]);
		}
	}

	public static void LaunchUp(float force)
	{
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		if (!RequireHost(Loc.T("act_stats")))
		{
			return;
		}
		foreach (NetworkUser item in Targets())
		{
			CharacterBody val = (Object.op_Implicit((Object)(object)item) ? item.GetCurrentBody() : null);
			CharacterMotor val2 = (Object.op_Implicit((Object)(object)val) ? val.characterMotor : null);
			if (Object.op_Implicit((Object)(object)val2))
			{
				if (Object.op_Implicit((Object)(object)((BaseCharacterController)val2).Motor))
				{
					((BaseCharacterController)val2).Motor.ForceUnground(0.1f);
				}
				val2.velocity = new Vector3(val2.velocity.x, force, val2.velocity.z);
			}
		}
		UiHelpers.Toast(Loc.T("fun_yeet"));
	}

	public static void FunnyKick(bool takeHealth)
	{
		if (!RequireHost(Loc.T("act_stats")))
		{
			return;
		}
		CleanupSelected();
		int num = 0;
		foreach (NetworkUser item in KickTargets())
		{
			if (TryFunnyKick(item, takeHealth))
			{
				num++;
			}
		}
		UiHelpers.Toast(Loc.F(takeHealth ? "fun_kick_hp_ok" : "fun_kick_ok", num));
	}

	public static void FunnyKickUser(NetworkUser user, bool takeHealth)
	{
		if (Object.op_Implicit((Object)(object)user) && RequireHost(Loc.T("act_stats")) && TryFunnyKick(user, takeHealth))
		{
			UiHelpers.Toast(Loc.F(takeHealth ? "fun_kick_hp_ok" : "fun_kick_ok", 1));
		}
	}

	private static IEnumerable<NetworkUser> KickTargets()
	{
		if (SelectedUsers.Count > 0)
		{
			foreach (NetworkUser selectedUser in SelectedUsers)
			{
				yield return selectedUser;
			}
			yield break;
		}
		int others = 0;
		foreach (NetworkUser item in AllUsers())
		{
			if (Object.op_Implicit((Object)(object)item) && !((Object)(object)item == (Object)(object)LocalNetworkUser))
			{
				others++;
				yield return item;
			}
		}
		if (others == 0 && Object.op_Implicit((Object)(object)LocalNetworkUser))
		{
			yield return LocalNetworkUser;
		}
	}

	private static bool TryFunnyKick(NetworkUser user, bool takeHealth)
	{
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		CharacterBody val = (Object.op_Implicit((Object)(object)user) ? user.GetCurrentBody() : null);
		if (!Object.op_Implicit((Object)(object)val))
		{
			return false;
		}
		CharacterMotor characterMotor = val.characterMotor;
		if (Object.op_Implicit((Object)(object)characterMotor))
		{
			if (Object.op_Implicit((Object)(object)((BaseCharacterController)characterMotor).Motor))
			{
				((BaseCharacterController)characterMotor).Motor.ForceUnground(0.1f);
			}
			Vector3 val2 = ((Component)val).transform.right * Random.Range(-2.2f, 2.2f);
			Vector3 val3 = -((Component)val).transform.forward * Random.Range(1.2f, 3.2f);
			characterMotor.velocity = new Vector3(val2.x + val3.x, Random.Range(11f, 15f), val2.z + val3.z);
		}
		if (takeHealth && Object.op_Implicit((Object)(object)val.healthComponent))
		{
			HealthComponent healthComponent = val.healthComponent;
			float num = Mathf.Max(1f, val.maxHealth * 0.1f);
			float networkhealth = Mathf.Max(1f, healthComponent.health - num);
			healthComponent.Networkhealth = networkhealth;
		}
		PlayKickSound(((Component)val).gameObject);
		return true;
	}

	private static void PlayKickSound(GameObject go)
	{
		if (Object.op_Implicit((Object)(object)go))
		{
			Util.PlaySound("Play_item_proc_behemoth", go);
			Util.PlaySound("Play_char_merc_m1", go);
			Util.PlaySound("Play_item_use_tonic", go);
		}
	}

	public static void ToastJoke()
	{
		UiHelpers.Toast(Loc.T("fun_q" + Random.Range(0, 8)));
	}

	public static void ToggleRandomArtifact()
	{
		if ((Object)(object)RunArtifactManager.instance == (Object)null || !RequireHost(Loc.T("act_art")))
		{
			return;
		}
		List<ArtifactDef> list = new List<ArtifactDef>();
		for (int i = 0; i < ArtifactCatalog.artifactCount; i++)
		{
			ArtifactDef artifactDef = ArtifactCatalog.GetArtifactDef((ArtifactIndex)i);
			if (Object.op_Implicit((Object)(object)artifactDef))
			{
				list.Add(artifactDef);
			}
		}
		if (list.Count == 0)
		{
			UiHelpers.Toast(Loc.T("fun_empty"));
			return;
		}
		ArtifactDef val = list[Random.Range(0, list.Count)];
		bool flag = !RunArtifactManager.instance.IsArtifactEnabled(val);
		SetArtifact(val, flag);
		UiHelpers.Toast((flag ? "+ " : "- ") + DisplayName(val));
	}

	public static void RainChests(int count)
	{
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_012a: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		//IL_013b: Unknown result type (might be due to invalid IL or missing references)
		//IL_013d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0142: Unknown result type (might be due to invalid IL or missing references)
		//IL_0143: Unknown result type (might be due to invalid IL or missing references)
		//IL_0145: Unknown result type (might be due to invalid IL or missing references)
		//IL_014c: Expected O, but got Unknown
		//IL_0152: Unknown result type (might be due to invalid IL or missing references)
		//IL_0159: Expected O, but got Unknown
		//IL_0181: Unknown result type (might be due to invalid IL or missing references)
		//IL_019c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a3: Unknown result type (might be due to invalid IL or missing references)
		if (!RequireHost(Loc.T("act_army")))
		{
			return;
		}
		CharacterBody localBody = LocalBody;
		if (!Object.op_Implicit((Object)(object)localBody))
		{
			UiHelpers.Toast(Loc.T("no_body"));
			return;
		}
		List<InteractableSpawnCard> list = new List<InteractableSpawnCard>();
		InteractableSpawnCard[] array = Resources.FindObjectsOfTypeAll<InteractableSpawnCard>();
		foreach (InteractableSpawnCard val in array)
		{
			if (Object.op_Implicit((Object)(object)val) && ContainsAny(((Object)val).name ?? "", "Chest", "Barrel", "TripleShop", "Scrapper", "Casino", "LunarShop", "CategoryChest"))
			{
				list.Add(val);
			}
		}
		if (list.Count == 0)
		{
			UiHelpers.Toast(Loc.T("fun_empty"));
			return;
		}
		int num = 0;
		Xoroshiro128Plus rng = RoR2Application.rng;
		for (int j = 0; j < count; j++)
		{
			InteractableSpawnCard val2 = list[Random.Range(0, list.Count)];
			Vector3 val3 = localBody.footPosition + new Vector3(Random.Range(-10f, 10f), Random.Range(6f, 14f), Random.Range(-10f, 10f));
			DirectorPlacementRule val4 = new DirectorPlacementRule
			{
				placementMode = (PlacementMode)0,
				position = val3
			};
			DirectorSpawnRequest val5 = new DirectorSpawnRequest((SpawnCard)(object)val2, val4, rng);
			GameObject val6 = (Object.op_Implicit((Object)(object)DirectorCore.instance) ? DirectorCore.instance.TrySpawnObject(val5) : null);
			if (!Object.op_Implicit((Object)(object)val6))
			{
				val6 = ((SpawnCard)val2).DoSpawn(val3, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), val5).spawnedInstance;
			}
			if (Object.op_Implicit((Object)(object)val6))
			{
				num++;
			}
		}
		PlayFunnySound(((Component)localBody).gameObject);
		UiHelpers.Toast(Loc.F("fun_chests_ok", num));
	}

	public static void GoldExplosion()
	{
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		if (!RequireHost(Loc.T("act_money")))
		{
			return;
		}
		CharacterBody localBody = LocalBody;
		if (!Object.op_Implicit((Object)(object)localBody))
		{
			UiHelpers.Toast(Loc.T("no_body"));
			return;
		}
		GiveMoney(750);
		GameObject val = FindMoneyPackPrefab();
		if (Object.op_Implicit((Object)(object)val))
		{
			for (int i = 0; i < 16; i++)
			{
				Vector3 val2 = localBody.corePosition + Vector3.up * 1.2f;
				GameObject val3 = Object.Instantiate<GameObject>(val, val2, Random.rotation);
				if (Object.op_Implicit((Object)(object)val3) && NetworkServer.active)
				{
					NetworkServer.Spawn(val3);
				}
				Rigidbody val4 = (Object.op_Implicit((Object)(object)val3) ? val3.GetComponent<Rigidbody>() : null);
				if (Object.op_Implicit((Object)(object)val4))
				{
					val4.velocity = Random.onUnitSphere * 18f + Vector3.up * 10f;
				}
			}
		}
		else
		{
			RainPickups(18);
		}
		LaunchUp(28f);
		PlayFunnySound(((Component)localBody).gameObject);
		UiHelpers.Toast(Loc.T("fun_gold_ok"));
	}

	public static void SetPlayerNick(string nick)
	{
		if (string.IsNullOrWhiteSpace(nick))
		{
			UiHelpers.Toast(Loc.T("fun_nick_empty"));
			return;
		}
		nick = nick.Trim();
		if (nick.Length > 24)
		{
			nick = nick.Substring(0, 24);
		}
		NetworkUser localNetworkUser = LocalNetworkUser;
		if (Object.op_Implicit((Object)(object)localNetworkUser))
		{
			try
			{
				localNetworkUser.userName = nick;
			}
			catch
			{
				(typeof(NetworkUser).GetField("userName") ?? typeof(NetworkUser).GetField("<userName>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic))?.SetValue(localNetworkUser, nick);
			}
		}
		LocalUser localUser = LocalUser;
		if (localUser != null && localUser.userProfile != null)
		{
			try
			{
				localUser.userProfile.name = nick;
			}
			catch
			{
			}
		}
		Chat.AddMessage(Loc.F("fun_nick_ok", nick));
		UiHelpers.Toast(Loc.F("fun_nick_ok", nick));
	}

	public static void BecomeBoss()
	{
		if (!RequireHost(Loc.T("act_survivor")))
		{
			return;
		}
		CharacterBody localBody = LocalBody;
		List<GameObject> list = new List<GameObject>();
		int bodyCount = BodyCatalog.bodyCount;
		for (int i = 0; i < bodyCount; i++)
		{
			GameObject bodyPrefab = BodyCatalog.GetBodyPrefab((BodyIndex)i);
			CharacterBody bodyPrefabBodyComponent = BodyCatalog.GetBodyPrefabBodyComponent((BodyIndex)i);
			if (Object.op_Implicit((Object)(object)bodyPrefab) && Object.op_Implicit((Object)(object)bodyPrefabBodyComponent) && bodyPrefabBodyComponent.isChampion && !NameLooksLikeOther(((Object)bodyPrefab).name))
			{
				list.Add(bodyPrefab);
			}
		}
		if (list.Count > 0)
		{
			TransformBodyPrefab(list[Random.Range(0, list.Count)]);
		}
		CheatRuntime.GodMode = true;
		CheatRuntime.BodyScale = 2.4f;
		if (Object.op_Implicit((Object)(object)localBody))
		{
			PlayBossFanfare(((Component)localBody).gameObject);
		}
		Chat.AddMessage(Loc.T("fun_boss_chat"));
		UiHelpers.Toast(Loc.T("fun_boss_ok"));
	}

	private static GameObject FindMoneyPackPrefab()
	{
		GameObject[] array = Resources.FindObjectsOfTypeAll<GameObject>();
		foreach (GameObject val in array)
		{
			if (Object.op_Implicit((Object)(object)val) && ContainsAny(((Object)val).name ?? "", "BonusMoneyPack", "MoneyPickup", "MoneyPack"))
			{
				return val;
			}
		}
		return null;
	}

	private static void PlayFunnySound(GameObject go)
	{
		if (Object.op_Implicit((Object)(object)go))
		{
			Util.PlaySound("Play_UI_achievement", go);
			Util.PlaySound("Play_item_use_tonic", go);
		}
	}

	private static void PlayBossFanfare(GameObject go)
	{
		if (Object.op_Implicit((Object)(object)go))
		{
			Util.PlaySound("Play_UI_achievement", go);
			Util.PlaySound("Play_moonBrother_spawn", go);
			Util.PlaySound("Play_captain_shift_ready", go);
			Util.PlaySound("Play_env_hiddenLab_unlock", go);
		}
	}

	private static ItemDef RandomVisibleItem()
	{
		List<ItemDef> list = new List<ItemDef>();
		for (int i = 0; i < ItemCatalog.itemCount; i++)
		{
			ItemDef itemDef = ItemCatalog.GetItemDef((ItemIndex)i);
			if (Object.op_Implicit((Object)(object)itemDef) && !itemDef.hidden)
			{
				list.Add(itemDef);
			}
		}
		if (list.Count == 0)
		{
			return null;
		}
		return list[Random.Range(0, list.Count)];
	}

	public static bool Matches(string query, params string[] parts)
	{
		if (string.IsNullOrWhiteSpace(query))
		{
			return true;
		}
		string value = query.Trim();
		foreach (string text in parts)
		{
			if (!string.IsNullOrEmpty(text) && text.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return true;
			}
		}
		return false;
	}

	public static string TierLabel(ItemTier tier)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Expected I4, but got Unknown
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		switch ((int)tier)
		{
		case 0:
			return Loc.T("tier_white");
		case 1:
			return Loc.T("tier_green");
		case 2:
			return Loc.T("tier_red");
		case 3:
			return Loc.T("tier_lunar");
		case 4:
			return Loc.T("tier_boss");
		case 6:
		case 7:
		case 8:
		case 9:
			return Loc.T("tier_void");
		case 10:
			return Loc.T("tier_food");
		default:
			try
			{
				ItemTierDef itemTierDef = ItemTierCatalog.GetItemTierDef(tier);
				if (Object.op_Implicit((Object)(object)itemTierDef) && !string.IsNullOrEmpty(((Object)itemTierDef).name))
				{
					return ((Object)itemTierDef).name;
				}
			}
			catch
			{
			}
			return Loc.T("tier_other");
		}
	}

	public static Color TierColor(ItemTier tier)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Expected I4, but got Unknown
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		switch ((int)tier)
		{
		case 0:
			return new Color(0.85f, 0.85f, 0.85f);
		case 1:
			return new Color(0.45f, 0.9f, 0.45f);
		case 2:
			return new Color(1f, 0.35f, 0.35f);
		case 3:
			return new Color(0.45f, 0.75f, 1f);
		case 4:
			return new Color(1f, 0.85f, 0.3f);
		case 6:
		case 7:
		case 8:
		case 9:
			return new Color(0.75f, 0.4f, 1f);
		case 10:
			return new Color(1f, 0.6f, 0.25f);
		default:
			try
			{
				ItemTierDef itemTierDef = ItemTierCatalog.GetItemTierDef(tier);
				if (Object.op_Implicit((Object)(object)itemTierDef))
				{
					return Color32.op_Implicit(ColorCatalog.GetColor(itemTierDef.colorIndex));
				}
			}
			catch
			{
			}
			return Color.gray;
		}
	}
}
