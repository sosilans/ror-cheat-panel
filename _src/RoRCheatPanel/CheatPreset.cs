using System.Collections.Generic;

namespace RoRCheatPanel;

internal sealed class CheatPreset
{
	public string Name;

	public string FilePath;

	public readonly List<ItemStack> Items = new List<ItemStack>();

	public string EquipmentId;
}
