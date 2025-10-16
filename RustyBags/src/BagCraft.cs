using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace RustyBags;

public static class BagCraft
{
    [HarmonyPatch(typeof(Player), nameof(Player.HaveRequirementItems))]
    private static class Player_HaveRequirementItems_Transpiler
    {
        [UsedImplicitly]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo? target = AccessTools.Method(typeof(Inventory), nameof(Inventory.CountItems));
            MethodInfo? method = AccessTools.Method(typeof(Player_HaveRequirementItems_Transpiler), nameof(AddBagCount));
            CodeInstruction[] newInstructions = new[]
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldloc_2),
                new CodeInstruction(OpCodes.Ldloc_S, 4),
                new CodeInstruction(OpCodes.Call, method)
            };

            return new CodeMatcher(instructions)
                .Start()
                .MatchStartForward(new CodeMatch(OpCodes.Callvirt, target))
                .Advance(1)
                .Insert(newInstructions)
                .InstructionEnumeration();
        }

        public static int AddBagCount(int inventoryCount, Player player, Piece.Requirement resource, int quality)
        {
            if (player.GetBag() is not { } bag) return inventoryCount;
            int bagCount = bag.inventory.CountItems(resource.m_resItem.m_itemData.m_shared.m_name, quality);
            return inventoryCount + bagCount;
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
            if (__instance.GetBag() is not { } bag) return true;
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
    private static class PlayerHaveRequirementsPatch
    {
        [UsedImplicitly]
        private static bool Prefix(Player __instance, Piece piece, Player.RequirementMode mode, ref bool __result)
        {
            if (__instance.GetBag() is not { } bag || mode == Player.RequirementMode.IsKnown) return true;
            
            if (piece.m_craftingStation)
            {
                string? stationName = piece.m_craftingStation.m_name;
                if (mode == Player.RequirementMode.CanAlmostBuild)
                {
                    if (!__instance.m_knownStations.ContainsKey(stationName))
                    {
                        __result = false;
                        return false;
                    }
                }
                else if (!CraftingStation.HaveBuildStationInRange(stationName, __instance.transform.position) &&
                         !ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoWorkbench))
                {
                    __result = false;
                    return false;
                }
            }

            if (piece.m_dlc.Length > 0 && !DLCMan.instance.IsDLCInstalled(piece.m_dlc))
            {
                __result = false;
                return false;
            }

            if (ZoneSystem.instance.GetGlobalKey(piece.FreeBuildKey()))
            {
                __result = true;
                return false;
            }

            foreach (Piece.Requirement? resource in piece.m_resources)
            {
                ItemDrop? item = resource.m_resItem;
                string? sharedName = item.m_itemData.m_shared.m_name;
                if (item && resource.m_amount > 0)
                {
                    switch (mode)
                    {
                        case Player.RequirementMode.CanBuild:
                            int count = bag.inventory.CountItems(sharedName) + __instance.m_inventory.CountItems(sharedName);
                            if (count < resource.m_amount)
                            {
                                __result = false;
                                return false;
                            }
                            continue;
                        case Player.RequirementMode.CanAlmostBuild:
                            bool haveItem = bag.inventory.HaveItem(sharedName) || __instance.m_inventory.HaveItem(sharedName);
                            if (!haveItem)
                            {
                                __result = false;
                                return false;
                            }
                            continue;
                        default:
                            continue;
                    }
                }
            }
            __result = true;
            return false;
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.ConsumeResources))]
    private static class PlayerConsumeResourcesPatch
    {
        [UsedImplicitly]
        private static bool Prefix(Player __instance, Piece.Requirement[] requirements, int qualityLevel,
            int itemQuality, int multiplier)
        {
            if (__instance.GetBag() is not { } bag) return true;

            foreach (Piece.Requirement requirement in requirements)
            {
                ItemDrop? item = requirement.m_resItem;
                if (item)
                {
                    int amount = requirement.GetAmount(qualityLevel) * multiplier;
                    if (amount <= 0) continue;
                    
                    int bagCount = bag.inventory.CountItems(item.m_itemData.m_shared.m_name, itemQuality);
                    if (bagCount > 0)
                    {
                        if (amount > bagCount)
                        {
                            bag.inventory.RemoveItem(item.m_itemData.m_shared.m_name, bagCount, itemQuality);
                            amount -= bagCount;
                        }
                        else
                        {
                            bag.inventory.RemoveItem(item.m_itemData.m_shared.m_name, amount, itemQuality);
                            amount = 0;
                        }
                    }

                    if (amount > 0)
                    {
                        __instance.m_inventory.RemoveItem(item.m_itemData.m_shared.m_name, amount, itemQuality);
                    }
                }
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.SetupRequirement))]
    private static class InventoryGuiSetupRequirementTranspiler
    {
        [UsedImplicitly]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo? target = AccessTools.Method(typeof(Inventory), nameof(Inventory.CountItems));
            MethodInfo? call = AccessTools.Method(typeof(InventoryGuiSetupRequirementTranspiler), nameof(AddBagCount));
            return new CodeMatcher(instructions)
                .Start()
                .MatchStartForward(new CodeMatch(OpCodes.Callvirt, target))
                .Insert(new CodeInstruction(OpCodes.Ldarg_2))
                .Advance(1)
                .Set(OpCodes.Call, call)
                .InstructionEnumeration();
        }

        private static int AddBagCount(Inventory inventory, string sharedName, int quality, bool worldLevel, Player player)
        {
            int num1 = inventory.CountItems(sharedName);
            if (player.GetBag() is not { } bag) return num1;
            num1 += bag.inventory.CountItems(sharedName);
            return num1;
        }
    }
}