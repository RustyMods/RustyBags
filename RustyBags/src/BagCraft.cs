using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RustyBags.Managers;

namespace RustyBags;

public static class BagCraft
{
    private static Harmony? harmony;
    private const string CraftFromBag_ID = "RustyBags.CraftFromBag";
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

    private static bool HasConflicts()
    {
        return Harmony.GetPatchInfo(AccessTools.Method(typeof(Player), nameof(Player.HaveRequirementItems)))?.Owners.Count > 0;
    }

    public static void Init()
    {
        if (HasConflicts())
        {
            RustyBagsPlugin.RustyBagsLogger.LogWarning("Found conflicts with craft from bags, disabling craft from bag.");
            return;
        }
        harmony = new Harmony(CraftFromBag_ID);
        harmony.Patch(AccessTools.DeclaredMethod(typeof(Player),nameof(Player.HaveRequirementItems)), transpiler: new HarmonyMethod(typeof(BagCraft), nameof(HaveRequirementItems_Transpiler)));
        harmony.Patch(AccessTools.DeclaredMethod(typeof(Player),nameof(Player.GetFirstRequiredItem)), prefix: new HarmonyMethod(typeof(BagCraft), nameof(GetFirstRequiredItem_Prefix)));
        harmony.Patch(AccessTools.DeclaredMethod(typeof(Player),nameof(Player.HaveRequirements), new []{typeof(Piece), typeof(Player.RequirementMode)}), transpiler: new HarmonyMethod(typeof(BagCraft), nameof(HaveRequirements_Transpiler)));
        harmony.Patch(AccessTools.DeclaredMethod(typeof(Player),nameof(Player.ConsumeResources)), transpiler: new HarmonyMethod(typeof(BagCraft),nameof(ConsumeResources_Transpiler)));
        harmony.Patch(AccessTools.DeclaredMethod(typeof(InventoryGui),nameof(InventoryGui.SetupRequirement)), transpiler: new HarmonyMethod(typeof(BagCraft), nameof(SetupRequirement_Transpiler)));
    }

    public static IEnumerable<CodeInstruction> HaveRequirementItems_Transpiler(IEnumerable<CodeInstruction> instructions)
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

    public static bool GetFirstRequiredItem_Prefix(Player __instance, Inventory inventory, Recipe recipe, int qualityLevel, out int amount, out int extraAmount, int craftMultiplier, ref ItemDrop.ItemData? __result)
    {
        amount = 0;
        extraAmount = 0;
        if (__instance.GetEquippedBag() is not { } bag  || !Configs.CraftFromBag) return true;
        foreach (Piece.Requirement? resource in recipe.m_resources)
        {
            ItemDrop? item = resource.m_resItem;
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

    public static IEnumerable<CodeInstruction> HaveRequirements_Transpiler(IEnumerable<CodeInstruction> instructions)
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

    public static IEnumerable<CodeInstruction> ConsumeResources_Transpiler(IEnumerable<CodeInstruction> instructions)
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

    public static IEnumerable<CodeInstruction> SetupRequirement_Transpiler(IEnumerable<CodeInstruction> instructions)
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