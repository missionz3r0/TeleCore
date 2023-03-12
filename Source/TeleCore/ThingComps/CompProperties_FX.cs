﻿using System.Collections.Generic;
using System.Linq;
using Verse;

namespace TeleCore
{
    public class CompProperties_FX : CompProperties
    {
        //Layers
        public List<FXLayerData> fxLayers = new List<FXLayerData>();
        public List<FXEffecterData> effectLayers = new List<FXEffecterData>();
        public IntRange tickOffset = new IntRange(0, 333);

        public override IEnumerable<string> ConfigErrors(ThingDef def)
        {
            if (def.drawerType == DrawerType.MapMeshOnly && Enumerable.Any(fxLayers, o => o.fxMode != FXMode.Static))
                yield return $"{def} has dynamic overlays but is MapMeshOnly";
        }

        public CompProperties_FX()
        {
            this.compClass = typeof(CompFX);
        }
    }
}
