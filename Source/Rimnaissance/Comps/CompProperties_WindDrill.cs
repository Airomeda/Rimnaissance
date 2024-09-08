using Verse;

namespace Rimnaissance
{
    public class CompProperties_WindDrill : CompProperties
    {
        public float obstructionFreeRadius = 2.9f;

        public GraphicData sailGraphicData;

        public CompProperties_WindDrill()
        {
            compClass = typeof(CompWindDrill);
        }
    }
}
