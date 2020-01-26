using System.Collections.Generic;
using Verse;

namespace WorkRebalancer
{
    public class RecipeWorkAmount : IWorkAmount
    {
        public static IEnumerable<IWorkAmount> GetAll()
        {
            foreach (var x in DefDatabase<RecipeDef>.AllDefsListForReading)
            {
                RecipeWorkAmount recipe = new RecipeWorkAmount(x);
                if (recipe.HasWorkValue())
                {
                    yield return recipe;
                }
            }
        }

        private float workAmount;

        public RecipeWorkAmount(RecipeDef def)
        {
            Ref = def;
            workAmount = def.workAmount;
        }

        public bool HasWorkValue() => true;

        public object Ref { get; set; }

        public void Restore()
        {
            RecipeDef def = (RecipeDef) Ref;
            def.workAmount = workAmount;
        }

        public void Set(float percentOfBaseValue)
        {
            RecipeDef def = (RecipeDef) Ref;
            Loger.Append($"{def.LabelCap} : {workAmount} => ");
            def.workAmount = workAmount * percentOfBaseValue;
            Loger.AppendLine($"{def.workAmount}");
        }
    }
}