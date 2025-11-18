using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace RustyBags;

public static class EpicLoot_Compat
{
    [HarmonyPatch]
    private static class Player_GetEquipment_Patch
    {
        [UsedImplicitly]
        private static MethodBase TargetMethod()
        {
            Assembly? epicLootAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "EpicLoot");
        
            if (epicLootAssembly == null)
            {
                return null;
            }
        
            Type? playerExtensionsType = epicLootAssembly.GetType("EpicLoot.PlayerExtensions");
            if (playerExtensionsType == null)
            {
                return null;
            }
        
            var method = playerExtensionsType.GetMethod("GetEquipment", BindingFlags.Public | BindingFlags.Static);
        
            if (method == null)
            {
                return null;
            }
        
            Debug.Log("Successfully found EpicLoot.PlayerExtensions.GetEquipment");
            return method;
        }

        [UsedImplicitly]
        private static void Postfix(Player player, ref List<ItemDrop.ItemData> __result)
        {
            if (player.GetEquippedBag() is not { } bag) return;
            __result.Add(bag);
        }
    }
}