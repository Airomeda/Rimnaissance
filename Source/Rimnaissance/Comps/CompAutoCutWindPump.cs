using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace Rimnaissance
{
    public class CompAutoCutWindPump : CompAutoCutWindTurbine
    {
        public override IEnumerable<IntVec3> GetAutoCutCells()
        {
            return CompWindPump.CalculateWindCells(parent.Position, parent.TryGetComp<CompWindPump>().Props.obstructionFreeRadius);
        }
    }
}
