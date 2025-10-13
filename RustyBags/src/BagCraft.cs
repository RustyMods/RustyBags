using HarmonyLib;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RustyBags;

public static class BagCraft
{
    [HarmonyPatch(typeof(Player), nameof(Player.HaveRequirementItems))]
    private static class PlayerHaveRequirementItemsPatch
    {
        [UsedImplicitly]
        private static void Postfix(Player __instance, Recipe piece, bool discover, int qualityLevel, int amount, ref bool __result)
        {
            if (__result || discover || __instance.GetBag() is not {} bag) return;
            
            foreach (var resource in piece.m_resources)
            {
                var item = resource.m_resItem;
                if (item)
                {
                    int num1 = resource.GetAmount(qualityLevel) * amount;
                    int num2 = 0;
                    for (int quality = 1; quality < item.m_itemData.m_shared.m_maxQuality + 1; ++quality)
                    {
                        int num3 = bag.inventory.CountItems(item.m_itemData.m_shared.m_name, quality);
                        num3 += __instance.m_inventory.CountItems(item.m_itemData.m_shared.m_name, quality);
                        if (num3 > num2) num2 = num3;
                    }

                    if (piece.m_requireOnlyOneIngredient)
                    {
                        if (num2 >= num1)
                        {
                            __result = true;
                            return;
                        }
                    }

                    if (num2 < num1) return;
                    __result = true;
                    return;
                }
            }
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
                var stationName = piece.m_craftingStation.m_name;
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
                var item = resource.m_resItem;
                var sharedName = item.m_itemData.m_shared.m_name;
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

            foreach (var requirement in requirements)
            {
                var item = requirement.m_resItem;
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
    private static class InventoryGuiSetupRequirementPatch
    {
        [UsedImplicitly]
        private static bool Prefix(Transform elementRoot, Piece.Requirement req, Player player, bool craft, int quality, int craftMultiplier, ref bool __result)
        {
            if (player.GetBag() is not { } bag) return true;
            
            Image icon = elementRoot.transform.Find("res_icon").GetComponent<Image>();
            TMP_Text name = elementRoot.transform.Find("res_name").GetComponent<TMP_Text>();
            TMP_Text amount = elementRoot.transform.Find("res_amount").GetComponent<TMP_Text>();
            UITooltip tooltip = elementRoot.GetComponent<UITooltip>();

            __result = true;
            if (req.m_resItem == null) return false;
            
            icon.gameObject.SetActive(true);
            name.gameObject.SetActive(true);
            amount.gameObject.SetActive(true);
            icon.sprite = req.m_resItem.m_itemData.GetIcon();
            icon.color = Color.white;
            tooltip.m_text = Localization.instance.Localize(req.m_resItem.m_itemData.m_shared.m_name);
            name.text = Localization.instance.Localize(req.m_resItem.m_itemData.m_shared.m_name);
            
            //TODO : learn how to transpile this
            int count = bag.inventory.CountItems(req.m_resItem.m_itemData.m_shared.m_name);
            count += player.GetInventory().CountItems(req.m_resItem.m_itemData.m_shared.m_name);
            // so it adds the bag to the total count
            
            int requiredAmount = req.GetAmount(quality) * craftMultiplier;
            if (requiredAmount <= 0)
            {
                InventoryGui.HideRequirement(elementRoot);
                __result = false;
                return false;
            }
            
            amount.text = requiredAmount.ToString();
            if (count < requiredAmount && (!craft && !ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoBuildCost) || craft && !ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoCraftCost)))
                amount.color = Mathf.Sin(Time.time * 10f) > 0.0 ? Color.red : Color.white;
            else amount.color = Color.white;

            return false;
        }
    }
}