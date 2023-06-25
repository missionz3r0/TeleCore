﻿using System;
using System.Collections.Generic;
using System.Linq;
using Multiplayer.API;
using TeleCore.Defs;
using UnityEngine;
using Verse;

namespace TeleCore.Generics.Container.Gizmos;

public class Gizmo_ContainerStorage : Gizmo_ContainerStorage<FlowValueDef, ValueContainerBase<FlowValueDef>>
{
    public Gizmo_ContainerStorage(object container) : base((ValueContainerBase<FlowValueDef>)container)
    {
    }
    
    public Gizmo_ContainerStorage(ValueContainerBase<FlowValueDef> container) : base(container)
    {
    }

    [SyncMethod]
    protected override void Debug_AddAll(int part)
    {
        foreach (var type in container.AcceptedTypes)
        {
            container.TryAdd(type, part);
        }
    }

    [SyncMethod]
    protected override void Debug_Clear()
    {
        container.Clear();
    }

    [SyncMethod]
    protected override void Debug_AddType(FlowValueDef type, int part)
    {
        container.TryAdd(type, part);
    }
}

public class Gizmo_ContainerStorage<TValue, TContainer> : Gizmo
    where TValue : FlowValueDef
    where TContainer : ValueContainerBase<TValue>
{
    protected TContainer container;

    public Gizmo_ContainerStorage(TContainer container)
    {
        this.container = container;
        this.order = -200f;
    }

    public override float GetWidth(float maxWidth)
    {
        return 150; //optionToggled ? 310 : 150f;
    }

    /*
    [SyncWorker]
    static void SyncWorkerGizNS(SyncWorker sync, ref Gizmo_ContainerStorage<TValue> type)
    {
        if (sync.isWriting)
        {
            var parent = type.container.Parent;
            var comp = (Comp_NetworkStructure)parent.Parent;
            sync.Write(comp);
            sync.Write(parent.NetworkDef);
        }
        else
        {
            var comp = sync.Read<Comp_NetworkStructure>();
            var def = sync.Read<NetworkDef>();
            type = comp[def].Container.ContainerGizmo;
        }
    }
    */

    public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
    {
        Rect MainRect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
        Find.WindowStack.ImmediateWindow(container.GetHashCode(), MainRect, WindowLayer.GameUI, delegate
        {
            Rect rect = MainRect.AtZero().ContractedBy(5f);
            Rect optionRect = new Rect(rect.xMax - 15, rect.y, 15, 15);
            bool mouseOver = Mouse.IsOver(rect);
            GUI.color = mouseOver ? Color.cyan : Color.white;
            Widgets.DrawTextureFitted(optionRect, TeleContent.InfoButton, 1f);
            GUI.color = Color.white;
            /*
            if (Widgets.ButtonInvisible(rect))
                optionToggled = !optionToggled;
            */
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.UpperCenter;
            Widgets.Label(rect, container.Label);
            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font = GameFont.Small;
            Widgets.Label(rect, $"{container.TotalStored}/{container.Capacity}");
            Text.Anchor = 0;
            Rect rect2 = rect.BottomHalf();
            Rect rect3 = rect2.BottomHalf();
            //
            Widgets.BeginGroup(rect3);
            {
                Rect BGRect = new Rect(0, 0, rect3.width, rect3.height);
                Rect BarRect = BGRect.ContractedBy(2.5f);
                float xPos = 0f;
                Widgets.DrawBoxSolid(BGRect, new Color(0.05f, 0.05f, 0.05f));
                Widgets.DrawBoxSolid(BarRect, new Color(0.25f, 0.25f, 0.25f));
                foreach (var type in container.StoredDefs)
                {
                    float percent = (container.StoredValueOf(type) / container.Capacity);
                    Rect typeRect = new Rect(2.5f + xPos, BarRect.y, BarRect.width * percent, BarRect.height);
                    Color color = type.valueColor;
                    xPos += BarRect.width * percent;
                    Widgets.DrawBoxSolid(typeRect, color);
                }
            }
            Widgets.EndGroup();

            //Right Click Input
            Event curEvent = Event.current;
            if (Mouse.IsOver(rect) && curEvent.type == EventType.MouseDown && curEvent.button == 1)
            {
                if (DebugSettings.godMode)
                {
                    FloatMenu menu = new FloatMenu(RightClickFloatMenuOptions.ToList(), $"Add NetworkValue", true);
                    menu.vanishIfMouseDistant = true;
                    Find.WindowStack.Add(menu);
                }
            }

        }, true, false, 1f);
        return new GizmoResult(GizmoState.Clear);
    }
    
    protected virtual void Debug_AddAll(int part)
    {
    }
    
    protected virtual void Debug_Clear()
    {
    }
    
    protected virtual void Debug_AddType(FlowValueDef type, int part)
    {
    }

    public override IEnumerable<FloatMenuOption> RightClickFloatMenuOptions
    {
        get
        {
            float part = container.Capacity / (float)container.AcceptedTypes.Count;
            yield return new FloatMenuOption("Add ALL", delegate { Debug_AddAll((int)part); });

            yield return new FloatMenuOption("Remove ALL", Debug_Clear);

            foreach (var type in container.AcceptedTypes)
            {
                yield return new FloatMenuOption($"Add {type}", delegate
                {
                    Debug_AddType(type, (int)part);
                });
            }
        }
    }
}