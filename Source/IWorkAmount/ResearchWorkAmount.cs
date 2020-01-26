using System.Collections.Generic;
using Verse;

namespace WorkRebalancer
{
    public class ResearchWorkAmount : IWorkAmount
    {
        public static IEnumerable<IWorkAmount> GetAll()
        {
            foreach (var x in DefDatabase<ResearchProjectDef>.AllDefsListForReading)
            {
                ResearchWorkAmount research = new ResearchWorkAmount(x);
                if (research.HasWorkValue())
                {
                    yield return research;
                }
            }
        }

        private float baseCost;

        public ResearchWorkAmount(ResearchProjectDef def)
        {
            Ref = def;
            baseCost = def.baseCost;
        }

        public object Ref { get; set; }

        public bool HasWorkValue() => true;

        public void Restore()
        {
            ResearchProjectDef def = (ResearchProjectDef) Ref;
            def.baseCost = baseCost;
        }

        public void Set(float percentOfBaseValue)
        {
            ResearchProjectDef def = (ResearchProjectDef) Ref;
            Loger.Append($"{def.LabelCap} : {baseCost} => ");
            def.baseCost = baseCost * percentOfBaseValue;
            Loger.AppendLine($"{def.baseCost}");
        }
    }
}