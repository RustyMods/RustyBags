using System.Collections.Generic;
using HarmonyLib;
using JetBrains.Annotations;

namespace RustyBags;

public class Quiver : Bag
{
    public ItemDrop.ItemData? ammoItem;
    
    public Quiver(ItemDrop.ItemData item) : base(item)
    {
    }

    protected override void SetupInventory()
    {
        BagSetup setup = BagSetup.bags[m_shared.m_name];
        BagSetup.Size? size = setup.sizes.TryGetValue(m_quality, out var s) ? s : new BagSetup.Size(1, 1); 
        inventory = new QuiverInventory("Quiver", this, Player.m_localPlayer?.GetInventory().m_bkg, size.width, size.height);
    }

    protected override void UpdateAttachments()
    {
        ammoItem = null;
        List<ItemDrop.ItemData>? list = inventory.GetAllItemsInGridOrder();
        for (var index = 0; index < list.Count; ++index)
        {
            ItemDrop.ItemData? item = list[index];
            if (ammoItem == null)
            {
                ammoItem = item;
                item.m_equipped = true;
            }
            else
            {
                item.m_equipped = false;
            }
        }

        m_bagEquipment?.SetArrowItem(ammoItem?.m_dropPrefab.name ?? "", ammoItem?.m_stack ?? 0);
    }

    [HarmonyPatch(typeof(Attack), nameof(Attack.EquipAmmoItem))]
    private static class Attack_EquipAmmoItem_Patch
    {
        [UsedImplicitly]
        private static bool Prefix(Humanoid character, ref bool __result)
        {
            if (character.GetEquippedBag() is not Quiver) return true;
            __result = true;
            return false;
        }
    }
    
    [HarmonyPatch(typeof(Attack), nameof(Attack.FindAmmo))]
    private static class Attack_FindAmmo_Patch
    {
        [UsedImplicitly]
        private static void Postfix(Humanoid character, ItemDrop.ItemData weapon, ref ItemDrop.ItemData? __result)
        {
            if (__result != null || character.GetEquippedBag() is not Quiver quiver || string.IsNullOrEmpty(weapon.m_shared.m_ammoType)) return;
            __result = quiver.ammoItem;
        }
    }

    [HarmonyPatch(typeof(Attack), nameof(Attack.HaveAmmo))]
    private static class Attack_HaveAmmo_Patch
    {
        [UsedImplicitly]
        private static bool Prefix(Humanoid character, ItemDrop.ItemData weapon, ref bool __result)
        {
            if (__result || character.GetEquippedBag() is not Quiver quiver || string.IsNullOrEmpty(weapon.m_shared.m_ammoType)) return true;
            if (quiver.ammoItem == null) return true;
            __result = quiver.ammoItem.m_shared.m_itemType != ItemType.Consumable || character.CanConsumeItem(quiver.ammoItem);
            return false;
        }
    }
    
    [HarmonyPatch(typeof(Attack), nameof(Attack.UseAmmo))]
    private static class Attack_UseAmmo_Patch
    {
        [UsedImplicitly]
        private static bool Prefix(Attack __instance, out ItemDrop.ItemData? ammoItem, ref bool __result)
        {
            ammoItem = __instance.m_character.GetAmmoItem();
            if (string.IsNullOrEmpty(__instance.m_weapon.m_shared.m_ammoType)) return true;
            if (ammoItem != null) return true;
            if (__instance.m_character.GetEquippedBag() is not Quiver quiver) return true;
            __instance.m_ammoItem = null;
            ammoItem = quiver.ammoItem;
            
            if (ammoItem?.m_shared.m_itemType is ItemType.Consumable) return true;
            
            if (ammoItem == null || ammoItem.m_shared.m_ammoType != __instance.m_weapon.m_shared.m_ammoType)
            {
                ammoItem = null;
            }

            if (ammoItem == null)
            {
                __result = false;
                return false;
            }

            quiver.inventory.RemoveItem(ammoItem, 1);
            __instance.m_ammoItem = ammoItem;
            __result = true;
            return false;
        }
    }
}