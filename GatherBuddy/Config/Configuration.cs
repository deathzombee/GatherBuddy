﻿using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Configuration;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.Text;
using GatherBuddy.Enums;
using OtterGui;

namespace GatherBuddy.Config;

public partial class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 4;

    // Set Names
    public string BotanistSetName { get; set; } = "BTN";
    public string MinerSetName    { get; set; } = "MIN";
    public string FisherSetName   { get; set; } = "FSH";

    // formats
    public string IdentifiedGatherableFormat { get; set; } = DefaultIdentifiedGatherableFormat;
    public string AlarmFormat                { get; set; } = DefaultAlarmFormat;


    // Interface
    public AetherytePreference AetherytePreference { get; set; } = AetherytePreference.Distance;
    public ItemFilter          ShowItems           { get; set; } = ItemFilter.All;
    public FishFilter          ShowFish            { get; set; } = FishFilter.All;
    public PatchFlag           HideFishPatch       { get; set; } = 0;
    public JobFlags            LocationFilter      { get; set; } = (JobFlags)0x3F;


    // General Config
    public bool             OpenOnStart            { get; set; } = false;
    public bool             MainWindowLockPosition { get; set; } = false;
    public bool             MainWindowLockResize   { get; set; } = false;
    public bool             UseGearChange          { get; set; } = true;
    public bool             UseTeleport            { get; set; } = true;
    public bool             UseCoordinates         { get; set; } = true;
    public bool             WriteCoordinates       { get; set; } = true;
    public bool             PrintUptime            { get; set; } = true;
    public bool             SkipTeleportIfClose    { get; set; } = true;
    public XivChatType      ChatTypeMessage        { get; set; } = XivChatType.Echo;
    public XivChatType      ChatTypeError          { get; set; } = XivChatType.ErrorMessage;
    public bool             AddIngameContextMenus  { get; set; } = true;
    public bool             StoreFishRecords       { get; set; } = true;
    public bool             PrintClipboardMessages { get; set; } = true;
    public bool             HideClippy             { get; set; } = false;
    public ModifiableHotkey MainInterfaceHotkey    { get; set; } = new();

    // Weather tab
    public bool ShowWeatherNames { get; set; } = true;

    // Alarms
    public bool AlarmsEnabled          { get; set; } = false;
    public bool AlarmsInDuty           { get; set; } = true;
    public bool AlarmsOnlyWhenLoggedIn { get; set; } = false;

    // Colors
    public Dictionary<ColorId, uint> Colors { get; set; }
        = Enum.GetValues<ColorId>().ToDictionary(c => c, c => c.Data().DefaultColor);

    public int SeColorNames     = DefaultSeColorNames;
    public int SeColorCommands  = DefaultSeColorCommands;
    public int SeColorArguments = DefaultSeColorArguments;
    public int SeColorAlarm     = DefaultSeColorAlarm;

    // Fish Timer
    public bool   ShowFishTimer        { get; set; } = true;
    public bool   FishTimerEdit        { get; set; } = true;
    public bool   HideUncaughtFish     { get; set; } = false;
    public bool   HideUnavailableFish  { get; set; } = false;
    public bool   ShowFishTimerUptimes { get; set; } = true;
    public bool   HideFishSizePopup    { get; set; } = false;
    public ushort FishTimerScale       { get; set; } = 40000;

    // Spearfish Helper
    public bool ShowSpearfishHelper          { get; set; } = true;
    public bool ShowSpearfishNames           { get; set; } = true;
    public bool ShowAvailableSpearfish       { get; set; } = true;
    public bool ShowSpearfishSpeed           { get; set; } = false;
    public bool ShowSpearfishCenterLine      { get; set; } = true;
    public bool ShowSpearfishListIconsAsText { get; set; } = false;
    public bool FixNamesOnPosition           { get; set; } = false;
    public byte FixNamesPercentage           { get; set; } = 55;


    // Gather Window
    public bool             ShowGatherWindow               { get; set; } = true;
    public bool             ShowGatherWindowTimers         { get; set; } = true;
    public bool             ShowGatherWindowAlarms         { get; set; } = true;
    public bool             SortGatherWindowByUptime       { get; set; } = false;
    public bool             ShowGatherWindowOnlyAvailable  { get; set; } = false;
    public bool             HideGatherWindowInDuty         { get; set; } = true;
    public bool             OnlyShowGatherWindowHoldingKey { get; set; } = false;
    public bool             LockGatherWindow               { get; set; } = false;
    public bool             GatherWindowBottomAnchor   { get; set; } = false;
    public ModifiableHotkey GatherWindowHotkey             { get; set; } = new(VirtualKey.G, VirtualKey.CONTROL);
    public ModifierHotkey   GatherWindowDeleteModifier     { get; set; } = VirtualKey.CONTROL;
    public VirtualKey       GatherWindowHoldKey            { get; set; } = VirtualKey.MENU;

    public void Save()
        => Dalamud.PluginInterface.SavePluginConfig(this);


    // Add missing colors to the dictionary if necessary.
    private void AddColors()
    {
        var save = false;
        foreach (var color in Enum.GetValues<ColorId>())
            save |= Colors.TryAdd(color, color.Data().DefaultColor);
        if (save)
            Save();
    }

    public static Configuration Load()
    {
        if (Dalamud.PluginInterface.GetPluginConfig() is Configuration config)
        {
            config.AddColors();
            return config;
        }

        config = new Configuration();
        config.Save();
        return config;
    }
}
