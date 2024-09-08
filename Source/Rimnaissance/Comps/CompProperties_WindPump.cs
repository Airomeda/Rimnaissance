using Verse;

namespace Rimnaissance
{
    public class CompProperties_WindPump : CompProperties
    {
        public GraphicData sailGraphicData;

        public float radius = 5.9f;

        public float daysToRadius = 5;

        public float obstructionFreeRadius = 1.5f;

        public CompProperties_WindPump()
        {
            compClass = typeof(CompWindPump);
        }
    }
}
