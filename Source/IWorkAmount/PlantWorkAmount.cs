using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace WorkRebalancer
{
    public class PlantWorkAmount : IWorkAmount
    {
        public static IEnumerable<IWorkAmount> GetAll()
        {
            foreach (var x in DefDatabase<ThingDef>.AllDefsListForReading.Where(x => x.plant != null))
            {
                PlantWorkAmount plantWork = new PlantWorkAmount(x);
                if (plantWork.HasWorkValue())
                {
                    yield return plantWork;
                }
            }
        }

        private float? sowWork;
        private float? harvestWork;

        public PlantWorkAmount(ThingDef def)
        {
            Ref = def;
            sowWork = def.plant.sowWork;
            harvestWork = def.plant.harvestWork;
        }

        public object Ref { get; set; }

        public bool HasWorkValue() => true;

        public void Restore()
        {
            ThingDef def = (ThingDef) Ref;
            def.plant.sowWork = (float) sowWork;
            def.plant.harvestWork = (float) harvestWork;
        }

        public void Set(float percentOfBaseValue)
        {
            ThingDef def = (ThingDef) Ref;
            Loger.Append($"{def.LabelCap} {def.plant.sowWork} => ");
            def.plant.sowWork = (float) sowWork * percentOfBaseValue;
            Loger.AppendLine($"{def.plant.sowWork}");

            Loger.Append($"{def.LabelCap} {def.plant.harvestWork} => ");
            def.plant.harvestWork = (float) harvestWork * percentOfBaseValue;
            Loger.AppendLine($"{def.plant.harvestWork}");
        }
    }
}