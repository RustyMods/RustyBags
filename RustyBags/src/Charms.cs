using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using ItemManager;
using JetBrains.Annotations;
using RustyBags.Utilities;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RustyBags;

public static class Charms
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
        
        Item? BrennaLantern = CreateCharm(scene, source, "TrophySkeletonHildir", "model", "SkullLantern_RS");
        if (BrennaLantern != null)
        {
            BrennaLantern.Name.English("Brenna Charm");
            BrennaLantern.Description.English("Crafted from the remains of Brenna");
            BrennaLantern.Crafting.Add(CraftingTable.Forge, 1);
            BrennaLantern.RequiredItems.Add("TrophySkeletonHildir", 1);
            BrennaLantern.RequiredItems.Add("Bronze", 3);
            BrennaLantern.RequiredItems.Add("Resin", 10);
            BrennaLantern.Configurable = Configurability.Disabled;
            Charm brennaCharm = new Charm($"${BrennaLantern.Name.Key}");
            brennaCharm.damageModifiers.m_fire = 0.1f;
        }
        
        Item? PoisonLantern = CreateCharm(scene, source, "TrophySkeletonPoison", "model", "PoisonSkullLantern_RS");
        if (PoisonLantern != null)
        {
            PoisonLantern.Name.English("Poison Skelett Charm");
            PoisonLantern.Description.English("Crafted from the remains of a poisoned skeleton");
            PoisonLantern.Crafting.Add(CraftingTable.Forge, 1);
            PoisonLantern.RequiredItems.Add("TrophySkeletonPoison", 1);
            PoisonLantern.RequiredItems.Add("Bronze", 3);
            PoisonLantern.RequiredItems.Add("Resin", 10);
            PoisonLantern.Configurable = Configurability.Disabled;
            Charm poisonCharm = new Charm($"${PoisonLantern.Name.Key}");
            poisonCharm.damageModifiers.m_poison = 0.1f;
        }
        
        Item? SkullLantern = CreateCharm(scene, source, "TrophySkeleton", "model", "SkeletonLantern_RS");
        if (SkullLantern != null)
        {
            SkullLantern.Name.English("Skelett Charm");
            SkullLantern.Description.English("Crafted from the remains of a skeleton");
            SkullLantern.Crafting.Add(CraftingTable.Forge, 1);
            SkullLantern.RequiredItems.Add("TrophySkeleton", 1);
            SkullLantern.RequiredItems.Add("Bronze", 3);
            SkullLantern.RequiredItems.Add("Resin", 10);
            SkullLantern.Configurable = Configurability.Disabled;
            var skullCharm = new Charm($"${SkullLantern.Name.Key}");
            skullCharm.speed = 0.05f;
        }
        
        Item? CharredLantern = CreateCharm(scene, source, "TrophyCharredMelee", "model", "CharredLantern_RS");
        if (CharredLantern != null)
        {
            CharredLantern.Name.English("Charred Charm");
            CharredLantern.Description.English("Crafted from the remains of a charred warrior");
            CharredLantern.Crafting.Add(CraftingTable.Forge, 1);
            CharredLantern.RequiredItems.Add("TrophyCharredMelee", 1);
            CharredLantern.RequiredItems.Add("Bronze", 3);
            CharredLantern.RequiredItems.Add("Resin", 10);
            CharredLantern.Configurable = Configurability.Disabled;
            var charredCharm = new Charm($"${CharredLantern.Name.Key}");
            charredCharm.damageModifiers.m_damage = 0.1f;
        }
        
        Item? GhostLantern = CreateCharm(scene, source, "TrophyGhost", "default", "GhostLantern_RS", new Vector3(0f, -0.083f, 0.023f));
        if (GhostLantern != null)
        {
            GhostLantern.Name.English("Ghastly Charm");
            GhostLantern.Description.English("Crafted from the remains of a ghost");
            GhostLantern.Crafting.Add(CraftingTable.Forge, 1);
            GhostLantern.RequiredItems.Add("TrophyGhost", 1);
            GhostLantern.RequiredItems.Add("Bronze", 3);
            GhostLantern.RequiredItems.Add("Resin", 10);
            GhostLantern.Configurable = Configurability.Disabled;
            var ghostCharm = new Charm($"${GhostLantern.Name.Key}");
            ghostCharm.damageModifiers.m_spirit = 0.1f;
        }

        Item? DragonLantern = CreateCharm(scene, source, "DragonTear", "", "DragonCharm_RS", new Vector3(0f, -0.15f, 0f), scale: 0.5f);
        if (DragonLantern != null)
        {
            DragonLantern.Name.English("Moder Charm");
            DragonLantern.Description.English("Crafted from the tear of a dragon");
            DragonLantern.Crafting.Add(CraftingTable.Forge, 1);
            DragonLantern.RequiredItems.Add("DragonTear", 1);
            DragonLantern.RequiredItems.Add("Bronze", 3);
            DragonLantern.RequiredItems.Add("Resin", 10);
            DragonLantern.Configurable = Configurability.Disabled;
            var dragonCharm = new Charm($"${DragonLantern.Name.Key}");
            dragonCharm.carryWeight = 50f;
        }
        
        Item? LuckyFoot = CreateCharm(scene, source, "TrophyHare", "default", "LuckyCharm_RS", rotation: Quaternion.Euler(-37.7f, 0.38f, -4f));
        if (LuckyFoot != null)
        {
            LuckyFoot.Name.English("Lucky Charm");
            LuckyFoot.Description.English("Crafted from the remains of a hare");
            LuckyFoot.Crafting.Add(CraftingTable.Forge, 1);
            LuckyFoot.RequiredItems.Add("TrophyHare", 1);
            LuckyFoot.RequiredItems.Add("Bronze", 3);
            LuckyFoot.RequiredItems.Add("Resin", 10);
            LuckyFoot.Configurable = Configurability.Disabled;
            var luckyCharm = new Charm($"${LuckyFoot.Name.Key}");
            luckyCharm.speed = 0.08f;
        }
        
        Item? valkyrieLantern = CreateCharm(scene, source, "TrophyFallenValkyrie", "model", "ValkyrieCharm_RS", new Vector3(-0.025f, -0.075f, 0.15f), Quaternion.Euler(5.035f, -188.424f, -4.692f), 0.5f);
        if (valkyrieLantern != null)
        {
            valkyrieLantern.Name.English("Fallen Charm");
            valkyrieLantern.Description.English("Crafted from the remains of a fallen valkyrie");
            valkyrieLantern.Crafting.Add(CraftingTable.Forge, 1);
            valkyrieLantern.RequiredItems.Add("TrophyFallenValkyrie", 1);
            valkyrieLantern.RequiredItems.Add("Bronze", 3);
            valkyrieLantern.RequiredItems.Add("Resin", 10);
            valkyrieLantern.Configurable = Configurability.Disabled;
            var valkyrieCharm = new Charm($"${valkyrieLantern.Name.Key}");
            valkyrieCharm.eitrRegen = 0.1f;
        }
        loaded = true;
    }

    private static Item? CreateCharm(ZNetScene scene, GameObject source, string trophy, string childName, string newName, Vector3? offset = null, Quaternion? rotation = null, float scale = 1f)
    {
        GameObject? trophyPrefab = scene.m_prefabs.Find(x => x.name == trophy);
        var search = "attach";
        if (!string.IsNullOrEmpty(childName)) search += $"/{childName}";
        Transform? model = trophyPrefab.transform.Find(search);
        if (model == null)
        {
            RustyBagsPlugin.RustyBagsLogger.LogError($"Failed to create charm: {newName}");
            return null;
        }
        
        GameObject prefab = Object.Instantiate(source, RustyBagsPlugin.root.transform);
        prefab.name = newName;

        Transform? defaultModel = prefab.transform.Find("default/replace_model");
        Transform? attachBack = prefab.transform.Find("attach_back/replace_model");
        Transform? attach = prefab.transform.Find("attach/equiped/replace_model");

        Transform[] transforms = new[] { defaultModel, attachBack, attach };
        foreach (var transform in transforms)
        {
            var go = Object.Instantiate(model, transform);
            go.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            if (offset.HasValue) go.transform.localPosition += offset.Value;
            if (rotation.HasValue) go.transform.localRotation = rotation.Value;
            go.transform.localScale *= scale;
        }

        prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_icons = trophyPrefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_icons;
        return new Item(prefab);
    }
}

public static class CharmExtensions
{
    public static Charm? GetCharmData(this ItemDrop.ItemData item) =>
        Charm.charms.TryGetValue(item.m_shared.m_name, out var data) ? data : null;
}

public class Charm
{
    public static readonly Dictionary<string, Charm> charms = new();
    private static readonly StringBuilder sb = new();
    public string tooltip = $"{Keys.IfEquippedToBag}:";
    public float speed;
    public float carryWeight;
    public float eitrRegen;
    public HitData.DamageTypes damageModifiers;
    public Charm(string sharedName)
    {
        Bag.RegisterLantern(sharedName);
        charms[sharedName] = this;
    }

    public string GetTooltip()
    {
        sb.Clear();
        if (!string.IsNullOrEmpty(tooltip)) sb.Append(tooltip + "\n");
        if (speed != 0.0)
        {
            sb.AppendFormat("{0}: <color=orange>{1:+0;-0}%</color>\n", "$item_movement_modifier", speed * 100f);
        }

        if (carryWeight != 0.0)
        {
            sb.AppendFormat("{0}: <color=orange>{1:+0;-0}</color>\n", "$se_max_carryweight", carryWeight);
        }

        if (eitrRegen != 0.0)
        {
            sb.AppendFormat("{0}: <color=orange>{1:+0;-0}%</color>\n", "$se_eitrregen", eitrRegen * 100f);
        }

        if (damageModifiers.m_blunt != 0.0)
        {
            sb.AppendFormat("{0}: <color=orange>{1:+0;-0}%</color>\n", "$inventory_blunt", damageModifiers.m_blunt * 100f);
        }

        if (damageModifiers.m_slash != 0.0)
        {
            sb.AppendFormat("{0}: <color=orange>{1:+0;-0}%</color>\n", "$inventory_slash", damageModifiers.m_slash * 100f);
        }

        if (damageModifiers.m_pierce != 0.0)
        {
            sb.AppendFormat("{0}: <color=orange>{1:+0;-0}%</color>\n", "$inventory_pierce", damageModifiers.m_pierce * 100f);
        }

        if (damageModifiers.m_fire != 0.0)
        {
            sb.AppendFormat("{0}: <color=orange>{1:+0;-0}%</color>\n", "$inventory_fire", damageModifiers.m_fire * 100f);
        }

        if (damageModifiers.m_frost != 0.0)
        {
            sb.AppendFormat("{0}: <color=orange>{1:+0;-0}%</color>\n", "$inventory_frost", damageModifiers.m_frost * 100f);
        }

        if (damageModifiers.m_lightning != 0.0)
        {
            sb.AppendFormat("{0}: <color=orange>{1:+0;-0}%</color>\n", "$inventory_lightning", damageModifiers.m_lightning * 100f);
        }

        if (damageModifiers.m_poison != 0.0)
        {
            sb.AppendFormat("{0}: <color=orange>{1:+0;-0}%</color>\n", "$inventory_poison", damageModifiers.m_poison * 100f);
        }

        if (damageModifiers.m_spirit != 0.0)
        {
            sb.AppendFormat("{0}: <color=orange>{1:+0;-0}%</color>\n", "$inventory_spirit", damageModifiers.m_spirit * 100f);
        }
        return sb.ToString();
    }
    
}