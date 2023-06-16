﻿using System;
using System.Collections.Generic;
using System.Text;
using TeleCore.Data.Events;
using TeleCore.Network;
using Verse;

namespace TeleCore;

public class NetworkPartSet
{
    private NetworkDef def;
    private INetworkSubPart parent;
    private string cachedString;

    private INetworkSubPart controller;
    private readonly HashSet<INetworkSubPart> fullSet;
    private readonly HashSet<INetworkSubPart> tickSet;
    private readonly Dictionary<NetworkRole, HashSet<INetworkSubPart>> structuresByRole;
    private readonly Dictionary<IntVec3,INetworkSubPart> structuresByPosition;
        
    public HashSet<INetworkSubPart>? this[NetworkRole role]
    {
        get => structuresByRole.TryGetValue(role, out var value) ? value : null;
    }
        
    public INetworkSubPart? this[IntVec3 pos]
    {
        get => structuresByPosition.TryGetValue(pos, out var value) ? value : null;
    }

    public HashSet<INetworkSubPart> FullSet => fullSet;
    internal HashSet<INetworkSubPart> TickList => tickSet;
    public INetworkSubPart Controller => controller;

    public NetworkPartSet(NetworkDef def, INetworkSubPart parent)
    {
        this.def = def;
        this.parent = parent;

        fullSet = new HashSet<INetworkSubPart>();
        tickSet = new HashSet<INetworkSubPart>();
        structuresByRole = new Dictionary<NetworkRole, HashSet<INetworkSubPart>>();
        structuresByPosition = new Dictionary<IntVec3, INetworkSubPart>();
        foreach (NetworkRole role in Enum.GetValues(typeof(NetworkRole)))
        {
            structuresByRole.Add(role, new HashSet<INetworkSubPart>());
        }
    }
        
    //TODO: Check relevance
    public void RegisterParentForEvents(PipeNetworkSystem parent)
    {
        parent.AddedPart += OnPartAdded;
        parent.RemovedPart += OnPartRemoved;
        parent.NetworkDestroyed += OnNetworkDestroyed;
    }

    public void Notify_ParentDestroyed()
    {
        foreach (INetworkSubPart part in fullSet)
        {
            //Remove direct connection from neighboring parts
            part.DirectPartSet.RemoveComponent(parent);
        }
    }

    //
    public bool AddNewComponent(INetworkSubPart part)
    {
        if (part == null) return false;
        if (!AddComponent(part)) return false;
        /*
        if (parent != null)
        {
            part.DirectPartSet.AddComponent(parent);
        }
        */
        return true;
    }

    private bool AddComponent(INetworkSubPart part)
    {
        if (part.NetworkDef != def) return false;
        if (fullSet.Contains(part)) return false;
        fullSet.Add(part);

        if (!part.IsNetworkEdge)
        {
            tickSet.Add(part);
        }

        //Special Case
        if (part.NetworkRole.HasFlag(NetworkRole.Controller))
        {
            controller = part;
        }

        //
        foreach (var flag in part.NetworkRole.AllFlags())
        {
            if (!structuresByRole[flag].Add(part))
            {
                TLog.Warning($"Trying to add existing item: {part} for role {flag}.");
            }
        }

        foreach (var cell in part.Parent.Thing.OccupiedRect())
        {
            if (!structuresByPosition.TryAdd(cell, part))
            {
                TLog.Warning($"Trying to add existing item on cell: {cell}:{part} with existing Thing: {structuresByPosition[cell]}");
            }
        }

        //
        UpdateString();
        return true;
    }

    public void RemoveComponent(INetworkSubPart part)
    {
        if (!fullSet.Contains(part)) return;

        //Special Case
        if (part.NetworkRole.HasFlag(NetworkRole.Controller))
        {
            controller = null;
            return;
        }

        //
        foreach (var flag in part.NetworkRole.AllFlags())
        {
            structuresByRole[flag].Remove(part);
        }
            
        foreach (var cell in part.Parent.Thing.OccupiedRect())
        {
            structuresByPosition.Remove(cell);
        }

        //
        fullSet.Remove(part);
        UpdateString();
    }

    private void UpdateString()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"CONTROLLER: {Controller?.Parent.Thing}");
        foreach (NetworkRole role in Enum.GetValues(typeof(NetworkRole)))
        {
            sb.AppendLine($"{role}: ");
            foreach (var part in structuresByRole[role])
            {
                sb.AppendLine($"    - {part.Parent.Thing}");
            }
        }
        sb.AppendLine($"Total Count: {fullSet.Count}");
        cachedString = sb.ToString();
    }

    public override string ToString()
    {
        if (cachedString == null)
            UpdateString();
        return cachedString;
    }
        
    #region EventHandling

    public event NetworkChangedEvent ParentDestroyed;
        
    private void OnPartAdded(NetworkChangedEventArgs args)
    {
        var part = args.Part;
    }

    private void OnPartRemoved(NetworkChangedEventArgs args)
    {
    }

    public void OnNetworkDestroyed(NetworkChangedEventArgs networkChangedEventArgs)
    {
            
    }
        
    //TODO: Add events for partset handling
    public void OnParentDestroyed(NetworkChangedEventArgs args)
    {
        foreach (INetworkSubPart part in fullSet)
        {
            //Remove direct connection from neighboring parts
            part.DirectPartSet.RemoveComponent(parent);
        }
    }
        
    #endregion
}