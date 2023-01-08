using HarmonyLib;
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
                CollectionDropReport dropReport = Traverse.Create(__instance).Field("DropReport").GetValue<CollectionDropReport>();
                if (dropReport.FromCard != null && dropReport.FromAction != null && dropReport.FromAction.HasSuccessfulDrop)
                {
                    actionTooltip.TooltipTitle = __instance.Title;
                    actionTooltip.TooltipContent = Traverse.Create(__instance).Field("MyTooltip").Field("TooltipContent").GetValue<string>() + '\n' + FormatCardDropList(dropReport, dropReport.FromCard);
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

        public static string FormatCardDropList(CollectionDropReport report, InGameCardBase fromCard, bool withStat = true,bool withCard = true, bool withDuability = true, int indent = 0)
        {
            List<string> texts = new();
            for(int i = 0; i < report.DropsInfo.Length; i++)
            {
                float dropRate = report.GetDropPercent(i, withStat, withCard, withDuability);
                if (dropRate < 0.00001) continue;
                texts.Add(FormatBasicEntry($"{dropRate:P2}", $"{report.DropsInfo[i].CollectionName}", indent: indent));
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
            }
            return $"<size=75%>{texts.Join(delimiter: "\n")}</size>";
        }
    }
}
