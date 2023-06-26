﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using TeleCore.Data.Events;
using TeleCore.Defs;
using TeleCore.Network;
using TeleCore.Network.Data;
using TeleCore.Network.IO;
using UnityEngine;
using Verse;

namespace TeleCore;

//TODO: Add leaking functionality, broken transmitters losing values
public class Comp_Network : FXThingComp, INetworkStructure
{
    //
    private PipeNetworkMapInfo _mapInfo;

    private List<INetworkPart> _allNetParts;
    private Dictionary<NetworkDef, INetworkPart> _netPartByDef;
    private NetworkIO io;

    //Debug
    protected static bool DebugConnectionCells = false;
    private IFXLayerProvider ifxHolderImplementation;

    //
    public INetworkPart this[NetworkDef def] => _netPartByDef.TryGetValue(def, out var value) ? value : null;

    //
    public CompProperties_Network Props => (CompProperties_Network)base.props;
    public CompPowerTrader CompPower { get; private set; }
    public CompFlickable CompFlick { get; private set; }
    public CompFX CompFX { get; private set; }

    //
    public Thing Thing => parent;
    public List<INetworkPart> NetworkParts => _allNetParts;
    public NetworkIO GeneralIO => io;

    public bool IsPowered => CompPower?.PowerOn ?? true;
    public bool IsWorking => IsWorkingOverride;

    //
    protected virtual bool IsWorkingOverride => true;

    #region FX Implementation

        
    // ## Layers ##
    // 0 - Container
    // 
    public override bool FX_ProvidesForLayer(FXArgs args)
    {
        if(args.categoryTag == "FXNetwork")
            return true;
        return false;
    }
        
    public override CompPowerTrader FX_PowerProviderFor(FXArgs args) => null;

    public override bool? FX_ShouldDraw(FXLayerArgs args)
    {
        return args.index switch
        {
            1 => _allNetParts.Any(t => t?.HasConnection ?? false),
            _ => true
        };
    }

    public override float? FX_GetOpacity(FXLayerArgs args) => 1f;
    public override float? FX_GetRotation(FXLayerArgs args) => null;
    public override float? FX_GetRotationSpeedOverride(FXLayerArgs args) => null;
    public override float? FX_GetAnimationSpeedFactor(FXLayerArgs args) => null;
        
    public override Color? FX_GetColor(FXLayerArgs args)         
    {
        return args.index switch
        {
            //TODO: Access color from flowsystem
            //0 => _allNetParts[0].Container.Color,
            _ => Color.white
        };
    }
        
    public override Vector3? FX_GetDrawPosition(FXLayerArgs args)  
    {
        return parent.DrawPos;
    }
        
    public override Func<RoutedDrawArgs, bool> FX_GetDrawFunc(FXLayerArgs args) => null!;

    public override bool? FX_ShouldThrowEffects(FXEffecterArgs args)
    {
        return base.FX_ShouldThrowEffects(args);
    }

    public override void FX_OnEffectSpawned(FXEffecterSpawnedEventArgs spawnedEventArgs)
    {
    }
        
    #endregion

    //SaveData
    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Collections.Look(ref _allNetParts, "networkParts", LookMode.Deep, this);

        //
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            if (_allNetParts.NullOrEmpty())
            {
                TLog.Warning($"Could not load network parts for {parent}... Correcting.");
            }
        }
    }

    //Init Construction
    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        //
        base.PostSpawnSetup(respawningAfterLoad);

        //
        CompPower = parent.TryGetComp<CompPowerTrader>();
        CompFlick = parent.TryGetComp<CompFlickable>();
        CompFX = parent.TryGetComp<CompFX>();

        //
        io = new NetworkIO(Props.generalIOConfig, parent.Position, parent);
        _mapInfo = parent.Map.TeleCore().NetworkInfo;

        //Create NetworkComponents
        if (respawningAfterLoad && (_allNetParts.Count != Props.networks.Count))
        {
            TLog.Warning($"Spawning {parent} after load with missing parts... Correcting.");
        }
            
        //
        if(!respawningAfterLoad)
            _allNetParts = new List<INetworkPart>(Math.Max(1, Props.networks.Count));
            
        _netPartByDef = new Dictionary<NetworkDef, INetworkPart>(Props.networks.Count);
        for (var i = 0; i < Props.networks.Count; i++)
        {
            var compProps = Props.networks[i];
            NetworkPart part = null;
            if (!_allNetParts.Any(p => p.Config.networkDef == compProps.networkDef))
            {
                part = (NetworkPart) Activator.CreateInstance(compProps.workerType, args: new object[] {this, compProps});
                _allNetParts.Add(part);
            }

            if (part == null)
                part = (NetworkPart)_allNetParts[i];

            _netPartByDef.Add(compProps.networkDef, part);
            part.PartSetup(respawningAfterLoad);
        }
            
        //Check for neighbor intersections
        //Regen network after all data is set
        _mapInfo.Notify_NewNetworkStructureSpawned(this);
    }
        
    //Deconstruction
    public override void PostDestroy(DestroyMode mode, Map previousMap)
    {
        base.PostDestroy(mode, previousMap);
        //Regen network after all data is set
        _mapInfo.Notify_NetworkStructureDespawned(this);

        foreach (var networkPart in NetworkParts)
        {
            networkPart.PostDestroy(mode, previousMap);
        }
    }

    //
    public virtual bool RoleIsActive(NetworkRole role)
    {
        return true;
    }
        
    public virtual bool CanInteractWith(INetworkPart otherPart)
    {
        return true;
    }

    public virtual void NetworkPostTick(NetworkPart networkSubPart, bool isPowered)
    {

    }

    public virtual void NetworkPartProcessorTick(INetworkPart netPart)
    {
    }

    public virtual void Notify_ReceivedValue()
    {
    }

    //
    public bool HasPartFor(NetworkDef networkDef)
    {
        return _netPartByDef.ContainsKey(networkDef);
    }
        
    //
    public void Notify_StructureAdded(INetworkStructure other)
    {
        //structureSet.AddNewStructure(other);
    }

    public void Notify_StructureRemoved(INetworkStructure other)
    {
        //structureSet.RemoveStructure(other);
    }

    //
    public virtual bool AcceptsValue(NetworkValueDef value)
    {
        return true;
    }
        
    public virtual bool CanConnectToOther(INetworkStructure other)
    {
        return true;
    }
        
    //UI
    public override void PostDraw()
    {
        base.PostDraw();
        foreach (var networkPart in NetworkParts)
        {
            networkPart.Draw();
            //TODO: legacy debug data
            // if (DebugConnectionCells && Find.Selector.IsSelected(parent))
            // {
            //     GenDraw.DrawFieldEdges(networkPart.CellIO.OuterConnnectionCells.Select(t => t.IntVec).ToList(), Color.cyan);
            //     GenDraw.DrawFieldEdges(networkPart.CellIO.InnerConnnectionCells.ToList(), Color.green);
            // }
        }
    }

    public override void PostPrintOnto(SectionLayer layer)
    {
        base.PostPrintOnto(layer);
        foreach (var networkPart in NetworkParts)
        {
            networkPart.Config.networkDef.TransmitterGraphic?.Print(layer, Thing, 0, networkPart);
        }
    }

    public override string CompInspectStringExtra()
    {
        StringBuilder sb = new StringBuilder();
        foreach (var networkSubPart in NetworkParts)
        {
            sb.AppendLine(networkSubPart.InspectString());
        }

        /*TODO: ADD THIS TO COMPONENT DESC
        if (!Network.IsWorking)
            sb.AppendLine("TR_MissingNetworkController".Translate());
        //TODO: Make reasons for multi roles
        if (!Network.ValidFor(Props.NetworkRole, out string reason))
        {
            sb.AppendLine("TR_MissingConnection".Translate() + ":");
            if (!reason.NullOrEmpty())
            {
                sb.AppendLine("   - " + reason.Translate());
            }
        }
        */
            
        return sb.ToString().TrimStart().TrimEndNewlines();
    }

    private Gizmo_NetworkOverview networkInfoGizmo;
    public Gizmo_NetworkOverview NetworkGizmo => networkInfoGizmo ??= new Gizmo_NetworkOverview(this);
        
    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        /*
        foreach (var gizmo in networkParts.Select(c => c.SpecialNetworkDescription))
        {
        yield return gizmo;
        }
        */
            
        yield return NetworkGizmo;

        foreach (var networkPart in NetworkParts)
        {
            foreach (var partGizmo in networkPart.GetPartGizmos())
            {
                yield return partGizmo;
            }
        }

        foreach (Gizmo g in base.CompGetGizmosExtra())
        {
            yield return g;
        }

        if (!DebugSettings.godMode) yield break;

        yield return new Command_Action()
        {
            defaultLabel = "Draw Networks",
            action = delegate
            {
                foreach (var networkPart in NetworkParts)
                {
                    _mapInfo[networkPart.NetworkDef].DEBUG_ToggleShowNetworks();
                }
            }
        };

        yield return new Command_Action
        {
            defaultLabel = "Draw Connections",
            action = delegate { DebugConnectionCells = !DebugConnectionCells; }
        };

        yield return new Command_Action
        {
            defaultLabel = "Set Node Dirty",
            action = delegate
            {
                NetworkParts[0].Network.Graph.Notify_StateChanged(NetworkParts[0]);
            }
        };
    }
}