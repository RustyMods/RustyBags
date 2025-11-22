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
    private Quiver? m_currentQuiverItem;

    public string m_quiverItem = "";
    public string m_bagItem = "";
    public string m_lanternItem = "";
    public string m_pickaxeItem = "";
    public string m_arrowItem = "";
    public int m_arrowStack;
    public bool m_requireCentering;
    public string m_fishingRodItem = "";
    public string m_cultivatorItem = "";
    public string m_hammerItem = "";
    public string m_meleeItem = "";
    public string m_hoeItem = "";
    public string m_atgeirItem = "";
    public string m_oreItem = "";
    public int m_oreStack;
    public string m_scytheItem = "";
    public string m_harpoonItem = "";

    public int m_currentQuiverHash;
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
    public int m_currentOreHash;
    public int m_currentOreStack;
    public int m_currentScythHash;
    public int m_currentHarpoonHash;

    public GameObject? m_quiverInstance;
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
    private readonly List<GameObject> m_oreInstances = new();
    private GameObject? m_scythInstance;
    private GameObject? m_harpoonInstance;

    public StatusEffect? m_currentEquipStatus;
    public StatusEffect? m_currentQuiverStatus;
    
    public void Awake()
    {
        m_nview = GetComponent<ZNetView>();
        m_visEquipment = GetComponent<VisEquipment>();
        m_player = GetComponent<Player>();
    }

    private bool IsBagEquipped(Bag bag) => m_currentBagItem == bag;
    private bool IsQuiverEquipped(Quiver quiver) => m_currentQuiverItem == quiver;

    public Bag? GetBag() => m_currentBagItem;
    public Quiver? GetQuiver() => m_currentQuiverItem;
    private bool SetBag(Bag? bag)
    {
        if (m_currentBagItem == bag) return false;
        m_currentBagItem?.OnUnequip();
        StatusEffect? oldSE = m_currentBagItem?.m_shared.m_equipStatusEffect;
        StatusEffect? newSE = bag?.m_shared.m_equipStatusEffect;
        m_currentBagItem = bag;
        
        m_currentBagItem?.OnEquip(this);
        
        SetBagItem(m_currentBagItem?.m_dropPrefab.name ?? "");
        
        SetLanternItem(m_currentBagItem?.lantern?.m_dropPrefab.name ?? "");
        SetPickaxeItem(m_currentBagItem?.pickaxe?.m_dropPrefab.name ?? "");
        SetFishingRodItem(m_currentBagItem?.fishingRod?.m_dropPrefab.name ?? "");
        SetCultivatorItem(m_currentBagItem?.cultivator?.m_dropPrefab.name ?? "");
        SetHammerItem(m_currentBagItem?.hammer?.m_dropPrefab.name ?? "");
        SetMeleeItem(m_currentBagItem?.melee?.m_dropPrefab.name ?? "");
        SetHoeItem(m_currentBagItem?.hoe?.m_dropPrefab.name ?? "");
        SetAtgeirItem(m_currentBagItem?.atgeir?.m_dropPrefab.name ?? "");
        SetOreItem(m_currentBagItem?.ore?.m_dropPrefab.name ?? "", m_currentBagItem?.ore?.m_stack ?? 0);
        SetScytheItem(m_currentBagItem?.scythe?.m_dropPrefab.name ?? "");
        SetHarpoonItem(m_currentBagItem?.harpoon?.m_dropPrefab.name ?? "");

        SetupEquipStatusEffect(oldSE, newSE, m_currentBagItem?.m_quality ?? 1, m_currentBagItem?.lantern?.GetCharmData());
        return true;
    }

    public bool SetQuiver(Quiver? quiver)
    {
        if (m_currentQuiverItem == quiver) return false;
        m_currentQuiverItem?.OnUnequip();
        StatusEffect? oldSE = m_currentQuiverItem?.m_shared.m_equipStatusEffect;
        StatusEffect? newSE = quiver?.m_shared.m_equipStatusEffect;
        m_currentQuiverItem = quiver;
        m_currentQuiverItem?.OnEquip(this);
        SetQuiverItem(m_currentQuiverItem?.m_dropPrefab.name ?? "");

        if (m_currentQuiverItem?.ammoItem?.m_shared.m_ammoType == "$ammo_bolts")
        {
            SetArrowItem(m_currentQuiverItem?.ammoItem?.m_dropPrefab.name ?? "",  m_currentQuiverItem?.ammoItem?.m_stack ?? 0, true);
        }
        else
        {
            SetArrowItem(m_currentQuiverItem?.ammoItem?.m_dropPrefab.name ?? "",  m_currentQuiverItem?.ammoItem?.m_stack ?? 0);
        }
        SetupQuiverStatusEffect(oldSE,newSE, m_currentQuiverItem?.m_quality ?? 1);
        return true;
    }

    public void SetupQuiverStatusEffect(StatusEffect? old, StatusEffect? se, int quality)
    {
        if (m_nview.GetZDO() == null || !m_nview.IsOwner()) return;
        if (old != null)
        {
            m_player.GetSEMan()?.RemoveStatusEffect(old.NameHash());
            m_currentQuiverStatus = null;
        }
        if (se != null)
        {
            m_currentQuiverStatus = m_player.GetSEMan()?.AddStatusEffect(se.NameHash(), false, quality);
        }
    }

    public void SetupEquipStatusEffect(StatusEffect? old, StatusEffect? se, int quality, Charm? charm)
    {
        if (m_nview.GetZDO() == null || !m_nview.IsOwner()) return;
        if (old != null)
        {
            m_player.GetSEMan()?.RemoveStatusEffect(old.NameHash());
            m_currentEquipStatus = null;
        }
        if (se != null)
        {
            m_currentEquipStatus = m_player.GetSEMan()?.AddStatusEffect(se.NameHash(), false, quality);
            if (Configs.CharmsAffectBag) (m_currentEquipStatus as SE_Bag)?.SetBag(m_currentBagItem);
        }
    }

    public void UpdateEquipStatusEffect()
    {
        if (!Configs.CharmsAffectBag || m_currentEquipStatus is not SE_Bag se) return;
        se.SetBag(m_currentBagItem);
    }

    public void SetFishingRodItem(string item)
    {
        m_fishingRodItem = item;
        if (m_nview.GetZDO() == null || !m_nview.IsOwner()) return;
        int fishingRodHash = string.IsNullOrEmpty(item) ? 0 : item.GetStableHashCode();
        m_nview.GetZDO().Set(BagVars.FishingRod, fishingRodHash);
    }

    public void SetCultivatorItem(string item)
    {
        m_cultivatorItem = item;
        if (m_nview.GetZDO() == null || !m_nview.IsOwner()) return;
        int cultivatorHash = string.IsNullOrEmpty(item) ? 0 : item.GetStableHashCode();
        m_nview.GetZDO().Set(BagVars.Cultivator, cultivatorHash);
    }

    public void SetHammerItem(string item)
    {
        m_hammerItem = item;
        if (m_nview.GetZDO() == null || !m_nview.IsOwner()) return;
        int hammerHash = string.IsNullOrEmpty(item) ? 0 : item.GetStableHashCode();
        m_nview.GetZDO().Set(BagVars.Hammer, hammerHash);
    }

    public void SetMeleeItem(string item)
    {
        m_meleeItem = item;
        if (m_nview.GetZDO() == null || !m_nview.IsOwner()) return;
        int meleeHash = string.IsNullOrEmpty(item) ? 0 : item.GetStableHashCode();
        m_nview.GetZDO().Set(BagVars.Melee, meleeHash);
    }

    public void SetAtgeirItem(string item)
    {
        m_atgeirItem = item;
        if (m_nview.GetZDO() == null || !m_nview.IsOwner()) return;
        int atgeirHash = string.IsNullOrEmpty(item) ? 0 : item.GetStableHashCode();
        m_nview.GetZDO().Set(BagVars.Atgeir, atgeirHash);
    }

    public void SetHoeItem(string item)
    {
        m_hoeItem = item;
        int hoeHash = string.IsNullOrEmpty(item) ? 0 : item.GetStableHashCode();
        if (m_nview.GetZDO() == null || !m_nview.IsOwner()) return;
        m_nview.GetZDO().Set(BagVars.Hoe, hoeHash);
    }

    public void SetQuiverItem(string item)
    {
        m_quiverItem = item;
        if (m_nview.GetZDO() == null || !m_nview.IsOwner()) return;
        int quiverHash = string.IsNullOrEmpty(item) ? 0 : item.GetStableHashCode();
        m_nview.GetZDO().Set(BagVars.Quiver, quiverHash);
    }

    public void SetArrowItem(string item, int stack, bool requireCentering = false)
    {
        m_arrowItem = item;
        m_arrowStack = stack;
        m_requireCentering = requireCentering;
        if (m_nview.GetZDO() == null || !m_nview.IsOwner()) return;
        int arrowHash = string.IsNullOrEmpty(item) ? 0 : item.GetStableHashCode();
        m_nview.GetZDO().Set(BagVars.Arrow, arrowHash);
        m_nview.GetZDO().Set(BagVars.ArrowStack, stack);
        m_nview.GetZDO().Set(BagVars.AutoCenter, requireCentering);
    }

    public void SetBagItem(string item)
    {
        m_bagItem = item;
        if (m_nview.GetZDO() == null || !m_nview.IsOwner()) return;
        int bagHash = string.IsNullOrEmpty(m_bagItem) ? 0 : m_bagItem.GetStableHashCode();
        m_nview.GetZDO().Set(BagVars.Bag, bagHash);
    }
    
    public void SetLanternItem(string item)
    {
        m_lanternItem = item;
        if (m_nview.GetZDO() == null || !m_nview.IsOwner()) return;
        int lanternHash = string.IsNullOrEmpty(item) ? 0 : item.GetStableHashCode();
        m_nview.GetZDO().Set(BagVars.Lantern, lanternHash);
    }

    public void SetPickaxeItem(string item)
    {
        m_pickaxeItem = item;
        if (m_nview.GetZDO() == null || !m_nview.IsOwner()) return;
        int pickaxeHash = string.IsNullOrEmpty(item) ? 0 : item.GetStableHashCode();
        m_nview.GetZDO().Set(BagVars.Pickaxe, pickaxeHash);
    }

    public void SetOreItem(string item, int stack)
    {
        m_oreItem = item;
        m_oreStack = stack;
        if (m_nview.GetZDO() == null || !m_nview.IsOwner()) return;
        int oreHash = string.IsNullOrEmpty(item) ? 0 : item.GetStableHashCode();
        m_nview.GetZDO().Set(BagVars.Ore, oreHash);
        m_nview.GetZDO().Set(BagVars.OreStack, stack);
    }

    public void SetScytheItem(string item)
    {
        m_scytheItem = item;
        if (m_nview.GetZDO() == null || !m_nview.IsOwner()) return;
        int scythHash = string.IsNullOrEmpty(item) ? 0 : item.GetStableHashCode();
        m_nview.GetZDO().Set(BagVars.Scyth, scythHash);
    }

    public void SetHarpoonItem(string item)
    {
        m_harpoonItem = item;
        if (m_nview.GetZDO() == null || !m_nview.IsOwner()) return;
        int harpoonHash = string.IsNullOrEmpty(item) ? 0 : item.GetStableHashCode();
        m_nview.GetZDO().Set(BagVars.Harpoon, harpoonHash);
    }
    
    private static GameObject? AttachItem(int hash, Transform joint)
    {
        Transform? attach = ObjectDB.instance?.GetItemPrefab(hash)?.transform.Find("attach");
        if (attach == null) return null;
        GameObject? go = Instantiate(attach.gameObject, joint);
        VisEquipment.CleanupInstance(go);
        go.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        return go;
    }

    public void ResetAttachInstances()
    {
        m_bagInstance = null;
        m_lanternInstance = null;
        m_pickaxeInstance = null;
        m_meleeInstance = null;
        m_atgeirInstance = null;
        m_hoeInstance = null;
        m_cultivatorInstance = null;
        m_hammerInstance = null;
        m_fishingRodInstance = null;
        m_oreInstances.Clear();
        m_scythInstance = null;
        m_harpoonInstance = null;
    }

    public void ResetAttachHashes()
    {
        m_currentLanternHash = 0;
        m_currentPickaxeHash = 0;
        m_currentFishingRodHash = 0;
        m_currentCultivatorHash = 0;
        m_currentHammerHash = 0;
        m_currentMeleeHash = 0;
        m_currentHoeHash = 0;
        m_currentAtgeirHash = 0;
        m_currentOreHash = 0;
        m_currentOreStack = 0;
        m_currentScythHash = 0;
        m_currentHarpoonHash = 0;
    }

    public void SetBagEquipped(int hash)
    {
        if (m_currentBagHash == hash) return;
        if (m_bagInstance != null)
        {
            Destroy(m_bagInstance);
            ResetAttachInstances();
        }
        m_currentBagHash = hash;
        ResetAttachHashes();
        if (hash == 0) return;
        m_bagInstance = m_visEquipment.AttachItem(hash, 0, m_visEquipment.m_backShield);
        if (m_bagInstance.transform.Find("hide") is {} hideOnEquip) hideOnEquip.gameObject.SetActive(false);
    }

    public void SetQuiverEquipped(int hash)
    {
        if (m_currentQuiverHash == hash) return;
        if (m_quiverInstance != null)
        {
            Destroy(m_quiverInstance);
            m_arrowInstances.Clear();
        }
        m_currentArrowHash = 0;
        m_currentArrowStack = 0;
        m_currentQuiverHash = hash;
        if (hash == 0) return;
        m_quiverInstance = m_visEquipment.AttachItem(hash, 0, m_visEquipment.m_backShield);
        if (m_quiverInstance.transform.Find("hide") is {} hideOnEquip) hideOnEquip.gameObject.SetActive(false);
    }

    public void SetHammerEquipped(int hash)
    {
        if (m_currentHammerHash == hash || m_bagInstance == null) return;
        if (m_hammerInstance) Destroy(m_hammerInstance);
        m_hammerInstance = null;
        m_currentHammerHash = hash;
        if (hash == 0 || m_bagInstance.transform.Find("attach_hammer") is not { } attachHammer) return;
        m_hammerInstance = AttachItem(hash, attachHammer);
    }

    public void SetAtgeirEquipped(int hash)
    {
        if (m_currentAtgeirHash == hash || m_bagInstance == null) return;
        if (m_atgeirInstance) Destroy(m_atgeirInstance);
        m_atgeirInstance = null;
        m_currentAtgeirHash = hash;
        if (hash == 0 || m_bagInstance.transform.Find("attach_atgeir") is not { } attachAtgeir) return;
        m_atgeirInstance = AttachItem(hash, attachAtgeir);
    }

    public void SetHarpoonEquipped(int hash)
    {
        if (m_currentHarpoonHash == hash || m_bagInstance == null) return;
        if (m_harpoonInstance) Destroy(m_harpoonInstance);
        m_harpoonInstance = null;
        m_currentHarpoonHash = hash;
        if (hash == 0 || m_bagInstance.transform.Find("attach_harpoon") is not { } attachHarpoon) return;
        m_harpoonInstance = AttachItem(hash, attachHarpoon);
    }

    public void SetMeleeEquipped(int hash)
    {
        if (m_currentMeleeHash == hash || m_bagInstance == null) return;
        if (m_meleeInstance) Destroy(m_meleeInstance);
        m_meleeInstance = null;
        m_currentMeleeHash = hash;
        if (hash == 0 || m_bagInstance.transform.Find("attach_melee") is not { } attachMelee) return;
        m_meleeInstance = AttachItem(hash, attachMelee);
    }

    public void SetHoeEquipped(int hash)
    {
        if (m_currentHoeHash == hash || m_bagInstance == null) return;
        if (m_hoeInstance) Destroy(m_hoeInstance);
        m_hoeInstance = null;
        m_currentHoeHash = hash;
        if (hash == 0 || m_bagInstance.transform.Find("attach_hoe") is not { } attachHoe) return;
        m_hoeInstance = AttachItem(hash, attachHoe);
    }

    public void SetFishingRodEquipped(int hash)
    {
        if (m_currentFishingRodHash == hash || m_bagInstance == null) return;
        if (m_fishingRodInstance) Destroy(m_fishingRodInstance);
        m_fishingRodInstance = null;
        m_currentFishingRodHash = hash;
        if (hash == 0 || m_bagInstance.transform.Find("attach_fishingrod") is not { } attachFishingRod) return;
        m_fishingRodInstance = AttachItem(hash, attachFishingRod);
    }
    
    public void SetLanternEquipped(int hash)
    {
        if (m_currentLanternHash == hash) return;
        if (m_bagInstance == null) return;
        if (m_lanternInstance) Destroy(m_lanternInstance);
        m_lanternInstance = null;
        m_currentLanternHash = hash;
        if (hash == 0 || m_bagInstance.transform.Find("attach_lantern") is not { } attachLantern) return;
        m_lanternInstance = m_visEquipment.AttachItem(hash, 0, attachLantern);
    }

    public void SetPickaxeEquipped(int hash)
    {
        if (m_currentPickaxeHash == hash || m_bagInstance == null) return;
        if (m_pickaxeInstance) Destroy(m_pickaxeInstance);
        m_pickaxeInstance = null;
        m_currentPickaxeHash = hash;
        if (hash == 0 || m_bagInstance.transform.Find("attach_pickaxe") is not {} attachPickaxe) return;
        m_pickaxeInstance = AttachItem(hash, attachPickaxe);
    }

    public void SetScytheEquipped(int hash)
    {
        if (m_currentScythHash == hash || m_bagInstance == null) return;
        if (m_scythInstance) Destroy(m_scythInstance);
        m_scythInstance = null;
        m_currentScythHash = hash;
        if (hash == 0 || m_bagInstance.transform.Find("attach_scyth") is not { } attachScyth) return;
        m_scythInstance = AttachItem(hash, attachScyth);
    }

    public void SetCultivatorEquipped(int hash)
    {
        if (m_currentCultivatorHash == hash || m_bagInstance == null) return;
        if (m_cultivatorInstance) Destroy(m_cultivatorInstance);
        m_cultivatorInstance = null;
        m_currentCultivatorHash = hash;
        if (hash == 0 || m_bagInstance.transform.Find("attach_cultivator") is not { } attachCultivator) return;
        m_cultivatorInstance = AttachItem(hash, attachCultivator);
    }

    public void ClearArrowInstances()
    {
        foreach (GameObject? instance in m_arrowInstances)
        {
            if (m_visEquipment.m_lodGroup) Utils.RemoveFromLodgroup(m_visEquipment.m_lodGroup, instance);
            Destroy(instance);
        }
        m_arrowInstances.Clear();
    }

    public void ClearOreInstances()
    {
        foreach (GameObject? instance in m_oreInstances)
        {
            if (instance) Destroy(instance);
        }
        m_oreInstances.Clear();
    }

    public void SetArrowEquipped(int hash, int stack, bool requireCentering)
    {
        if ((m_currentArrowHash == hash && m_currentArrowStack == stack) || m_quiverInstance == null) return;

        bool hashChanged = m_currentArrowHash != hash;
        int previousStack = m_currentArrowStack;
        
        m_currentArrowHash = hash;
        m_currentArrowStack = stack;

        if (hash == 0 || m_quiverInstance.transform.Find("attach_arrows") is not { } arrows)
        {
            ClearArrowInstances();
            return;
        }

        GameObject? model = ObjectDB.instance.GetItemPrefab(hash)?.transform.GetChild(0)?.gameObject;
        if (model == null)
        {
            ClearArrowInstances();
            return;
        }

        if (hashChanged)
        {
            ClearArrowInstances();
            int count = 0;

            foreach (Transform child in arrows)
            {
                if (count >= stack) break;
                GameObject? go = Instantiate(model, child);
                VisEquipment.CleanupInstance(go);
                go.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                CleanUpArrow(go);
                if (requireCentering) CenterArrowByBounds(go);
                m_arrowInstances.Add(go);
                ++count;
            }
        }
        else
        {
            int maxAttachPoints = arrows.childCount;
            int previousVisualCount = Mathf.Min(previousStack, maxAttachPoints);
            int currentVisualCount = Mathf.Min(m_currentArrowStack, maxAttachPoints);
            
            if (currentVisualCount < previousVisualCount)
            {
                int difference = previousVisualCount - currentVisualCount;
                for (int i = m_arrowInstances.Count - 1; i >= 0 && difference > 0; --i)
                {
                    GameObject? instance = m_arrowInstances[i];
                    if (m_visEquipment.m_lodGroup) Utils.RemoveFromLodgroup(m_visEquipment.m_lodGroup, instance);
                    Destroy(instance);
                    m_arrowInstances.RemoveAt(i);
                    --difference;
                }
            }
            else if (currentVisualCount > previousVisualCount)
            {
                int difference = currentVisualCount - previousVisualCount;
                foreach (Transform child in arrows)
                {
                    if (difference == 0) break;
                    if (child.childCount > 0) continue;
                    GameObject? go = Instantiate(model, child);
                    VisEquipment.CleanupInstance(go);
                    go.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                    CleanUpArrow(go);
                    if (requireCentering) CenterArrowByBounds(go);
                    m_arrowInstances.Add(go);
                    --difference;
                }
            }
        }
    }
    
    private static void CenterArrowByBounds(GameObject arrowInstance)
    {
        Renderer[] renderers = arrowInstance.GetComponentsInChildren<Renderer>();
    
        if (renderers.Length == 0) return;
    
        // Calculate combined bounds
        Bounds combinedBounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            combinedBounds.Encapsulate(renderers[i].bounds);
        }
    
        // Get world space center
        Vector3 boundsCenter = combinedBounds.center;
    
        // Convert to local space of the arrow instance
        Vector3 localOffset = arrowInstance.transform.InverseTransformPoint(boundsCenter);
    
        Vector3 correction = new Vector3(
            -localOffset.x,  // Center horizontally
            -localOffset.y,  // Center vertically
            -localOffset.z     // Align to back of arrow (nock end)
        );
    
        arrowInstance.transform.localPosition = correction;
    }

    private static void CleanUpArrow(GameObject go)
    {
        if (go.GetComponentInChildren<Light>() is { } light) light.enabled = false;
        if (go.GetComponentInChildren<ParticleSystem>() is {} ps) ps.Stop();
    }

    public void SetOreEquipped(int hash, int stack)
    {
        if ((m_currentOreHash == hash && m_currentOreStack == stack) || m_bagInstance == null) return;

        bool hashChanged = m_currentOreHash != hash;
        int previousStack = m_currentOreStack;
        
        m_currentOreHash = hash;
        m_currentOreStack = stack;
        bool open = stack > 0;
        
        if (hash == 0 || m_bagInstance.transform.Find("attach_ores") is not { } ores)
        {
            ClearOreInstances();
            m_bagInstance.transform.Find("open")?.gameObject.SetActive(false);
            m_bagInstance.transform.Find("closed")?.gameObject.SetActive(true);
            return;
        }

        m_bagInstance.transform.Find("open")?.gameObject.SetActive(open);
        m_bagInstance.transform.Find("closed")?.gameObject.SetActive(!open);
        
        GameObject? model = ObjectDB.instance.GetItemPrefab(hash)?.transform.GetChild(0)?.gameObject;
        if (model == null)
        {
            ClearOreInstances();
            return;
        }
        
        if (hashChanged)
        {
            ClearOreInstances();
            int count = 0;

            foreach (Transform child in ores)
            {
                if (count >= stack) break;
                GameObject? go = Instantiate(model, child);
                VisEquipment.CleanupInstance(go);
                go.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                m_oreInstances.Add(go);
                ++count;
            }
        }
        else
        {
            int maxAttachPoints = ores.childCount;
            int previousVisualCount = Mathf.Min(previousStack, maxAttachPoints);
            int currentVisualCount = Mathf.Min(m_currentOreStack, maxAttachPoints);
            
            if (currentVisualCount < previousVisualCount)
            {
                int difference = previousVisualCount - currentVisualCount;
                for (int i = m_oreInstances.Count - 1; i >= 0 && difference > 0; --i)
                {
                    GameObject? instance = m_oreInstances[i];
                    Destroy(instance);
                    m_oreInstances.RemoveAt(i);
                    --difference;
                }
            }
            else if (currentVisualCount > previousVisualCount)
            {
                int difference = currentVisualCount - previousVisualCount;
                foreach (Transform child in ores)
                {
                    if (difference == 0) break;
                    if (child.childCount > 0) continue;
                    GameObject? go = Instantiate(model, child);
                    VisEquipment.CleanupInstance(go);
                    go.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                    m_oreInstances.Add(go);
                    --difference;
                }
            }
        }
    }
    
    public void UpdateVisuals()
    {
        ZDO? zdo = m_nview.GetZDO();
        int bagHash = zdo?.GetInt(BagVars.Bag) ?? (string.IsNullOrEmpty(m_bagItem) ? 0 : m_bagItem.GetStableHashCode());
        int quiverHash = zdo?.GetInt(BagVars.Quiver) ?? (string.IsNullOrEmpty(m_quiverItem) ? 0 : m_quiverItem.GetStableHashCode());
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
        int oreHash = zdo?.GetInt(BagVars.Ore) ?? (string.IsNullOrEmpty(m_oreItem) ? 0 : m_oreItem.GetStableHashCode());
        int oreStack = zdo?.GetInt(BagVars.OreStack) ?? m_oreStack;
        int scythHash = zdo?.GetInt(BagVars.Scyth) ?? (string.IsNullOrEmpty(m_scytheItem) ? 0 : m_scytheItem.GetStableHashCode());
        int harpoonHash = zdo?.GetInt(BagVars.Harpoon) ?? (string.IsNullOrEmpty(m_harpoonItem) ? 0 : m_harpoonItem.GetStableHashCode());
        bool requireCentering = zdo?.GetBool(BagVars.AutoCenter) ?? m_requireCentering;
        
        SetBagEquipped(bagHash);
        SetQuiverEquipped(quiverHash);
        SetLanternEquipped(lanternHash);
        SetPickaxeEquipped(pickaxeHash);
        SetArrowEquipped(arrowHash, arrowStack, requireCentering);
        SetFishingRodEquipped(fishingRodHash);
        SetCultivatorEquipped(cultivatorHash);
        SetHammerEquipped(hammerHash);
        SetHoeEquipped(hoeHash);
        SetMeleeEquipped(meleeHash);
        SetAtgeirEquipped(atgeirHash);
        SetOreEquipped(oreHash, oreStack);
        SetScytheEquipped(scythHash);
        SetHarpoonEquipped(harpoonHash);
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
            if (item is Quiver quiver)
            {
                __result = component.IsQuiverEquipped(quiver);
            }
            else
            {
                __result = component.IsBagEquipped(bag);
            }
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

            if (item is Quiver quiver)
            {
                __result = component.SetQuiver(quiver);
            }
            else
            {
                __result = component.SetBag(bag);
            }
            
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
            if (item is Quiver quiver)
            {
                if (!component.IsQuiverEquipped(quiver)) return false;
                component.SetQuiver(null);
            }
            else
            {
                if (!component.IsBagEquipped(bag)) return false;
                component.SetBag(null);
            }
            if (triggerEquipEffects) __instance.TriggerEquipEffect(item);
            return false;
        }
    }
    
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UnequipAllItems))]
    private static class Humanoid_UnequipAllItems_Patch
    {
        [UsedImplicitly]
        private static void Prefix(Humanoid __instance)
        {
            if (!__instance.TryGetComponent(out BagEquipment component)) return;
            component.SetBag(null);
            component.SetQuiver(null);
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.EquipInventoryItems))]
    private static class Player_EquipInventoryItems_Patch
    {
        [UsedImplicitly]
        private static void Postfix(Player __instance)
        {
            foreach (var item in __instance.m_inventory.GetAllItems())
            {
                if (item is not Bag bag) continue;
                // make sure even unequipped bags are loaded to account for total player inventory weight
                bag.Load();
            }
        }
    }
}

public static class BagVars
{
    public static readonly int Quiver = "ExtraSlot.Quiver".GetStableHashCode();
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
    public static readonly int Ore = "ExtraSlot.Bag.Ore".GetStableHashCode();
    public static readonly int OreStack = "ExtraSlot.Bag.OreStack".GetStableHashCode();
    public static readonly int Scyth = "ExtraSlot.Bag.Scyth".GetStableHashCode();
    public static readonly int Harpoon = "ExtraSlot.Bag.Harpoon".GetStableHashCode();
    public static readonly int AutoCenter = "ExtraSlot.Quiver.AutoCenter".GetStableHashCode();
}