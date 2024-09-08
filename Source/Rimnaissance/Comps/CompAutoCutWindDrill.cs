using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace Rimnaissance
{
    public class CompAutoCutWindDrill : CompAutoCutWindTurbine
    {
        public override IEnumerable<IntVec3> GetAutoCutCells()
        {
            return CompWindDrill.CalculateWindCells(parent.Position, parent.TryGetComp<CompWindDrill>().Props.obstructionFreeRadius);
        }
    }
}
