using System;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using JetBrains.Annotations;
using ServerSync;

namespace RustyBags.Managers;

public enum Toggle { On = 1, Off = 0 }

public static class Configs
{
    private static ConfigEntry<Toggle> _serverConfigLocked = null!;
    private static ConfigEntry<Toggle> _autoStack = null!;
    private static ConfigEntry<Toggle> _multipleBags = null!;
    private static ConfigEntry<Toggle> _craftFromBag = null!;
    private static ConfigEntry<Toggle> _charmsAffectBag = null!;
    private static ConfigEntry<Toggle> _autoOpen = null!;
    private static ConfigEntry<Toggle> _hideBag = null!;

    public static bool AutoStack => _autoStack.Value is Toggle.On;
    public static bool MultipleBags => _multipleBags.Value is Toggle.On;
    public static bool CraftFromBag => _craftFromBag.Value is Toggle.On;
    
    public static bool CharmsAffectBag => _charmsAffectBag.Value is Toggle.On;
    public static bool AutoOpen => _autoOpen.Value is Toggle.On;

    public static bool HideBag => _hideBag.Value is Toggle.On;

    public static void Setup()
    {
        _serverConfigLocked = config("1 - General", "Lock Configuration", Toggle.On, "If on, the configuration is locked and can be changed by server admins only.");
        _ = RustyBagsPlugin.ConfigSync.AddLockingConfigEntry(_serverConfigLocked);
        _autoStack = config("1 - General", "Stack Into Bag", Toggle.On, "If on, equipped bag will try to stack items on pickup", false);
        _multipleBags = config("1 - General", "Multiple Bags", Toggle.Off, "If on, player can carry multiple bags");
        _craftFromBag = config("1 - General", "Craft From Bag", Toggle.On, "If on, player can build and craft with equipped bag contents");
        _charmsAffectBag = config("1 - General", "Attachment Bonuses", Toggle.Off, "If on, bag attachments affect bag");
        _autoOpen = config("1 - General", "Auto-Open", Toggle.On, "If on, bag will open alongside inventory, else hover over bag to open");
        _hideBag = config("1 - General", "Hide Bag", Toggle.Off, "If on, bag will be hidden");
        
        foreach(BagSetup? bagSetup in BagSetup.bags.Values) bagSetup.SetupConfigs();
        SetupWatcher();
    }
    
    private static void SetupWatcher()
    {
        FileSystemWatcher watcher = new(Paths.ConfigPath, RustyBagsPlugin.ConfigFileName);
        watcher.Changed += ReadConfigValues;
        watcher.Created += ReadConfigValues;
        watcher.Renamed += ReadConfigValues;
        watcher.IncludeSubdirectories = true;
        watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
        watcher.EnableRaisingEvents = true;
    }

    private static void ReadConfigValues(object sender, FileSystemEventArgs e)
    {
        if (!File.Exists(RustyBagsPlugin.ConfigFileFullPath)) return;
        try
        {
            RustyBagsPlugin.RustyBagsLogger.LogDebug("ReadConfigValues called");
            RustyBagsPlugin.instance.Config.Reload();
        }
        catch
        {
            RustyBagsPlugin.RustyBagsLogger.LogError($"There was an issue loading your {RustyBagsPlugin.ConfigFileName}");
            RustyBagsPlugin.RustyBagsLogger.LogError("Please check your config entries for spelling and format!");
        }
    }

    public static ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description,
        bool synchronizedSetting = true)
    {
        ConfigDescription extendedDescription =
            new(
                description.Description +
                (synchronizedSetting ? " [Synced with Server]" : " [Not Synced with Server]"),
                description.AcceptableValues, description.Tags);
        ConfigEntry<T> configEntry = RustyBagsPlugin.instance.Config.Bind(group, name, value, extendedDescription);
        //var configEntry = Config.Bind(group, name, value, description);

        SyncedConfigEntry<T> syncedConfigEntry = RustyBagsPlugin.ConfigSync.AddConfigEntry(configEntry);
        syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

        return configEntry;
    }

    public static ConfigEntry<T> config<T>(string group, string name, T value, string description,
        bool synchronizedSetting = true)
    {
        return config(group, name, value, new ConfigDescription(description), synchronizedSetting);
    }

    public class ConfigurationManagerAttributes
    {
        [UsedImplicitly] public int? Order = null!;
        [UsedImplicitly] public bool? Browsable = null!;
        [UsedImplicitly] public string? Category = null!;
        [UsedImplicitly] public Action<ConfigEntryBase>? CustomDrawer = null!;
    }

    class AcceptableShortcuts : AcceptableValueBase
    {
        public AcceptableShortcuts() : base(typeof(KeyboardShortcut))
        {
        }

        public override object Clamp(object value) => value;
        public override bool IsValid(object value) => true;

        public override string ToDescriptionString() =>
            "# Acceptable values: " + string.Join(", ", UnityInput.Current.SupportedKeyCodes);
    }
}