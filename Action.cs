﻿using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CstiDetailedCardProgress.Utils;

namespace CstiDetailedCardProgress
{
    class Action
    {
        public static TooltipText actionTooltip = new();
        [HarmonyPostfix, HarmonyPatch(typeof(DismantleActionButton), "OnHoverEnter")]
        public static void DismantleActionButtonOnHoverEnterPatch(DismantleActionButton __instance)
        {
            if (Plugin.Enabled)
            {
                List<string> texts = new();
                GameManager GM = GameManager.Instance;
                CollectionDropReport dropReport = Traverse.Create(__instance).Field("DropReport").GetValue<CollectionDropReport>();
                if (dropReport.FromCard != null && dropReport.FromAction != null && dropReport.FromAction.HasSuccessfulDrop && dropReport.DropsInfo.Length > 0)
                {
                    texts.Add(FormatCardDropList(dropReport, dropReport.FromCard));
                }
                InspectionPopup popup = __instance.GetComponentInParent<InspectionPopup>();
                ExplorationPopup explorationPopup = __instance.GetComponentInParent<ExplorationPopup>();
                InGameCardBase currentCard = null;
                DismantleCardAction action = null;
                if (popup && popup.CurrentCard)
                {
                    currentCard = popup.CurrentCard.ContainedLiquid ?? popup.CurrentCard;
                    if (currentCard.CardModel.CardType != CardTypes.Blueprint && __instance.Index > -1 && __instance.Index < currentCard.DismantleActions.Length)
                    {
                        action = currentCard.DismantleActions[__instance.Index];
                    }
                }
                else if (explorationPopup && explorationPopup.ExplorationCard)
                {
                    if (__instance.name == "Button" || Traverse.Create(explorationPopup).Field("CurrentPhase").GetValue<int>() != 0) return;
                    currentCard = explorationPopup.ExplorationCard;
                    action = currentCard.CardModel.DismantleActions[0];
                }

                if (action != null)
                {
                    if ((dropReport.DropsInfo == null || dropReport.DropsInfo.Length == 0) && action.ProducedCards != null && action.ProducedCards.Length > 0)
                    {
                        dropReport = GM.GetCollectionDropsReport(action, currentCard, true);
                        texts.Add(FormatCardDropList(dropReport, currentCard, action: action));
                    }
                    texts.Add(FormatCardAction(action, currentCard));
                }

                string newContent = texts.Join(delimiter: "\n");
                if (!string.IsNullOrWhiteSpace(newContent))
                {
                    actionTooltip.TooltipTitle = __instance.Title;
                    string orgContent = Traverse.Create(__instance).Field("MyTooltip").Field("TooltipContent").GetValue<string>();
                    actionTooltip.TooltipContent = orgContent + (string.IsNullOrEmpty(orgContent) ? "" : "\n") + "<size=70%>" + newContent + "</size>";
                    actionTooltip.HoldText = Traverse.Create(__instance).Field("MyTooltip").Field("HoldText").GetValue<string>();
                    // Traverse.Create(__instance).Method("CancelTooltip").GetValue();
                    Tooltip.AddTooltip(actionTooltip);
                }

            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(DismantleActionButton), "OnHoverExit")]
        public static void DismantleActionButtonOnHoverExitPatch(DismantleActionButton __instance)
        {
            Tooltip.RemoveTooltip(actionTooltip);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(DismantleActionButton), "OnDisable")]
        public static void DismantleActionButtonOnDisablePatch(DismantleActionButton __instance)
        {
            Tooltip.RemoveTooltip(actionTooltip);
        }

        public static string FormatCardDropList(CollectionDropReport report, InGameCardBase fromCard, bool withStat = true,bool withCard = true, bool withDuability = true, CardAction action = null, int indent = 0)
        {
            List<string> texts = new();
            for(int i = 0; i < report.DropsInfo.Length; i++)
            {
                float dropRate = report.GetDropPercent(i, withStat, withCard, withDuability);
                if (report.DropsInfo.Length != 1 && dropRate < 0.00001 && (!report.DropsInfo[i].IsSuccess || report.DropsInfo[i].FinalWeight < -10000)) continue;
                string dropCardTexts = report.DropsInfo[i].Drops.Where(c => c != null).GroupBy(c => new { c.CardType, c.CardName }, c => c, (k, cs) => new { name = k.CardName, count = cs.Count(), type = k.CardType })
                    .Select(r => $"{ColorFloat(r.count)} ({r.type}){r.name}").Join();
                if (action != null && action.ProducedCards != null && action.ProducedCards[0].DropsLiquid)
                {
                    LiquidDrop currentLiquidDrop = action.ProducedCards[0].CurrentLiquidDrop;
                    string liquidDropText = $"{FormatMinMaxValue(currentLiquidDrop.Quantity)} ({currentLiquidDrop.LiquidCard.CardType}){currentLiquidDrop.LiquidCard.CardName}";
                    dropCardTexts = report.DropsInfo[i].Drops.Length == 0 ? liquidDropText : string.Join(", ", dropCardTexts, liquidDropText);
                }
                if (dropRate == 0 && report.DropsInfo.Length == 1)
                {
                    return FormatBasicEntry($"{ new LocalizedString { LocalizationKey = "CstiDetailedCardProgress.Action.CardDrops", DefaultText = "Card Drops" }}", dropCardTexts, indent: indent);
                }
                texts.Add(FormatBasicEntry($"{dropRate:P2}", $"{report.DropsInfo[i].CollectionName}", indent: indent));
                if (!string.IsNullOrWhiteSpace(dropCardTexts))
                    texts.Add(FormatBasicEntry($"<size=55%>{ new LocalizedString { LocalizationKey = "CstiDetailedCardProgress.Action.CardDrops", DefaultText = "Card Drops" }}</size>", "<size=55%>" + dropCardTexts + "</size>", indent: 2 + indent));
                texts.Add(FormatBasicEntry($"{report.DropsInfo[i].FinalWeight}/{report.TotalValue}", new LocalizedString { LocalizationKey = "CstiDetailedCardProgress.Action.Weight", DefaultText = "Weight" }, indent: 2 + indent));
                if (report.DropsInfo[i].BaseWeight != 0)
                {
                    texts.Add(FormatTooltipEntry(report.DropsInfo[i].BaseWeight, new LocalizedString { LocalizationKey = "CstiDetailedCardProgress.Action.Base", DefaultText = "Base" }, 4 + indent));
                }
                if (withStat && report.DropsInfo[i].StatMods != null)
                {
                    report.DropsInfo[i].StatWeightMods.Do(statmod =>
                    {
                        if(statmod.BonusWeight!=0) texts.Add(FormatTooltipEntry(statmod.BonusWeight, $"{statmod.Stat.GameName}", 4 + indent));
                    });
                }
                if(withCard && report.DropsInfo[i].CardWeightMods != null)
                {
                    report.DropsInfo[i].CardWeightMods.Do(cardmod =>
                    {
                        if(cardmod.BonusWeight!=0) texts.Add(FormatTooltipEntry(cardmod.BonusWeight, $"{cardmod.Card.CardModel.CardName}", 4 + indent));
                    });
                }
                if (withDuability)
                {
                    if (report.DropsInfo[i].DurabilitiesWeightMods.SpoilageWeight != 0)
                        texts.Add(FormatTooltipEntry(report.DropsInfo[i].DurabilitiesWeightMods.SpoilageWeight, $"{fromCard.CardModel.SpoilageTime.CardStatName}", 4 + indent));
                    if(report.DropsInfo[i].DurabilitiesWeightMods.UsageWeight!=0)
                        texts.Add(FormatTooltipEntry(report.DropsInfo[i].DurabilitiesWeightMods.UsageWeight, $"{fromCard.CardModel.UsageDurability.CardStatName}", 4 + indent));
                    if(report.DropsInfo[i].DurabilitiesWeightMods.FuelWeight!=0)
                        texts.Add(FormatTooltipEntry(report.DropsInfo[i].DurabilitiesWeightMods.FuelWeight, $"{fromCard.CardModel.FuelCapacity.CardStatName}", 4 + indent));
                    if(report.DropsInfo[i].DurabilitiesWeightMods.ProgressWeight!=0)
                        texts.Add(FormatTooltipEntry(report.DropsInfo[i].DurabilitiesWeightMods.ProgressWeight, $"{fromCard.CardModel.Progress.CardStatName}", 4 + indent));
                    if(report.DropsInfo[i].DurabilitiesWeightMods.Special1Weight!=0)
                        texts.Add(FormatTooltipEntry(report.DropsInfo[i].DurabilitiesWeightMods.Special1Weight, $"{fromCard.CardModel.SpecialDurability1.CardStatName}", 4 + indent));
                    if(report.DropsInfo[i].DurabilitiesWeightMods.Special2Weight!=0)
                        texts.Add(FormatTooltipEntry(report.DropsInfo[i].DurabilitiesWeightMods.Special2Weight, $"{fromCard.CardModel.SpecialDurability2.CardStatName}", 4 + indent));
                    if (report.DropsInfo[i].DurabilitiesWeightMods.Special3Weight != 0)
                        texts.Add(FormatTooltipEntry(report.DropsInfo[i].DurabilitiesWeightMods.Special3Weight, $"{fromCard.CardModel.SpecialDurability3.CardStatName}", 4 + indent));
                    if (report.DropsInfo[i].DurabilitiesWeightMods.Special4Weight != 0)
                        texts.Add(FormatTooltipEntry(report.DropsInfo[i].DurabilitiesWeightMods.Special4Weight, $"{fromCard.CardModel.SpecialDurability4.CardStatName}", 4 + indent));
                }
                
                if (report.DropsInfo[i].StatMods != null)
                {
                    List<string> stateModTexts = new();
                    foreach (StatModifier statModifier in report.DropsInfo[i].StatMods)
                    {
                        stateModTexts.Add(FormatStatModifier(statModifier, indent: 4 + indent));
                    }
                    if (stateModTexts.Count > 0)
                    {
                        texts.Add(FormatBasicEntry(new LocalizedString { LocalizationKey = "CstiDetailedCardProgress.StatModifier", DefaultText = "Stat Modifier" }, "", indent: 2 + indent));
                        texts.Add(stateModTexts.Join(delimiter: "\n"));
                    }
                }

            }
            return $"{texts.Join(delimiter: "\n")}";
        }
    }
}
