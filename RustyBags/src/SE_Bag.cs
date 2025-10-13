
using System.Text;
using RustyBags.Utilities;
using UnityEngine;

namespace RustyBags;

public class SE_Bag : SE_Stats
{
    private static readonly StringBuilder sb = new();

    public BagSetup data = null!;
    public int m_quality = 1;
    public float m_baseCarryWeight;
    public float m_inventoryWeightModifier;

    public override void SetLevel(int itemLevel, float skillLevel)
    {
        m_quality = itemLevel;
        if (m_baseCarryWeight > 0f) m_addMaxCarryWeight = m_baseCarryWeight + m_quality * 10;
    }

    public void ModifyInventoryWeight(ref float weight)
    {
        weight *= Mathf.Max(1f - m_inventoryWeightModifier, 0f);
    }

    public override string GetTooltipString()
    {
        sb.Clear();
        string tooltip = base.GetTooltipString();
        sb.Append(tooltip);
        if (m_inventoryWeightModifier != 0f)
        {
            sb.AppendFormat("{0}: <color=orange>{1:+0;-0}%</color>\n", Keys.BagWeight, (m_inventoryWeightModifier - 1f) * 100);
        }
        if (data.sizes.TryGetValue(m_quality, out var size))
        {
            sb.AppendFormat("{0}: <color=orange>{1}x{2}</color>", Keys.InventorySize, size.width, size.height);
        }

        return sb.ToString();
    }
}