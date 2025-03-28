﻿using UnityEngine;
using Verse;

namespace Rimnaissance
{
    public class PlaceWorker_WindDrill : PlaceWorker
    {
        public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
        {
            float radius = ((ThingDef)checkingDef).GetCompProperties<CompProperties_WindDrill>().obstructionFreeRadius;

            if (CompWindDrill.ObstructedCellsWithinRadius(loc, map, radius))
            {
                return new AcceptanceReport("Rimnaissance.MustBeAwayFromOtherStructures".Translate(checkingDef.LabelCap, radius - 1));
            }

            if (CompWindDrill.OtherWindDrillOrBlueprintWithinRadius(loc, map, radius))
            {
                return new AcceptanceReport("Rimnaissance.MustBeAwayFromOtherWindDevices".Translate(checkingDef.LabelCap, radius - 1));
            }

            return true;
        }

        public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null)
        {
            GenDraw.DrawRadiusRing(center, def.GetCompProperties<CompProperties_WindDrill>().obstructionFreeRadius, new Color(1.14f, 1.45f, 1.92f, 0.75f));
        }
    }
}
