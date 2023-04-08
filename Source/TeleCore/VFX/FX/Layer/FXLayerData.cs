﻿using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace TeleCore
{
    public class FXLayerData
    {
        internal const string _ThingHolderTag = "FXParentThing";
        internal const string _NetworkHolderTag = "FXNetwork";

        //Main graphic
        public GraphicData? graphicData;
        public List<DynamicTextureParameter> textureParams;
        
        public AltitudeLayer? altitude = null;
        public FXMode fxMode = FXMode.Static;

        //
        public int? renderPriority; //Otherwise set by index
        public string categoryTag;
        public string layerTag;
        public bool skip = false;
        public bool needsPower = false;
        public int? drawLayer = null;

        //
        public RotateProperties? rotate;
        public BlinkProperties? blink;
        public FadeProperties? fade;
        public ResizeProperties? resize;

        //Texture UV Data
        public Rect texCoords = new Rect(0, 0, 1, 1);
        public Vector2 textureSize = Vector2.one;
        public Vector3 drawOffset = Vector3.zero;
        public Vector3? pivotOffset = null;
        public Vector3? pivotPixelOffset = null;

        //public List<EffecterDef> effecters;

        public Vector3? PivotOffset
        {
            get
            {
                if (pivotOffset != null) return pivotOffset;
                if (pivotPixelOffset != null)
                {
                    var pixelOffset = pivotPixelOffset.Value;

                    float width = (pixelOffset.x / textureSize.x) * graphicData.drawSize.x;
                    float height = (pixelOffset.z / textureSize.y) * graphicData.drawSize.y;

                    pivotOffset = new Vector3(width, 0, height);
                }

                return pivotOffset;
            }
        }

        public void PostLoad()
        {
            if (graphicData != null)
            {
                LongEventHandler.ExecuteWhenFinished(delegate
                {
                    graphicData.shaderType ??= ShaderTypeDefOf.Cutout;
                    graphicData.Init();
                });
            }
        }
    }
}
