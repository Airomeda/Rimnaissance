using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace Rimnaissance
{
    public class WorkGiver_WindDrill : WorkGiver_Scanner
    {
        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForDef(RimnaissanceDefOf.RNS_WindDrill);

        public override PathEndMode PathEndMode => PathEndMode.InteractionCell;

        public override Danger MaxPathDanger(Pawn pawn)
        {
            return Danger.Deadly;
        }

        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            List<Building> allBuildingsColonist = pawn.Map.listerBuildings.allBuildingsColonist;
            for (int i = 0; i < allBuildingsColonist.Count; i++)
            {
                Building building = allBuildingsColonist[i];
                if (building.def == RimnaissanceDefOf.RNS_WindDrill)
                {
                    var cwd = building.TryGetComp<CompWindDrill>();
                    if (cwd.Working && building.Map.designationManager.DesignationOn(building, DesignationDefOf.Uninstall) == null)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (t.Faction != pawn.Faction)
            {
                return false;
            }
            if (!(t is Building building))
            {
                return false;
            }
            if (building.IsForbidden(pawn))
            {
                return false;
            }
            if (!pawn.CanReserve(building, 1, -1, null, forced))
            {
                return false;
            }
            if (!building.TryGetComp<CompWindDrill>().CanWindDrillNow())
            {
                return false;
            }
            if (building.Map.designationManager.DesignationOn(building, DesignationDefOf.Uninstall) != null)
            {
                return false;
            }
            if (building.IsBurning())
            {
                return false;
            }
            return true;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return JobMaker.MakeJob(RimnaissanceDefOf.RNS_OperateWindDrill, t, 1500, checkOverrideOnExpiry: true);
        }
    }
}
