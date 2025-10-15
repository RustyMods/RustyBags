using HarmonyLib;
using JetBrains.Annotations;
using RustyBags.Utilities;
using UnityEngine;

namespace RustyBags;

[HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.DropItem))]
public static class InventoryGrid_DropItem_Patch
{
    [UsedImplicitly]
    private static bool Prefix(InventoryGrid __instance, Inventory fromInventory, ItemDrop.ItemData item, Vector2i pos)
    {
        return __instance.m_inventory is not BagInventory bagInventory || bagInventory.CanAddItem(item);
    }
}

[HarmonyPatch(typeof(Inventory), nameof(Inventory.AddItem), typeof(ItemDrop.ItemData))]
public static class Inventory_AddItem_Patch
{
    [UsedImplicitly]
    private static bool Prefix(Inventory __instance, ItemDrop.ItemData item, ref bool __result)
    {
        if (__instance is not BagInventory bagInventory || bagInventory.CanAddItem(item)) return true;
        __result = false;
        return false;
    }
}

public class BagInventory : Inventory
{
    private readonly Bag? bag;
    public BagInventory(string name, Bag? bag, Sprite? bkg, int w, int h) : base(name, bkg, w, h)
    {
        this.bag = bag;
    }

    public virtual bool CanAddItem(ItemDrop.ItemData item)
    {
        if (item is not Bag)
        {
            if (bag is null) return true;
            BagSetup.Restriction restrictions = bag.GetSetup().restrictConfig?.Value ?? BagSetup.Restriction.None;
            if (restrictions is BagSetup.Restriction.None) return true;
            switch (item.m_shared.m_itemType)
            {
                case ItemDrop.ItemData.ItemType.Consumable:
                    return !restrictions.HasFlag(BagSetup.Restriction.NoConsumables);
                case ItemDrop.ItemData.ItemType.Material:
                    return !restrictions.HasFlag(BagSetup.Restriction.NoMaterials);
                case ItemDrop.ItemData.ItemType.Fish:
                    return !restrictions.HasFlag(BagSetup.Restriction.NoFishes);
                case ItemDrop.ItemData.ItemType.Trophy:
                    return !restrictions.HasFlag(BagSetup.Restriction.NoTrophies);
                default:
                    return true;
            }
        }
        Player.m_localPlayer.Message(MessageHud.MessageType.Center, Keys.CannotStackBags);
        return false;
    }
}

public class QuiverInventory : BagInventory
{
    private readonly string ammoType;
    public QuiverInventory(string name, Bag bag, Sprite? bkg, int w, int h) : base(name, bag, bkg, w, h)
    {
        ammoType = bag.m_shared.m_ammoType;
    }

    public override bool CanAddItem(ItemDrop.ItemData item)
    {
        if (item.m_shared.m_ammoType == ammoType) return true;
        Player.m_localPlayer.Message(MessageHud.MessageType.Center, $"{Keys.Only} {ammoType} {Keys.Allowed}");
        return false;
    }
}