using System;
using System.Runtime.CompilerServices;
using RoR2;
using RoR2.Networking;
using UnityEngine;
using UnityEngine.Networking;

namespace RoRCheatPanel;

internal static class AdminNet
{
	[Serializable]
	[CompilerGenerated]
	private sealed class _003C_003Ec
	{
		public static readonly _003C_003Ec _003C_003E9 = new _003C_003Ec();

		public static Action<NetworkClient> _003C_003E9__5_0;

		public static NetworkMessageDelegate _003C_003E9__10_0;

		internal void _003CInit_003Eb__5_0(NetworkClient _)
		{
			RegisterClient();
		}

		internal void _003CRegisterServer_003Eb__10_0(NetworkMessage _)
		{
		}
	}

	public const byte GrantAch = 1;

	public const byte GrantAllAch = 2;

	private const short MsgId = 19971;

	private static bool _inited;

	private static bool _clientHandler;

	public static void Init()
	{
		if (!_inited)
		{
			_inited = true;
			NetworkManagerSystem.onStartClientGlobal += delegate
			{
				RegisterClient();
			};
			NetworkManagerSystem.onStartServerGlobal += RegisterServer;
			RoR2Application.onLoadFinished = (Action)Delegate.Combine(RoR2Application.onLoadFinished, new Action(TryRegisterNow));
		}
	}

	public static bool SendGrant(NetworkUser user, string achievementId)
	{
		return Send(user, 1, achievementId ?? "");
	}

	public static bool SendGrantAll(NetworkUser user)
	{
		return Send(user, 2, "");
	}

	private static bool Send(NetworkUser user, byte op, string data)
	{
		if (!Object.op_Implicit((Object)(object)user) || !NetworkServer.active)
		{
			return false;
		}
		NetworkConnection connectionToClient = ((NetworkBehaviour)user).connectionToClient;
		if (connectionToClient == null)
		{
			return false;
		}
		NetworkServer.SendToClient(connectionToClient.connectionId, (short)19971, (MessageBase)(object)new AdminClientMessage
		{
			Op = op,
			Data = data
		});
		return true;
	}

	private static void TryRegisterNow()
	{
		if (NetworkServer.active)
		{
			RegisterServer();
		}
		RegisterClient();
	}

	private static void RegisterServer()
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Expected O, but got Unknown
		object obj = _003C_003Ec._003C_003E9__10_0;
		if (obj == null)
		{
			NetworkMessageDelegate val = delegate
			{
			};
			_003C_003Ec._003C_003E9__10_0 = val;
			obj = (object)val;
		}
		NetworkServer.RegisterHandler((short)19971, (NetworkMessageDelegate)obj);
	}

	private static void RegisterClient()
	{
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Expected O, but got Unknown
		NetworkManagerSystem singleton = NetworkManagerSystem.singleton;
		NetworkClient val = (Object.op_Implicit((Object)(object)singleton) ? ((NetworkManager)singleton).client : null);
		if (val != null && !_clientHandler)
		{
			val.RegisterHandler((short)19971, new NetworkMessageDelegate(OnClient));
			_clientHandler = true;
		}
	}

	private static void OnClient(NetworkMessage netMsg)
	{
		AdminClientMessage adminClientMessage = netMsg.ReadMessage<AdminClientMessage>();
		if (adminClientMessage == null)
		{
			return;
		}
		if (adminClientMessage.Op == 1)
		{
			AchievementDef achievementDef = AchievementManager.GetAchievementDef(adminClientMessage.Data);
			if (achievementDef != null)
			{
				GameUtil.GrantAchievement(achievementDef);
			}
		}
		else if (adminClientMessage.Op == 2)
		{
			GameUtil.GrantAllAchievements();
		}
	}
}
