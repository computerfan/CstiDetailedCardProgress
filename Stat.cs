﻿using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using static CstiDetailedCardProgress.Utils;

namespace CstiDetailedCardProgress;

internal class Stat
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(StatStatusGraphics), "Update")]
    public static void StatStatusGraphicsPatch(StatStatusGraphics __instance)
    {
        if (Plugin.Enabled)
            __instance.SetTooltip(__instance.Title,
                $"{(string.IsNullOrWhiteSpace(__instance.ModelStatus.Description) ? "" : $"{__instance.ModelStatus.Description.ToString()}\n")}{FormatInGameStat(__instance.ModelStatus.ParentStat)}",
                "");
        else
            //Reset the tool tip to the base game settings.
            __instance.SetTooltip(__instance.ModelStatus.GameName, __instance.ModelStatus.Description, "");
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(AffectedStatInfo), "Setup")]
    public static void PatchAffectedStatInfoInteractable(AffectedStatInfo __instance, GameStat _Stat)
    {
        if (!Plugin.Enabled || !Plugin.ForceInspectStatInfos) return;
        _Stat.CannotBeInspected = false;
        __instance.InteractionButton.interactable = true;
    }
    [HarmonyPostfix]
    [HarmonyPatch(typeof(StatInfluenceInfo), "SetupStatSource")]
    public static void PatchStatInfluenceInfoInteractable(StatInfluenceInfo __instance, InGameStat _Stat)
    {
        if (!Plugin.Enabled || !Plugin.ForceInspectStatInfos) return;
        _Stat.StatModel.CannotBeInspected = false;
        __instance.InteractionButton.interactable = true;
    }

    public static string FormatInGameStat(InGameStat stat)
    {
        List<string> texts = new();
        List<string> valueModsTexts = new();
        List<string> rateModsTexts = new();
        if (stat.CurrentBaseValue != 0)
            valueModsTexts.Add(FormatBasicEntry($"{stat.CurrentBaseValue:0.##}",
                new LocalizedString { LocalizationKey = "CstiDetailedCardProgress.Stat.Base", DefaultText = "Base" }.ToString(),
                indent: 2));
        GameManager gm = MBSingleton<GameManager>.Instance;
        // Seems deplicated with some sources in stat.ModifierSources
        //if (gm != null && !gm.NotInBase)
        //{
        //    if (stat.AtBaseModifiedValue != 0) valueModsTexts.Add(FormatTooltipEntry(stat.AtBaseModifiedValue, "At base", 2));
        //    if (stat.AtBaseModifiedRate != 0) rateModsTexts.Add(FormatRateEntry(stat.AtBaseModifiedRate, "At base"));
        //}
        if (stat.CurrentBaseRate != 0)
            rateModsTexts.Add(FormatRateEntry(stat.CurrentBaseRate,
                new LocalizedString { LocalizationKey = "CstiDetailedCardProgress.Stat.Base", DefaultText = "Base" }.ToString()));
        foreach (StatModifierSource modifierSource in stat.ModifierSources)
            if (modifierSource.ValidNumbers && modifierSource.ValidSource)
            {
                string source = GetModifierSourceName(modifierSource);
                if (modifierSource.Rate != 0) rateModsTexts.Add(FormatRateEntry(modifierSource.Rate, source));
                if (modifierSource.Value != 0) valueModsTexts.Add(FormatTooltipEntry(modifierSource.Value, source, 2));
            }

        texts.Add(FormatBasicEntry(
            $"{stat.SimpleCurrentValue:0.##}: [{stat.StatModel.MinMaxValue.x:0.##}, {stat.StatModel.MinMaxValue.y:0.##}]",
            stat.StatModel.GameName));
        if (valueModsTexts.Count > 0) texts.Add(valueModsTexts.Join(delimiter: "\n"));
        texts.Add(FormatRate(stat.SimpleRatePerTick, stat.SimpleCurrentValue, stat.StatModel.MinMaxValue.y,
            stat.StatModel.MinMaxValue.x));
        if (rateModsTexts.Count > 0) texts.Add(rateModsTexts.Join(delimiter: "\n"));

        if (stat.CurrentStatuses != null && stat.CurrentStatuses.Count > 0)
        {
            foreach (var status in stat.CurrentStatuses)
            {
                if (status?.EffectsOnActions == null || status.EffectsOnActions.Length == 0) continue;
                texts.Add(FormatBasicEntry(new LocalizedString()
                {
                    LocalizationKey = "CstiDetailedCardProgress.TimeCostModifiers",
                    DefaultText = "Time Cost Modifiers"
                }, ""));
                foreach (var effect in status.EffectsOnActions)
                {
                    if (effect.DurationModifier == 0) continue;
                    texts.Add(FormatActionDurationModifiers(effect, 2));
                }
            }
        }

        if (stat.StatModel.UsesNovelty && stat.StalenessValues.Count > 0)
        {
            List<string> stalenessText = new();
            foreach (var staleness in stat.StalenessValues)
            {
                if (staleness.Quantity > -1 && stat.StatModel.StalenessMultiplier != 0)
                    stalenessText.Add(FormatBasicEntry(
                        $"{Mathf.Pow(stat.StatModel.StalenessMultiplier, staleness.Quantity + 1):G3}x",
                        $"(est. {stat.StatModel.NoveltyCooldownDuration - gm.CurrentTickInfo.z + staleness.LastTick + Math.Max(0, staleness.Quantity) * stat.StatModel.NoveltyCooldownDuration}t) {staleness.ModifierSource}",
                        indent: 2));
            };
            if (stalenessText.Count > 0)
            {
                texts.Add(FormatBasicEntry(
                    new LocalizedString
                    {
                        LocalizationKey = "CstiDetailedCardProgress.Stat.ActionStaleness",
                        DefaultText = "Action Staleness"
                    }, $"(×{1 / stat.StatModel.StalenessMultiplier:0.##}/{stat.StatModel.NoveltyCooldownDuration}t)"));
                texts.Add(stalenessText.Join(delimiter: "\n"));
            }
        }

        return $"<size=75%>{texts.Join(delimiter: "\n")}</size>";
    }

    public static string GetModifierSourceName(StatModifierSource modifierSource)
    {
        string sourceName = "";
        if (modifierSource.Stat != null)
            sourceName = modifierSource.Stat.StatModel.GameName;
        else if (modifierSource.Card != null)
            sourceName = modifierSource.Card.CardModel.CardName.ToString();
        else if (modifierSource.Character != null)
            sourceName = modifierSource.Character.CharacterName;
        else if (modifierSource.Perk != null)
            sourceName = modifierSource.Perk.PerkName;
        else if (modifierSource.TimeOfDay != null)
            sourceName =
                $"{new LocalizedString { LocalizationKey = "CstiDetailedCardProgress.Stat.Time", DefaultText = "Time" }.ToString()} {modifierSource.TimeOfDay.EffectStartingTime}~{modifierSource.TimeOfDay.EffectEndTime}";
        return sourceName;
    }
}