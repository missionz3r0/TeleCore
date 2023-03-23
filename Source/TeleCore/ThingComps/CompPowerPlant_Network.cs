﻿using System.Collections.Generic;
using System.Text;
using RimWorld;
using TeleCore.FlowCore;
using UnityEngine;
using Verse;

namespace TeleCore;

public class CompPowerPlant_Network : CompPowerPlant
{
    private int powerTicksRemaining;
    private float internalPowerOutput = 0f;

    private Comp_Network compNetwork;
    private NetworkSubPart _networkComponent;

    public new CompProperties_NetworkStructurePowerPlant Props =>
        (CompProperties_NetworkStructurePowerPlant) base.Props;

    public bool GeneratesPowerNow => powerTicksRemaining > 0;
    public override float DesiredPowerOutput => internalPowerOutput;
    public bool IsAtCapacity => powerTicksRemaining >= Props.maxWorkTime.TotalTicks;

    public override void PostExposeData()
    {
        base.PostExposeData();
        //Scribe_Values.Look(ref powerProductionTicks, "powerTicks");
    }

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        compNetwork = parent.GetComp<Comp_Network>();
        _networkComponent = compNetwork[Props.fromNetwork];
    }

    public override void CompTick()
    {
        base.CompTick();
        PowerTick();
    }

    private void PowerTick()
    {
        base.CompTick();
        //If no power-generation possible
        if (!PowerOn || Props.valueToTickRules.NullOrEmpty())
        {
            internalPowerOutput = 0f;
            return;
        }

        //If no value
        if (!IsAtCapacity && !_networkComponent.Requester.RequestingNow && _networkComponent.Container.FillState != ContainerFillState.Empty)
        {
            foreach (var conversion in Props.valueToTickRules)
            {
                if (_networkComponent.Container.StoredValueOf(conversion.valueDef) <= 0) continue;
                if (_networkComponent.Container.TryConsume(conversion.valueDef, conversion.cost))
                {
                    powerTicksRemaining += Mathf.RoundToInt(conversion.seconds.SecondsToTicks());
                }
            }
        }

        if (powerTicksRemaining > 0)
        {
            internalPowerOutput = -base.Props.basePowerConsumption;
            powerTicksRemaining--;
        }
    }

    public override string CompInspectStringExtra()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine(base.CompInspectStringExtra());
        if (GeneratesPowerNow)
            sb.AppendLine("TR_PowerLeft".Translate(powerTicksRemaining.ToStringTicksToPeriod()));
        return sb.ToString().TrimEndNewlines();
    }
}

public class CompProperties_NetworkStructurePowerPlant : CompProperties_Power
{
    public NetworkDef fromNetwork;
    public List<ValueConversion> valueToTickRules;
    public TickTime maxWorkTime = new TickTime(GenDate.TicksPerDay);
}

public class ValueConversion
{
    public NetworkValueDef valueDef;
    public float cost;
    public float seconds;
}

