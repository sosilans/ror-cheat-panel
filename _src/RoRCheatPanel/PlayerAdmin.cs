using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using HarmonyLib;
using KinematicCharacterController;
using RoR2;
using RoR2.Networking;
using RoRCheatPanel.UI;
using UnityEngine;
using UnityEngine.Networking;

namespace RoRCheatPanel;

internal static class PlayerAdmin
{
	private static readonly HashSet<string> Banned = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

	private static readonly Dictionary<string, string> BanNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

	private static readonly HashSet<string> Muted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

	private static readonly HashSet<string> NoclipUsers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

	private static readonly HashSet<string> GodUsers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

	private static readonly HashSet<string> FrozenUsers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

	private static readonly Dictionary<string, CharacterMotor> NoclipMotors = new Dictionary<string, CharacterMotor>(StringComparer.OrdinalIgnoreCase);

	private static readonly Dictionary<string, Vector3> FreezePos = new Dictionary<string, Vector3>(StringComparer.OrdinalIgnoreCase);

	private static bool _inited;

	public static void Init()
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Expected O, but got Unknown
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Expected O, but got Unknown
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Expected O, but got Unknown
		if (!_inited)
		{
			_inited = true;
			LoadBans();
			NetworkUser.onPostNetworkUserStart += new NetworkUserGenericDelegate(OnUserReady);
			Harmony val = new Harmony("com.rorcheatpanel.plugin.chatmute");
			MethodInfo methodInfo = AccessTools.Method(typeof(Chat), "SendBroadcastChat", new Type[1] { typeof(ChatMessageBase) }, (Type[])null);
			MethodInfo methodInfo2 = AccessTools.Method(typeof(Chat), "SendBroadcastChat", new Type[2]
			{
				typeof(ChatMessageBase),
				typeof(int)
			}, (Type[])null);
			HarmonyMethod val2 = new HarmonyMethod(typeof(PlayerAdmin), "ChatPrefix", (Type[])null);
			if (methodInfo != null)
			{
				val.Patch((MethodBase)methodInfo, val2, (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
			}
			if (methodInfo2 != null)
			{
				val.Patch((MethodBase)methodInfo2, val2, (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
			}
		}
	}

	public static void Tick()
	{
		if (NetworkServer.active)
		{
			ApplyNoclip();
			ApplyGod();
			ApplyFreeze();
		}
	}

	public static string UserKey(NetworkUser user)
	{
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		if (!Object.op_Implicit((Object)(object)user))
		{
			return "";
		}
		try
		{
			NetworkUserId id = user.id;
			if (id.HasValidValue() && user.id.value != 0L)
			{
				id = user.id;
				return id.value.ToString();
			}
			id = user.id;
			string text = ((object)id.steamId/*cast due to .constrained prefix*/).ToString();
			if (!string.IsNullOrEmpty(text) && text != "0")
			{
				return text;
			}
		}
		catch
		{
		}
		NetworkInstanceId netId = ((NetworkBehaviour)user).netId;
		return "nid:" + netId.Value;
	}

	public static bool IsMuted(NetworkUser user)
	{
		string text = UserKey(user);
		if (text.Length > 0)
		{
			return Muted.Contains(text);
		}
		return false;
	}

	public static bool IsNoclip(NetworkUser user)
	{
		string text = UserKey(user);
		if (text.Length > 0)
		{
			return NoclipUsers.Contains(text);
		}
		return false;
	}

	public static bool IsGod(NetworkUser user)
	{
		string text = UserKey(user);
		if (text.Length > 0)
		{
			return GodUsers.Contains(text);
		}
		return false;
	}

	public static bool IsFrozen(NetworkUser user)
	{
		string text = UserKey(user);
		if (text.Length > 0)
		{
			return FrozenUsers.Contains(text);
		}
		return false;
	}

	public static bool IsBannedKey(string key)
	{
		if (!string.IsNullOrEmpty(key))
		{
			return Banned.Contains(key);
		}
		return false;
	}

	public static bool IsNoclipBody(CharacterBody body)
	{
		if (!Object.op_Implicit((Object)(object)body) || NoclipUsers.Count == 0)
		{
			return false;
		}
		PlayerCharacterMasterController val = (Object.op_Implicit((Object)(object)body.master) ? body.master.playerCharacterMasterController : null);
		return IsNoclip(Object.op_Implicit((Object)(object)val) ? val.networkUser : null);
	}

	public static IEnumerable<KeyValuePair<string, string>> BanEntries()
	{
		foreach (string item in Banned)
		{
			BanNames.TryGetValue(item, out var value);
			yield return new KeyValuePair<string, string>(item, value ?? item);
		}
	}

	public static void KickUser(NetworkUser user)
	{
		if (GameUtil.RequireHost(Loc.T("act_admin")) && Object.op_Implicit((Object)(object)user))
		{
			if ((Object)(object)user == (Object)(object)GameUtil.LocalNetworkUser)
			{
				UiHelpers.Toast(Loc.T("admin_no_self"));
				return;
			}
			string userName = user.userName;
			DisconnectUser(user);
			UiHelpers.Toast(Loc.F("admin_kicked", userName));
			Announce(Loc.F("admin_kicked", userName));
		}
	}

	public static void BanUser(NetworkUser user)
	{
		if (!GameUtil.RequireHost(Loc.T("act_admin")) || !Object.op_Implicit((Object)(object)user))
		{
			return;
		}
		if ((Object)(object)user == (Object)(object)GameUtil.LocalNetworkUser)
		{
			UiHelpers.Toast(Loc.T("admin_no_self"));
			return;
		}
		string text = UserKey(user);
		string userName = user.userName;
		if (text.Length > 0)
		{
			Banned.Add(text);
			BanNames[text] = userName;
			SaveBans();
		}
		DisconnectUser(user);
		UiHelpers.Toast(Loc.F("admin_banned", userName));
		Announce(Loc.F("admin_banned", userName));
	}

	public static void Unban(string key)
	{
		if (!string.IsNullOrEmpty(key) && Banned.Remove(key))
		{
			BanNames.TryGetValue(key, out var value);
			BanNames.Remove(key);
			SaveBans();
			UiHelpers.Toast(Loc.F("admin_unbanned", string.IsNullOrEmpty(value) ? key : value));
		}
	}

	public static void SetMuted(NetworkUser user, bool mute)
	{
		if (!GameUtil.RequireHost(Loc.T("act_admin")) || !Object.op_Implicit((Object)(object)user))
		{
			return;
		}
		string text = UserKey(user);
		if (text.Length != 0)
		{
			if (mute)
			{
				Muted.Add(text);
			}
			else
			{
				Muted.Remove(text);
			}
			UiHelpers.Toast(Loc.F(mute ? "admin_muted" : "admin_unmuted", user.userName));
		}
	}

	public static void SetNoclip(NetworkUser user, bool on)
	{
		if (!GameUtil.RequireHost(Loc.T("act_admin")) || !Object.op_Implicit((Object)(object)user))
		{
			return;
		}
		string text = UserKey(user);
		if (text.Length == 0)
		{
			return;
		}
		if (on)
		{
			NoclipUsers.Add(text);
		}
		else
		{
			NoclipUsers.Remove(text);
			if (NoclipMotors.TryGetValue(text, out var value))
			{
				DisableMotorNoclip(value);
				NoclipMotors.Remove(text);
			}
		}
		UiHelpers.Toast(Loc.F(on ? "admin_noclip_on" : "admin_noclip_off", user.userName));
	}

	public static void SetGod(NetworkUser user, bool on)
	{
		if (!GameUtil.RequireHost(Loc.T("act_admin")) || !Object.op_Implicit((Object)(object)user))
		{
			return;
		}
		string text = UserKey(user);
		if (text.Length == 0)
		{
			return;
		}
		if (on)
		{
			GodUsers.Add(text);
		}
		else
		{
			GodUsers.Remove(text);
			CharacterBody currentBody = user.GetCurrentBody();
			if (Object.op_Implicit((Object)(object)currentBody) && Object.op_Implicit((Object)(object)currentBody.healthComponent))
			{
				currentBody.healthComponent.godMode = false;
			}
		}
		UiHelpers.Toast(Loc.F(on ? "admin_god_on" : "admin_god_off", user.userName));
	}

	public static void SetFrozen(NetworkUser user, bool on)
	{
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		if (!GameUtil.RequireHost(Loc.T("act_admin")) || !Object.op_Implicit((Object)(object)user))
		{
			return;
		}
		string text = UserKey(user);
		if (text.Length == 0)
		{
			return;
		}
		if (on)
		{
			FrozenUsers.Add(text);
			CharacterBody currentBody = user.GetCurrentBody();
			if (Object.op_Implicit((Object)(object)currentBody))
			{
				FreezePos[text] = currentBody.footPosition;
			}
		}
		else
		{
			FrozenUsers.Remove(text);
			FreezePos.Remove(text);
		}
		UiHelpers.Toast(Loc.F(on ? "admin_freeze_on" : "admin_freeze_off", user.userName));
	}

	public static void KickSelected()
	{
		ForSelectedOthers(KickUser);
	}

	public static void BanSelected()
	{
		ForSelectedOthers(BanUser);
	}

	public static void Announce(string text)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Expected O, but got Unknown
		if (!string.IsNullOrWhiteSpace(text) && NetworkServer.active)
		{
			NetworkUser localNetworkUser = GameUtil.LocalNetworkUser;
			Chat.SendBroadcastChat((ChatMessageBase)new UserChatMessage
			{
				sender = (Object.op_Implicit((Object)(object)localNetworkUser) ? ((Component)localNetworkUser).gameObject : null),
				text = text
			});
		}
	}

	public static List<NetworkUser> SelectedOthers()
	{
		GameUtil.CleanupSelected();
		List<NetworkUser> list = new List<NetworkUser>();
		foreach (NetworkUser selectedUser in GameUtil.SelectedUsers)
		{
			if (Object.op_Implicit((Object)(object)selectedUser) && (Object)(object)selectedUser != (Object)(object)GameUtil.LocalNetworkUser)
			{
				list.Add(selectedUser);
			}
		}
		return list;
	}

	private static void ForSelectedOthers(Action<NetworkUser> action)
	{
		List<NetworkUser> list = SelectedOthers();
		if (list.Count == 0)
		{
			UiHelpers.Toast(Loc.T("admin_need_sel"));
			return;
		}
		foreach (NetworkUser item in list)
		{
			action(item);
		}
	}

	private static void DisconnectUser(NetworkUser user)
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Expected O, but got Unknown
		NetworkConnection connectionToClient = ((NetworkBehaviour)user).connectionToClient;
		NetworkManagerSystem singleton = NetworkManagerSystem.singleton;
		if (Object.op_Implicit((Object)(object)singleton) && connectionToClient != null)
		{
			try
			{
				singleton.ServerKickClient(connectionToClient, (BaseKickReason)new SimpleLocalizedKickReason("PLAYER_KICKED", new string[0]));
				return;
			}
			catch (Exception ex)
			{
				if (Plugin.Log != null)
				{
					Plugin.Log.LogWarning((object)("ServerKickClient failed: " + ex.Message));
				}
			}
		}
		if (connectionToClient != null)
		{
			connectionToClient.Disconnect();
		}
	}

	private static void OnUserReady(NetworkUser user)
	{
		if (NetworkServer.active && Object.op_Implicit((Object)(object)user) && !((Object)(object)user == (Object)(object)GameUtil.LocalNetworkUser) && IsBannedKey(UserKey(user)))
		{
			DisconnectUser(user);
		}
	}

	private static bool ChatPrefix(ChatMessageBase message)
	{
		UserChatMessage val = (UserChatMessage)(object)((message is UserChatMessage) ? message : null);
		if (val == null || !Object.op_Implicit((Object)(object)val.sender))
		{
			return true;
		}
		return !IsMuted(val.sender.GetComponent<NetworkUser>());
	}

	private static void ApplyNoclip()
	{
		if (NoclipUsers.Count == 0)
		{
			if (NoclipMotors.Count <= 0)
			{
				return;
			}
			foreach (KeyValuePair<string, CharacterMotor> noclipMotor in NoclipMotors)
			{
				DisableMotorNoclip(noclipMotor.Value);
			}
			NoclipMotors.Clear();
			return;
		}
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (NetworkUser item in GameUtil.AllUsers())
		{
			string text = UserKey(item);
			if (!NoclipUsers.Contains(text))
			{
				continue;
			}
			hashSet.Add(text);
			CharacterBody currentBody = item.GetCurrentBody();
			CharacterMotor val = (Object.op_Implicit((Object)(object)currentBody) ? currentBody.characterMotor : null);
			if (!Object.op_Implicit((Object)(object)val))
			{
				continue;
			}
			if (!NoclipMotors.TryGetValue(text, out var value) || (Object)(object)value != (Object)(object)val)
			{
				if (Object.op_Implicit((Object)(object)value))
				{
					DisableMotorNoclip(value);
				}
				EnableMotorNoclip(val);
				NoclipMotors[text] = val;
			}
			FlyRemote(currentBody, val);
		}
		List<string> list = new List<string>();
		foreach (KeyValuePair<string, CharacterMotor> noclipMotor2 in NoclipMotors)
		{
			if (!hashSet.Contains(noclipMotor2.Key))
			{
				list.Add(noclipMotor2.Key);
			}
		}
		foreach (string item2 in list)
		{
			DisableMotorNoclip(NoclipMotors[item2]);
			NoclipMotors.Remove(item2);
		}
	}

	private static void ApplyGod()
	{
		if (GodUsers.Count == 0)
		{
			return;
		}
		foreach (NetworkUser item in GameUtil.AllUsers())
		{
			if (IsGod(item))
			{
				CharacterBody currentBody = item.GetCurrentBody();
				if (Object.op_Implicit((Object)(object)currentBody) && Object.op_Implicit((Object)(object)currentBody.healthComponent))
				{
					currentBody.healthComponent.godMode = true;
				}
			}
		}
	}

	private static void ApplyFreeze()
	{
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		if (FrozenUsers.Count == 0)
		{
			return;
		}
		foreach (NetworkUser item in GameUtil.AllUsers())
		{
			string text = UserKey(item);
			if (!FrozenUsers.Contains(text))
			{
				continue;
			}
			CharacterBody currentBody = item.GetCurrentBody();
			if (!Object.op_Implicit((Object)(object)currentBody))
			{
				continue;
			}
			if (!FreezePos.TryGetValue(text, out var value))
			{
				value = currentBody.footPosition;
				FreezePos[text] = value;
			}
			CharacterMotor characterMotor = currentBody.characterMotor;
			if (Object.op_Implicit((Object)(object)characterMotor))
			{
				characterMotor.velocity = Vector3.zero;
				if (Object.op_Implicit((Object)(object)((BaseCharacterController)characterMotor).Motor))
				{
					((BaseCharacterController)characterMotor).Motor.SetPosition(value, true);
				}
				else
				{
					((Component)currentBody).transform.position = value;
				}
			}
			else
			{
				((Component)currentBody).transform.position = value;
			}
		}
	}

	private static void FlyRemote(CharacterBody body, CharacterMotor motor)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		InputBankTest inputBank = body.inputBank;
		Vector3 val = (Object.op_Implicit((Object)(object)inputBank) ? inputBank.moveVector : Vector3.zero);
		if (Object.op_Implicit((Object)(object)inputBank))
		{
			if (inputBank.jump.down)
			{
				val += Vector3.up;
			}
			if (inputBank.rawMoveDown.down)
			{
				val += Vector3.down;
			}
		}
		motor.velocity = Vector3.zero;
		if (!(val.sqrMagnitude < 0.0001f))
		{
			float num = CheatRuntime.NoclipSpeed;
			if (Object.op_Implicit((Object)(object)inputBank) && inputBank.sprint.down)
			{
				num *= 3f;
			}
			Vector3 val2 = ((Component)body).transform.position + val.normalized * num * Time.unscaledDeltaTime;
			if (Object.op_Implicit((Object)(object)((BaseCharacterController)motor).Motor))
			{
				((BaseCharacterController)motor).Motor.SetPosition(val2, true);
			}
			else
			{
				((Component)body).transform.position = val2;
			}
		}
	}

	private static void EnableMotorNoclip(CharacterMotor motor)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		if (Object.op_Implicit((Object)(object)motor))
		{
			CharacterFlightParameters flightParameters = motor.flightParameters;
			flightParameters.channeledFlightGranterCount++;
			motor.flightParameters = flightParameters;
			CharacterGravityParameters gravityParameters = motor.gravityParameters;
			gravityParameters.channeledAntiGravityGranterCount++;
			motor.gravityParameters = gravityParameters;
			if (Object.op_Implicit((Object)(object)((BaseCharacterController)motor).Motor))
			{
				((BaseCharacterController)motor).Motor.SetCapsuleCollisionsActivation(false);
				((BaseCharacterController)motor).Motor.SetMovementCollisionsSolvingActivation(false);
				((BaseCharacterController)motor).Motor.SetGroundSolvingActivation(false);
			}
		}
	}

	private static void DisableMotorNoclip(CharacterMotor motor)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		if (Object.op_Implicit((Object)(object)motor))
		{
			CharacterFlightParameters flightParameters = motor.flightParameters;
			if (flightParameters.channeledFlightGranterCount > 0)
			{
				flightParameters.channeledFlightGranterCount--;
			}
			motor.flightParameters = flightParameters;
			CharacterGravityParameters gravityParameters = motor.gravityParameters;
			if (gravityParameters.channeledAntiGravityGranterCount > 0)
			{
				gravityParameters.channeledAntiGravityGranterCount--;
			}
			motor.gravityParameters = gravityParameters;
			if (Object.op_Implicit((Object)(object)((BaseCharacterController)motor).Motor))
			{
				((BaseCharacterController)motor).Motor.SetCapsuleCollisionsActivation(true);
				((BaseCharacterController)motor).Motor.SetMovementCollisionsSolvingActivation(true);
				((BaseCharacterController)motor).Motor.SetGroundSolvingActivation(true);
			}
		}
	}

	private static string BanPath()
	{
		string text = Path.GetDirectoryName(typeof(Plugin).Assembly.Location);
		if (string.IsNullOrEmpty(text))
		{
			text = ".";
		}
		return Path.Combine(text, "bans.txt");
	}

	private static void LoadBans()
	{
		Banned.Clear();
		BanNames.Clear();
		string path = BanPath();
		if (!File.Exists(path))
		{
			return;
		}
		try
		{
			string[] array = File.ReadAllLines(path);
			foreach (string text in array)
			{
				string text2 = ((text == null) ? "" : text.Trim());
				if (text2.Length == 0 || text2[0] == '#')
				{
					continue;
				}
				string text3 = "";
				int num = text2.IndexOf('#');
				if (num >= 0)
				{
					text3 = text2.Substring(num + 1).Trim();
					text2 = text2.Substring(0, num).Trim();
				}
				if (text2.Length != 0)
				{
					Banned.Add(text2);
					if (text3.Length > 0)
					{
						BanNames[text2] = text3;
					}
				}
			}
		}
		catch (Exception ex)
		{
			if (Plugin.Log != null)
			{
				Plugin.Log.LogWarning((object)("ban list: " + ex.Message));
			}
		}
	}

	private static void SaveBans()
	{
		try
		{
			StringBuilder stringBuilder = new StringBuilder();
			foreach (string item in Banned)
			{
				BanNames.TryGetValue(item, out var value);
				if (string.IsNullOrEmpty(value))
				{
					stringBuilder.AppendLine(item);
				}
				else
				{
					stringBuilder.AppendLine(item + " # " + value);
				}
			}
			File.WriteAllText(BanPath(), stringBuilder.ToString());
		}
		catch (Exception ex)
		{
			if (Plugin.Log != null)
			{
				Plugin.Log.LogWarning((object)("ban save: " + ex.Message));
			}
		}
	}
}
