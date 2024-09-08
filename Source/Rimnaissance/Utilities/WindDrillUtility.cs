using RimWorld;
using UnityEngine;
using Verse;

namespace Rimnaissance
{
    public static class WindDrillUtility
    {
        public const int NumCellsToScan = 9;

        public static bool GetNextResource(IntVec3 p, Map map, out ThingDef resDef, out int countPresent, out IntVec3 cell)
        {
            for (int i = 0; i < NumCellsToScan; i++)
            {
                IntVec3 intVec = p + GenRadial.RadialPattern[i];
                if (intVec.InBounds(map))
                {
                    ThingDef thingDef = map.deepResourceGrid.ThingDefAt(intVec);
                    if (thingDef != null)
                    {
                        resDef = thingDef;
                        countPresent = map.deepResourceGrid.CountAt(intVec);
                        cell = intVec;
                        return true;
                    }
                }
            }
            resDef = DeepDrillUtility.GetBaseResource(map, p);
            countPresent = int.MaxValue;
            cell = p;
            return false;
        }

        public static float GetPerformFactor(Building windDrill, out float windFactor)
        {
            float performFactor = 1f;
            windFactor = Mathf.Min(windDrill.Map.windManager.WindSpeed, 1.5f);

            if (windDrill.TryGetComp<CompWindDrill>().parent.Map != null && windDrill.Spawned)
            {
                return performFactor * windFactor;
            }

            if (windDrill.Map != null)
            {
                windFactor = Mathf.Min(windDrill.Map.windManager.WindSpeed, 1.5f);
            }

            return performFactor * windFactor;
        }
    }
}
