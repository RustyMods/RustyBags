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
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace RustyBags;

public static class BagExtensions
{
    private static Bag? currentBag;
    private static readonly int visible = Animator.StringToHash("visible");
    
    public static bool IsBag(this ItemDrop.ItemData item) => BagSetup.bags.ContainsKey(item.m_shared.m_name);
    public static bool IsQuiver(this ItemDrop.ItemData item) => BagSetup.bags.TryGetValue(item.m_shared.m_name, out var setup) && setup.isQuiver;

    public static bool IsBound(this ItemDrop.ItemData item) => item.m_gridPos.y == 0;

    public static Bag? GetBag(this Humanoid humanoid)
    {
        foreach (ItemDrop.ItemData? item in humanoid.GetInventory().GetAllItems())
        {
            if (item is Bag { m_equipped: true } bag) return bag;
        }
        return null;
    }

    public static void OnUnequip(this Bag bag)
    {
        if (currentBag != bag) return;
        currentBag = null;
    }

    public static void OnEquip(this Bag bag)
    {
        currentBag = bag;
        currentBag.Load();
    }

    private static bool UpdateBag(this InventoryGui instance, Player player)
    {
        if (!instance.m_animator.GetBool(visible)) return true;
        if (instance.m_currentContainer != null || currentBag == null) return true;
        instance.m_container.gameObject.SetActive(true);
        instance.m_containerGrid.UpdateInventory(currentBag.inventory, player, instance.m_dragItem);
        instance.m_containerName.text = Localization.instance.Localize(currentBag.m_shared.m_name);
        if (instance.m_firstContainerUpdate)
        {
            instance.m_containerGrid.ResetView();
            instance.m_firstContainerUpdate = false;
            instance.m_containerHoldTime = 0.0f;
            instance.m_containerHoldState = 0;
        }
        return false;
    }

    // TODO: learn how to transpile this
    // Left.Ctrl click to move items directly into bag
    // Vanilla implementation only accounts for containers
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.OnSelectedItem))]
    private static class InventoryGui_OnSelectedItem_Patch
    {
        [UsedImplicitly]
        private static bool Prefix(InventoryGui __instance, InventoryGrid grid, ItemDrop.ItemData? item, InventoryGrid.Modifier mod)
        {
            if (item == null || item.m_shared.m_questItem || mod is not InventoryGrid.Modifier.Move || __instance.m_dragGo || currentBag == null || __instance.m_currentContainer != null) return true;
            
            var localPlayer = Player.m_localPlayer;
            if (localPlayer.IsTeleporting()) return false;
            
            localPlayer.RemoveEquipAction(item);
            localPlayer.UnequipItem(item);
            if (grid.GetInventory() == currentBag.inventory) localPlayer.GetInventory().MoveItemToThis(grid.GetInventory(), item);
            else currentBag.inventory.MoveItemToThis(localPlayer.GetInventory(), item);
            __instance.m_moveItemEffects.Create(__instance.transform.position, Quaternion.identity); 

            return false;
        }
    }

    [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.UpdateGui))]
    private static class InventoryGrid_UpdateGui_Patch
    {
        // Remove numbers in the grid gui
        [UsedImplicitly]
        private static void Postfix(InventoryGrid __instance)
        {
            if (__instance.m_inventory is not BagInventory) return;
            foreach (var element in __instance.m_elements)
            {
                element.m_go.transform.Find("binding").GetComponent<TMP_Text>().enabled = false;
            }
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.DoCrafting))]
    private static class InventoryGui_DoCraft_Transpile
    {
        [UsedImplicitly]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo? targetMethod = AccessTools.Method(typeof(Inventory), nameof(Inventory.AddItem),
                new[]
                {
                    typeof(string), typeof(int), typeof(int), typeof(int), typeof(long), typeof(string),
                    typeof(Vector2i), typeof(bool)
                });
            MethodInfo? method = AccessTools.Method(typeof(InventoryGui_DoCraft_Transpile), nameof(AddCustomData));
            CodeInstruction[] newInstructions = new[]
            {
                new CodeInstruction(OpCodes.Dup),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call, method)
            };
            return new CodeMatcher(instructions)
                .Start()
                .MatchStartForward(new CodeMatch(OpCodes.Callvirt, targetMethod))
                .Advance(1)
                .Insert(newInstructions)
                .InstructionEnumeration();
        }

        public static void AddCustomData(ItemDrop.ItemData? item, InventoryGui instance)
        {
            if (item is not Bag bag || instance.m_craftUpgradeItem is not Bag oldBag) return;
            bag.m_customData = new(oldBag.m_customData);
            bag.Load();
        }
    }

    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetWeight))]
    private static class ItemDrop_ItemData_GetWeight_Patch
    {
        [UsedImplicitly]
        private static void Prefix(ItemDrop.ItemData __instance)
        {
            if (__instance is not Bag bag) return;
            bag.UpdateWeight();
        }
    }
    
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateInventoryWeight))]
    private static class InventoryGui_UpdateInventoryWeight_Patch
    {
        [UsedImplicitly]
        private static void Prefix(InventoryGui __instance, Player player)
        {
            currentBag?.UpdateWeight();
            player.GetInventory().Changed();
        }
    }
    
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateContainerWeight))]
    private static class InventoryGui_UpdateContainerWeight_Patch
    {
        [UsedImplicitly]
        private static void Postfix(InventoryGui __instance)
        {
            if (__instance.m_currentContainer != null || currentBag == null) return;
            __instance.m_containerWeight.text = Mathf.CeilToInt(currentBag.GetInventoryWeight()).ToString();
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Show))]
    private static class InventoryGui_Show_Patch
    {
        [UsedImplicitly]
        private static void Prefix(Container? container)
        {
            currentBag?.Load();
            Player.m_localPlayer?.GetInventory().Changed();
        }
    }
    
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateContainer))]
    private static class InventoryGui_UpdateContainer_Patch
    {
        [UsedImplicitly]
        private static bool Prefix(InventoryGui __instance, Player player) => __instance.UpdateBag(player);
    }
    
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.OnStackAll))]
    private static class InventoryGui_OnStackAll_Patch
    {
        [UsedImplicitly]
        private static void Postfix(InventoryGui __instance)
        {
            if (Player.m_localPlayer.IsTeleporting() || __instance.m_currentContainer != null || currentBag == null) return;
            currentBag.inventory.StackAll(Player.m_localPlayer.GetInventory());
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.OnTakeAll))]
    private static class InventoryGui_OnTakeAll_Patch
    {
        [UsedImplicitly]
        private static void Postfix(InventoryGui __instance)
        {
            if (Player.m_localPlayer.IsTeleporting() || __instance.m_currentContainer != null || currentBag == null) return;
            __instance.SetupDragItem(null, null, 1);
            Inventory inventory = currentBag.inventory;
            Player.m_localPlayer.GetInventory().MoveAll(inventory);
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.HaveRepairableItems))]
    private static class InventoryGui_HaveRepairableItems_Transpiler
    {
        [UsedImplicitly]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo? target = AccessTools.Method(typeof(Inventory), nameof(Inventory.GetWornItems));
            MethodInfo? method = AccessTools.Method(typeof(BagExtensions), nameof(GetBagWornItems));
            var newInstructions = new[]
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call, method)
            };

            return new CodeMatcher(instructions)
                .Start()
                .MatchStartForward(new CodeMatch(OpCodes.Callvirt, target))
                .Advance(1)
                .Insert(newInstructions)
                .InstructionEnumeration();
        }
    }
    
    public static void GetBagWornItems(InventoryGui instance)
    {
        if (Player.m_localPlayer.GetBag() is not { } bag) return;
        bag.inventory.GetWornItems(instance.m_tempWornItems);
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.RepairOneItem))]
    private static class InventoryGui_RepairOneItem_Transpiler
    {
        [UsedImplicitly]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo? target = AccessTools.Method(typeof(Inventory), nameof(Inventory.GetWornItems));
            MethodInfo? method = AccessTools.Method(typeof(BagExtensions), nameof(GetBagWornItems));
            CodeInstruction[] newInstructions = new[]
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call, method)
            };

            return new CodeMatcher(instructions)
                .Start()
                .MatchStartForward(new CodeMatch(OpCodes.Callvirt, target))
                .Advance(1)
                .Insert(newInstructions)
                .InstructionEnumeration();
        }
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
        NoFishes = 8
    }
    
    public static readonly Dictionary<string, BagSetup> bags = new();

    public readonly bool isQuiver;
    public readonly Dictionary<int, Size> sizes = new();
    public readonly SE_Bag statusEffect;
    public readonly string englishName;
    public readonly ConfigEntry<Restriction>? restrictConfig;

    public BagSetup(Item item, int width, int height, bool isQuiver = false, bool replaceShader = true)
    {
        var sharedName = $"${item.Name.Key}";
        englishName = new Regex(@"[=\n\t\\""\'\[\]]*").Replace(Item.english.Localize(sharedName), "").Trim();
        sizes[1] = new Size(width, height);
        bags[sharedName] = this;
        this.isQuiver = isQuiver;
        statusEffect = ScriptableObject.CreateInstance<SE_Bag>();
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
    }

    public void AddSizePerQuality(int width, int height, int quality)
    {
        sizes[quality] = new Size(width, height);
    }

    public void SetupConfigs()
    {
        foreach (var size in sizes)
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
            if (!__instance.m_itemData.IsBag()) return;
            __instance.m_itemData = __instance.m_itemData.IsQuiver() ? new Quiver(__instance.m_itemData) : new Bag(__instance.m_itemData);
        }
    }
}

public class Bag : ItemDrop.ItemData
{
    private const string BAG_DATA_KEY = "RustyBag.CustomData.Inventory.Data";
    public BagInventory inventory = new("Bag", null, null, 8, 4);
    private bool isLoaded;
    private readonly float baseWeight;

    protected BagEquipment? m_bagEquipment;
    
    public ItemDrop.ItemData? lantern;
    public ItemDrop.ItemData? pickaxe;
    public ItemDrop.ItemData? fishingRod;
    public ItemDrop.ItemData? cultivator;
    public ItemDrop.ItemData? melee;
    public ItemDrop.ItemData? hammer;
    public ItemDrop.ItemData? hoe;
    public ItemDrop.ItemData? atgeir;

    public Bag(ItemDrop.ItemData item)
    {
        m_shared = item.m_shared;
        m_shared.m_itemType = ItemType.Trinket;
        m_customData = item.m_customData;
        baseWeight = m_shared.m_weight;
    }

    private void OnChanged()
    {
        m_shared.m_teleportable = inventory.IsTeleportable();
        UpdateWeight();
        ZPackage pkg = new ZPackage();
        inventory.Save(pkg);
        m_customData[BAG_DATA_KEY] = pkg.GetBase64();
        UpdateAttachments();
    }

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
        foreach (ItemDrop.ItemData? item in inventory.GetAllItemsInGridOrder())
        {
            if (!item.IsEquipable()) continue;
            if (item.m_shared.m_name == "$item_lantern" && lantern == null)
            {
                lantern = item;
            }
            else if (item.m_shared.m_name == "$item_fishingrod" && fishingRod == null)
            {
                fishingRod = item;
            }
            else if (item.m_shared.m_name == "$item_cultivator" && cultivator == null)
            {
                cultivator = item;
            }
            else if (item.m_shared.m_name == "$item_hoe" && hoe == null)
            {
                hoe = item;
            }
            else if (item.m_shared.m_name == "$item_hammer" && hammer == null)
            {
                hammer = item;
            }
            else if (item.m_shared.m_skillType is Skills.SkillType.Pickaxes && pickaxe == null)
            {
                pickaxe = item;
            }
            else if (item.m_shared.m_itemType is ItemType.OneHandedWeapon && melee == null)
            {
                melee = item;
            }
            else if (item.m_shared.m_skillType is Skills.SkillType.Polearms && atgeir == null)
            {
                atgeir = item;
            }

            if (lantern != null && fishingRod != null && cultivator != null && hoe != null && hammer != null &&
                pickaxe != null && melee != null && atgeir != null) break;
        }
        
        m_bagEquipment?.SetLanternItem(lantern?.m_dropPrefab.name ?? "");
        m_bagEquipment?.SetPickaxeItem(pickaxe?.m_dropPrefab.name ?? "");
        m_bagEquipment?.SetFishingRodItem(fishingRod?.m_dropPrefab.name ?? "");
        m_bagEquipment?.SetCultivatorItem(cultivator?.m_dropPrefab.name ?? "");
        m_bagEquipment?.SetHammerItem(hammer?.m_dropPrefab.name ?? "");
        m_bagEquipment?.SetHoeItem(hoe?.m_dropPrefab.name ?? "");
        m_bagEquipment?.SetMeleeItem(melee?.m_dropPrefab.name ?? "");
        m_bagEquipment?.SetAtgeirItem(atgeir?.m_dropPrefab.name ?? "");
    }

    public void Load()
    {
        if (isLoaded) return;
        SetupInventory();
        if (m_customData.TryGetValue(BAG_DATA_KEY, out string data))
        {
            ZPackage pkg = new ZPackage(data);
            inventory.Load(pkg);
            UpdateWeight();
            UpdateAttachments();
            isLoaded = true;
        }
        inventory.m_onChanged = OnChanged;
    }

    protected virtual void SetupInventory()
    {
        BagSetup setup = GetSetup();
        BagSetup.Size size = setup.sizes.TryGetValue(m_quality, out var s) ? s : new BagSetup.Size(1, 1); 
        inventory = new BagInventory("Bag", this, Player.m_localPlayer?.GetInventory().m_bkg, size.width, size.height);
    }
    
    public BagSetup GetSetup() => BagSetup.bags[m_shared.m_name];

    public void UpdateWeight()
    {
        m_shared.m_weight = baseWeight + GetInventoryWeight();
    }

    public float GetInventoryWeight()
    {
        var total = inventory.GetTotalWeight();
        if (m_shared.m_equipStatusEffect is SE_Bag se) se.ModifyInventoryWeight(ref total);
        return total;
    }

    private void GrabAll(Inventory fromInventory)
    {
        List<ItemDrop.ItemData> itemDataList = new List<ItemDrop.ItemData>(fromInventory.GetAllItems());
        foreach (ItemDrop.ItemData itemData in itemDataList)
        {
            if (itemData.IsBound()) continue;
            if (inventory.ContainsItemByName(itemData.m_shared.m_name) && !Player.m_localPlayer.IsItemEquiped(itemData) && inventory.AddItem(itemData)) fromInventory.RemoveItem(itemData);
        }
        inventory.Changed();
        fromInventory.UpdateTotalWeight();
    }
    
    public void SetEquipped(BagEquipment? equipment)
    {
        m_equipped = equipment != null;
        m_bagEquipment = equipment;
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
            if (Configs.AutoStack && instance.GetBag() is { } bag && bag.inventory.ContainsItemByName(item.m_shared.m_name)) return bag.inventory.AddItem(item);
            return inventory.AddItem(item);
        }
    }
}

