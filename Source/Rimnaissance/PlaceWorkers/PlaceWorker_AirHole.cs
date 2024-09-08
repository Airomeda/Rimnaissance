using Verse;

namespace Rimnaissance
{
    public class PlaceWorker_AirHole : PlaceWorker
    {
        public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
        {
            return ((loc.GetEdifice(map)?.def?.graphicData?.linkFlags & (LinkFlags.Wall | LinkFlags.Rock)) > 0 && (loc.GetEdifice(map)?.def?.building?.isNaturalRock != true)) ? AcceptanceReport.WasAccepted : "Rimnaissance.PlaceOnlyOnTheWall".Translate();
        }
    }
}
