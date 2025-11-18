using System.Collections.Generic;
using HarmonyLib;

namespace RustyBags;

public static class BoltExclusion
{
    private static readonly List<int> validBolts = new();
    private static readonly List<string> validNames = new();
    
    [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.Awake))]
    private static class FejdStartup_Awake_Patch
    {
        [HarmonyPriority(Priority.First)]
        private static void Postfix(FejdStartup __instance)
        {
            var db = __instance.m_objectDBPrefab.GetComponent<ObjectDB>();
            foreach (var prefab in db.m_items)
            {
                if (prefab == null || !prefab.TryGetComponent(out ItemDrop component) ||
                    component.m_itemData.m_shared.m_ammoType != "$ammo_bolts") continue;
                validBolts.Add(prefab.name.GetStableHashCode());
                validBolts.Add(component.m_itemData.m_shared.m_name.GetStableHashCode());
                validNames.Add(prefab.name);
            }
        }
    }

    public static bool IsValidBolt(this ItemDrop item) => validBolts.Contains(item.name.GetStableHashCode());
    public static bool IsValidBolt(this ItemDrop.ItemData item) => validBolts.Contains(item.m_shared.m_name.GetStableHashCode());
    public static string GetRandomBoltName() => validNames[UnityEngine.Random.Range(0, validNames.Count)];
}