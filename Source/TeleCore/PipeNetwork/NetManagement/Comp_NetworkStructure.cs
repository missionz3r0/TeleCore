﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using TeleCore.Static.Utilities;
using UnityEngine;
using Verse;

namespace TeleCore
{
    //TODO: Add leaking functionality, broken transmitters losing values
    public class Comp_NetworkStructure : ThingComp, INetworkStructure, IFXObject
    {
        //
        private NetworkMapInfo networkInfo;

        private List<NetworkSubPart> networkParts;
        private Dictionary<NetworkDef, NetworkSubPart> networkPartByDef;
        private NetworkCellIO cellIO;

        //Debug
        protected static bool DebugConnectionCells = false;

        //
        public NetworkSubPart this[NetworkDef def] => networkPartByDef.TryGetValue(def, out var value) ? value : null;

        //
        public CompProperties_NetworkStructure Props => (CompProperties_NetworkStructure)base.props;
        public CompPowerTrader CompPower { get; private set; }
        public CompFlickable CompFlick { get; private set; }
        public CompFX CompFX { get; private set; }

        //
        public Thing Thing => parent;
        public List<NetworkSubPart> NetworkParts => networkParts;

        public NetworkCellIO GeneralIO => cellIO;

        public bool IsPowered => CompPower?.PowerOn ?? true;

        //FX
        public virtual bool IsMain => true;
        public virtual int Priority => 10;
        public virtual bool ShouldThrowFlecks => true;
        public virtual CompPower ForcedPowerComp => CompPower;

        public virtual bool FX_AffectsLayerAt(int index)
        {
            return true;
        }

        public virtual bool FX_ShouldDrawAt(int index)
        {
            return index switch
            {
                1 => networkParts.Any(t => t?.HasConnection ?? false),
                _ => true,
            };
        }

        public virtual Color? FX_GetColorAt(int index)
        {
            return index switch
            {
                0 => networkParts[0].Container.Color,
                _ => Color.white
            };
        }

        public virtual Vector3? FX_GetDrawPositionAt(int index)
        {
            return parent.DrawPos;
        }

        public virtual float FX_GetOpacityAt(int index) => 1f;
        public virtual float? FX_GetRotationAt(int index) => null;
        public virtual float? FX_GetRotationSpeedAt(int index) => null;
        public virtual Action<FXGraphic> FX_GetActionAt(int index) => null;

        //BEGIN OF ACTUAL CLASS
        
        //SaveData
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Collections.Look(ref networkParts, "networkParts", LookMode.Deep, this);

            //
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (networkParts.NullOrEmpty())
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
            cellIO = new NetworkCellIO(Props.generalIOPattern, parent);
            networkInfo = parent.Map.TeleCore().NetworkInfo;

            //Create NetworkComponents
            if (respawningAfterLoad && (networkParts.Count != Props.networks.Count))
            {
                TLog.Warning($"Spawning {parent} after load with missing parts... Correcting.");
            }
            
            //
            if(!respawningAfterLoad)
                networkParts = new List<NetworkSubPart>(Props.networks.Count);
            
            networkPartByDef = new Dictionary<NetworkDef, NetworkSubPart>(Props.networks.Count);
            for (var i = 0; i < Props.networks.Count; i++)
            {
                var compProps = Props.networks[i];
                NetworkSubPart subPart = null;
                if (!networkParts.Any(p => p.NetworkDef == compProps.networkDef))
                {
                    subPart = (NetworkSubPart) Activator.CreateInstance(compProps.workerType, args: new object[] {this, compProps});
                    networkParts.Add(subPart);
                }

                if (subPart == null)
                    subPart = networkParts[i];

                networkPartByDef.Add(compProps.networkDef, subPart);
                subPart.SubPartSetup(respawningAfterLoad);
            }
            
            //Check for neighbor intersections
            //Regen network after all data is set
            networkInfo.Notify_NewNetworkStructureSpawned(this);
        }
        
        //Deconstruction
        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);
            //Regen network after all data is set
            networkInfo.Notify_NetworkStructureDespawned(this);

            foreach (var networkPart in NetworkParts)
            {
                networkPart.PostDestroy(mode, previousMap);
            }
        }

        public virtual void NetworkPostTick(bool isPowered)
        {

        }

        public virtual void NetworkPartProcessor(INetworkSubPart netPart)
        {
        }

        public virtual void Notify_ReceivedValue()
        {
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

        public bool CanConnectToOther(INetworkStructure other)
        {
            return GeneralIO.ConnectsTo(other.GeneralIO);
        }

        //UI
        public override void PostDraw()
        {
            base.PostDraw();

            //   
            if (DebugConnectionCells && Find.Selector.IsSelected(parent))
            {
                GenDraw.DrawFieldEdges(GeneralIO.ConnectionCells.ToList(), Color.cyan);
                GenDraw.DrawFieldEdges(GeneralIO.InnerConnectionCells.ToList(), Color.green);
            }

            foreach (var networkPart in NetworkParts)
            {
                networkPart.Draw();
            }
        }

        public override void PostPrintOnto(SectionLayer layer)
        {
            base.PostPrintOnto(layer);

            //
            foreach (var networkPart in NetworkParts)
            {
                networkPart.NetworkDef.TransmitterGraphic?.Print(layer, Thing, 0);
            }
        }

        public override string CompInspectStringExtra()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var networkSubPart in networkParts)
            {
                sb.AppendLine(networkSubPart.NetworkInspectString());
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

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            /*
            foreach (var gizmo in networkParts.Select(c => c.SpecialNetworkDescription))
            {
            yield return gizmo;
            }
            */

            foreach (var networkPart in networkParts)
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
                    foreach (var networkPart in networkParts)
                    {
                        networkInfo[networkPart.NetworkDef].ToggleShowNetworks();
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

            yield return new Command_Target
            {
                defaultLabel = "Get Path",
                targetingParams = TargetingParameters.ForBuilding(),
                action = delegate (LocalTargetInfo target) {
                    if (target.Thing is not ThingWithComps compThing) return;
                    var netComp = compThing.TryGetComp<Comp_NetworkStructure>();
                    var part = netComp[NetworkParts[0].NetworkDef];
                    if (part == null) return;

                    var path = part.Network.Graph.ProcessRequest(new NetworkGraphPathRequest(networkParts[0], part)); //GenGraph.Dijkstra(part.Network.Graph, NetworkParts[0], (x) => x == part);
                    TLog.Message($"{path.allTargets.ToStringSafeEnumerable()}");
                }
            };
        }
    }
}
