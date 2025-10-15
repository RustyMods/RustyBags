using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;

namespace RustyBags;

public static class BagGui
{
    private static readonly int visible = Animator.StringToHash("visible");

    public static Bag? m_currentBag;

    [HarmonyPatch(typeof(Player), nameof(Player.SetLocalPlayer))]
    private static class Player_SetLocalPlayer_Patch
    {
        [UsedImplicitly]
        // make sure new local player reset current bag state
        private static void Postfix() => m_currentBag = null;
    }
    
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.OnSelectedItem))]
    private static class InventoryGui_OnSelectedItem_Patch
    {
        [UsedImplicitly]
        private static bool Prefix(InventoryGui __instance, InventoryGrid grid, ItemDrop.ItemData? item, InventoryGrid.Modifier mod)
        {
            if (item == null || item.m_shared.m_questItem || mod is not InventoryGrid.Modifier.Move || __instance.m_dragGo || m_currentBag == null || __instance.m_currentContainer != null || __instance.IsJewelBagOpen()) return true;
            
            var localPlayer = Player.m_localPlayer;
            if (localPlayer.IsTeleporting()) return false;
            
            localPlayer.RemoveEquipAction(item);
            localPlayer.UnequipItem(item);
            if (grid.GetInventory() == m_currentBag.inventory) localPlayer.GetInventory().MoveItemToThis(grid.GetInventory(), item);
            else m_currentBag.inventory.MoveItemToThis(localPlayer.GetInventory(), item);
            __instance.m_moveItemEffects.Create(__instance.transform.position, Quaternion.identity); 

            return false;
        }
    }
    
    [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.UpdateGui))]
    private static class InventoryGrid_UpdateGui_Patch
    {
        // Remove numbers in the grid gui
        [UsedImplicitly]
        private static void Postfix(InventoryGrid __instance)
        {
            if (__instance.m_inventory is not BagInventory) return;
            foreach (InventoryGrid.Element? element in __instance.m_elements)
            {
                element.m_go.transform.Find("binding").GetComponent<TMP_Text>().enabled = false;
            }
        }
    }
    
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.DoCrafting))]
    private static class InventoryGui_DoCraft_Transpile
    {
        [UsedImplicitly]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo? targetMethod = AccessTools.Method(typeof(Inventory), nameof(Inventory.AddItem),
                new[]
                {
                    typeof(string), typeof(int), typeof(int), typeof(int), typeof(long), typeof(string),
                    typeof(Vector2i), typeof(bool)
                });
            MethodInfo? method = AccessTools.Method(typeof(InventoryGui_DoCraft_Transpile), nameof(AddCustomData));
            CodeInstruction[] newInstructions = {
                new (OpCodes.Dup),
                new (OpCodes.Ldarg_0),
                new (OpCodes.Call, method)
            };
            return new CodeMatcher(instructions)
                .Start()
                .MatchStartForward(new CodeMatch(OpCodes.Callvirt, targetMethod))
                .Advance(1)
                .Insert(newInstructions)
                .InstructionEnumeration();
        }

        public static void AddCustomData(ItemDrop.ItemData? item, InventoryGui instance)
        {
            if (item is not Bag bag || instance.m_craftUpgradeItem is not Bag oldBag) return;
            bag.m_customData = new(oldBag.m_customData);
            bag.Load();
        }
    }
    
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateInventoryWeight))]
    private static class InventoryGui_UpdateInventoryWeight_Patch
    {
        [UsedImplicitly]
        private static void Prefix(InventoryGui __instance, Player player)
        {
            if (m_currentBag == null) return;
            m_currentBag.UpdateWeight();
            player.GetInventory().UpdateTotalWeight();
        }
    }
    
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateContainerWeight))]
    private static class InventoryGui_UpdateContainerWeight_Patch
    {
        [UsedImplicitly]
        private static void Postfix(InventoryGui __instance)
        {
            if (__instance.m_currentContainer != null || m_currentBag == null || __instance.IsJewelBagOpen()) return;
            __instance.m_containerWeight.text = Mathf.CeilToInt(m_currentBag.GetInventoryWeight()).ToString();
        }
    }
    
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Show))]
    private static class InventoryGui_Show_Patch
    {
        [UsedImplicitly]
        private static void Prefix(Container? container)
        {
            if (m_currentBag?.Load() ?? false) Player.m_localPlayer?.GetInventory().Changed();;
        }
    }
    
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateContainer))]
    private static class InventoryGui_UpdateContainer_Patch
    {
        [UsedImplicitly]
        [HarmonyPriority(Priority.Last)]
        private static bool Prefix(InventoryGui __instance, Player player)
        {
            if (!__instance.m_animator.GetBool(visible)) return true;
            if (__instance.m_currentContainer != null || m_currentBag == null) return true;
            if (__instance.IsJewelBagOpen()) return true;
            
            __instance.m_container.gameObject.SetActive(true);
            __instance.m_containerGrid.UpdateInventory(m_currentBag.inventory, player, __instance.m_dragItem);
            __instance.m_containerName.text = Localization.instance.Localize(m_currentBag.m_shared.m_name);
            if (__instance.m_firstContainerUpdate)
            {
                __instance.m_containerGrid.ResetView();
                __instance.m_firstContainerUpdate = false;
                __instance.m_containerHoldTime = 0.0f;
                __instance.m_containerHoldState = 0;
            }
            return false;
        }
    }

    private static bool IsJewelBagOpen(this InventoryGui instance)
    {
        // jewelcrafting and backpacks disables take all button,
        // so use this to check if jewelcrafting bag or backpack is active
        return !instance.m_stackAllButton.gameObject.activeSelf;
    }

    
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.OnStackAll))]
    private static class InventoryGui_OnStackAll_Patch
    {
        [UsedImplicitly]
        private static void Postfix(InventoryGui __instance)
        {
            if (Player.m_localPlayer.IsTeleporting() || __instance.m_currentContainer != null || m_currentBag == null || __instance.IsJewelBagOpen()) return;
            m_currentBag.inventory.StackAll(Player.m_localPlayer.GetInventory());
        }
    }
    
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.OnTakeAll))]
    private static class InventoryGui_OnTakeAll_Patch
    {
        [UsedImplicitly]
        private static void Postfix(InventoryGui __instance)
        {
            if (Player.m_localPlayer.IsTeleporting() || __instance.m_currentContainer != null || m_currentBag == null || __instance.IsJewelBagOpen()) return;
            __instance.SetupDragItem(null, null, 1);
            Inventory inventory = m_currentBag.inventory;
            Player.m_localPlayer.GetInventory().MoveAll(inventory);
        }
    }
    
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.HaveRepairableItems))]
    private static class InventoryGui_HaveRepairableItems_Transpiler
    {
        [UsedImplicitly]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) => AddBagWornItems(instructions);
    }
    
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.RepairOneItem))]
    private static class InventoryGui_RepairOneItem_Transpiler
    {
        [UsedImplicitly]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) => AddBagWornItems(instructions);
    }

    private static IEnumerable<CodeInstruction> AddBagWornItems(IEnumerable<CodeInstruction> instructions)
    {
        MethodInfo? target = AccessTools.Method(typeof(Inventory), nameof(Inventory.GetWornItems));
        MethodInfo? method = AccessTools.Method(typeof(BagGui), nameof(GetBagWornItems));
        CodeInstruction[] newInstructions = new[]
        {
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Call, method)
        };

        return new CodeMatcher(instructions)
            .Start()
            .MatchStartForward(new CodeMatch(OpCodes.Callvirt, target))
            .Advance(1)
            .Insert(newInstructions)
            .InstructionEnumeration();
    }
    
    public static void GetBagWornItems(InventoryGui instance)
    {
        if (Player.m_localPlayer.GetBag() is not { } bag) return;
        bag.inventory.GetWornItems(instance.m_tempWornItems);
    }
}