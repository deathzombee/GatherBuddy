﻿using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.Text.SeStringHandling;
using GatherBuddy.Classes;
using GatherBuddy.Enums;
using GatherBuddy.Interfaces;
using GatherBuddy.SeFunctions;
using GatherBuddy.Time;
using GatherBuddy.Utility;
using CommandManager = GatherBuddy.SeFunctions.CommandManager;
using GatheringType = GatherBuddy.Enums.GatheringType;

namespace GatherBuddy.Plugin;

public class Executor
{
    private enum IdentifyType
    {
        None,
        Item,
        Fish,
    }

    private readonly CommandManager _commandManager = new(Dalamud.GameGui, Dalamud.SigScanner);
    private readonly MacroManager   _macroManager   = new();
    private readonly GatherBuddy    _plugin;
    public readonly  Identificator  Identificator = new();

    public Executor(GatherBuddy plugin)
        => _plugin = plugin;

    private IdentifyType _identifyType = IdentifyType.None;
    private string       _name         = string.Empty;

    private IGatherable? _item = null;

    private GatheringType? _gatheringType = null;
    private ILocation?     _location      = null;
    private TimeInterval   _uptime        = TimeInterval.Always;

    private          IGatherable?    _lastItem             = null;
    private readonly List<ILocation> _visitedLocations     = new();
    private          bool            _keepVisitedLocations = false;
    private          TimeStamp       _lastGatherReset      = TimeStamp.Epoch;

    private void FindGatherableLogged(string itemName)
    {
        _item = Identificator.IdentifyGatherable(itemName);
        Communicator.PrintIdentifiedItem(itemName, _item);
    }

    private void FindFishLogged(string fishName)
    {
        _item = Identificator.IdentifyFish(fishName);
        Communicator.PrintIdentifiedItem(fishName, _item);
    }

    private void CheckVisitedLocations()
    {
        _lastGatherReset = GatherBuddy.Time.ServerTime.AddEorzeaHours(1);

        if (_keepVisitedLocations)
            _item = _lastItem;
        else
            _visitedLocations.Clear();
        _lastItem = _item;
        if (_item != null && _location != null)
            _visitedLocations.Add(_location);

        if ((_lastItem?.Locations.Count() ?? 0) == _visitedLocations.Count)
            _visitedLocations.Clear();
    }

    private void HandleAlarm()
    {
        switch (_identifyType)
        {
            case IdentifyType.None: return;
            case IdentifyType.Item:
                _item = _plugin.AlarmManager.LastItemAlarm?.Item1.Item;
                return;
            case IdentifyType.Fish:
                _item = _plugin.AlarmManager.LastFishAlarm?.Item1.Item;
                return;
            default: throw new ArgumentOutOfRangeException();
        }
    }

    private void HandleNext()
    {
        _item = _lastItem;
        if (_lastGatherReset < GatherBuddy.Time.ServerTime)
            _visitedLocations.Clear();
        _keepVisitedLocations = true;
        if (_item == null)
            Communicator.Print("No previous gather command registered.");
    }

    private void DoIdentify()
    {
        _keepVisitedLocations = false;
        if (_name.Length == 0)
            return;

        switch (_name)
        {
            case "alarm":
                HandleAlarm();
                return;
            case "next":
                HandleNext();
                return;
        }

        switch (_identifyType)
        {
            case IdentifyType.None: return;
            case IdentifyType.Item:
                FindGatherableLogged(_name);
                return;
            case IdentifyType.Fish:
                FindFishLogged(_name);
                return;
            default: throw new ArgumentOutOfRangeException();
        }
    }

    private void FindClosestLocation()
    {
        if (_item == null)
            return;

        _location = null;
        (_location, _uptime) = (_keepVisitedLocations, _gatheringType) switch
        {
            (false, null)     => GatherBuddy.UptimeManager.BestLocation(_item),
            (false, not null) => GatherBuddy.UptimeManager.NextUptime((Gatherable)_item, _gatheringType.Value, GatherBuddy.Time.ServerTime),
            (true, null)      => GatherBuddy.UptimeManager.NextUptime(_item,             GatherBuddy.Time.ServerTime, _visitedLocations),
            (true, not null) => GatherBuddy.UptimeManager.NextUptime((Gatherable)_item, _gatheringType.Value, GatherBuddy.Time.ServerTime,
                _visitedLocations),
        };

        if (_location == null)
            Communicator.LocationNotFound(_item, _gatheringType);
    }

    private void DoTeleport()
    {
        if (!GatherBuddy.Config.UseTeleport || _location?.ClosestAetheryte == null)
            return;

        if (GatherBuddy.Config.SkipTeleportIfClose
         && Dalamud.ClientState.TerritoryType == _location.Territory.Id
         && Dalamud.ClientState.LocalPlayer != null)
        {
            // Check distance of player to node against distance of aetheryte to node.
            var playerPos = Dalamud.ClientState.LocalPlayer.Position;
            var aetheryte = _location.ClosestAetheryte;
            var posX      = Maps.NodeToMap(playerPos.X, _location.Territory.SizeFactor);
            var posY      = Maps.NodeToMap(playerPos.Z, _location.Territory.SizeFactor);
            var distAetheryte = aetheryte != null
                ? System.Math.Sqrt(aetheryte.WorldDistance(_location.Territory.Id, _location.IntegralXCoord, _location.IntegralYCoord))
                : double.PositiveInfinity;
            var distPlayer = System.Math.Sqrt(Utility.Math.SquaredDistance(posX, posY, _location.IntegralXCoord, _location.IntegralYCoord));
            // Allow for some leeway due to teleport cost and time.
            if (distPlayer < distAetheryte * 1.5)
                return;
        }

        TeleportToAetheryte(_location.ClosestAetheryte);
    }

    private void DoGearChange()
    {
        if (!GatherBuddy.Config.UseGearChange || _location == null)
            return;

        var set = _location.GatheringType.ToGroup() switch
        {
            GatheringType.Fisher   => GatherBuddy.Config.FisherSetName,
            GatheringType.Botanist => GatherBuddy.Config.BotanistSetName,
            GatheringType.Miner    => GatherBuddy.Config.MinerSetName,
            _                      => null,
        };
        if (set == null)
        {
            Communicator.PrintError("No job type associated with location ", _location.Name, GatherBuddy.Config.SeColorArguments, ".");
            return;
        }

        if (set.Length == 0)
        {
            Communicator.PrintError("No gear set for ", _location.GatheringType.ToString(), GatherBuddy.Config.SeColorArguments,
                " configured.");
            return;
        }


        _commandManager.Execute($"/gearset change \"{set}\"");
    }


    private void DoMapFlag()
    {
        if (!GatherBuddy.Config.WriteCoordinates && !GatherBuddy.Config.UseCoordinates || _location == null)
            return;

        if (_location.IntegralXCoord == 100 || _location.IntegralYCoord == 100)
            return;

        var link = new SeStringBuilder().AddFullMapLink(_location.Name, _location.Territory, _location.IntegralXCoord / 100f,
            _location.IntegralYCoord / 100f, true).BuiltString;
        Communicator.PrintCoordinates(link);
    }

    private void DoAdditionalInfo()
    {
        Communicator.PrintUptime(_uptime);
    }

    public bool DoCommand(string argument)
    {
        switch (argument)
        {
            case GatherBuddy.IdentifyCommand:
                DoIdentify();
                FindClosestLocation();
                CheckVisitedLocations();
                return true;
            case GatherBuddy.MapMarkerCommand:
                DoMapFlag();
                return true;
            case GatherBuddy.GearChangeCommand:
                DoGearChange();
                return true;
            case GatherBuddy.TeleportCommand:
                DoTeleport();
                return true;
            case GatherBuddy.AdditionalInfoCommand:
                DoAdditionalInfo();
                return true;
            default: return false;
        }
    }

    public void GatherLocation(ILocation location)
    {
        _identifyType  = IdentifyType.None;
        _name          = string.Empty;
        _item          = null;
        _gatheringType = location.GatheringType.ToGroup();
        _location      = location;
        if (location is GatheringNode n)
            _uptime = n.Times.NextUptime(GatherBuddy.Time.ServerTime);
        else
            _uptime = TimeInterval.Always;

        _macroManager.Execute();
    }

    public void GatherItem(IGatherable? item, GatheringType? type = null)
    {
        if (item == null)
            return;

        _identifyType  = IdentifyType.None;
        _name          = string.Empty;
        _item          = item;
        _location      = null;
        _gatheringType = type?.ToGroup();
        _uptime        = TimeInterval.Always;

        _macroManager.Execute();
    }

    public void GatherFishByName(string fishName)
    {
        if (fishName.Length == 0)
            return;

        _identifyType  = IdentifyType.Fish;
        _name          = fishName.ToLowerInvariant();
        _item          = null;
        _location      = null;
        _gatheringType = null;
        _uptime        = TimeInterval.Always;

        _macroManager.Execute();
    }

    public void GatherItemByName(string itemName, GatheringType? type = null)
    {
        if (itemName.Length == 0)
            return;

        _identifyType  = IdentifyType.Item;
        _name          = itemName.ToLowerInvariant();
        _item          = null;
        _location      = null;
        _gatheringType = type;
        _uptime        = TimeInterval.Always;

        _macroManager.Execute();
    }

    public static void TeleportToAetheryte(Aetheryte aetheryte)
    {
        if (aetheryte.Id == 0)
            return;

        Teleporter.Teleport(aetheryte.Id);
    }

    public static void TeleportToTerritory(Territory territory)
    {
        if (territory.Aetherytes.Count == 0)
        {
            Communicator.PrintError(string.Empty, territory.Name, GatherBuddy.Config.SeColorArguments, " has no valid aetheryte.");
            return;
        }

        var aetheryte = territory.Aetherytes.FirstOrDefault(a => Teleporter.IsAttuned(a.Id));
        if (aetheryte == null)
        {
            Communicator.PrintError("Not attuned to any aetheryte in ", territory.Name, GatherBuddy.Config.SeColorArguments, ".");
            return;
        }

        Teleporter.TeleportUnchecked(aetheryte.Id);
    }
}
