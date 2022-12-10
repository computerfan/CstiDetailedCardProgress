using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using static CstiDetailedCardProgress.Utils;

namespace CstiDetailedCardProgress
{
    class Stat
    {

        [HarmonyPostfix, HarmonyPatch(typeof(StatStatusGraphics), "Update")]
        public static void StatStatusGraphicsPatch(StatStatusGraphics __instance, TooltipText ___MyTooltip)
        {
            if (Plugin.Enabled)
            {
                __instance.SetTooltip(__instance.Title, $"{(string.IsNullOrWhiteSpace(__instance.ModelStatus.Description) ? "" : $"{__instance.ModelStatus.Description}\n")}{FormatInGameStat(__instance.ModelStatus.ParentStat)}", "");
            }
            else
            {
                //Reset the tool tip to the base game settings.
                __instance.SetTooltip(__instance.ModelStatus.GameName, __instance.ModelStatus.Description, "");
            }
        }
        public static string FormatInGameStat(InGameStat stat)
        {
            List<string> texts = new();
            List<string> valueModsTexts = new();
            List<string> rateModsTexts = new();
            if (stat.CurrentBaseValue != 0) { valueModsTexts.Add(FormatBasicEntry($"{stat.CurrentBaseValue:0.##}", "Base", indent: 2)); }
            GameManager gm = MBSingleton<GameManager>.Instance;
            if (gm != null && !gm.NotInBase)
            {
                if (stat.AtBaseModifiedValue != 0) valueModsTexts.Add(FormatTooltipEntry(stat.AtBaseModifiedValue, "At base", 2));
                if (stat.AtBaseModifiedRate != 0) rateModsTexts.Add(FormatRateEntry(stat.AtBaseModifiedRate, "At base"));
            }
            if (stat.CurrentBaseRate != 0) { rateModsTexts.Add(FormatRateEntry(stat.CurrentBaseRate, "Base")); }
            stat.ModifierSources.ForEach(modifierSource =>
            {
                string source = GetModifierSourceName(modifierSource);
                if (modifierSource.Rate != 0)
                { 
                    rateModsTexts.Add(FormatRateEntry(modifierSource.Rate, source));
                }
                if (modifierSource.Value != 0)
                {
                    valueModsTexts.Add(FormatTooltipEntry(modifierSource.Value, source, 2));
                }
            });
            texts.Add(FormatBasicEntry($"{stat.SimpleCurrentValue:0.##}: [{stat.StatModel.MinMaxValue.x:0.##}, {stat.StatModel.MinMaxValue.y:0.##}]", stat.StatModel.GameName));
            if (valueModsTexts.Count > 0)
            {
                texts.Add(valueModsTexts.Join(delimiter: "\n"));
            }
            texts.Add(FormatRate(stat.SimpleRatePerTick, stat.SimpleCurrentValue, stat.StatModel.MinMaxValue.y, stat.StatModel.MinMaxValue.x));
            if (rateModsTexts.Count > 0)
            {
                texts.Add(rateModsTexts.Join(delimiter: "\n"));
            }
            return $"<size=75%>{texts.Join(delimiter: "\n")}</size>";
        }

        public static string GetModifierSourceName(StatModifierSource modifierSource)
        {
            string sourceName = "";
            if (modifierSource.Stat != null)
            {
                sourceName = modifierSource.Stat.StatModel.GameName;
            }
            else if (modifierSource.Card != null)
            {
                sourceName = modifierSource.Card.CardModel.CardName;
            }
            else if (modifierSource.Character != null)
            {
                sourceName = modifierSource.Character.CharacterName;
            }
            else if (modifierSource.Perk != null)
            {
                sourceName = modifierSource.Perk.PerkName;
            }
            else if (modifierSource.TimeOfDay != null)
            {
                sourceName = $"Time {modifierSource.TimeOfDay.EffectStartingTime}~{modifierSource.TimeOfDay.EffectEndTime}";
            }
            return sourceName;
        }
    }
}
