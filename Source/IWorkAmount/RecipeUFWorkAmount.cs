using System.Collections.Generic;
using System.Linq;
using UniversalFermenterSK;
using Verse;

namespace WorkRebalancer
{
    public class RecipeUFWorkAmount : IWorkAmount
    {
        // if used IEnumerable<...> return type or Linq, vanilla - assembly resolution exception!
        public static List<IWorkAmount> GetAll()
        {
            // return DefDatabase<RecipeDef_UF>.AllDefsListForReading
            //     .Select(x => new RecipeWorkAmount(new RecipeUFWorkAmount(x)))
            //     .Cast<IWorkAmount>()
            //     .ToList();
            var result = new List<IWorkAmount>();
            foreach (var x in DefDatabase<RecipeDef_UF>.AllDefsListForReading)
            {
                RecipeWorkAmount recipe = new RecipeWorkAmount(new RecipeUFWorkAmount(x));
                result.Add(recipe);
            }
            return result;
        }

        private float processDays;

        public RecipeUFWorkAmount(object def)
        {
            RecipeDef_UF _def = (RecipeDef_UF)def;
            Ref = _def;
            processDays = _def.processDays;
        }

        public bool HasWorkValue() => true;

        public object Ref { get; set; }

        public void Restore()
        {
            RecipeDef_UF def = (RecipeDef_UF)Ref;
            def.processDays = processDays;
        }

        public void Set(float percentOfBaseValue)
        {
            RecipeDef_UF def = (RecipeDef_UF)Ref;
            Loger.Append($"[UF] {def.LabelCap} : {processDays} => ");
            def.processDays = processDays * percentOfBaseValue;
            Loger.AppendLine($"{def.processDays}");
        }
    }
}