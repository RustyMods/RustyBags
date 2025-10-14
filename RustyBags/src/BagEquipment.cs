using System.Collections.Generic;
using HarmonyLib;
using JetBrains.Annotations;
using RustyBags.Managers;
using UnityEngine;

namespace RustyBags;

public class BagEquipment : MonoBehaviour
{
    public ZNetView m_nview = null!;
    public VisEquipment m_visEquipment = null!;
    public Player m_player = null!;

    private Bag? m_currentBagItem;
    
    public string m_bagItem = "";
    public string m_lanternItem = "";
    public string m_pickaxeItem = "";
    public string m_arrowItem = "";
    public int m_arrowStack;
    public string m_fishingRodItem = "";
    public string m_cultivatorItem = "";
    public string m_hammerItem = "";
    public string m_meleeItem = "";
    public string m_hoeItem = "";
    public string m_atgeirItem = "";
    
    public int m_currentBagHash;
    public int m_currentLanternHash;
    public int m_currentPickaxeHash;
    public int m_currentArrowHash;
    public int m_currentArrowStack;
    public int m_currentFishingRodHash;
    public int m_currentCultivatorHash;
    public int m_currentHammerHash;
    public int m_currentMeleeHash;
    public int m_currentHoeHash;
    public int m_currentAtgeirHash;

    public string m_bagOverrideItem = "";
    public int m_bagOverrideHash;
    
    public GameObject? m_bagInstance;
    private GameObject? m_lanternInstance;
    private GameObject? m_pickaxeInstance;
    private readonly List<GameObject> m_arrowInstances = new();
    private GameObject? m_fishingRodInstance;
    private GameObject? m_cultivatorInstance;
    private GameObject? m_hammerInstance;
    private GameObject? m_meleeInstance;
    private GameObject? m_hoeInstance;
    private GameObject? m_atgeirInstance;
    
    public void Awake()
    {
        m_nview = GetComponent<ZNetView>();
        m_visEquipment = GetComponent<VisEquipment>();
        m_player = GetComponent<Player>();
    }

    private bool IsBagEquipped(Bag bag) => m_currentBagItem == bag;
    public Bag? GetBag() => m_currentBagItem;

    private bool SetBag(Bag? bag)
    {
        if (m_currentBagItem == bag) return false;
        m_currentBagItem?.SetEquipped(null);
        m_currentBagItem?.OnUnequip();
        var oldSE = m_currentBagItem?.m_shared.m_equipStatusEffect;
        var newSE = bag?.m_shared.m_equipStatusEffect;
        m_currentBagItem = bag;
        
        m_currentBagItem?.SetEquipped(this);
        m_currentBagItem?.OnEquip();
        
        SetBagItem(m_currentBagItem?.m_dropPrefab.name ?? "");

        if (m_currentBagItem is Quiver quiver)
        {
            SetArrowItem(quiver.ammoItem?.m_dropPrefab.name ?? "", quiver.ammoItem?.m_stack ?? 0);
        }
        else
        {
            SetLanternItem(m_currentBagItem?.lantern?.m_dropPrefab.name ?? "");
            SetPickaxeItem(m_currentBagItem?.pickaxe?.m_dropPrefab.name ?? "");
            SetFishingRodItem(m_currentBagItem?.fishingRod?.m_dropPrefab.name ?? "");
            SetCultivatorItem(m_currentBagItem?.cultivator?.m_dropPrefab.name ?? "");
            SetHammerItem(m_currentBagItem?.hammer?.m_dropPrefab.name ?? "");
            SetMeleeItem(m_currentBagItem?.melee?.m_dropPrefab.name ?? "");
            SetHoeItem(m_currentBagItem?.hoe?.m_dropPrefab.name ?? "");
            SetAtgeirItem(m_currentBagItem?.atgeir?.m_dropPrefab.name ?? "");
            SetArrowItem("", 0);
        }

        SetupEquipStatusEffect(oldSE, newSE, m_currentBagItem?.m_quality ?? 1);
        return true;
    }

    public void SetupEquipStatusEffect(StatusEffect? old, StatusEffect? se, int quality)
    {
        if (m_nview.GetZDO() == null || !m_nview.IsOwner()) return;
        if (old != null) m_player.GetSEMan()?.RemoveStatusEffect(old.NameHash());
        if (se != null) m_player.GetSEMan()?.AddStatusEffect(se, false, quality);
    }

    public void SetFishingRodItem(string item)
    {
        m_fishingRodItem = item;
        var fishingRodHash = string.IsNullOrEmpty(item) ? 0 : item.GetStableHashCode();
        if (m_nview.GetZDO() == null || !m_nview.IsOwner()) return;
        m_nview.GetZDO().Set(BagVars.FishingRod, fishingRodHash);
    }

    public void SetCultivatorItem(string item)
    {
        m_cultivatorItem = item;
        var cultivatorHash = string.IsNullOrEmpty(item) ? 0 : item.GetStableHashCode();
        if (m_nview.GetZDO() == null || !m_nview.IsOwner()) return;
        m_nview.GetZDO().Set(BagVars.Cultivator, cultivatorHash);
    }

    public void SetHammerItem(string item)
    {
        m_hammerItem = item;
        var hammerHash = string.IsNullOrEmpty(item) ? 0 : item.GetStableHashCode();
        if (m_nview.GetZDO() == null || !m_nview.IsOwner()) return;
        m_nview.GetZDO().Set(BagVars.Hammer, hammerHash);
    }

    public void SetMeleeItem(string item)
    {
        m_meleeItem = item;
        var meleeHash = string.IsNullOrEmpty(item) ? 0 : item.GetStableHashCode();
        if (m_nview.GetZDO() == null || !m_nview.IsOwner()) return;
        m_nview.GetZDO().Set(BagVars.Melee, meleeHash);
    }

    public void SetAtgeirItem(string item)
    {
        m_atgeirItem = item;
        var atgeirHash = string.IsNullOrEmpty(item) ? 0 : item.GetStableHashCode();
        if (m_nview.GetZDO() == null || !m_nview.IsOwner()) return;
        m_nview.GetZDO().Set(BagVars.Atgeir, atgeirHash);
    }

    public void SetHoeItem(string item)
    {
        m_hoeItem = item;
        var hoeHash = string.IsNullOrEmpty(item) ? 0 : item.GetStableHashCode();
        if (m_nview.GetZDO() == null || !m_nview.IsOwner()) return;
        m_nview.GetZDO().Set(BagVars.Hoe, hoeHash);
    }

    public void SetArrowItem(string item, int stack)
    {
        m_arrowItem = item;
        m_arrowStack = stack;
        var arrowHash = string.IsNullOrEmpty(item) ? 0 : item.GetStableHashCode();
        if (m_nview.GetZDO() == null || !m_nview.IsOwner()) return;
        m_nview.GetZDO().Set(BagVars.Arrow, arrowHash);
        m_nview.GetZDO().Set(BagVars.ArrowStack, stack);
    }

    public void SetBagItem(string item)
    {
        m_bagItem = item;
        var bagHash = string.IsNullOrEmpty(m_bagItem) ? 0 : m_bagItem.GetStableHashCode();
        if (m_nview.GetZDO() == null || !m_nview.IsOwner()) return;
        m_nview.GetZDO().Set(BagVars.Bag, bagHash);
    }

    public void SetBagOverrideItem(string item)
    {
        m_bagOverrideItem = item;
        var bagHash = string.IsNullOrEmpty(item) ? 0 : item.GetStableHashCode();
        if (m_nview.GetZDO() == null || !m_nview.IsOwner()) return;
        m_nview.GetZDO().Set(BagVars.BagOverride, bagHash);
    }
    
    public void SetLanternItem(string item)
    {
        m_lanternItem = item;
        if (m_nview.GetZDO() == null || !m_nview.IsOwner()) return;
        var lanternHash = string.IsNullOrEmpty(item) ? 0 : item.GetStableHashCode();
        m_nview.GetZDO().Set(BagVars.Lantern, lanternHash);
    }

    public void SetPickaxeItem(string item)
    {
        m_pickaxeItem = item;
        if (m_nview.GetZDO() == null || !m_nview.IsOwner()) return;
        var pickaxeHash = string.IsNullOrEmpty(item) ? 0 : item.GetStableHashCode();
        m_nview.GetZDO().Set(BagVars.Pickaxe, pickaxeHash);
    }

    public void SetBagEquipped(int hash)
    {
        if (m_currentBagHash == hash) return;
        if (m_bagInstance != null)
        {
            Destroy(m_bagInstance);
            m_bagInstance = null;
            m_lanternInstance = null;
            m_pickaxeInstance = null;
            m_meleeInstance = null;
            m_atgeirInstance = null;
            m_hoeInstance = null;
            m_cultivatorInstance = null;
            m_arrowInstances.Clear();
            m_hammerInstance = null;
            m_fishingRodInstance = null;
        }

        m_currentBagHash = hash;
        m_currentLanternHash = 0;
        m_currentPickaxeHash = 0;
        m_currentFishingRodHash = 0;
        m_currentCultivatorHash = 0;
        m_currentHammerHash = 0;
        m_currentMeleeHash = 0;
        m_currentHoeHash = 0;
        m_currentAtgeirHash = 0;
        m_currentArrowHash = 0;
        m_currentArrowStack = 0;
        
        if (hash != 0)
        {
            m_bagInstance = m_visEquipment.AttachItem(hash, 0, m_visEquipment.m_backShield);
        }
    }

    public void SetHammerEquipped(int hash)
    {
        if (m_currentHammerHash == hash) return;
        if (m_bagInstance == null) return;
        if (m_hammerInstance) Destroy(m_hammerInstance);
        m_hammerInstance = null;

        m_currentHammerHash = hash;
        if (hash == 0) return;
        if (m_bagInstance.transform.Find("attach_hammer") is not { } attachHammer) return;
        var prefab = ObjectDB.instance.GetItemPrefab(hash);
        if (prefab == null) return;
        var attach = prefab.transform.Find("attach");
        if (attach == null) return;
        
        m_hammerInstance = Instantiate(attach.gameObject, attachHammer);
        VisEquipment.CleanupInstance(m_hammerInstance);
        m_hammerInstance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
    }

    public void SetAtgeirEquipped(int hash)
    {
        if (m_currentAtgeirHash == hash) return;
        if (m_bagInstance == null) return;
        if (m_atgeirInstance) Destroy(m_atgeirInstance);
        m_atgeirInstance = null;

        m_currentAtgeirHash = hash;
        if (hash == 0) return;
        if (m_bagInstance.transform.Find("attach_atgeir") is not { } attachAtgeir) return;
        var prefab = ObjectDB.instance.GetItemPrefab(hash);
        if (prefab == null) return;
        var attach = prefab.transform.Find("attach");
        if (attach == null) return;
        
        m_atgeirInstance = Instantiate(attach.gameObject, attachAtgeir);
        VisEquipment.CleanupInstance(m_atgeirInstance);
        m_atgeirInstance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
    }

    public void SetMeleeEquipped(int hash)
    {
        if (m_currentMeleeHash == hash) return;
        if (m_bagInstance == null) return;
        if (m_meleeInstance) Destroy(m_meleeInstance);
        m_meleeInstance = null;

        m_currentMeleeHash = hash;
        if (hash == 0) return;
        if (m_bagInstance.transform.Find("attach_melee") is not { } attachMelee) return;
        var prefab = ObjectDB.instance.GetItemPrefab(hash);
        if (prefab == null) return;
        var attach = prefab.transform.Find("attach");
        if (attach == null) return;
        
        m_meleeInstance = Instantiate(attach.gameObject, attachMelee);
        VisEquipment.CleanupInstance(m_meleeInstance);
        m_meleeInstance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
    }

    public void SetHoeEquipped(int hash)
    {
        if (m_currentHoeHash == hash) return;
        if (m_bagInstance == null) return;
        if (m_hoeInstance) Destroy(m_hoeInstance);
        m_hoeInstance = null;

        m_currentHoeHash = hash;
        if (hash == 0) return;
        if (m_bagInstance.transform.Find("attach_hoe") is not { } attachHoe) return;
        var prefab = ObjectDB.instance.GetItemPrefab(hash);
        if (prefab == null) return;
        var attach = prefab.transform.Find("attach");
        if (attach == null) return;
        
        m_hoeInstance = Instantiate(attach.gameObject, attachHoe);
        VisEquipment.CleanupInstance(m_hoeInstance);
        m_hoeInstance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
    }

    public void SetFishingRodEquipped(int hash)
    {
        if (m_currentFishingRodHash == hash) return;
        if (m_bagInstance == null) return;
        if (m_fishingRodInstance) Destroy(m_fishingRodInstance);
        m_fishingRodInstance = null;

        m_currentFishingRodHash = hash;
        if (hash == 0) return;
        if (m_bagInstance.transform.Find("attach_fishingrod") is not { } attachFishingRod) return;

        var prefab = ObjectDB.instance.GetItemPrefab(hash);
        if (prefab == null) return;
        
        var attach = prefab.transform.Find("attach");
        if (attach == null) return;

        m_fishingRodInstance = Instantiate(attach.gameObject, attachFishingRod);
        VisEquipment.CleanupInstance(m_fishingRodInstance);
        m_fishingRodInstance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
    }
    
    public void SetLanternEquipped(int hash)
    {
        if (m_currentLanternHash == hash) return;
        if (m_bagInstance == null) return;
        if (m_lanternInstance) Destroy(m_lanternInstance);
        m_lanternInstance = null;

        m_currentLanternHash = hash;
        if (hash == 0) return;
        if (m_bagInstance.transform.Find("attach_lantern") is not { } attachLantern) return;
        
        var prefab = ObjectDB.instance.GetItemPrefab(hash);
        if (prefab == null) return;
        
        var body = attachLantern.GetComponent<Rigidbody>();
        var equipped = prefab.transform.Find("attach/equiped");
        m_lanternInstance = Instantiate(equipped.gameObject, attachLantern);
        m_lanternInstance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        m_lanternInstance.SetActive(true);
        if (m_lanternInstance.TryGetComponent(out ConfigurableJoint joint)) joint.connectedBody = body;
    }

    public void SetPickaxeEquipped(int hash)
    {
        if (m_currentPickaxeHash == hash) return;
        if (m_bagInstance == null) return;
        if (m_pickaxeInstance) Destroy(m_pickaxeInstance);
        m_pickaxeInstance = null;
        m_currentPickaxeHash = hash;
        if (hash == 0) return;
        if (m_bagInstance.transform.Find("attach_pickaxe") is not {} attachPickaxe) return;
        var prefab = ObjectDB.instance.GetItemPrefab(hash);
        if (prefab == null) return;
        var attach = prefab.transform.Find("attach");
        if (attach == null) return;
        m_pickaxeInstance = Instantiate(attach.gameObject, attachPickaxe);
        VisEquipment.CleanupInstance(m_pickaxeInstance);
        m_pickaxeInstance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
    }

    public void SetCultivatorEquipped(int hash)
    {
        if (m_currentCultivatorHash == hash) return;
        if (m_bagInstance == null) return;
        if (m_cultivatorInstance) Destroy(m_cultivatorInstance);
        m_cultivatorInstance = null;
        m_currentCultivatorHash = hash;
        if (hash == 0) return;
        if (m_bagInstance.transform.Find("attach_cultivator") is not { } attachCultivator) return;
        var prefab = ObjectDB.instance.GetItemPrefab(hash);
        if (prefab == null) return;
        var attach = prefab.transform.Find("attach");
        if (attach == null) return;
        m_cultivatorInstance = Instantiate(attach.gameObject, attachCultivator);
        VisEquipment.CleanupInstance(m_cultivatorInstance);
        m_cultivatorInstance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
    }

    public void SetArrowEquipped(int hash, int stack)
    {
        if (m_currentArrowHash == hash && m_currentArrowStack == stack) return;
        if (m_bagInstance == null) return;
        foreach (var instance in m_arrowInstances)
        {
            Destroy(instance);
        }
        m_arrowInstances.Clear();
        m_currentArrowHash = hash;
        m_currentArrowStack = stack;
        
        if (m_bagInstance.transform.Find("Quiver/arrows") is not {} arrows) return;
        if (hash == 0) return;
        var prefab = ObjectDB.instance.GetItemPrefab(hash);
        if (prefab == null) return;
        var model = prefab.transform.GetChild(0).gameObject;
        if (model == null) return;

        int count = 0;

        foreach (Transform child in arrows)
        {
            if (count >= stack) break;
            var go = Instantiate(model, child);
            VisEquipment.CleanupInstance(go);
            go.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            go.transform.localScale *= 0.5f;
            if (go.GetComponentInChildren<Light>() is {} light) Destroy(light.gameObject);
            if (go.GetComponentInChildren<ParticleSystem>() is {} ps) Destroy(ps.gameObject);
            m_arrowInstances.Add(go);
            ++count;
        }
    }
    
    public void UpdateVisuals()
    {
        ZDO? zdo = m_nview.GetZDO();
        int bagHash = zdo?.GetInt(BagVars.Bag) ?? (string.IsNullOrEmpty(m_bagItem) ? 0 : m_bagItem.GetStableHashCode());
        int lanternHash = zdo?.GetInt(BagVars.Lantern) ?? (string.IsNullOrEmpty(m_lanternItem) ? 0 : m_lanternItem.GetStableHashCode());
        int pickaxeHash = zdo?.GetInt(BagVars.Pickaxe) ?? (string.IsNullOrEmpty(m_pickaxeItem) ? 0 : m_pickaxeItem.GetStableHashCode());
        int arrowHash = zdo?.GetInt(BagVars.Arrow) ?? (string.IsNullOrEmpty(m_arrowItem) ? 0 :  m_arrowItem.GetStableHashCode());
        int arrowStack = zdo?.GetInt(BagVars.ArrowStack) ?? m_arrowStack;
        int fishingRodHash = zdo?.GetInt(BagVars.FishingRod) ?? (string.IsNullOrEmpty(m_fishingRodItem) ? 0 : m_fishingRodItem.GetStableHashCode());
        int cultivatorHash = zdo?.GetInt(BagVars.Cultivator) ?? (string.IsNullOrEmpty(m_cultivatorItem) ? 0 : m_cultivatorItem.GetStableHashCode());
        int hammerHash = zdo?.GetInt(BagVars.Hammer) ?? (string.IsNullOrEmpty(m_hammerItem) ? 0 : m_hammerItem.GetStableHashCode());
        int hoeHash = zdo?.GetInt(BagVars.Hoe) ?? (string.IsNullOrEmpty(m_hoeItem) ? 0 : m_hoeItem.GetStableHashCode());
        int meleeHash = zdo?.GetInt(BagVars.Melee) ?? (string.IsNullOrEmpty(m_meleeItem) ? 0 :  m_meleeItem.GetStableHashCode());
        int atgeirHash = zdo?.GetInt(BagVars.Atgeir) ?? (string.IsNullOrEmpty(m_atgeirItem) ? 0 : m_atgeirItem.GetStableHashCode());
        
        // int bagOverrideHash = zdo?.GetInt(BagVars.BagOverride) ?? (string.IsNullOrEmpty(m_bagOverrideItem) ? 0 : m_bagOverrideItem.GetStableHashCode());
        // if (bagOverrideHash != 0) bagHash = bagOverrideHash;
        
        SetBagEquipped(bagHash);
        SetLanternEquipped(lanternHash);
        SetPickaxeEquipped(pickaxeHash);
        SetArrowEquipped(arrowHash, arrowStack);
        SetFishingRodEquipped(fishingRodHash);
        SetCultivatorEquipped(cultivatorHash);
        SetHammerEquipped(hammerHash);
        SetHoeEquipped(hoeHash);
        SetMeleeEquipped(meleeHash);
        SetAtgeirEquipped(atgeirHash);
    }
    
    [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.UpdateVisuals))]
    private static class VisEquipment_UpdateVisuals_Patch
    {
        [UsedImplicitly]
        private static void Postfix(VisEquipment __instance)
        {
            if (!__instance.TryGetComponent(out BagEquipment component)) return;
            component.UpdateVisuals();
        }
    }

    [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.Awake))]
    private static class FejdStartup_Awake_Patch
    {
        [UsedImplicitly]
        private static void Prefix(FejdStartup __instance)
        {
            __instance.m_playerPrefab.AddComponent<BagEquipment>();
        }
    }

    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.IsItemEquiped))]
    private static class Humanoid_IsItemEquiped_Patch
    {
        [UsedImplicitly]
        private static void Postfix(Humanoid __instance, ItemDrop.ItemData item, ref bool __result)
        {
            if (!__instance.TryGetComponent(out BagEquipment component) || item is not Bag bag) return;
            __result = component.IsBagEquipped(bag);
        }
    }
    
    
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.EquipItem))]
    private static class Humanoid_EquipItem_Patch
    {
        [UsedImplicitly]
        private static bool Prefix(Humanoid __instance, ItemDrop.ItemData item, bool triggerEquipEffects, ref bool __result)
        {
            if (!__instance.TryGetComponent(out BagEquipment component) || item is not Bag bag) return true;
            if (!__instance.m_inventory.ContainsItem(item))
            {
                __result = false;
                return false;
            }
            __result = component.SetBag(bag);
            if (triggerEquipEffects) __instance.TriggerEquipEffect(item);
            return false;
        }
    }

    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UnequipItem))]
    private static class Humanoid_UnequipItem_Patch
    {
        [UsedImplicitly]
        private static bool Prefix(Humanoid __instance, ItemDrop.ItemData item, bool triggerEquipEffects)
        {
            if (!__instance.TryGetComponent(out BagEquipment component) || item is not Bag bag) return true;
            if (!component.IsBagEquipped(bag)) return false;
            component.SetBag(null);
            if (triggerEquipEffects) __instance.TriggerEquipEffect(item);
            return false;
        }
    }
}

public static class BagVars
{
    public static readonly int Bag = "ExtraSlot.Bag".GetStableHashCode();
    public static readonly int Lantern = "ExtraSlot.Bag.Lantern".GetStableHashCode();
    public static readonly int Pickaxe = "ExtraSlot.Bag.Pickaxe".GetStableHashCode();
    public static readonly int Arrow = "ExtraSlot.Quiver.Arrow".GetStableHashCode();
    public static readonly int ArrowStack = "ExtraSlot.Quiver.ArrowStack".GetStableHashCode();
    public static readonly int FishingRod = "ExtraSlot.Bag.FishingRod".GetStableHashCode();
    public static readonly int Cultivator = "ExtraSlot.Bag.Cultivator".GetStableHashCode();
    public static readonly int Hammer = "ExtraSlot.Bag.Hammer".GetStableHashCode();
    public static readonly int Hoe = "ExtraSlot.Bag.Hoe".GetStableHashCode();
    public static readonly int Melee = "ExtraSlot.Bag.Melee".GetStableHashCode();
    public static readonly int Atgeir = "ExtraSlot.Bag.Atgeir".GetStableHashCode();
    public static readonly int BagOverride = "ExtraSlot.Bag.BagOverride".GetStableHashCode();
}