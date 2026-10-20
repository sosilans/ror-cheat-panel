using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using BepInEx;
using RoR2;
using RoRCheatPanel.UI;
using UnityEngine;

namespace RoRCheatPanel;

internal static class PresetStore
{
	public static string Folder => Path.Combine(Paths.PluginPath, "RoRCheatPanel", "Presets");

	public static string LegacyFolder => Path.Combine(Paths.PluginPath, "SimpleRoR2Cheat", "ItemPresets");

	public static List<CheatPreset> LoadAll()
	{
		Directory.CreateDirectory(Folder);
		List<CheatPreset> list = new List<CheatPreset>();
		LoadFrom(Folder, list);
		if (Directory.Exists(LegacyFolder))
		{
			LoadFrom(LegacyFolder, list);
		}
		list.Sort((CheatPreset a, CheatPreset b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
		return list;
	}

	private static void LoadFrom(string folder, List<CheatPreset> list)
	{
		string[] files = Directory.GetFiles(folder, "*.xml");
		foreach (string text in files)
		{
			try
			{
				CheatPreset cheatPreset = Read(text);
				if (cheatPreset != null && !string.IsNullOrEmpty(cheatPreset.Name))
				{
					list.Add(cheatPreset);
				}
			}
			catch (Exception ex)
			{
				Plugin.Log.LogWarning((object)("Preset read failed " + text + ": " + ex.Message));
			}
		}
	}

	public static CheatPreset CaptureCurrent(string name)
	{
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		CheatPreset cheatPreset = new CheatPreset
		{
			Name = name
		};
		Inventory val = (Object.op_Implicit((Object)(object)GameUtil.LocalMaster) ? GameUtil.LocalMaster.inventory : null);
		if (!Object.op_Implicit((Object)(object)val))
		{
			return cheatPreset;
		}
		for (int i = 0; i < ItemCatalog.itemCount; i++)
		{
			ItemDef itemDef = ItemCatalog.GetItemDef((ItemIndex)i);
			if (Object.op_Implicit((Object)(object)itemDef))
			{
				int itemCountPermanent = val.GetItemCountPermanent(itemDef);
				if (itemCountPermanent > 0)
				{
					cheatPreset.Items.Add(new ItemStack
					{
						Id = ((Object)itemDef).name,
						Count = itemCountPermanent
					});
				}
			}
		}
		EquipmentDef equipmentDef = EquipmentCatalog.GetEquipmentDef(val.GetEquipmentIndex());
		cheatPreset.EquipmentId = (Object.op_Implicit((Object)(object)equipmentDef) ? ((Object)equipmentDef).name : "");
		return cheatPreset;
	}

	public static void Save(CheatPreset preset)
	{
		Directory.CreateDirectory(Folder);
		string text = Sanitize(preset.Name);
		preset.FilePath = Path.Combine(Folder, text + ".xml");
		XElement xElement = new XElement("CheatPreset", new XAttribute("name", preset.Name ?? "preset"), new XAttribute("version", "3"));
		foreach (ItemStack item in preset.Items)
		{
			xElement.Add(new XElement("Item", new XAttribute("id", item.Id ?? ""), new XAttribute("count", item.Count)));
		}
		if (!string.IsNullOrEmpty(preset.EquipmentId))
		{
			xElement.Add(new XElement("Equipment", new XAttribute("id", preset.EquipmentId)));
		}
		new XDocument(xElement).Save(preset.FilePath);
	}

	public static void Delete(CheatPreset preset)
	{
		if (preset != null && !string.IsNullOrEmpty(preset.FilePath) && File.Exists(preset.FilePath))
		{
			File.Delete(preset.FilePath);
		}
	}

	public static void Apply(CheatPreset preset, bool replace)
	{
		if (preset == null || !GameUtil.RequireHost("пресет"))
		{
			return;
		}
		if (replace)
		{
			GameUtil.ClearInventory();
		}
		foreach (ItemStack item in preset.Items)
		{
			ItemDef val = ResolveItem(item.Id);
			if (Object.op_Implicit((Object)(object)val))
			{
				GameUtil.GiveItem(val, item.Count);
			}
		}
		if (!string.IsNullOrEmpty(preset.EquipmentId))
		{
			EquipmentDef val2 = ResolveEquipment(preset.EquipmentId);
			if (Object.op_Implicit((Object)(object)val2))
			{
				GameUtil.SetEquipment(val2);
			}
		}
		UiHelpers.Toast(Loc.F("preset_ok", preset.Name));
	}

	public static ItemDef ResolveItem(string id)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		if (string.IsNullOrEmpty(id))
		{
			return null;
		}
		ItemDef itemDef = ItemCatalog.GetItemDef(ItemCatalog.FindItemIndex(id));
		if (Object.op_Implicit((Object)(object)itemDef))
		{
			return itemDef;
		}
		if (int.TryParse(id, out var result))
		{
			return ItemCatalog.GetItemDef((ItemIndex)result);
		}
		for (int i = 0; i < ItemCatalog.itemCount; i++)
		{
			ItemDef itemDef2 = ItemCatalog.GetItemDef((ItemIndex)i);
			if (Object.op_Implicit((Object)(object)itemDef2) && (string.Equals(((Object)itemDef2).name, id, StringComparison.OrdinalIgnoreCase) || string.Equals(itemDef2.nameToken, id, StringComparison.OrdinalIgnoreCase)))
			{
				return itemDef2;
			}
		}
		return null;
	}

	public static EquipmentDef ResolveEquipment(string id)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		if (string.IsNullOrEmpty(id))
		{
			return null;
		}
		EquipmentDef equipmentDef = EquipmentCatalog.GetEquipmentDef(EquipmentCatalog.FindEquipmentIndex(id));
		if (Object.op_Implicit((Object)(object)equipmentDef))
		{
			return equipmentDef;
		}
		if (int.TryParse(id, out var result))
		{
			return EquipmentCatalog.GetEquipmentDef((EquipmentIndex)result);
		}
		return null;
	}

	private static CheatPreset Read(string file)
	{
		XElement root = XDocument.Load(file).Root;
		if (root == null)
		{
			return null;
		}
		CheatPreset cheatPreset = new CheatPreset
		{
			FilePath = file,
			Name = (Attr(root, "name") ?? El(root, "PresetName") ?? Path.GetFileNameWithoutExtension(file))
		};
		foreach (XElement item in root.Descendants())
		{
			switch (item.Name.LocalName)
			{
			case "Item":
				cheatPreset.Items.Add(new ItemStack
				{
					Id = (Attr(item, "id") ?? Attr(item, "name")),
					Count = IntAttr(item, "count", 1)
				});
				break;
			case "ItemPresetItemData":
			case "ItemPresetItem":
			{
				string text = El(item, "ItemIndex");
				int.TryParse(El(item, "Quantity") ?? El(item, "ItemCount"), out var result);
				if (result <= 0)
				{
					result = 1;
				}
				ItemDef val2 = ResolveItem(text);
				cheatPreset.Items.Add(new ItemStack
				{
					Id = (Object.op_Implicit((Object)(object)val2) ? ((Object)val2).name : text),
					Count = result
				});
				break;
			}
			case "Equipment":
				cheatPreset.EquipmentId = Attr(item, "id") ?? item.Value;
				break;
			case "EquipmentIndex":
			{
				EquipmentDef val = ResolveEquipment(item.Value);
				cheatPreset.EquipmentId = (Object.op_Implicit((Object)(object)val) ? ((Object)val).name : item.Value);
				break;
			}
			}
		}
		return cheatPreset;
	}

	private static string Attr(XElement el, string name)
	{
		return (string?)el.Attribute(name);
	}

	private static string El(XElement el, string name)
	{
		return (el.Element(name) ?? el.Element(XName.Get(name, el.Name.NamespaceName)))?.Value;
	}

	private static int IntAttr(XElement el, string name, int fallback)
	{
		if (!int.TryParse(Attr(el, name), out var result))
		{
			return fallback;
		}
		return result;
	}

	private static string Sanitize(string name)
	{
		char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
		char[] array = (name ?? "preset").ToCharArray();
		for (int i = 0; i < array.Length; i++)
		{
			if (Array.IndexOf(invalidFileNameChars, array[i]) >= 0)
			{
				array[i] = '_';
			}
		}
		string text = new string(array).Trim();
		if (!string.IsNullOrEmpty(text))
		{
			return text;
		}
		return "preset";
	}
}
