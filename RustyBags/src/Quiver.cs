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

        if (ammoItem?.m_shared.m_ammoType == "$ammo_bolts")
        {
            if (!ammoItem.IsValidBolt())
            {
                string randomBolt = BoltExclusion.GetRandomBoltName();
                m_bagEquipment?.SetArrowItem(ammoItem != null ? randomBolt : "", ammoItem?.m_stack ?? 0, true);
            }
            else
            {
                m_bagEquipment?.SetArrowItem(ammoItem?.m_dropPrefab.name ?? "", ammoItem?.m_stack ?? 0, true);
            }
        }
        else
        {
            m_bagEquipment?.SetArrowItem(ammoItem?.m_dropPrefab.name ?? "", ammoItem?.m_stack ?? 0);
        }
    }

    [HarmonyPatch(typeof(Attack), nameof(Attack.EquipAmmoItem))]
    private static class Attack_EquipAmmoItem_Patch
    {
        [UsedImplicitly]
        private static bool Prefix(Humanoid character, ItemDrop.ItemData weapon, ref bool __result)
        {
            if (character.GetEquippedQuiver() is not {} quiver || quiver.ammoItem == null || weapon.m_shared.m_ammoType != quiver.ammoItem.m_shared.m_ammoType) return true;
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
            if (__result != null || character.GetEquippedQuiver() is not {} quiver || quiver.ammoItem == null || quiver.ammoItem.m_shared.m_ammoType != weapon.m_shared.m_ammoType) return;
            __result = quiver.ammoItem;
        }
    }

    [HarmonyPatch(typeof(Attack), nameof(Attack.HaveAmmo))]
    private static class Attack_HaveAmmo_Patch
    {
        [UsedImplicitly]
        private static bool Prefix(Humanoid character, ItemDrop.ItemData weapon, ref bool __result)
        {
            if (__result || character.GetEquippedQuiver() is not {} quiver || quiver.ammoItem == null || quiver.ammoItem.m_shared.m_ammoType != weapon.m_shared.m_ammoType) return true;
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
            if (ammoItem?.m_shared.m_ammoType != __instance.m_weapon.m_shared.m_ammoType)
            {
                ammoItem = null;
            }
            if (ammoItem != null) return true;
            
            if (__instance.m_character.GetEquippedQuiver() is not {} quiver)
            {
                return true;
            }

            if (quiver.ammoItem == null)
            {
                return true;
            }

            if (quiver.ammoItem.m_shared.m_ammoType != __instance.m_weapon.m_shared.m_ammoType)
            {
                return true;
            }
            
            ammoItem = quiver.ammoItem!;
            __instance.m_ammoItem = ammoItem;

            if (ammoItem.m_shared.m_itemType is ItemType.Consumable)
            {
                __result = __instance.m_character.ConsumeItem(quiver.inventory, ammoItem);
            }
            else
            {
                quiver.inventory.RemoveItem(ammoItem, 1);
                __result = true;
            }
            return false;
        }
    }
}