using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace RustyBags;

public static class EpicLoot_Compat
{
    public static void Load()
    {
        if (!RustyBagsPlugin.isEpicLootLoaded) return;

        MethodBase? method = TryGetEquipmentMethod();
        if (method == null) return;
        
        RustyBagsPlugin.instance._harmony.Patch(method, postfix: new HarmonyMethod(typeof(EpicLoot_Compat), nameof(Patch_EpicLoot_Player_GetEquipment)));
    }

    private static MethodBase? TryGetEquipmentMethod()
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
        
        MethodInfo? method = playerExtensionsType.GetMethod("GetEquipment", BindingFlags.Public | BindingFlags.Static);
        
        if (method == null)
        {
            return null;
        }
        
        Debug.Log("Found EpicLoot.PlayerExtensions.GetEquipment");
        return method;
    }
    
    private static void Patch_EpicLoot_Player_GetEquipment(Player player, ref List<ItemDrop.ItemData> __result)
    {
        if (player.GetEquippedBag() is { } bag) __result.Add(bag);
        if (player.GetEquippedQuiver() is {} quiver) __result.Add(quiver);
    }
}