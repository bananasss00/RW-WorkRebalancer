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

            if (Utils.UF_Active)
            {
                foreach (var recipe in RecipeUFWorkAmount.GetAll())
                {
                    if (recipe.HasWorkValue())
                    {
                        yield return recipe;
                    }
                }
            }
        }

        private IWorkAmount customRecipeWork;
        private float workAmount;

        public RecipeWorkAmount(RecipeDef def)
        {
            Ref = def;
            workAmount = def.workAmount;
        }

        public RecipeWorkAmount(IWorkAmount workAmount)
        {
            customRecipeWork = workAmount;
            Ref = workAmount.Ref;
        }

        public bool HasWorkValue()
        {
            if (customRecipeWork != null)
            {
                return customRecipeWork.HasWorkValue();
            }
            return true;
        }

        public object Ref { get; set; }

        public void Restore()
        {
            if (customRecipeWork != null)
            {
                customRecipeWork.Restore();
                return;
            }
            RecipeDef def = (RecipeDef) Ref;
            def.workAmount = workAmount;
        }

        public void Set(float percentOfBaseValue)
        {
            if (customRecipeWork != null)
            {
                customRecipeWork.Set(percentOfBaseValue);
                return;
            }

            RecipeDef def = (RecipeDef) Ref;
            Loger.Append($"{def.LabelCap} : {workAmount} => ");
            def.workAmount = workAmount * percentOfBaseValue;
            Loger.AppendLine($"{def.workAmount}");
        }
    }
}