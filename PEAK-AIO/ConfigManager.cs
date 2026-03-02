using BepInEx.Logging;
using BepInEx.Configuration;
using UnityEngine;

public static class ConfigManager
{
    public static ManualLogSource Logger;

    // Cheats
    public static ConfigEntry<bool> InfiniteStamina;
    public static ConfigEntry<bool> FlyMod;
    public static ConfigEntry<float> FlySpeed;
    public static ConfigEntry<float> FlyAcceleration;

    // Afflictions
    public static ConfigEntry<bool> LockStatus;
    public static ConfigEntry<bool> NoWeight;

    // Character Toggles
    public static ConfigEntry<bool> SpeedMod;
    public static ConfigEntry<bool> JumpMod;
    public static ConfigEntry<bool> NoFallDmg;
    public static ConfigEntry<bool> ClimbMod;
    public static ConfigEntry<bool> VineClimbMod;
    public static ConfigEntry<bool> RopeClimbMod;

    // Character Amounts
    public static ConfigEntry<float> SpeedAmount;
    public static ConfigEntry<float> JumpAmount;
    public static ConfigEntry<float> ClimbAmount;
    public static ConfigEntry<float> VineClimbAmount;
    public static ConfigEntry<float> RopeClimbAmount;

    // Inventory Recharge
    public static ConfigEntry<float> RechargeAmountSlot1;
    public static ConfigEntry<float> RechargeAmountSlot2;
    public static ConfigEntry<float> RechargeAmountSlot3;

    // Teleport
    public static ConfigEntry<bool> TeleportToPing;

    // General
    public static ConfigEntry<KeyCode> MenuToggleKey;

    // Language
    public static ConfigEntry<int> LanguageIndex;

    public static void Init(ConfigFile config, ManualLogSource logger)
    {
        Logger = logger;

        // General
        MenuToggleKey = config.Bind("General", "MenuToggleKey", KeyCode.Insert,
            "Key to toggle the mod menu overlay. Uses UnityEngine.KeyCode names (e.g. Insert, F1, F2, Home, RightShift).");

        // Cheats
        InfiniteStamina = config.Bind("Cheats", "InfiniteStamina", false, "Enable infinite stamina");
        TeleportToPing = config.Bind("Cheats", "TeleportToPing", false, "Automatically teleport to ping location");
        FlyMod = config.Bind("Cheats", "Fly Mod", false, "Enables fly mode when checked.");
        FlySpeed = config.Bind("Cheats", "Fly Speed", 100f, "Speed used when flying.");
        FlyAcceleration = config.Bind("Cheats", "Fly Acceleration", 300f, "Acceleration used when flying.");

        // Afflictions
        LockStatus = config.Bind("Afflictions", "LockStatus", false);
        NoWeight = config.Bind("Afflictions", "NoWeight", false);

        // Character Toggles
        SpeedMod = config.Bind("Character", "SpeedMod", false);
        JumpMod = config.Bind("Character", "JumpMod", false);
        NoFallDmg = config.Bind("Character", "NoFallDmg", false);
        ClimbMod = config.Bind("Character", "ClimbMod", false);
        VineClimbMod = config.Bind("Character", "VineClimbMod", false);
        RopeClimbMod = config.Bind("Character", "RopeClimbMod", false);

        // Character Amounts
        SpeedAmount = config.Bind("Character", "SpeedAmount", 1.0f);
        JumpAmount = config.Bind("Character", "JumpAmount", 10.0f);
        ClimbAmount = config.Bind("Character", "ClimbAmount", 1.0f);
        VineClimbAmount = config.Bind("Character", "VineClimbAmount", 1.0f);
        RopeClimbAmount = config.Bind("Character", "RopeClimbAmount", 1.0f);

        // Inventory 
        RechargeAmountSlot1 = config.Bind("Inventory", "RechargeAmountSlot1", 100f, new ConfigDescription("Recharge amount for slot 1", new AcceptableValueRange<float>(0f, 999f)));
        RechargeAmountSlot2 = config.Bind("Inventory", "RechargeAmountSlot2", 100f, new ConfigDescription("Recharge amount for slot 2", new AcceptableValueRange<float>(0f, 999f)));
        RechargeAmountSlot3 = config.Bind("Inventory", "RechargeAmountSlot3", 100f, new ConfigDescription("Recharge amount for slot 3", new AcceptableValueRange<float>(0f, 999f)));

        // Language
        LanguageIndex = config.Bind("UI", "Language", 0, new ConfigDescription("Language: 0=English, 1=简体中文, 2=日本語, 3=한국어", new AcceptableValueRange<int>(0, 3)));
        Localization.SetLanguage(LanguageIndex.Value);

        Logger.LogInfo("[PEAK AIO][ConfigManager] Config Loaded.");
    }
}
