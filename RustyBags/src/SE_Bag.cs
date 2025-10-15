
using System.Text;
using BepInEx.Configuration;
using RustyBags.Managers;
using RustyBags.Utilities;
using UnityEngine;

namespace RustyBags;

public class SE_Bag : SE_Stats
{
    private static readonly StringBuilder sb = new();

    public BagSetup data = null!;
    public int m_quality = 1;
    public float m_baseCarryWeight;
    public float m_carryWeightPerQuality = 10f;
    public float m_inventoryWeightModifier;
    
    private ConfigEntry<float>? baseCarryWeightCfg;
    private ConfigEntry<float>? inventoryWeightModifierCfg;
    private float baseCarryWeight => baseCarryWeightCfg?.Value ?? m_baseCarryWeight;
    private float inventoryWeightModifier => inventoryWeightModifierCfg?.Value ?? m_inventoryWeightModifier;

    public override void SetLevel(int itemLevel, float skillLevel)
    {
        m_quality = itemLevel;
        if (baseCarryWeight > 0f) m_addMaxCarryWeight = baseCarryWeight + m_quality * m_carryWeightPerQuality;
    }

    public void ModifyInventoryWeight(ref float weight)
    {
        weight *= Mathf.Max(1f - inventoryWeightModifier, 0f);
    }

    public void SetupConfigs()
    {
        baseCarryWeightCfg = Configs.config(data.englishName, "Base Carry Weight", m_baseCarryWeight, "Setup base carry weight, increase by 10 per item quality");
        inventoryWeightModifierCfg = Configs.config(data.englishName, "Inventory Weight Modifier", m_inventoryWeightModifier,
            new ConfigDescription("Setup inventory weight multiplier", new AcceptableValueRange<float>(0f, 1f)));
    }

    public override string GetTooltipString()
    {
        sb.Clear();
        string tooltip = base.GetTooltipString();
        sb.Append(tooltip);
        if (m_inventoryWeightModifier != 0f)
        {
            sb.AppendFormat("{0}: <color=orange>{1:+0;-0}%</color>\n", Keys.BagWeight, (inventoryWeightModifier - 1f) * 100);
        }
        if (data.sizes.TryGetValue(m_quality, out var size))
        {
            sb.AppendFormat("{0}: <color=orange>{1}x{2}</color>", Keys.InventorySize, size.width, size.height);
        }

        return sb.ToString();
    }
}