using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace Rimnaissance
{
    [HarmonyPatch(typeof(Building), "Destroy")]
    public static class Harmony_Building_Destroy_DestroyAirHole
    {
        public static void Prefix(Building __instance)
        {
            ThingDef def = __instance.def;
            if (def == null)
            {
                return;
            }

            Map map = __instance.Map;
            if (map != null)
            {
                BuildingProperties building = def.building;
                if (building != null )
                {
                    CheckAirHole();
                }
            }

            void CheckAirHole()
            {
                List<Thing> list = map.thingGrid.ThingsListAtFast(__instance.Position);
                int count = list.Count;

                while (count-- > 0)
                {
                    Thing thing = list[count];
                    List<Type> placeWorkers = thing.def.placeWorkers;

                    if (placeWorkers != null && placeWorkers.Contains(typeof(PlaceWorker_AirHole)))
                    {
                        thing.Destroy();
                        break;
                    }
                }
            }
        }
    }
}
