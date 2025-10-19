using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;
using RustyBags.Managers;

namespace RustyBags;

public static class BagCraft
{
    public static int CountItems(Inventory inventory, string sharedName, int quality, bool matchWorldLevel, Player player)
    {
        if (player.GetEquippedBag() is not {} bag || !Configs.CraftFromBag) return inventory.CountItems(sharedName, quality);
        return inventory.CountItems(sharedName, quality) + bag.inventory.CountItems(sharedName,quality);
    }
    
    public static bool HaveItem(Inventory inventory, string sharedName, bool worldLevelBased, Player player)
    {
        if (player.GetEquippedBag() is not {} bag  || !Configs.CraftFromBag) return inventory.HaveItem(sharedName);
        return inventory.HaveItem(sharedName) || bag.inventory.HaveItem(sharedName);
    }
    
    public static void RemoveItem(Inventory inventory, string sharedName, int amount, int quality, bool worldLevelBased, Player player)
    {
        if (player.GetEquippedBag() is not { } bag  || !Configs.CraftFromBag)
        {
            inventory.RemoveItem(sharedName, amount, quality);
        }
        else
        {
            int bagCount = bag.inventory.CountItems(sharedName, quality);
            if (bagCount > 0)
            {
                if (amount > bagCount)
                {
                    bag.inventory.RemoveItem(sharedName, bagCount, quality);
                    amount -= bagCount;
                }
                else
                {
                    bag.inventory.RemoveItem(sharedName, amount, quality);
                    amount = 0;
                }
            }

            if (amount > 0)
            {
                inventory.RemoveItem(sharedName, amount, quality);
            }
        }
    }
    
    [HarmonyPatch(typeof(Player), nameof(Player.HaveRequirementItems))]
    private static class Player_HaveRequirementItems_Transpiler
    {
        [UsedImplicitly]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo? target = AccessTools.Method(typeof(Inventory), nameof(Inventory.CountItems));
            MethodInfo? replacement = AccessTools.Method(typeof(BagCraft), nameof(CountItems));
            return new CodeMatcher(instructions)
                .Start()
                .MatchStartForward(new CodeMatch(OpCodes.Callvirt, target))
                .Insert(new CodeInstruction(OpCodes.Ldarg_0))
                .Advance(1)
                .Set(OpCodes.Call, replacement)
                .InstructionEnumeration();
        }
    }
    
    [HarmonyPatch(typeof(Player), nameof(Player.GetFirstRequiredItem))]
    private static class PlayerGetFirstRequiredItemPatch
    {
        [UsedImplicitly]
        private static bool Prefix(Player __instance, Inventory inventory, Recipe recipe, int qualityLevel,
            out int amount, out int extraAmount, int craftMultiplier, ref ItemDrop.ItemData? __result)
        {
            amount = 0;
            extraAmount = 0;
            if (__instance.GetEquippedBag() is not { } bag  || !Configs.CraftFromBag) return true;
            foreach (var resource in recipe.m_resources)
            {
                var item = resource.m_resItem;
                if (item)
                {
                    int num = resource.GetAmount(qualityLevel) * craftMultiplier;
                    for (int quality = 0; quality <= item.m_itemData.m_shared.m_maxQuality; ++quality)
                    {
                        if (bag.inventory.CountItems(item.m_itemData.m_shared.m_name, quality) >= num)
                        {
                            amount = num;
                            extraAmount = resource.m_extraAmountOnlyOneIngredient;
                            __result = bag.inventory.GetItem(item.m_itemData.m_shared.m_name, quality);
                            return false;
                        }

                        if (inventory.CountItems(item.m_itemData.m_shared.m_name, quality) >= num)
                        {
                            amount = num;
                            extraAmount = resource.m_extraAmountOnlyOneIngredient;
                            __result = inventory.GetItem(item.m_itemData.m_shared.m_name, quality);
                            return false;
                        }
                    }
                }
            }

            amount = 0;
            extraAmount = 0;
            __result = null;
            return false;
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.HaveRequirements), typeof(Piece), typeof(Player.RequirementMode))]
    private static class Player_HaveRequirements_Transpiler
    {
        [UsedImplicitly]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo? target1 = AccessTools.Method(typeof(Inventory), nameof(Inventory.CountItems));
            MethodInfo? target2 = AccessTools.Method(typeof(Inventory), nameof(Inventory.HaveItem), new Type[]{typeof(string), typeof(bool)});
            
            MethodInfo? getCount = AccessTools.Method(typeof(BagCraft), nameof(CountItems));
            MethodInfo? haveItem = AccessTools.Method(typeof(BagCraft), nameof(HaveItem));
            
            return new CodeMatcher(instructions)
                .Start()
                .MatchStartForward(new CodeMatch(OpCodes.Callvirt, target1))
                .Insert(new CodeInstruction(OpCodes.Ldarg_0))
                .Advance(1)
                .Set(OpCodes.Call, getCount)
                .Start()
                .MatchStartForward(new CodeMatch(OpCodes.Callvirt, target2))
                .Insert(new CodeInstruction(OpCodes.Ldarg_0))
                .Advance(1)
                .Set(OpCodes.Call, haveItem)
                .InstructionEnumeration();
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.ConsumeResources))]
    private static class Player_ConsumeResources_Transpiler
    {
        [UsedImplicitly]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo? target = AccessTools.Method(typeof(Inventory), nameof(Inventory.RemoveItem), new Type[] { typeof(string), typeof(int), typeof(int), typeof(bool) });
            MethodInfo? method = AccessTools.Method(typeof(BagCraft), nameof(RemoveItem));
            return new CodeMatcher(instructions)
                .Start()
                .MatchStartForward(new CodeMatch(OpCodes.Callvirt, target))
                .Insert(new CodeInstruction(OpCodes.Ldarg_0))
                .Advance(1)
                .Set(OpCodes.Call, method)
                .InstructionEnumeration();
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.SetupRequirement))]
    private static class InventoryGui_SetupRequirement_Transpiler
    {
        [UsedImplicitly]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo? target = AccessTools.Method(typeof(Inventory), nameof(Inventory.CountItems));
            MethodInfo? replacement = AccessTools.Method(typeof(BagCraft), nameof(CountItems));
            return new CodeMatcher(instructions)
                .Start()
                .MatchStartForward(new CodeMatch(OpCodes.Callvirt, target))
                .Insert(new CodeInstruction(OpCodes.Ldarg_2))
                .Advance(1)
                .Set(OpCodes.Call, replacement)
                .InstructionEnumeration();
        }
    }
}