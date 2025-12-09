using System.Collections.Generic;
using System.Linq;

namespace RustyBags.Utilities;

public static class Keys
{
    private static readonly Dictionary<string, string> keys = new();
    
    public static void Write()
    {
        List<string> lines = new();
        foreach (KeyValuePair<string, string> kvp in keys.OrderBy(x => x.Key))
        {
            lines.Add($"{kvp.Key}: \"{kvp.Value}\"");
        }
        RustyBagsPlugin.BagDir.WriteAllLines($"{RustyBagsPlugin.ModName}.English.yml", lines);
    }

    public class Key
    {
        public readonly string key;

        public Key(string key, string english)
        {
            this.key = key;
            keys[key.Replace("$", string.Empty)] = english;
        }
    }

    public static readonly string BagWeight = new Key("$se_bagweight", "Bag Weight").key;
    public static readonly string InventorySize = new Key("$se_inventorysize", "Inventory Size").key;
    public static readonly string CannotStackBags = new Key("$msg_cannotstackbags", "Cannot stack bags").key;
    public static readonly string Only = new Key("$msg_only", "Only").key;
    public static readonly string Allowed = new Key("$msg_allowed", "allowed").key;
    public static readonly string OreWeight = new Key("$se_oreweight", "Ore Weight").key;
    public static readonly string Bag = new Key("$label_bag", "Bag").key;
    public static readonly string Quiver = new Key("$label_quiver_rs", "Quiver Bag").key;
    public static readonly string IfEquippedToBag = new Key("$se_charm_equipped", "If equipped to bag").key;
    public static readonly string Hide = new Key("$bag_hide", "Hide").key;
    public static readonly string Show = new Key("$bag_show", "Show").key;
    public static readonly string Auto = new Key("$bag_auto", "Auto").key;
    public static readonly string Manual = new Key("$bag_manual", "Manual").key;
    
    public const string Blunt = "$inventory_blunt";
    public const string Slash = "$inventory_slash";
    public const string Pierce = "$inventory_pierce";
    public const string Chop = "$inventory_chop";
    public const string Pickaxe = "$inventory_pickaxe";
    public const string Fire = "$inventory_fire";
    public const string Frost = "$inventory_frost";
    public const string Lightning = "$inventory_lightning";
    public const string Poison = "$inventory_poison";
    public const string Spirit = "$inventory_spirit";
    public const string FallDamage = "$se_falldamage";
    public const string HealthRegen = "$se_healthregen";
    public const string StaminaRegen = "$se_staminaregen";
    public const string EitRegen = "$se_eitregen";
    public const string Damage = "$inventory_damage";
    public const string HomeItemModifier = "$item_homeitem_modifier";
    public const string HeatModifier = "$item_heat_modifier";
    public const string JumpStamina = "$se_jumpstamina";
    public const string AttackStamina = "$se_attackstamina";
    public const string BlockStamina = "$se_blockstamina";
    public const string DodgeStamina = "$se_dodgestamina";
    public const string SwimStamina = "$se_swimstamina";
    public const string SneakStamina = "$se_sneakstamina";
    public const string RunStamina = "$se_runstamina";
    public const string MovementModifier = "$item_movement_modifier";
    public const string EitrRegenModifier = "$item_eitrregen_modifier";
    public const string BlockForce = "$item_blockforce";
    public const string ParryBonus = "$item_parrybonus";
    public const string DLC = "$item_dlc";
    public const string WorldLevel = "$item_newgameplusitem";
    public const string Crafter = "$item_crafter";
    public const string NoTeleport = "$item_noteleport";
    public const string Trophies = "$inventory_trophies";
    public const string Swords = "$skill_swords";
    public const string Knives = "$skill_knives";
    public const string Clubs = "$skill_clubs";
    public const string Polearms = "$skill_polearms";
    public static readonly string Blocking = "$skill_blocking";
    public const string Axes = "$skill_axes";
    public const string Bows = "$skill_bows";
    public static readonly string ElementalMagic = "$skill_elementalmagic";
    public static readonly string BloodMagic = "$skill_bloodmagic";
    public static readonly string All = "$skill_all";
    public static readonly string Cooking = "$skill_cooking";
    public static readonly string Crafting = "$skill_crafting";
    public static readonly string Crossbows = "$skill_crossbows";
    public static readonly string Farming = "$skill_farming";
    public static readonly string Fishing = "$skill_fishing";
    public const string Jump = "$skill_jump";
    public static readonly string Pickaxes = "$skill_pickaxes";
    public static readonly string Ride = "$skill_ride";
    public static readonly string Run = "$skill_run";
    public static readonly string Sneak = "$skill_sneak";
    public const string Spears = "$skill_spears";
    public static readonly string Swim = "$skill_swim";
    public static readonly string Unarmed ="$skill_unarmed";
    public static readonly string WoodCutting = "$skill_woodcutting";
    public static readonly string Adrenaline = "$item_fulladrenaline";
    public const string EitrUse = "$item_eitruse";
    public const string HealthUse = "$item_healthuse";
    public const string HealthUsePercentage = "$item_healthuse_percentage";
    public const string StaminaHold = "$item_staminahold";
    public const string Knockback = "$item_knockback";
    public const string Backstab =  "$item_backstab";
    public const string Health = "$se_health";
    public const string Stamina = "$item_food_stamina";
    public const string Eitr = "$item_food_eitr";
    public const string Healing = "$item_food_regen";
    public const string Armor = "$item_armor";
    public const string Leech = "$enemy_leech";
    
}