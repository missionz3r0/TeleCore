﻿using System.Collections.Generic;
using Verse;

namespace TeleCore
{
    public class NetworkMapInfo : MapInformation
    {
        private readonly Dictionary<NetworkDef, PipeNetworkManager> NetworksByType = new ();
        private readonly List<PipeNetworkManager> PipeNetworks = new ();

        public NetworkMapInfo(Map map) : base(map)
        {
        }

        public PipeNetworkManager this[NetworkDef type] => NetworksByType.TryGetValue(type);

        public PipeNetworkManager GetOrCreateNewNetworkSystemFor(NetworkDef networkDef)
        {
            if (NetworksByType.TryGetValue(networkDef, out var network)) return network;

            //Make New
            TLog.Debug($"Creating NetworkSystem: {networkDef}");
            var networkMaster = new PipeNetworkManager(Map, networkDef);
            NetworksByType.Add(networkDef, networkMaster);
            PipeNetworks.Add(networkMaster);
            return networkMaster;
        }

        public void Notify_NewNetworkStructureSpawned(Comp_NetworkStructure structure)
        {
            foreach (var networkComponent in structure.NetworkParts)
            {
                GetOrCreateNewNetworkSystemFor(networkComponent.NetworkDef).RegisterComponent(networkComponent, structure);
            }
        }

        public void Notify_NetworkStructureDespawned(Comp_NetworkStructure structure)
        {
            foreach (var networkComponent in structure.NetworkParts)
            {
                GetOrCreateNewNetworkSystemFor(networkComponent.NetworkDef).DeregisterComponent(networkComponent, structure);
            }
        }

        //Data Getters
        public bool HasConnectionAtFor(Thing thing, IntVec3 c)
        {
            var networkStructure = thing.TryGetComp<Comp_NetworkStructure>();
            if (networkStructure == null) return false;
            foreach (var networkPart in networkStructure.NetworkParts)
            {
                if (this[networkPart.NetworkDef].HasNetworkConnectionAt(c))
                {
                    return true;
                }
            }
            return false;
        }

        public override void Tick()
        {
            /*
            foreach (var networkSystem in PipeNetworks)
            {
                networkSystem.TickNetworks();
            }
            */
        }

        [TweakValue("Network", 0, 100)] 
        public static int NetworkTickInterval = 50;

        public override void TeleTick()
        {
            if (TFind.TickManager.CurrentMapTick % NetworkTickInterval == 0)
            {
                //TLog.Message($"Ticking all networks | {TFind.TickManager.CurrentTick}");
                foreach (var networkSystem in PipeNetworks)
                {
                    networkSystem.TickNetworks();
                }
            }
        }

        public override void UpdateOnGUI()
        {
            foreach (var networkSystem in PipeNetworks)
            {
                networkSystem.DrawNetworkOnGUI();
            }
        }

        public override void Update()
        {
            foreach (var networkSystem in PipeNetworks)
            {
                networkSystem.DrawNetwork();
            }
        }
    }
}
