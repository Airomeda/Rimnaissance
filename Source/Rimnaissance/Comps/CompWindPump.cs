using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace Rimnaissance
{
    public class CompWindPump : ThingComp
    {
        public CompProperties_WindPump Props => (CompProperties_WindPump)props;

        public int progressTicks;

        public float ProgressDays => (float) progressTicks / 60000f;

        public float CurrentRadius => Mathf.Min(Props.radius, ProgressDays / Props.daysToRadius * Props.radius);

        public int CachedNumOfCells = 0;

        public int weatherUpdateEveryXTicks = 250;

        public int ticksUntilWeatherUpdate;

        public List<IntVec3> windPathCells = new List<IntVec3>();

        public List<Thing> windPathBlockedByThings = new List<Thing>();

        public List<IntVec3> windPathBlockedCells = new List<IntVec3>();

        public float windSpeed;

        public float sailRotation;

        public Rot4 sailBase;

        public bool Working => windSpeed >= 0.1f;

        public int TicksUntilRadiusInteger
        {
            get
            {
                float num = Mathf.Ceil(CurrentRadius) - CurrentRadius;
                if (num < 1E-05f)
                {
                    num = 1f;
                }
                float num2 = Props.radius / Props.daysToRadius;
                return (int)(num / num2 * 60000f);
            }
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
        }

        public override void PostDeSpawn(Map map)
        {
            progressTicks = 0;
        }

        public override void CompTick()
        {
            base.CompTick();

            if (sailRotation >= 360f)
            {
                sailRotation = 0f;
            }
            else
            {
                sailRotation += windSpeed / 1.5f * 3f; ;
            }

            ticksUntilWeatherUpdate++;

            if (ticksUntilWeatherUpdate >= weatherUpdateEveryXTicks)
            {
                windSpeed = Mathf.Min(parent.Map.windManager.WindSpeed, 1.5f);

                ticksUntilWeatherUpdate = 0;

                CheckForBlockages();

                if (windPathBlockedCells.Count > 0)
                {
                    float num = 0f;

                    for (int i = 0; i < windPathBlockedCells.Count; i++)
                    {
                        num += windPathBlockedCells.Count * 0.1f;

                        windSpeed -= num;

                        if (windSpeed < 0)
                        {
                            windSpeed = 0;
                        }
                    }
                }
            }

            if (parent.IsHashIntervalTick(250))
            {
                if (Working)
                {
                    progressTicks += 250;

                    int num = GenRadial.NumCellsInRadius(CurrentRadius);

                    if (num > CachedNumOfCells)
                    {
                        for (int i = 0; i < num; i++)
                        {
                            AffectCell(parent.Position + GenRadial.RadialPattern[i]);
                        }
                        CachedNumOfCells = num;
                    }
                }
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref windSpeed, "windSpeed", 0f);
            Scribe_Values.Look(ref sailBase, "sailBase", defaultValue: Rot4.North, forceSave: true);
            Scribe_Values.Look(ref ticksUntilWeatherUpdate, "updateCounter", 0);
            Scribe_Values.Look(ref progressTicks, "progressTicks", 0);
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

        public override void PostDrawExtraSelectionOverlays()
        {
            if (CurrentRadius < Props.radius - 0.0001f)
            {
                GenDraw.DrawRadiusRing(parent.Position, CurrentRadius, Color.green);
                GenDraw.DrawRadiusRing(parent.Position, Props.radius);
            }
        }

        public override string CompInspectStringExtra()
        {
            StringBuilder stringBuilder = new StringBuilder();

            string devWindMsg;
            string timeMsg;
            string statusMsg;

            timeMsg = "TimePassed".Translate().CapitalizeFirst() + ": " + progressTicks.ToStringTicksToPeriod() + "\n" + "CurrentRadius".Translate().CapitalizeFirst() + ": " + CurrentRadius.ToString("F1");
            stringBuilder.AppendLine(timeMsg);

            if (parent.Spawned)
            {
                if (DebugSettings.ShowDevGizmos)
                {
                    devWindMsg = "DEV: Wind speed (" + windSpeed + " / " + parent.Map.windManager.WindSpeed + ")";
                    stringBuilder.AppendLine(devWindMsg);
                }

                if (ProgressDays < Props.daysToRadius)
                {
                    if (Working)
                    {
                        statusMsg = "RadiusExpandsIn".Translate().CapitalizeFirst() + ": " + TicksUntilRadiusInteger.ToStringTicksToPeriod();
                        stringBuilder.AppendLine(statusMsg);
                    }
                    else
                    {
                        statusMsg = "Rimnaissance.InsufficientWind".Translate();
                        stringBuilder.AppendLine(statusMsg);
                    }
                }
            }

            return stringBuilder.ToString().TrimEndNewlines();
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (DebugSettings.ShowDevGizmos)
            {
                Command_Action command_Action = new Command_Action();
                command_Action.defaultLabel = "DEV: Progress 1 day";
                command_Action.action = delegate
                {
                    progressTicks += 60000;
                };
                yield return command_Action;
            }
        }

        public void AffectCell(IntVec3 c)
        {
            AffectCell(parent.Map, c);
        }

        public static void AffectCell(Map map, IntVec3 c)
        {
            TerrainDef terrain = c.GetTerrain(map);
            TerrainDef terrainToDryTo = GetTerrainToDryTo(map, terrain);
            if (terrainToDryTo != null)
            {
                map.terrainGrid.SetTerrain(c, terrainToDryTo);
            }
            TerrainDef terrainDef = map.terrainGrid.UnderTerrainAt(c);
            if (terrainDef != null)
            {
                TerrainDef terrainToDryTo2 = GetTerrainToDryTo(map, terrainDef);
                if (terrainToDryTo2 != null)
                {
                    map.terrainGrid.SetUnderTerrain(c, terrainToDryTo2);
                }
            }
        }

        public static TerrainDef GetTerrainToDryTo(Map map, TerrainDef terrainDef)
        {
            if (terrainDef.driesTo == null)
            {
                return null;
            }
            if (map.Biome == BiomeDefOf.SeaIce)
            {
                return TerrainDefOf.Ice;
            }
            return terrainDef.driesTo;
        }

        public static IEnumerable<IntVec3> CalculateWindCells(IntVec3 position, float radius)
        {
            return GenRadial.RadialCellsAround(position, radius, true);
        }

        public static bool ObstructedCellsWithinRadius(IntVec3 position, Map map, float radius)
        {
            return GenRadial.RadialCellsAround(position, radius, true).Where(c => c.InBounds(map) && c.Impassable(map)).Count() > 0;
        }

        public static bool OtherWindPumpOrBlueprintWithinRadius(IntVec3 position, Map map, float radius)
        {
            return GenRadial.RadialCellsAround(position, radius, true).Where(c => c.InBounds(map) && (c.GetFirstThing(map, RimnaissanceDefOf.Blueprint_RNS_WindPump) != null || c.GetFirstThing(map, RimnaissanceDefOf.RNS_WindPump) != null)).Count() > 0;
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
