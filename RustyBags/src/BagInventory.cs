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
    public BagInventory(string name, Sprite? bkg, int w, int h) : base(name, bkg, w, h)
    {
    }

    public virtual bool CanAddItem(ItemDrop.ItemData item)
    {
        var flag = item is not Bag;
        if (!flag) Player.m_localPlayer.Message(MessageHud.MessageType.Center, Keys.CannotStackBags);
        return flag;
    }
}

public class QuiverInventory : BagInventory
{
    private readonly string ammoType;
    public QuiverInventory(string name, string ammoType, Sprite? bkg, int w, int h) : base(name, bkg, w, h)
    {
        this.ammoType = ammoType;
    }

    public override bool CanAddItem(ItemDrop.ItemData item)
    {
        var flag = item.m_shared.m_ammoType == ammoType;
        if (!flag) Player.m_localPlayer.Message(MessageHud.MessageType.Center, $"{Keys.Only} {ammoType} {Keys.Allowed}");
        return flag;
    }
}