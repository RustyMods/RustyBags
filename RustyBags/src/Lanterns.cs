using HarmonyLib;
using ItemManager;
using JetBrains.Annotations;
using UnityEngine;

namespace RustyBags;

public static class Lanterns
{
    private static bool loaded;
    [HarmonyPriority(Priority.First)]
    [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.Awake))]
    private static class FejdStartup_Awake_Patch
    {
        [UsedImplicitly]
        private static void Postfix(FejdStartup __instance) => Setup(__instance);
    }

    private static void Setup(FejdStartup __instance)
    {
        if (loaded) return;
        AssetBundle bundle = PrefabManager.RegisterAssetBundle("bags_bundle");
        GameObject? source = bundle.LoadAsset<GameObject>("SkullLantern_RS");
        ZNetScene? scene = __instance.m_objectDBPrefab.GetComponent<ZNetScene>();
        
        Item BrennaLantern = CreateCharm(scene, source, "TrophySkeletonHildir", "model", "SkullLantern_RS");
        BrennaLantern.Name.English("Brenna Charm");
        BrennaLantern.Description.English("Crafted from the remains of Brenna");
        BrennaLantern.Crafting.Add(CraftingTable.Forge, 1);
        BrennaLantern.RequiredItems.Add("TrophySkeletonHildir", 1);
        BrennaLantern.RequiredItems.Add("Bronze", 3);
        BrennaLantern.RequiredItems.Add("Resin", 10);
        BrennaLantern.Configurable = Configurability.Disabled;
        Bag.RegisterLantern($"${BrennaLantern.Name.Key}");
        
        Item PoisonLantern = CreateCharm(scene, source, "TrophySkeletonPoison", "model", "PoisonSkullLantern_RS");
        PoisonLantern.Name.English("Poison Skelett Charm");
        PoisonLantern.Description.English("Crafted from the remains of a poisoned skeleton");
        PoisonLantern.Crafting.Add(CraftingTable.Forge, 1);
        PoisonLantern.RequiredItems.Add("TrophySkeletonPoison", 1);
        PoisonLantern.RequiredItems.Add("Bronze", 3);
        PoisonLantern.RequiredItems.Add("Resin", 10);
        PoisonLantern.Configurable = Configurability.Disabled;
        Bag.RegisterLantern($"${PoisonLantern.Name.Key}");
        
        Item SkullLantern = CreateCharm(scene, source, "TrophySkeleton", "model", "SkeletonLantern_RS");
        SkullLantern.Name.English("Skelett Charm");
        SkullLantern.Description.English("Crafted from the remains of a skeleton");
        SkullLantern.Crafting.Add(CraftingTable.Forge, 1);
        SkullLantern.RequiredItems.Add("TrophySkeleton", 1);
        SkullLantern.RequiredItems.Add("Bronze", 3);
        SkullLantern.RequiredItems.Add("Resin", 10);
        SkullLantern.Configurable = Configurability.Disabled;
        Bag.RegisterLantern($"${SkullLantern.Name.Key}");
        
        Item CharredLantern = CreateCharm(scene, source, "TrophyCharredMelee", "model", "CharredLantern_RS");
        CharredLantern.Name.English("Charred Charm");
        CharredLantern.Description.English("Crafted from the remains of a charred warrior");
        CharredLantern.Crafting.Add(CraftingTable.Forge, 1);
        CharredLantern.RequiredItems.Add("TrophyCharredMelee", 1);
        CharredLantern.RequiredItems.Add("Bronze", 3);
        CharredLantern.RequiredItems.Add("Resin", 10);
        CharredLantern.Configurable = Configurability.Disabled;
        Bag.RegisterLantern($"${CharredLantern.Name.Key}");
        
        Item GhostLantern = CreateCharm(scene, source, "TrophyGhost", "default", "GhostLantern_RS", new Vector3(0f, -0.083f, 0.023f));
        GhostLantern.Name.English("Ghastly Charm");
        GhostLantern.Description.English("Crafted from the remains of a ghost");
        GhostLantern.Crafting.Add(CraftingTable.Forge, 1);
        GhostLantern.RequiredItems.Add("TrophyGhost", 1);
        GhostLantern.RequiredItems.Add("Bronze", 3);
        GhostLantern.RequiredItems.Add("Resin", 10);
        GhostLantern.Configurable = Configurability.Disabled;
        Bag.RegisterLantern($"${GhostLantern.Name.Key}");
        loaded = true;
    }

    private static Item CreateCharm(ZNetScene scene, GameObject source, string trophy, string childName, string newName, Vector3? offset = null)
    {
        GameObject prefab = Object.Instantiate(source, RustyBagsPlugin.root.transform);
        prefab.name = newName;
        
        GameObject? trophyPrefab = scene.m_prefabs.Find(x => x.name == trophy);
        Transform? model = trophyPrefab.transform.Find($"attach/{childName}");
        
        Transform? defaultModel = prefab.transform.Find("default/replace_model");
        Transform? attachBack = prefab.transform.Find("attach_back/replace_model");
        Transform? attach = prefab.transform.Find("attach/equiped/replace_model");
        
        Transform[] transforms = new []{defaultModel, attachBack, attach};
        foreach (var transform in transforms)
        {
            var go = Object.Instantiate(model, transform);
            go.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            if (offset.HasValue) go.transform.localPosition += offset.Value;
        }
        prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_icons = trophyPrefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_icons;
        return new Item(prefab);;
    }
}