
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BepInEx.Configuration;
using RustyBags.Managers;
using RustyBags.Utilities;
using UnityEngine;

namespace RustyBags;

public class SE_Bag : SE_Stats
{
    protected static readonly StringBuilder sb = new();

    public BagSetup data = null!;
    public int m_quality = 1;
    public float m_baseCarryWeight;
    public float m_carryWeightPerQuality = 10f;
    public float m_inventoryWeightModifier;
    
    private ConfigEntry<float>? baseCarryWeightCfg;
    private ConfigEntry<float>? inventoryWeightModifierCfg;
    private ConfigEntry<float>? movementSpeedModifierCfg;
    private float baseCarryWeight => baseCarryWeightCfg?.Value ?? m_baseCarryWeight;
    protected float inventoryWeightModifier => inventoryWeightModifierCfg?.Value ?? m_inventoryWeightModifier;

    public override void SetLevel(int itemLevel, float skillLevel)
    {
        m_quality = itemLevel;
        m_percentigeDamageModifiers = new();
        m_addMaxCarryWeight = 0f;
        m_speedModifier = 0f;
        m_eitrRegenMultiplier = 1f;
        m_swimStaminaUseModifier = 0f;
        m_homeItemStaminaUseModifier = 0f;
        m_swimSpeedModifier = 0f;

        m_addMaxCarryWeight += baseCarryWeight;
        if (baseCarryWeight > 0f) m_addMaxCarryWeight += m_quality * m_carryWeightPerQuality;
        m_speedModifier += movementSpeedModifierCfg?.Value ?? 0f;
        
        AddBagAttachmentModifiers();
    }

    private void AddBagAttachmentModifiers()
    {
        m_addMaxCarryWeight += m_currentCharm?.carryWeight ?? 0f;
        m_speedModifier += m_currentCharm?.speed ?? 0f;
        if (m_currentCharm != null) m_percentigeDamageModifiers.Add(m_currentCharm.damageModifiers);
        m_eitrRegenMultiplier += m_currentCharm?.eitrRegen ?? 0f;
        m_swimSpeedModifier = m_currentBag?.harpoon == null ? 0f : 0.1f;
        m_homeItemStaminaUseModifier = !HasHomeItem() ? 0f : -0.1f;
        m_addMaxCarryWeight += m_currentBag?.pickaxe == null ? 0f : 10f;
        if (m_currentBag?.atgeir != null) m_percentigeDamageModifiers.m_damage += 0.05f;
        m_swimStaminaUseModifier += m_currentBag?.fishingRod == null ? 0f : -0.1f;
    }

    private bool HasHomeItem() => m_currentBag?.hammer != null || m_currentBag?.hoe != null ||
                                  m_currentBag?.cultivator != null || m_currentBag?.scythe != null;

    private Charm? m_currentCharm;
    private Bag? m_currentBag;

    public void SetBag(Bag? bag)
    {
        m_currentBag = bag;
        m_currentCharm = bag?.lantern?.GetCharmData();
        SetLevel(m_quality, 0f);
    }

    public virtual void ModifyInventoryWeight(Inventory inventory, ref float weight)
    {
        weight *= Mathf.Max(1f - inventoryWeightModifier, 0f);
    }

    public void SetupConfigs()
    {
        baseCarryWeightCfg = Configs.config(data.englishName, "Base Carry Weight", m_baseCarryWeight, "Setup base carry weight, increase by 10 per item quality");
        inventoryWeightModifierCfg = Configs.config(data.englishName, "Inventory Weight Modifier", m_inventoryWeightModifier, new ConfigDescription("Setup inventory weight multiplier", new AcceptableValueRange<float>(0f, 1f)));
        movementSpeedModifierCfg = Configs.config(data.englishName, "Movement Speed", m_speedModifier, "Set speed modifier");
    }

    public override string GetTooltipString()
    {
        sb.Clear();
        string tooltip = base.GetTooltipString();
        sb.Append(tooltip);
        AddInventoryWeightTooltip();
        if (data.sizes.TryGetValue(m_quality, out BagSetup.Size? size))
        {
            sb.AppendFormat("{0}: <color=orange>{1}x{2}</color>", Keys.InventorySize, size.width, size.height);
        }
        return sb.ToString();
    }

    public virtual void AddInventoryWeightTooltip()
    {
        if (m_inventoryWeightModifier == 0f) return;
        sb.AppendFormat("{0}: <color=orange>{1:+0;-0}%</color>\n", Keys.BagWeight, (inventoryWeightModifier - 1f) * 100);
    }
}

public class SE_OreBag : SE_Bag
{
    private static readonly HashSet<string> _ores = new();

    public static HashSet<string> ores
    {
        get
        {
            if (_ores.Count > 0 || !ZNetScene.instance) return _ores;
            IEnumerable<Smelter> smelters = ZNetScene.instance.m_prefabs.Select(x => x.GetComponent<Smelter>()).Where(x => x != null);
            foreach (Smelter? smelter in smelters)
            {
                foreach (Smelter.ItemConversion? conversion in smelter.m_conversion)
                {
                    if (conversion.m_from == null || conversion.m_from.m_itemData.m_shared.m_teleportable) continue;
                    _ores.Add(conversion.m_from.m_itemData.m_shared.m_name);
                }
            }
            return _ores;
        }
    }
    public override void ModifyInventoryWeight(Inventory inventory, ref float weight)
    {
        float total = 0f;
        List<ItemDrop.ItemData>? list = inventory.GetAllItems();
        for (int index = 0; index < list.Count; ++index)
        {
            ItemDrop.ItemData? item = list[index];
            var w = item.GetWeight();
            if (ores.Contains(item.m_shared.m_name))
            {
                w *= Mathf.Max(1f - inventoryWeightModifier, 0f);
            }

            total += w;
        }

        weight = total;
    }
    
    public override void AddInventoryWeightTooltip()
    {
        if (m_inventoryWeightModifier != 0f)
        {
            sb.AppendFormat("{0}: <color=orange>{1:+0;-0}%</color>\n", Keys.OreWeight, (inventoryWeightModifier - 1f) * 100);
        }
    }
    
}