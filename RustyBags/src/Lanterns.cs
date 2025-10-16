using HarmonyLib;
using ItemManager;
using JetBrains.Annotations;
using UnityEngine;

namespace RustyBags;

public static class Lanterns
{
    [HarmonyPriority(Priority.First)]
    [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.Awake))]
    private static class FejdStartup_Awake_Patch
    {
        [UsedImplicitly]
        private static void Postfix(FejdStartup __instance) => Setup(__instance);
    }

    private static void Setup(FejdStartup __instance)
    {
        AssetBundle bundle = PrefabManager.RegisterAssetBundle("bags_bundle");
        GameObject? source = bundle.LoadAsset<GameObject>("SkullLantern_RS");
        GameObject? prefab = Object.Instantiate(source, RustyBagsPlugin.root.transform);
        prefab.name = source.name;
        GameObject? trophySkeletonHildir = __instance.m_objectDBPrefab.GetComponent<ZNetScene>().m_prefabs.Find(x => x.name == "TrophySkeletonHildir");
        Transform? model = trophySkeletonHildir.transform.Find("attach/model");
        
        Transform? defaultModel = prefab.transform.Find("default/replace_model");
        Transform? attachBack = prefab.transform.Find("attach_back/replace_model");
        Transform? attach = prefab.transform.Find("attach/equiped/replace_model");
        
        Transform[] transforms = new []{defaultModel, attachBack, attach};
        foreach (var transform in transforms)
        {
            var go = Object.Instantiate(model, transform);
            go.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        }
        
        prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_icons = trophySkeletonHildir.GetComponent<ItemDrop>().m_itemData.m_shared.m_icons;

        Item lantern = new Item(prefab);
        lantern.Name.English("Brenna Lantern");
        lantern.Description.English("Crafted from the remains of Brenna");
        lantern.Crafting.Add(CraftingTable.Forge, 1);
        lantern.RequiredItems.Add("TrophySkeletonHildir", 1);
        lantern.RequiredItems.Add("Bronze", 3);
        lantern.RequiredItems.Add("Resin", 10);
        lantern.Configurable = Configurability.Disabled;
    }
}