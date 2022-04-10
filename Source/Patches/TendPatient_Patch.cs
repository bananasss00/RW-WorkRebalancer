using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using HaulExplicitly;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace WorkRebalancer.Patches
{
    public class HaulExplicityFix
    {
        public static bool HaulExplicityActive => ModLister.GetActiveModWithIdentifier("likeafox.haulexplicitly") != null;

        public static bool ApplyFix(Harmony h)
        {
            var method = typeof(Toils_Haul).GetMethod(nameof(Toils_Haul.CheckForGetOpportunityDuplicate));
            if (method == null)
                return false;

            var prefixMethod = new HarmonyMethod(typeof(HaulExplicityFix).GetMethod(nameof(Prefix), AccessTools.all));
            h.Patch(method, prefix: prefixMethod);
            // unpatch HaulExplicity prefix without try .. catch
            h.Unpatch(method, HarmonyPatchType.Prefix, harmonyID: "likeafox.rimworld.haulexplicitly");
            return true;
        }

        // stackFrame.GetMethod().GetMethodBody() cause exception on harmony patched methods!
        /*
        Exception: System.InvalidOperationException
          at (wrapper managed-to-native) System.Reflection.MethodBase.GetMethodBodyInternal(intptr)
          at System.Reflection.MethodBase.GetMethodBody (System.IntPtr handle) [0x00000] in <eae584ce26bc40229c1b1aa476bfa589>:0 
          at System.Reflection.MonoMethod.GetMethodBody () [0x00000] in <eae584ce26bc40229c1b1aa476bfa589>:0 
          at RuntimeCode.Initializer.CheckForGetOpportunityDuplicatePrefix (System.Predicate`1[Verse.Thing]& extraValidator) [0x00020] in <1f26a3fc82844b4b99365927a3abb878>:0 . 
         */
        // Added try ... catch block
        private static void Prefix(ref Predicate<Thing> extraValidator)
        {
            StackFrame stackFrame = MiscUtil.StackFrameWithMethod("<MakeNewToils>", 3);
            if (stackFrame != null)
            {
                try
                {
                    IList<LocalVariableInfo> localVariables = stackFrame.GetMethod().GetMethodBody().LocalVariables;
                    bool flag = (from l in localVariables
                        select l.LocalType into lt
                        where lt == typeof(JobDriver_HaulToCell)
                        select lt).Any<Type>();

                    if (flag)
                    {
                        Predicate<Thing> other_test = extraValidator;
                        Predicate<Thing> predicate = (Thing t) => t.IsAHaulableSetToHaulable() && (other_test == null || other_test(t));
                        extraValidator = predicate;
                    }
                }
                catch 
                {
                }
            }
        }
    }

    public class TendPatient_Patch
    {
        public const int vanillaValue = 600; // vanilla multiplier value
        private static bool result = false;

        public static bool Apply(Harmony h)
        {
            // in dnSpy it's function MakeNewToils, but this real place!
            var makeNewToils = typeof(JobDriver_TendPatient)
                    .GetNestedTypes(AccessTools.all)?
                    .FirstOrDefault(t => t.Name.Equals("<MakeNewToils>d__17"))?
                    .GetMethods(AccessTools.all)
                    .FirstOrDefault(m => m.Name.Equals("MoveNext"));

            if (makeNewToils == null)
                return false;

            h.Patch(makeNewToils, transpiler: new HarmonyMethod(typeof(TendPatient_Patch), nameof(Transpiler)));

            if (result && HaulExplicityFix.HaulExplicityActive)
            {
                if (HaulExplicityFix.ApplyFix(h))
                    Log.Message($"[WorkRebalancer] Apply HaulExplicityFix for support TendPatient_Patch");
                else
                    Log.Error($"[WorkRebalancer] Can't apply HaulExplicityFix for support TendPatient_Patch");
            }

            return result;
        }

        public static float GetTendTicks()
        {
            if (WorkRebalancerMod.Instance.Prof.RestoreWhenHostileDetected &&
                HostileHandler.HostileDetected)
                return vanillaValue;

            return vanillaValue * (WorkRebalancerMod.Instance.Prof.PercentOfBaseTendPatient / 100f);
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instr)
        {
            var getTendTicks = typeof(TendPatient_Patch).GetMethod(nameof(GetTendTicks));
            foreach (var instruction in instr)
            {
                if (instruction.opcode == OpCodes.Ldc_R4 && (float)instruction.operand == 600)
                {
                    yield return new CodeInstruction(OpCodes.Call, getTendTicks);
                    result = true;
                }
                else
                {
                    yield return instruction;
                }
            }
        }
    }
}

/*
protected override IEnumerable<Toil> MakeNewToils()
{
	....
	Toil toil = Toils_General.Wait((int)(1f / this.pawn.GetStatValue(StatDefOf.MedicalTendSpeed, true) * 600f), 
    ....
}
 */