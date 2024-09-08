using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace Rimnaissance
{
    [StaticConstructorOnStartup]
    public class CompWindDrill : ThingComp
    {
        public CompProperties_WindDrill Props => (CompProperties_WindDrill)props;

        public float portionProgress;

        public float portionYieldPct;

        public int lastUsedTick = -99999;

        public int weatherUpdateEveryXTicks = 250;

        public int ticksUntilWeatherUpdate;

        public List<IntVec3> windPathCells = new List<IntVec3>();

        public List<Thing> windPathBlockedByThings = new List<Thing>();

        public List<IntVec3> windPathBlockedCells = new List<IntVec3>();

        public float windSpeed;

        public float sailRotation;

        public Rot4 sailBase;

        public bool Working => windSpeed >= 0.25f;

        public const float WorkPerPortionBase = 12000f;

        public static float WorkPerPortionCurrentDifficulty => 12000f / Find.Storyteller.difficulty.mineYieldFactor;

        public float ProgressToNextPortionPercent => portionProgress / WorkPerPortionCurrentDifficulty;

        public Building windDrill => parent as Building;

        public override void CompTick()
        {
            base.CompTick();

            if (sailRotation >= 360f)
            {
                sailRotation = 0f;
            }
            else
            {
                sailRotation += windSpeed / 1.5f * 4f;
            }

            ticksUntilWeatherUpdate++;

            if (ticksUntilWeatherUpdate >= weatherUpdateEveryXTicks)
            {
                GetPerformFactor(windDrill, out float windFactor);

                windSpeed = windFactor;

                ticksUntilWeatherUpdate = 0;

                CheckForBlockages();

                if (windPathBlockedCells.Count > 0)
                {
                    float num = 0f;

                    for (int i = 0; i < windPathBlockedCells.Count; i++)
                    {
                        num += windSpeed * 0.1f;
                    }

                    windSpeed -= num;

                    if (windSpeed < 0)
                    {
                        windSpeed = 0;
                    }
                }
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref portionProgress, "portionProgress", 0f);
            Scribe_Values.Look(ref portionYieldPct, "portionYieldPct", 0f);
            Scribe_Values.Look(ref lastUsedTick, "lastUsedTick", 0);
            Scribe_Values.Look(ref ticksUntilWeatherUpdate, "updateCounter", 0);
            Scribe_Values.Look(ref windSpeed, "windSpeed", 0f);
            Scribe_Values.Look(ref sailBase, "sailBase", defaultValue: Rot4.North, forceSave: true);
        }

        public override void PostDraw()
        {
            base.PostDraw();

            GraphicData sailGraphicData = Props.sailGraphicData;

            if (sailGraphicData != null)
            {
                Mesh sailMesh = sailGraphicData.Graphic.MeshAt(sailBase);
                Material sailMats;
                
                Vector3 sailDrawOffset = sailGraphicData.drawOffset;
                Vector3 sailDrawPos = parent.DrawPos + sailDrawOffset;
                sailDrawPos.y = AltitudeLayer.Gas.AltitudeFor();
                Matrix4x4 sailMatrix = Matrix4x4.TRS(sailDrawPos, Quaternion.AngleAxis(sailRotation, Vector3.up), Vector3.one);

                if (sailGraphicData.shaderType == ShaderTypeDefOf.CutoutComplex)
                {
                    sailMats = sailGraphicData.Graphic.GetColoredVersion(sailGraphicData.Graphic.Shader, parent.Graphic.Color, parent.Graphic.ColorTwo).MatAt(sailBase);

                    Graphics.DrawMesh(sailMesh, sailMatrix, sailMats, 0);
                }
                else if (sailGraphicData.shaderType == null)
                {
                    sailGraphicData.shaderType = ShaderTypeDefOf.Cutout;
                    sailMats = sailGraphicData.Graphic.MatAt(sailBase);

                    Graphics.DrawMesh(sailMesh, sailMatrix, sailMats, 0);
                }
            }
        }

        public void WindDrillWorkDone(Pawn driller)
        {
            float performFactor = GetPerformFactor(windDrill, out float windFactor);

            if (Working)
            {
                float statValue = driller.GetStatValue(StatDefOf.DeepDrillingSpeed) / 2f * performFactor;
                portionProgress += statValue;
                portionYieldPct += statValue * driller.GetStatValue(StatDefOf.MiningYield) / WorkPerPortionCurrentDifficulty;
            }

            lastUsedTick = Find.TickManager.TicksGame;
            if (portionProgress > WorkPerPortionCurrentDifficulty)
            {
                TryProducePortion(portionYieldPct, driller);
                portionProgress = 0f;
                portionYieldPct = 0f;
            }

            windSpeed = windFactor;
        }

        public override void PostDeSpawn(Map map)
        {
            portionProgress = 0f;
            portionYieldPct = 0f;
            lastUsedTick = -99999;
        }

        public void TryProducePortion(float yieldPct, Pawn driller = null)
        {
            ThingDef resDef;
            int countPresent;
            IntVec3 cell;
            bool nextResource = GetNextResource(out resDef, out countPresent, out cell);
            if (resDef == null)
            {
                return;
            }
            int num = Mathf.Min(countPresent, resDef.deepCountPerPortion);
            if (nextResource)
            {
                parent.Map.deepResourceGrid.SetAt(cell, resDef, countPresent - num);
            }
            int stackCount = Mathf.Max(1, GenMath.RoundRandom((float)num * yieldPct));
            Thing thing = ThingMaker.MakeThing(resDef);
            thing.stackCount = stackCount;
            GenPlace.TryPlaceThing(thing, parent.InteractionCell, parent.Map, ThingPlaceMode.Near, null, (IntVec3 p) => p != parent.Position && p != parent.InteractionCell);
            if (driller != null)
            {
                Find.HistoryEventsManager.RecordEvent(new HistoryEvent(HistoryEventDefOf.Mined, driller.Named(HistoryEventArgsNames.Doer)));
            }
            if (!nextResource || ValuableResourcesPresent())
            {
                return;
            }
            if (DeepDrillUtility.GetBaseResource(parent.Map, parent.Position) == null)
            {
                Messages.Message("DeepDrillExhaustedNoFallback".Translate(), parent, MessageTypeDefOf.TaskCompletion);
                return;
            }
            Messages.Message("DeepDrillExhausted".Translate(Find.ActiveLanguageWorker.Pluralize(DeepDrillUtility.GetBaseResource(parent.Map, parent.Position).label)), parent, MessageTypeDefOf.TaskCompletion);
            for (int i = 0; i < 9; i++)
            {
                IntVec3 c = cell + GenRadial.RadialPattern[i];
                if (c.InBounds(parent.Map))
                {
                    ThingWithComps firstThingWithComp = c.GetFirstThingWithComp<CompDeepDrill>(parent.Map);
                    if (firstThingWithComp != null && !firstThingWithComp.GetComp<CompDeepDrill>().ValuableResourcesPresent())
                    {
                        firstThingWithComp.SetForbidden(value: true);
                    }
                }
            }
        }

        public bool GetNextResource(out ThingDef resDef, out int countPresent, out IntVec3 cell)
        {
            return WindDrillUtility.GetNextResource(parent.Position, parent.Map, out resDef, out countPresent, out cell);
        }

        public float GetPerformFactor(Building windDrill, out float performFactor)
        {
            return WindDrillUtility.GetPerformFactor(windDrill, out performFactor);
        }

        public bool CanWindDrillNow()
        {
            if (!Working)
            {
                return false;
            }
            if (DeepDrillUtility.GetBaseResource(parent.Map, parent.Position) != null)
            {
                return true;
            }

            return ValuableResourcesPresent();
        }

        public bool ValuableResourcesPresent()
        {
            ThingDef resDef;
            int countPresent;
            IntVec3 cell;
            return GetNextResource(out resDef, out countPresent, out cell);
        }

        public bool UsedLastTick()
        {
            return lastUsedTick >= Find.TickManager.TicksGame - 1;
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo item in base.CompGetGizmosExtra())
            {
                yield return item;
            }
            if (DebugSettings.ShowDevGizmos)
            {
                Command_Action command_Action = new Command_Action();
                command_Action.defaultLabel = "DEV: Produce portion (100% yield)";
                command_Action.action = delegate
                {
                    TryProducePortion(1f);
                };
                yield return command_Action;
            }
        }

        public override string CompInspectStringExtra()
        {
            StringBuilder stringBuilder = new StringBuilder();

            string devWindMsg;
            string resourceMsg;
            string oreMsg;
            string progressMsg;
            string weatherMsg;

            if (parent.Spawned)
            {
                if (DebugSettings.ShowDevGizmos)
                {
                    devWindMsg = "DEV: Wind speed (" + windSpeed + " / " + parent.Map.windManager.WindSpeed + ")";
                    stringBuilder.AppendLine(devWindMsg);
                }

                GetNextResource(out var resDef, out var _, out var _);
                if (resDef == null)
                {
                    resourceMsg = "DrillNoResources".Translate();
                    stringBuilder.AppendLine(resourceMsg);
                }

                oreMsg = "ResourceBelow".Translate() + ": " + resDef.LabelCap;
                stringBuilder.AppendLine(oreMsg);

                progressMsg = "ProgressToNextPortion".Translate() + ": " + ProgressToNextPortionPercent.ToStringPercent("F0");
                stringBuilder.AppendLine(progressMsg);

                if (!Working)
                {
                    weatherMsg = "Rimnaissance.InsufficientWind".Translate();
                    stringBuilder.AppendLine(weatherMsg);
                }
                else if (UsedLastTick())
                {
                    float windPercent = windSpeed / 1f;
                    weatherMsg = "Rimnaissance.WindPerform".Translate(windPercent.ToStringPercent("F0"));
                    stringBuilder.AppendLine(weatherMsg);
                }
            }
            return stringBuilder.ToString().TrimEndNewlines();
        }

        public static IEnumerable<IntVec3> CalculateWindCells(IntVec3 position, float radius)
        {
            return GenRadial.RadialCellsAround(position, radius, true);
        }

        public static bool ObstructedCellsWithinRadius(IntVec3 position, Map map, float radius)
        {
            return GenRadial.RadialCellsAround(position, radius, true).Where(c => c.InBounds(map) && c.Impassable(map)).Count() > 0;
        }

        public static bool OtherWindDrillOrBlueprintWithinRadius(IntVec3 position, Map map, float radius)
        {
            return GenRadial.RadialCellsAround(position, radius, true).Where(c => c.InBounds(map) && (c.GetFirstThing(map, RimnaissanceDefOf.Blueprint_RNS_WindDrill) != null || c.GetFirstThing(map, RimnaissanceDefOf.RNS_WindDrill) != null)).Count() > 0;
        }

        public void CheckForBlockages()
        {
            if (windPathCells.Count == 0)
            {
                IEnumerable<IntVec3> collection = CalculateWindCells(parent.Position, Props.obstructionFreeRadius);
                windPathCells.AddRange(collection);
            }
            windPathBlockedCells.Clear();
            windPathBlockedByThings.Clear();

            for (int i = 0; i < windPathCells.Count; i++)
            {
                IntVec3 vec = windPathCells[i];
                if (parent.Map.roofGrid.Roofed(vec))
                {
                    windPathBlockedCells.Add(vec);
                    windPathBlockedByThings.Add(null);
                    continue;
                }
                List<Thing> list = parent.Map.thingGrid.ThingsListAt(vec);

                for (int j = 0; j < list.Count; j++)
                {
                    Thing thing = list[j];

                    if (thing.def.blockWind && thing.def != parent.def)
                    {
                        windPathBlockedCells.Add(vec);
                        windPathBlockedByThings.Add(thing);
                        break;
                    }
                }
            }
        }
    }
}
