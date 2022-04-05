using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using HugsLib;
using HugsLib.Settings;
using Verse;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse.AI;
using WorkRebalancer.Patches;

namespace WorkRebalancer
{
    /// <summary>
    /// Pawns teleporter. Control icon
    /// </summary>
    [HarmonyPatch(typeof(PlaySettings), "DoPlaySettingsGlobalControls")]
    public static class PlaySettings_Patch
    {
        private static void Postfix(WidgetRow row, bool worldView)
        {
            if (worldView)
                return;

            if (WorkRebalancerMod.Instance.Prof.ShowInstantMovingIcon)
            {
                row?.ToggleableIcon(ref Pawn_PathFollower_Patch.InstantMoving, FastMovingTex,
                    "FastMovingIconDesc".Translate(), SoundDefOf.Mouseover_ButtonToggle);

                if (WorkRebalancerMod.Instance.Prof.InstantMovingAutooffOnPause && Find.TickManager.Paused)
                    Pawn_PathFollower_Patch.InstantMoving = false;
            }
            //if (WorkRebalancerMod.Instance.Prof.ShowFastPawnsTicksIcon)
            //{
            //    row?.ToggleableIcon(ref Pawn_Tick_Patch.FastPawnsTicks, FastPawnsTicksTex,
            //        "FastPawnsTicksIconDesc".Translate(), SoundDefOf.Mouseover_ButtonToggle);
            //}
            if (WorkRebalancerMod.Instance.Prof.ShowFastTimeIcon)
            {
                row?.ToggleableIcon(ref TickManager_DoSingleTick_Patch.FastTime, FastTimeTex,
                    "FastTimeIconDesc".Translate(), SoundDefOf.Mouseover_ButtonToggle);
            }
        }

        public static readonly Texture2D FastTimeTex = ContentFinder<Texture2D>.Get("UIcons/FastTime");
        public static readonly Texture2D FastPawnsTicksTex = ContentFinder<Texture2D>.Get("UIcons/FastPawnsTicks");
        public static readonly Texture2D FastMovingTex = ContentFinder<Texture2D>.Get("UIcons/FastMoving");
    }

    /// <summary>
    /// Time booster
    /// </summary>
    [HarmonyPatch(typeof(TickManager), nameof(TickManager.DoSingleTick))]
    public static class TickManager_DoSingleTick_Patch
    {
        private static void Postfix(TickManager __instance)
        {
            if (!WorkRebalancerMod.Instance.Prof.ShowFastTimeIcon)
                FastTime = false; // if icon disabled, disable this feature too

            if (FastTime)
            {
                int add = WorkRebalancerMod.Instance.Prof.FastTimeMultiplier;
                if (add > 0)
                    __instance.ticksGameInt += add;
            }
        }

        public static bool FastTime = false;
    }

    /// <summary>
    /// Pawns teleporter
    /// </summary>
    [HarmonyPatch(typeof(Pawn_PathFollower), nameof(Pawn_PathFollower.PatherTick))]
    [HarmonyPriority(9999999)]
    public static class Pawn_PathFollower_Patch
    {
        public static bool InstantMoving;

        [HarmonyPrefix]
        public static void PatherTick(Pawn_PathFollower __instance)
        {
            if (!InstantMoving)
                return;

            if (!WorkRebalancerMod.Instance.Prof.ShowInstantMovingIcon)
            {
                InstantMoving = false;
                return;
            }

            if (WorkRebalancerMod.Instance.Prof.RestoreWhenHostileDetected &&
                HostileHandler.HostileDetected)
                return;

            if (WorkRebalancerMod.Instance.Prof.InstantMovingOnlyColonists && __instance.pawn.Faction != Faction.OfPlayer)
                return;

            if (__instance.destination.Cell == IntVec3.Zero)
                return;

            // move in middle path points
            if (WorkRebalancerMod.Instance.Prof.InstantMovingSmoother)
            {
                __instance.pawn.Position = __instance.nextCell;
            }
            // move in end point
            else
            {
                __instance.pawn.Position = __instance.destination.Cell;
                if (CellFinder.TryFindBestPawnStandCell(__instance.pawn, out IntVec3 intVec, true) && intVec != __instance.pawn.Position)
                    __instance.pawn.Position = intVec;
            }
            __instance.ResetToCurrentPosition();
        }
    }

    /// <summary>
    /// Handle signals for forceNormalSpeed = fast hostile detected
    /// </summary>
    [StaticConstructorOnStartup]
    public static class TimeSlower_Handler_Patch
    {
        public static bool SignalForceNormal = false;

        static TimeSlower_Handler_Patch()
        {
            var m1 = AccessTools.Method(typeof(TimeSlower), nameof(TimeSlower.SignalForceNormalSpeed));
            var m2 = AccessTools.Method(typeof(TimeSlower), nameof(TimeSlower.SignalForceNormalSpeedShort));
            var p = new HarmonyMethod(AccessTools.Method(typeof(TimeSlower_Handler_Patch), nameof(Postfix)));
            var h = new Harmony("pirateby.WorkRebalancerMod");
            h.Patch(m1, postfix: p);
            h.Patch(m2, postfix: p);
        }

        public static void Postfix() => SignalForceNormal = true;
    }

    public class WorkRebalancerMod : ModBase
    {
        public static WorkRebalancerMod Instance { get; private set; }

        public bool SettingsApplied { get; private set; }
        private List<IWorkAmount> workDefDatabase;
        private ModSettingsPack modSettingsPack;

        public WorkRebalancerMod()
        {
            Instance = this;
            workDefDatabase = new List<IWorkAmount>();

            var h = new Harmony("pirateby.WorkRebalancerMod");
            //h.PatchAll(Assembly.GetExecutingAssembly()); // HugsLib auto patch

            void applyPatch(string patchName, bool result) {
                if (!result) Log.Message($"[WorkRebalancer] Apply {patchName}... Result = {result}");
            }

            applyPatch("TendPatient_Patch", TendPatient_Patch.Apply(h));
            applyPatch("JobDriver_WearApparel_Patch", JobDriver_WearApparel_Patch.Apply(h));
            applyPatch("CompScanner_Patch", CompScanner_Patch.Apply(h));
            applyPatch("JobDriver_Mine_Patch", JobDriver_Mine_Patch.Apply(h));
            applyPatch("JobDriver_Repair_Patch", JobDriver_Repair_Patch.Apply(h));
            applyPatch("JobDriver_RemoveFloor_Patch", JobDriver_RemoveFloor_Patch.Apply(h));
            applyPatch("JobDriver_Deconstruct_Patch", JobDriver_Deconstruct_Patch.Apply(h));
            applyPatch("HSK_CollectJobs_Patch", HSKCollectJobsPatched = HSK_CollectJobs_Patch.Apply(h));
            applyPatch("HSK_Extractors_Patch", HSKExtractorsPatched = HSK_Extractors_Patch.Apply(h));
            applyPatch("RimatomicsResearch_Patch", RAtomicsResearchPatched = RimatomicsResearch_Patch.Apply(h));
            applyPatch("RF_Drill_Patch", RFDrillJobPatched = RF_Drill_Patch.Apply(h));
            applyPatch("RF_Crude_Patch", RFCrudeJobPatched = RF_Crude_Patch.Apply(h));
            applyPatch("RF_Refinery_Patch", RFRefineryJobPatched = RF_Refinery_Patch.Apply(h));
            applyPatch("Androids_Patch", AndroidsPatched = Androids_Patch.Apply(h));
            applyPatch("JobDriver_MineQuarry_Patch", HSKMineQuarryPatched = JobDriver_MineQuarry_Patch.Apply(h));
            applyPatch("SkillRecord_Learn_Patch", SkillRecord_Learn_Patch.Apply(h));
            applyPatch("Pawn_Tick_Patch", Pawn_Tick_Patch.Apply(h));
            applyPatch("RJW_Hediff_BasePregnancy_Tick_Patch", RjwPregnancyPatched = RJW_Hediff_BasePregnancy_Tick_Patch.Apply(h));
            applyPatch("RJW_Hediff_InsectEgg_Tick_Patch", RjwInsectEggPatched = RJW_Hediff_InsectEgg_Tick_Patch.Apply(h));
            applyPatch("CompHatcher_CompTick_Patch", CompHatcher_CompTick_Patch.Apply(h));
            applyPatch("Arachnophobia_CompMultiHatcher_CompTick_Patch", Arachnophobia_CompMultiHatcher_CompTick_Patch.Apply(h));
            applyPatch("CompEggLayer_CompTick_Patch", CompEggLayer_CompTick_Patch.Apply(h));
            applyPatch("Breakdowns_Maintenance_Patch", FluffyBreakdownsPatched = Breakdowns_Maintenance_Patch.Apply(h));
        }

        public override string ModIdentifier => "WorkRebalancer";

        public override void Tick(int currentTick)
        {
            base.Tick(currentTick);

            if (Current.ProgramState != ProgramState.Playing)
                return;

            // check every cfg delay
            //if (Prof.RestoreWhenHostileDetected
            //    && !HostileHandler.HostileDetected
            //    && Prof.CheckHostileDelay != 0
            //    && currentTick % (Prof.CheckHostileDelay * 2) == 0)
            //    HostileHandler.UpdateHostilesLong();

            // check every cfg delay or if was signal to force normal speed
            if (Prof.CheckHostileDelay != 0 && (currentTick % Prof.CheckHostileDelay) != 0 && !TimeSlower_Handler_Patch.SignalForceNormal)
                return;

            TimeSlower_Handler_Patch.SignalForceNormal = false;

            // if option off reset to config
            if (!Prof.RestoreWhenHostileDetected)
            {
                if (!SettingsApplied)
                    ApplySettings();
                return;
            }

            HostileHandler.UpdateHostiles();
            if (!SettingsApplied && !HostileHandler.HostileDetected) // detected hostiles rip
            {
                ApplySettings();
                if (DebugLog)
                    Log.Message($"[WorkRebalancer] Apply configured settings");
            }
            else if (SettingsApplied && HostileHandler.HostileDetected) // detected new hostiles
            {
                ApplySettingsDefaults();
                Pawn_PathFollower_Patch.InstantMoving = false;
                if (DebugLog)
                    Log.Message($"[WorkRebalancer] Apply default 100% settings");
            }
        }

        public override void DefsLoaded()
        {
            workDefDatabase.Clear();
            workDefDatabase.AddRange(RecipeWorkAmount.GetAll());
            workDefDatabase.AddRange(ThingWorkAmount.GetAll());
            workDefDatabase.AddRange(TerrainWorkAmount.GetAll());
            workDefDatabase.AddRange(ResearchWorkAmount.GetAll());
            workDefDatabase.AddRange(PlantGrowDays.GetAll());
            workDefDatabase.AddRange(PlantWorkAmount.GetAll());

            InitializeSettings();
            ApplySettings();

            Log.Message($"WorkRebalancerMod :: DefsLoaded");
        }
        
        public bool ValueValidator(string value, int min, int max) => int.TryParse(value, out int num) && num >= min && num <= max;
        public bool ValueValidator(string value, float min, float max) => float.TryParse(value, out float num) && num >= min && num <= max;

        public enum Tabs
        {
            generalTab,
            pawnsTab,
            fastAging,
            otherTab,
            fastMoving,
            fastTime,
            //fastPawnsTicks,
            none // not drawing
        }

        private static readonly string[] tabNames = Enum.GetNames(typeof(Tabs)).Where(x => !x.Equals("none")).Select(x => x.Translate().ToString()).ToArray();

        private static readonly Color SelectedOptionColor = new Color(0.5f, 1f, 0.5f, 1f);

        public static bool CustomDrawer_Tabs(Rect rect, SettingHandle<string> selected, string[] defaultValues)
        {
            int tabIdx = 0;
            int width = 140, height = 20;
            int nextX = 0, nextY = 0;
            bool result = false;
            foreach (string text in defaultValues) {
                Rect buttonRect = new Rect(rect) {
                    height = height,
                    width = width,
                    position = new Vector2(rect.position.x + nextX, rect.position.y + nextY)
                };

                Color color = GUI.color;
                bool isCurrentTab = text == selected.Value;
                if (isCurrentTab) GUI.color = SelectedOptionColor;
                bool buttonPressed = Widgets.ButtonText(buttonRect, text);
                if (isCurrentTab) GUI.color = color;

                if (buttonPressed) {
                    if (selected.Value != text) selected.Value = text;
                    else selected.Value = "none";
                    result = true;
                }
                tabIdx++;
                
                // draw 2 buttons in a row
                if ((tabIdx % 2) == 0)
                {
                    nextY += height;
                    nextX = 0;
                } else nextX += width;
            }
            return result;
        }

        /// <summary>
        /// Get from workDefDatabase all T : IWorkAmount types and apply percent value
        /// </summary>
        /// <typeparam name="T">IWorkAmount type</typeparam>
        /// <param name="value">percent value</param>
        /// <param name="customAction">if defined: customAction(workAmount, percentValue)</param>
        public void ApplySetting<T>(int value, Action<T, float> customAction = null) where T : IWorkAmount 
        {
            float percent = value / 100f;
            foreach (T w in workDefDatabase.Where(x => x.GetType() == typeof(T)))
            {
                if (customAction == null) w.Set(percent); else customAction(w, percent);
            }
        }

        public void CreateWorkAmountSetting<T>(ref SettingHandle<int> settingHandle, string settingName, Tabs tabIndex, Action<T, float> customAction = null) where T : IWorkAmount 
        {
            settingHandle = modSettingsPack.GetHandle(
                settingName,
                settingName.Translate(),
                $"{settingName}Desc".Translate(),
                100,
                value => ValueValidator(value, 1, 100));

            var _settingHandle = settingHandle;// CS1628
            settingHandle.ValueChanged += newVal =>
            {
                ApplySetting(_settingHandle.Value, customAction);
            };

            settingHandle.VisibilityPredicate = () => tabsHandler.Value == tabNames[(int) tabIndex];
        }

        public void CreateCustomSetting<T>(ref SettingHandle<T> settingHandle, string settingName, T defaultValue, Tabs tabIndex) 
        {
            settingHandle = modSettingsPack.GetHandle(settingName, settingName.Translate(), $"{settingName}Desc".Translate(), defaultValue);
            if (tabIndex != Tabs.none)
                settingHandle.VisibilityPredicate = () => tabsHandler.Value == tabNames[(int) tabIndex];
        }

        public void HugsLabelWtf(string label, Tabs tab)
        {
            var lbl = modSettingsPack.GetHandle($"lbl{label}", label.Translate(), "", "");
            lbl.CustomDrawer = r => false;
            lbl.Unsaved = true;
            if (tab != Tabs.none)
                lbl.VisibilityPredicate = () => tabsHandler.Value == tabNames[(int) tab];
        }

        private static string _saveName = "profileName";
        public void InitializeSettings()
        {
            modSettingsPack = HugsLibController.Instance.Settings.GetModSettings("WorkRebalancer");

            // profiles //
            SettingHandle<bool> profiles = modSettingsPack.GetHandle(
                "ProfilesDrawer",
                "Profiles".Translate(),
                "Profiles".Translate(),
                false);
            profiles.CustomDrawerHeight = 40;
            profiles.CustomDrawer = rect =>
            {
                //var lister = new Listing_Standard();
                //lister.Begin(rect);
                //lister.verticalSpacing = 1f;
                IEnumerable<FloatMenuOption> loadMenu() {
                    foreach (var profName in Profile.GetAllProfiles()) {
                        yield return new FloatMenuOption(profName, () =>
                        {
                            _saveName = Path.GetFileNameWithoutExtension(profName);
                            Prof.Load(profName);
                        });
                    }
                }
                IEnumerable<FloatMenuOption> deleteMenu() {
                    foreach (var profName in Profile.GetAllProfiles()) {
                        yield return new FloatMenuOption(profName, () => { Profile.Delete(profName); });
                    }
                }

                Rect buttonRect1 = new Rect(rect) {height = 20f, width = rect.width / 3},
                    buttonRect2 = new Rect(buttonRect1) { x = buttonRect1.xMax},
                    buttonRect3 = new Rect(buttonRect2) { x = buttonRect2.xMax};

                if (Widgets.ButtonText(buttonRect1, "LoadProfileBtn".Translate()))
                {
                    Find.WindowStack.Add(new FloatMenu(new List<FloatMenuOption>(loadMenu())));
                }
                if (Widgets.ButtonText(buttonRect2, "DeleteProfileBtn".Translate()))
                {
                    Find.WindowStack.Add(new FloatMenu(new List<FloatMenuOption>(deleteMenu())));
                }
                if (Widgets.ButtonText(buttonRect3, "ResetBtn".Translate()))
                {
                    Prof.ResetToDefault();
                    ApplySettings();
                }

                Rect textRect1 = new Rect(rect) {height = 20f, width = rect.width / 2, y = buttonRect1.yMax},
                    buttonRect4 = new Rect(textRect1) { x = textRect1.xMax};

                _saveName = Widgets.TextField(textRect1, _saveName);
                if (Widgets.ButtonText(buttonRect4, "SaveProfileBtn".Translate()))
                    Prof.Save(_saveName);

                //if (lister.ButtonText("LoadProfileBtn".Translate()))
                //    Find.WindowStack.Add(new FloatMenu(new List<FloatMenuOption>(loadMenu())));
                //if (lister.ButtonText("DeleteProfileBtn".Translate()))
                //    Find.WindowStack.Add(new FloatMenu(new List<FloatMenuOption>(deleteMenu())));

                //_saveName = lister.TextEntryLabeled("savename", _saveName);
                //if (lister.ButtonText("SaveProfileBtn".Translate()))
                //    Prof.Save(_saveName);
                //lister.End();
                return true;
            };

            CreateCustomSetting(ref Prof.CheckHostileDelay, "CheckHostileDelay", 60, Tabs.none);
            CreateCustomSetting(ref Prof.RestoreWhenHostileDetected, "RestoreWhenHostileDetected", true, Tabs.none);

            var marks = modSettingsPack.GetHandle("marks", "marksTitle".Translate(), "", "");
            marks.CustomDrawer = rect =>
            {
                Widgets.Label(rect, "marksDesc".Translate());
                return false;
            };
            marks.CustomDrawerHeight = int.Parse( "marksDescHeight".Translate() );

            tabsHandler = modSettingsPack.GetHandle("tabs", "tabsTitle".Translate(), "", "none");
            tabsHandler.CustomDrawer = rect => CustomDrawer_Tabs(rect, tabsHandler, tabNames);
            tabsHandler.CustomDrawerHeight = (float)Math.Ceiling((double)tabNames.Length / 2) * 20; 

            // generalTab //
            CreateWorkAmountSetting<ResearchWorkAmount>(ref Prof.PercentOfBaseResearches, "PercentOfBaseResearches", Tabs.generalTab);
            CreateWorkAmountSetting<TerrainWorkAmount>(ref Prof.PercentOfBaseTerrains, "PercentOfBaseTerrains", Tabs.generalTab);
            CreateWorkAmountSetting<RecipeWorkAmount>(ref Prof.PercentOfBaseRecipes, "PercentOfBaseRecipes", Tabs.generalTab);
            CreateWorkAmountSetting<ThingWorkAmount>(ref Prof.PercentOfBaseThingStats, "PercentOfBaseThingStats", Tabs.generalTab, (w, p) => w.SetStats(p));
            CreateWorkAmountSetting<ThingWorkAmount>(ref Prof.PercentOfBaseThingFactors, "PercentOfBaseThingFactors", Tabs.generalTab, (w, p) => w.SetFactors(p));
            CreateWorkAmountSetting<PlantWorkAmount>(ref Prof.PercentOfBasePlantsWork, "PercentOfBasePlantsWork", Tabs.generalTab);
            CreateCustomSetting(ref Prof.PercentOfBaseWearApparel, "PercentOfBaseWearApparel", 100, Tabs.generalTab);
            CreateCustomSetting(ref Prof.PercentOfBaseTendPatient, "PercentOfBaseTendPatient", 100, Tabs.generalTab);
            CreateCustomSetting(ref Prof.PercentOfBaseMineJob, "PercentOfBaseMineJob", 100, Tabs.generalTab);
            CreateCustomSetting(ref Prof.DeepScannerJob, "DeepScannerJob", 1f, Tabs.generalTab);
            if (FluffyBreakdownsPatched)
            {
                CreateCustomSetting(ref Prof.PercentOfBaseFluffyBreakdowns, "PercentOfBaseFluffyBreakdowns", 100, Tabs.generalTab);
            }
            CreateCustomSetting(ref Prof.RepairJobAddX, "RepairJobAddX", 1, Tabs.generalTab);
            if (HSKCollectJobsPatched)
            {
                CreateCustomSetting(ref Prof.PercentOfBaseHSKCollectJobs, "PercentOfBaseHSKCollectJobs", 100, Tabs.generalTab);
            }
            if (RAtomicsResearchPatched)
            {
                CreateCustomSetting(ref Prof.RAtomicsResearchMultiplier, "RAtomicsResearchMultiplier", 1f, Tabs.generalTab);
            }
            if (RFDrillJobPatched)
            {
                CreateCustomSetting(ref Prof.RFDrillJobMultiplier, "RFDrillJobMultiplier", 1f, Tabs.generalTab);
            }
            if (RFCrudeJobPatched)
            {
                CreateCustomSetting(ref Prof.RFCrudeJobMultiplier, "RFCrudeJobMultiplier", 1f, Tabs.generalTab);
            }
            if (RFRefineryJobPatched)
            {
                CreateCustomSetting(ref Prof.RFRefineryJobMultiplier, "RFRefineryJobMultiplier", 1f, Tabs.generalTab);
            }
            if (HSKMineQuarryPatched)
            {
                CreateCustomSetting(ref Prof.PercentOfBaseHSKMineQuarry, "PercentOfBaseHSKMineQuarry", 100, Tabs.generalTab);
            }
            if (HSKExtractorsPatched)
            {
                CreateCustomSetting(ref Prof.PercentOfBaseHSKExtractorsMine, "PercentOfBaseHSKExtractorsMine", 100, Tabs.generalTab);
            }
            if (AndroidsPatched)
            {
                CreateCustomSetting(ref Prof.AndroidsCraftAddX, "AndroidsCraftAddX", 1, Tabs.generalTab);
            }

            // otherTab //
            HugsLabelWtf("boostXpTitle", Tabs.otherTab);
            CreateCustomSetting(ref Prof.SkillLearnMultiplier, "SkillLearnMultiplier", 1f, Tabs.otherTab);
            CreateCustomSetting(ref Prof.SkillLearnAllowMax, "SkillLearnAllowMax", 0, Tabs.otherTab);

            // fast aging //
            HugsLabelWtf("humanoidTitle", Tabs.fastAging);
            CreateCustomSetting(ref Prof.PawnSpeedMultBeforeCutoff, "PawnSpeedMultBeforeCutoff", 1, Tabs.fastAging);
            CreateCustomSetting(ref Prof.PawnSpeedMultAfterCutoff, "PawnSpeedMultAfterCutoff", 1, Tabs.fastAging);
            CreateCustomSetting(ref Prof.PawnCutoffAge, "PawnCutoffAge", 16, Tabs.fastAging);

            HugsLabelWtf("animalsTitle", Tabs.fastAging);
            CreateCustomSetting(ref Prof.AnimalSpeedMultBeforeCutoff, "AnimalSpeedMultBeforeCutoff", 1, Tabs.fastAging);
            CreateCustomSetting(ref Prof.AnimalSpeedMultAfterCutoff, "AnimalSpeedMultAfterCutoff", 1, Tabs.fastAging);
            CreateCustomSetting(ref Prof.AnimalCutoffAge, "AnimalCutoffAge", 1, Tabs.fastAging);

            // pawnsTab //
            CreateWorkAmountSetting<PlantGrowDays>(ref Prof.PercentOfBasePlantsGrowDays, "PercentOfBasePlantsGrowDays", Tabs.pawnsTab);
            CreateCustomSetting(ref Prof.EggHatchSpeedMult, "EggHatchSpeedMult", 1f, Tabs.pawnsTab);
            CreateCustomSetting(ref Prof.EggLayerSpeedMult, "EggLayerSpeedMult", 1f, Tabs.pawnsTab);
            if (RjwPregnancyPatched)
            {
                CreateCustomSetting(ref Prof.RjwPregnancySpeedMult, "RjwPregnancySpeedMult", 1f, Tabs.pawnsTab);
            }
            if (RjwInsectEggPatched)
            {
                CreateCustomSetting(ref Prof.RjwInsectEggSpeedMult, "RjwInsectEggSpeedMult", 1, Tabs.pawnsTab);
            }

            // fastMoving //
            CreateCustomSetting(ref Prof.ShowInstantMovingIcon, "ShowInstantMovingIcon", false, Tabs.fastMoving);
            CreateCustomSetting(ref Prof.InstantMovingSmoother, "InstantMovingSmoother", true, Tabs.fastMoving);
            CreateCustomSetting(ref Prof.InstantMovingOnlyColonists, "InstantMovingOnlyColonists", true, Tabs.fastMoving);
            CreateCustomSetting(ref Prof.InstantMovingAutooffOnPause, "InstantMovingAutooffOnPause", false, Tabs.fastMoving);

            // fastTime //
            CreateCustomSetting(ref Prof.ShowFastTimeIcon, "ShowFastTimeIcon", false, Tabs.fastTime);
            CreateCustomSetting(ref Prof.FastTimeMultiplier, "FastTimeMultiplier", 0, Tabs.fastTime);

            // fastPawnsTicks //
            // CreateCustomSetting(ref Prof.ShowFastPawnsTicksIcon, "ShowFastPawnsTicksIcon", false, Tabs.fastPawnsTicks);
            // CreateCustomSetting(ref Prof.FastPawnsTicksMultiplier, "FastPawnsTicksMultiplier", 1, Tabs.fastPawnsTicks);

            // debug //
            DebugLog = modSettingsPack.GetHandle(
                "DebugLog",
                "DebugLog",
                "DebugLog",
                false);


            //SettingHandle<bool> handleRebalance = modSettingsPack.GetHandle("Rebalance", "RebalanceBtn".Translate(), "RebalanceBtnDesc".Translate(), false);
            //handleRebalance.CustomDrawer = rect =>
            //{
            //    if (Widgets.ButtonText(rect, "RebalanceBtn".Translate())) ApplySettings();
            //    return false;
            //};
            

            
        }

        // hostile detected
        public void ApplySettingsDefaults()
        {
            workDefDatabase.ForEach(w => w.Restore());
            SettingsApplied = false;
        }

        public void ApplySettings()
        {
            //if (RestoreWhenHostileDetected && HostileDetected)
            //    return;

            //Loger.Clear();
            ApplySetting<ResearchWorkAmount>(Prof.PercentOfBaseResearches);
            ApplySetting<TerrainWorkAmount>(Prof.PercentOfBaseTerrains);
            ApplySetting<RecipeWorkAmount>(Prof.PercentOfBaseRecipes);
            ApplySetting<ThingWorkAmount>(Prof.PercentOfBaseThingStats, (w, p) => w.SetStats(p));
            ApplySetting<ThingWorkAmount>(Prof.PercentOfBaseThingFactors, (w, p) => w.SetFactors(p));
            ApplySetting<PlantWorkAmount>(Prof.PercentOfBasePlantsWork);
            ApplySetting<PlantGrowDays>(Prof.PercentOfBasePlantsGrowDays);
            //Loger.Save("dumpRebuilder.txt");
            SettingsApplied = true;
        }

        
        public SettingHandle<string> tabsHandler;
        
        public Profile Prof { get; private set; } = new Profile();

        public SettingHandle<bool> DebugLog;

        public bool FluffyBreakdownsPatched { get; }
        public bool HSKExtractorsPatched { get; }
        public bool HSKCollectJobsPatched { get; }
        public bool RAtomicsResearchPatched { get; }
        public bool RFDrillJobPatched { get; }
        public bool RFCrudeJobPatched { get; }
        public bool RFRefineryJobPatched { get; }
        public bool HSKMineQuarryPatched { get; }
        public bool RjwPregnancyPatched { get; }
        public bool RjwInsectEggPatched { get; }
        public bool AndroidsPatched { get; }
    }
}
