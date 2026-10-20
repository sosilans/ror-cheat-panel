using UnityEngine.Networking;

namespace RoRCheatPanel;

internal class AdminClientMessage : MessageBase
{
	public byte Op;

	public string Data;

	public override void Serialize(NetworkWriter writer)
	{
		writer.Write(Op);
		writer.Write(Data ?? "");
	}

	public override void Deserialize(NetworkReader reader)
	{
		Op = reader.ReadByte();
		Data = reader.ReadString();
	}
}
