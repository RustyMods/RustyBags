using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using BepInEx.Configuration;
using HarmonyLib;
using ItemManager;
using JetBrains.Annotations;
using Managers;
using RustyBags.Managers;
using RustyBags.Utilities;
using UnityEngine;

namespace RustyBags;

public static class BagExtensions
{
    public static bool IsBag(this ItemDrop.ItemData item, out BagSetup setup) => BagSetup.bags.TryGetValue(item.m_shared.m_name, out setup);
    public static bool IsQuiver(this ItemDrop.ItemData item) => BagSetup.bags.TryGetValue(item.m_shared.m_name, out var setup) && setup.isQuiver;
    public static bool IsBound(this ItemDrop.ItemData item) => item.m_gridPos.y == 0;
    public static Bag? GetEquippedBag(this Humanoid humanoid)
    {
        foreach (ItemDrop.ItemData? item in humanoid.GetInventory().GetAllItems())
        {
            if (item is Bag { m_equipped: true } bag) return bag;
        }
        return null;
    }

    public static Bag? GetAnyBag(this Humanoid humanoid)
    {
        foreach (ItemDrop.ItemData? item in humanoid.GetInventory().GetAllItems())
        {
            if (item is Bag bag) return bag;
        }
        return null;
    }

    public static bool HasBag(this Humanoid humanoid)
    {
        return humanoid.GetAnyBag() != null;
    }
}

public class BagSetup
{
    [Flags]
    public enum Restriction
    {
        None = 0, 
        NoMaterials = 1, 
        NoConsumables = 2, 
        NoTrophies = 4, 
        NoFishes = 8,
    }
    
    public static readonly Dictionary<string, BagSetup> bags = new();

    public readonly bool isQuiver;
    public readonly bool isOreBag;
    public readonly Dictionary<int, Size> sizes = new();
    public readonly SE_Bag statusEffect;
    public readonly string englishName;
    public readonly ConfigEntry<Restriction>? restrictConfig;

    public BagSetup(Item item, int width, int height, bool isQuiver = false, bool isOreBag = false, bool replaceShader = true)
    {
        string sharedName = $"${item.Name.Key}";
        englishName = new Regex(@"[=\n\t\\""\'\[\]]*").Replace(Item.english.Localize(sharedName), "").Trim();
        sizes[1] = new Size(width, height);
        this.isQuiver = isQuiver;
        this.isOreBag = isOreBag;
        statusEffect = isOreBag ? ScriptableObject.CreateInstance<SE_OreBag>() : ScriptableObject.CreateInstance<SE_Bag>();
        statusEffect.name = $"SE_Bag_{item.Prefab.name}";
        statusEffect.m_name = sharedName;
        ItemDrop? drop = item.Prefab.GetComponent<ItemDrop>();
        ItemDrop.ItemData? data = drop.m_itemData;
        statusEffect.m_icon = data.GetIcon();
        statusEffect.data = this;
        statusEffect.m_speedModifier = -0.05f;
        drop.m_itemData = new Bag(data);
        drop.m_itemData.m_dropPrefab = item.Prefab;
        drop.m_itemData.m_shared.m_equipStatusEffect = statusEffect;
        if (!isQuiver) restrictConfig = Configs.config(englishName, "Restrictions", Restriction.None, "Set restrictions");
        if(replaceShader) MaterialReplacer.RegisterGameObjectForShaderSwap(item.Prefab, MaterialReplacer.ShaderType.CustomCreature);
        bags[sharedName] = this;
    }

    public void AddSizePerQuality(int width, int height, int quality)
    {
        sizes[quality] = new Size(width, height);
    }

    public void SetupConfigs()
    {
        foreach (KeyValuePair<int, Size> size in sizes)
        {
            ConfigEntry<string> config = Configs.config(englishName, $"Inventory Size Qlty.{size.Key}",
                string.Join("x", size.Value.width, size.Value.height), new ConfigDescription(
                    $"Setup inventory size for quality {size.Key}, width x height", null, new Configs.ConfigurationManagerAttributes()
                    {
                        CustomDrawer = Size.Draw
                    }));
            config.SettingChanged += (_, _) => OnConfigChange();
            OnConfigChange();
            
            void OnConfigChange()
            {
                string[] values = config.Value.Split('x');
                if (values.Length != 2) return;
                if (!int.TryParse(values[0], out int width) || !int.TryParse(values[1], out int height)) return;
                sizes[size.Key].width = width;
                sizes[size.Key].height = height;
            }
        }
        statusEffect.SetupConfigs();
    }

    public class Size
    {
        public int width;
        public int height;

        public Size(int width, int height)
        {
            this.width = width;
            this.height = height;
        }

        public static void Draw(ConfigEntryBase cfg)
        {
            bool locked = cfg.Description.Tags
                .Select(a =>
                    a.GetType().Name == "ConfigurationManagerAttributes"
                        ? (bool?)a.GetType().GetField("ReadOnly")?.GetValue(a)
                        : null).FirstOrDefault(v => v != null) ?? false;
            
            var values = ((string)cfg.BoxedValue).Split('x');
            if (values.Length != 2) return;
            
            string widthCfg = values[0].Trim();
            string heightCfg = values[1].Trim();
            
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            string width = GUILayout.TextField(widthCfg, new GUIStyle(GUI.skin.textField));
            string height = GUILayout.TextField(heightCfg, new GUIStyle(GUI.skin.textField));
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            
            if (!locked && (width != widthCfg || height != heightCfg))
            {
                cfg.BoxedValue = string.Join("x", width, height);
            }
        }
    }
    
    [HarmonyPatch(typeof(ItemDrop), nameof(ItemDrop.Awake))]
    private static class ItemDrop_Awake_Patch 
    {
        [UsedImplicitly]
        private static void Prefix(ItemDrop __instance)
        {
            if (!__instance.m_itemData.IsBag(out BagSetup setup)) return;
            __instance.m_itemData = setup.isQuiver ? new Quiver(__instance.m_itemData) : new Bag(__instance.m_itemData);
        }
    }
}

public class Bag : ItemDrop.ItemData
{
    private const string BAG_DATA_KEY = "RustyBag.CustomData.Inventory.Data";
    private static readonly HashSet<string> lanternNames = new() { "$item_lantern" };
    public static void RegisterLantern(string name) => lanternNames.Add(name);
    
    public BagInventory inventory = new("Bag", null, null, 8, 4);
    public bool isLoaded;
    private readonly float baseWeight;
    public bool isOpen;

    protected BagEquipment? m_bagEquipment;
    
    public ItemDrop.ItemData? lantern;
    public ItemDrop.ItemData? pickaxe;
    public ItemDrop.ItemData? fishingRod;
    public ItemDrop.ItemData? cultivator;
    public ItemDrop.ItemData? melee;
    public ItemDrop.ItemData? hammer;
    public ItemDrop.ItemData? hoe;
    public ItemDrop.ItemData? atgeir;
    public ItemDrop.ItemData? ore;
    public ItemDrop.ItemData? scythe;
    public ItemDrop.ItemData? harpoon;
    public Bag(ItemDrop.ItemData item)
    {
        m_shared = item.m_shared;
        m_shared.m_itemType = ItemType.Misc;
        m_customData = item.m_customData;
        baseWeight = m_shared.m_weight;

        inventory.m_onChanged = OnChanged;
    }

    public void Open()
    {
        if (isOpen) return;
        isOpen = true;
    }
    public void Close()
    {
        if (!isOpen) return;
        isOpen = false;
    }
    
    public void OnEquip(BagEquipment equipment)
    {
        BagGui.m_currentBag = this;
        m_bagEquipment = equipment;
        m_equipped = true;
        Load();
    }

    public void OnUnequip()
    {
        m_equipped = false;
        UpdateWeight();
        SaveInventory();
        m_bagEquipment = null;
        if (BagGui.m_currentBag != this) return;
        BagGui.m_currentBag = null;
    }

    private void OnChanged()
    {
        SaveInventory();
        UpdateTeleportable();
        UpdateWeight();
        UpdateAttachments();
    }

    private void SaveInventory()
    {
        ZPackage pkg = new ZPackage();
        inventory.Save(pkg);
        m_customData[BAG_DATA_KEY] = pkg.GetBase64();
    }

    private void UpdateTeleportable() => m_shared.m_teleportable = ZoneSystem.instance && inventory.IsTeleportable();

    protected virtual void UpdateAttachments()
    {
        lantern = null;
        pickaxe = null;
        fishingRod = null;
        cultivator = null;
        melee = null;
        hammer = null;
        hoe = null;
        atgeir = null;
        ore = null;
        scythe = null;
        harpoon = null;

        List<ItemDrop.ItemData>? list = inventory.GetAllItemsInGridOrder();
        for (int index = 0; index < list.Count; ++index)
        {
            if (lantern != null && fishingRod != null && cultivator != null && hoe != null && hammer != null &&
                pickaxe != null && melee != null && atgeir != null && ore != null && scythe != null && harpoon != null) break;
            
            ItemDrop.ItemData? item = list[index];
            if (SE_OreBag.ores.Contains(item.m_shared.m_name))
            {
                ore ??= item;
                continue;
            }

            if (!item.IsEquipable()) continue;
            
            if (lanternNames.Contains(item.m_shared.m_name))
            {
                lantern ??= item;
            }
            else if (item.m_shared.m_name == "$item_fishingrod")
            {
                fishingRod ??= item;
            }
            else if (item.m_shared.m_name == "$item_cultivator")
            {
                cultivator ??= item;
            }
            else if (item.m_shared.m_name == "$item_spear_chitin")
            {
                harpoon ??= item;
            }
            else if (item.m_shared.m_name == "$item_hoe")
            {
                hoe ??= item;
            }
            else if (item.m_shared.m_name == "$item_hammer")
            {
                hammer ??= item;
            }
            else if (item.m_shared.m_name == "$item_scythe")
            {
                scythe ??= item;
            }
            else if (item.m_shared.m_skillType is Skills.SkillType.Pickaxes)
            {
                pickaxe ??= item;
            }
            else if (item.m_shared.m_itemType is ItemType.OneHandedWeapon)
            {
                melee ??= item;
            }
            else if (item.m_shared.m_skillType is Skills.SkillType.Polearms)
            {
                atgeir ??= item;
            }
        }

        if (m_bagEquipment == null) return;
        m_bagEquipment.SetLanternItem(lantern?.m_dropPrefab.name ?? "");
        m_bagEquipment.SetPickaxeItem(pickaxe?.m_dropPrefab.name ?? "");
        m_bagEquipment.SetFishingRodItem(fishingRod?.m_dropPrefab.name ?? "");
        m_bagEquipment.SetCultivatorItem(cultivator?.m_dropPrefab.name ?? "");
        m_bagEquipment.SetHammerItem(hammer?.m_dropPrefab.name ?? "");
        m_bagEquipment.SetHoeItem(hoe?.m_dropPrefab.name ?? "");
        m_bagEquipment.SetMeleeItem(melee?.m_dropPrefab.name ?? "");
        m_bagEquipment.SetAtgeirItem(atgeir?.m_dropPrefab.name ?? "");
        m_bagEquipment.SetOreItem(ore?.m_dropPrefab.name ?? "", ore?.m_stack ?? 0);
        m_bagEquipment.SetScytheItem(scythe?.m_dropPrefab.name ?? "");
        m_bagEquipment.SetHarpoonItem(harpoon?.m_dropPrefab.name ?? "");
        m_bagEquipment.UpdateEquipStatusEffect();
    }

    public void Load()
    {
        if (isLoaded) return;
        SetupInventory();
        if (m_customData.TryGetValue(BAG_DATA_KEY, out string data))
        {
            ZPackage pkg = new ZPackage(data);
            inventory.Load(pkg);
            UpdateTeleportable();
            UpdateWeight();
            UpdateAttachments();
        }
        inventory.m_onChanged = OnChanged;
        isLoaded = true;
    }

    protected virtual void SetupInventory()
    {
        BagSetup setup = GetSetup();
        BagSetup.Size size = setup.sizes.TryGetValue(m_quality, out var s) ? s : setup.sizes[4];
        inventory = new BagInventory("Bag", this, Player.m_localPlayer?.GetInventory().m_bkg, size.width, size.height);
    }
    
    public BagSetup GetSetup() => BagSetup.bags[m_shared.m_name];

    private void UpdateWeight()
    {
        m_shared.m_weight = baseWeight + GetInventoryWeight();
        m_bagEquipment?.m_player.GetInventory().UpdateTotalWeight();
    }

    public float GetInventoryWeight()
    {
        float total = inventory.GetTotalWeight();
        if (m_shared.m_equipStatusEffect is SE_Bag se && m_equipped)
        {
            SE_Bag effect = (se.Clone() as SE_Bag)!;
            effect.SetLevel(m_quality, 0f);
            effect.ModifyInventoryWeight(inventory, ref total);
        }
        return total;
    }

    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(GetWeight))]
    private static class ItemDrop_ItemData_GetWeight_Patch
    {
        [UsedImplicitly]
        private static void Prefix(ItemDrop.ItemData __instance) => (__instance as Bag)?.Load();
    }
    
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.Pickup))]
    private static class Humanoid_Pickup_Transpiler
    {
        [UsedImplicitly]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo? targetMethod = AccessTools.Method(typeof(Inventory), nameof(Inventory.AddItem), new[]{typeof(ItemDrop.ItemData)});
            MethodInfo? newMethod = AccessTools.Method(typeof(Humanoid_Pickup_Transpiler), nameof(AddIntoBag));

            return new CodeMatcher(instructions)
                .Start()
                .MatchStartForward(new CodeMatch(OpCodes.Callvirt, targetMethod))
                .Insert(new CodeInstruction(OpCodes.Ldarg_0))
                .Advance(1)
                .Set(OpCodes.Call, newMethod)
                .InstructionEnumeration();
        }

        public static bool AddIntoBag(Inventory inventory, ItemDrop.ItemData item, Humanoid instance)
        {
            if (Configs.AutoStack && instance.GetEquippedBag() is { } bag && bag.inventory.ContainsItemByName(item.m_shared.m_name))
            {
                return bag.inventory.AddItem(item) || inventory.AddItem(item);
            }
            return inventory.AddItem(item);
        }
    }

    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(IsEquipable))]
    private static class ItemDrop_IsEquipable_Patch
    {
        [UsedImplicitly]
        private static void Postfix(ItemDrop.ItemData __instance, ref bool __result) => __result |= __instance is Bag;
    }

    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(GetStatusEffectTooltip))]
    private static class ItemDrop_GetStatusEffectTooltip_Patch
    {
        [UsedImplicitly]
        private static void Prefix(ItemDrop.ItemData __instance, ref float skillLevel)
        {
            if (__instance is not Bag bag) return;
            skillLevel = bag.lantern != null ? 1f : 0f;
        }

        [UsedImplicitly]
        private static void Postfix(ItemDrop.ItemData __instance, ref string __result)
        {
            if (!lanternNames.Contains(__instance.m_shared.m_name) || !Configs.CharmsAffectBag) return;
            __result += $"\n{Keys.SECharm}";
        }
    }
}
