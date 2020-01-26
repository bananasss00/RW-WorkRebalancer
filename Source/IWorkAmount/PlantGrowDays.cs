using System.Collections.Generic;
using System.Linq;
using Verse;

namespace WorkRebalancer
{
    public class PlantGrowDays : IWorkAmount
    {
        public static IEnumerable<IWorkAmount> GetAll()
        {
            foreach (var x in DefDatabase<ThingDef>.AllDefsListForReading.Where(x => x.plant != null))
            {
                PlantGrowDays plantGrowDays = new PlantGrowDays(x);
                if (plantGrowDays.HasWorkValue())
                {
                    yield return plantGrowDays;
                }
            }
        }

        private float? growDays;

        public PlantGrowDays(ThingDef def)
        {
            Ref = def;
            growDays = def.plant.growDays;
        }

        public object Ref { get; set; }

        public bool HasWorkValue() => true;

        public void Restore()
        {
            ThingDef def = (ThingDef) Ref;
            def.plant.growDays = (float) growDays;
        }

        public void Set(float percentOfBaseValue)
        {
            ThingDef def = (ThingDef) Ref;
            Loger.Append($"{def.LabelCap} {def.plant.growDays} => ");
            def.plant.growDays = (float) growDays * percentOfBaseValue;
            Loger.AppendLine($"{def.plant.growDays}");
        }
    }
}