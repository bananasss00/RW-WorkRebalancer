﻿using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Harmony;
using HugsLib;
using HugsLib.Settings;
using Verse;
using RimWorld;
using UnityEngine;
using WorkRebalancer.Patches;

namespace WorkRebalancer
{
    public class WorkRebalancerMod : ModBase
    {
        public static WorkRebalancerMod Instance { get; private set; }

        private List<IWorkAmount> workDefDatabase;
        private ModSettingsPack modSettingsPack;

        public WorkRebalancerMod()
        {
            Instance = this;
            workDefDatabase = new List<IWorkAmount>();

            HarmonyInstance h = HarmonyInstance.Create("pirateby.WorkRebalancerMod");
            h.PatchAll(Assembly.GetExecutingAssembly());
            Log.Message($"[WorkRebalancer] Apply JobDriver_Mine_Patch... Result = {JobDriver_Mine_Patch.Apply(h)}");
            Log.Message($"[WorkRebalancer] Apply JobDriver_Repair_Patch... Result = {JobDriver_Repair_Patch.Apply(h)}");
            Log.Message($"[WorkRebalancer] Apply JobDriver_Deconstruct_Patch... Result = {JobDriver_Deconstruct_Patch.Apply(h)}");
            Log.Message($"[WorkRebalancer] Apply HSK_CollectJobs_Patch... Result = {HSKCollectJobsPatched = HSK_CollectJobs_Patch.Apply(h)}");
            Log.Message($"[WorkRebalancer] Apply HSK_Extractors_Patch... Result = {HSKExtractorsPatched = HSK_Extractors_Patch.Apply(h)}");
            Log.Message($"[WorkRebalancer] Apply RF_Drill_Patch... Result = {RFDrillJobPatched = RF_Drill_Patch.Apply(h)}");
            Log.Message($"[WorkRebalancer] Apply Androids_Patch... Result = {AndroidsPatched = Androids_Patch.Apply(h)}");
            Log.Message($"[WorkRebalancer] Apply JobDriver_MineQuarry_Patch... Result = {HSKMineQuarryPatched = JobDriver_MineQuarry_Patch.Apply(h)}");
            Log.Message($"[WorkRebalancer] Apply SkillRecord_Learn_Patch... Result = {SkillRecord_Learn_Patch.Apply(h)}");
            Log.Message($"[WorkRebalancer] Apply Pawn_Tick_Patch... Result = {Pawn_Tick_Patch.Apply(h)}");
            Log.Message($"[WorkRebalancer] Apply RJW_Hediff_BasePregnancy_Tick_Patch... Result = {RjwPregnancyPatched = RJW_Hediff_BasePregnancy_Tick_Patch.Apply(h)}");
            Log.Message($"[WorkRebalancer] Apply RJW_Hediff_InsectEgg_Tick_Patch... Result = {RjwInsectEggPatched = RJW_Hediff_InsectEgg_Tick_Patch.Apply(h)}");
            Log.Message($"[WorkRebalancer] Apply CompHatcher_CompTick_Patch... Result = {CompHatcher_CompTick_Patch.Apply(h)}");
            Log.Message($"[WorkRebalancer] Apply Arachnophobia_CompMultiHatcher_CompTick_Patch... Result = {Arachnophobia_CompMultiHatcher_CompTick_Patch.Apply(h)}");
            Log.Message($"[WorkRebalancer] Apply CompEggLayer_CompTick_Patch... Result = {CompEggLayer_CompTick_Patch.Apply(h)}");
        }

        public override string ModIdentifier => "WorkRebalancer";

        public override void Tick(int currentTick)
        {
            base.Tick(currentTick);

            if (Current.ProgramState != ProgramState.Playing)
                return;

            // check every 7 sec
            if ((currentTick % Prof.CheckHostileDelay) != 0)
                return;

            // if option off reset to config
            if (!Prof.RestoreWhenHostileDetected)
            {
                if (HostileDetected)
                {
                    ApplySettings();
                    HostileDetected = false;
                }
                return;
            }

            bool hostileDetected = Utils.HostileExistsOnMaps();
            if (HostileDetected && !hostileDetected) // detected hostiles rip
            {
                ApplySettings();
                HostileDetected = false;
                if (DebugLog)
                {
                    Log.Message($"[WorkRebalancer] Apply configured settings");
                }
            }
            else if (!HostileDetected && hostileDetected) // detected new hostiles
            {
                ApplySettingsDefaults();
                HostileDetected = true;
                if (DebugLog)
                {
                    Log.Message($"[WorkRebalancer] Apply default 100% settings");
                }
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
            none // not drawing
        }

        private static readonly string[] tabNames = Enum.GetNames(typeof(Tabs)).Where(x => !x.Equals("none")).Select(x => x.Translate()).ToArray();

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

            settingHandle.OnValueChanged = newVal =>
            {
                ApplySetting(newVal, customAction);
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
                        yield return new FloatMenuOption(profName, () => { Prof.Load(profName); });
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

            CreateCustomSetting(ref Prof.CheckHostileDelay, "CheckHostileDelay", 420, Tabs.none);
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
            CreateCustomSetting(ref Prof.PercentOfBaseMineJob, "PercentOfBaseMineJob", 100, Tabs.generalTab);
            CreateCustomSetting(ref Prof.RepairJobAddX, "RepairJobAddX", 1, Tabs.generalTab);
            if (HSKCollectJobsPatched)
            {
                CreateCustomSetting(ref Prof.PercentOfBaseHSKCollectJobs, "PercentOfBaseHSKCollectJobs", 100, Tabs.generalTab);
            }
            if (RFDrillJobPatched)
            {
                CreateCustomSetting(ref Prof.RFDrillJobMultiplier, "RFDrillJobMultiplier", 1f, Tabs.generalTab);
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

            //HugsLabelWtf("otherTitle", Tabs.fastAging);
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
        public void ApplySettingsDefaults() => workDefDatabase.ForEach(w => w.Restore());

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
        }

        
        public SettingHandle<string> tabsHandler;
        
        public Profile Prof { get; private set; } = new Profile();

        public SettingHandle<bool> DebugLog;

        public bool HSKExtractorsPatched { get; }
        public bool HSKCollectJobsPatched { get; }
        public bool RFDrillJobPatched { get; }
        public bool HSKMineQuarryPatched { get; }
        public bool RjwPregnancyPatched { get; }
        public bool RjwInsectEggPatched { get; }
        public bool AndroidsPatched { get; }
        public bool HostileDetected { get; private set; }
    }
}
