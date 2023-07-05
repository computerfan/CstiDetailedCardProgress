using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using static CstiDetailedCardProgress.Utils;

namespace CstiDetailedCardProgress;

internal class Action
{
    public static TooltipText ActionTooltip = new();

    [HarmonyPostfix]
    [HarmonyPatch(typeof(DismantleActionButton), "OnHoverEnter")]
    public static void DismantleActionButtonOnHoverEnterPatch(DismantleActionButton __instance)
    {
        if (!Plugin.Enabled) return;
        List<string> texts = new();
        GameManager gm = GameManager.Instance;
        CollectionDropReport dropReport =
#if MELON_LOADER
            __instance.DropReport;
#else
            Traverse.Create(__instance).Field("DropReport").GetValue<CollectionDropReport>();
#endif
        if (dropReport.FromCard != null && dropReport.FromAction != null &&
            dropReport.FromAction.HasSuccessfulDrop &&
            dropReport.DropsInfo.Length > 0) texts.Add(FormatCardDropList(dropReport, dropReport.FromCard));
        InspectionPopup popup = __instance.GetComponentInParent<InspectionPopup>();
        ExplorationPopup explorationPopup = __instance.GetComponentInParent<ExplorationPopup>();
#if MELON_LOADER
        BlueprintConstructionPopup blueprintConstructionPopup = __instance.GetComponentInParent<BlueprintConstructionPopup>();
#endif
        InGameCardBase currentCard = null;
        DismantleCardAction action = null;
        if (popup && popup.CurrentCard)
        {
            currentCard = popup.CurrentCard.ContainedLiquid ?? popup.CurrentCard;
            if (currentCard.IsBlueprintInstance)
            {
                if (__instance.Index == -2)
#if MELON_LOADER
                    action = blueprintConstructionPopup.CurrentBuildAction;
#else
                    action = Traverse.Create(popup).Field("CurrentBuildAction").GetValue<DismantleCardAction>();
#endif

                else if (__instance.Index == -1)
#if MELON_LOADER
                    action = blueprintConstructionPopup.CurrentDeconstructAction;
#else
                    action = Traverse.Create(popup).Field("CurrentDeconstructAction")
                        .GetValue<DismantleCardAction>();
#endif
            }
            else if (__instance.Index > -1 && __instance.Index < currentCard.DismantleActions.Length)
            {
                action = currentCard.DismantleActions[__instance.Index];
            }
        }
        else if (explorationPopup && explorationPopup.ExplorationCard)
        {
#if MELON_LOADER
            if (__instance.name == "Button" || explorationPopup.CurrentPhase != 0) return;
#else
            if (__instance.name == "Button" ||
                            Traverse.Create(explorationPopup).Field("CurrentPhase").GetValue<int>() != 0) return;
#endif
            currentCard = explorationPopup.ExplorationCard;
            action = currentCard.CardModel?.DismantleActions.get_Item(0);
            if (action != null)
            {
                if (currentCard.ExplorationData != null)
                    texts.Add(FormatBasicEntry($"{currentCard.ExplorationData.CurrentExploration:P2}",
                        new LocalizedString
                        {
                            LocalizationKey = "CstiDetailedCardProgress.Action.CurrentExploration",
                            DefaultText = "Current Exploration"
                        }));
                texts.Add(FormatBasicEntry(ColorFloat(action.ExplorationValue, true),
                    new LocalizedString
                    {
                        LocalizationKey = "CstiDetailedCardProgress.Action.ExplorationValue",
                        DefaultText = "Explored"
                    }));
                currentCard.CardModel?.ExplorationResults?
                    .Where(r => currentCard.ExplorationData.CurrentExploration < r.TriggerValue &&
                                currentCard.ExplorationData.CurrentExploration + action.ExplorationValue >=
                                r.TriggerValue)
                    .Select(r => r.Action)
                    .Do(exploreAction =>
                    {
                        texts.Add(FormatBasicEntry(exploreAction.ActionName, ""));
                        string dropList = FormatCardDropList(
                            gm.GetCollectionDropsReport(exploreAction, currentCard, false), currentCard,
                            action: action, indent: 2);
                        if (!string.IsNullOrWhiteSpace(dropList)) texts.Add(dropList);
                        string actionText = FormatCardAction(exploreAction, currentCard, 2);
                        if (!string.IsNullOrWhiteSpace(actionText)) texts.Add(actionText);
                    });
                texts.Add(FormatBasicEntry(FormatMinMaxValue(action.MinMaxExplorationDrops),
                    new LocalizedString
                    {
                        LocalizationKey = "CstiDetailedCardProgress.Action.ExplorationDropsCount",
                        DefaultText = "Exploration Drops Count"
                    }));
            }
        }

        if (action != null)
        {
            if ((dropReport.DropsInfo == null || dropReport.DropsInfo.Length == 0) &&
                action.ProducedCards != null && action.ProducedCards.Length > 0)
            {
                dropReport = gm.GetCollectionDropsReport(action, currentCard, true);
                texts.Add(FormatCardDropList(dropReport, currentCard, action: action));
            }

            texts.Add(FormatCardAction(action, currentCard));
        }

        string newContent = texts.Join(delimiter: "\n");
        if (!string.IsNullOrWhiteSpace(newContent))
        {
            ActionTooltip.TooltipTitle = __instance.Title;
#if MELON_LOADER
            string orgContent = __instance.MyTooltip.TooltipContent;
#else
            string orgContent = Traverse.Create(__instance).Field("MyTooltip").Field("TooltipContent")
                .GetValue<string>();
#endif
            ActionTooltip.TooltipContent = orgContent + (string.IsNullOrEmpty(orgContent) ? "" : "\n") +
                                           "<size=70%>" + newContent + "</size>";
#if MELON_LOADER
            ActionTooltip.HoldText = __instance.MyTooltip.HoldText;
#else
            ActionTooltip.HoldText =
                Traverse.Create(__instance).Field("MyTooltip").Field("HoldText").GetValue<string>();
            // Traverse.Create(__instance).Method("CancelTooltip").GetValue();
#endif
            Tooltip.AddTooltip(ActionTooltip);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(DismantleActionButton), "OnHoverExit")]
    public static void DismantleActionButtonOnHoverExitPatch(DismantleActionButton __instance)
    {
        Tooltip.RemoveTooltip(ActionTooltip);
        Tooltip.Instance.TooltipContent.pageToDisplay = 1;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(DismantleActionButton), "OnDisable")]
    public static void DismantleActionButtonOnDisablePatch(DismantleActionButton __instance)
    {
        Tooltip.RemoveTooltip(ActionTooltip);
        Tooltip.Instance.TooltipContent.pageToDisplay = 1;
    }

    public static string FormatCardDropList(CollectionDropReport report, InGameCardBase fromCard, bool withStat = true,
        bool withCard = true, bool withDuability = true, CardAction action = null, int indent = 0)
    {
        List<string> texts = new();
        for (int i = 0; i < report.DropsInfo.Length; i++)
        {
            float dropRate = report.GetDropPercent(i, withStat, withCard, withDuability);
            if (Plugin.HideImpossibleDropSet && report.DropsInfo.Length != 1 && dropRate < 0.00001 &&
                (!report.DropsInfo[i].IsSuccess || report.DropsInfo[i].FinalWeight < -10000)) continue;
            string dropCardTexts = report.DropsInfo[i].Drops.Where(c => c != null).GroupBy(
                    c => new { c.CardType, c.CardName }, c => c,
                    (k, cs) => new { name = k.CardName.ToString(), count = cs.Count(), type = k.CardType })
                .Select(r => $"{ColorFloat(r.count)} ({r.type}){r.name}").Join();
            if (action != null && action.ProducedCards != null && action.ProducedCards[0].DropsLiquid)
            {
                LiquidDrop currentLiquidDrop = action.ProducedCards[0].CurrentLiquidDrop;
                string liquidDropText =
                    $"{FormatMinMaxValue(currentLiquidDrop.Quantity)} ({currentLiquidDrop.LiquidCard.CardType}){currentLiquidDrop.LiquidCard.CardName.ToString()}";
                dropCardTexts = report.DropsInfo[i].Drops.Length == 0
                    ? liquidDropText
                    : string.Join(", ", dropCardTexts, liquidDropText);
            }

            if (dropRate == 0 && report.DropsInfo.Length == 1)
            {
                if (string.IsNullOrWhiteSpace(dropCardTexts)) return "";
                return FormatBasicEntry(
                    $"{new LocalizedString { LocalizationKey = "CstiDetailedCardProgress.Action.CardDrops", DefaultText = "Card Drops" }.ToString()}",
                    dropCardTexts, indent: indent);
            }

            texts.Add(FormatBasicEntry($"{dropRate:P2}", $"{report.DropsInfo[i].CollectionName}", indent: indent));
            if (!string.IsNullOrWhiteSpace(dropCardTexts))
                texts.Add(FormatBasicEntry(
                    $"<size=55%>{new LocalizedString { LocalizationKey = "CstiDetailedCardProgress.Action.CardDrops", DefaultText = "Card Drops" }.ToString()}</size>",
                    "<size=55%>" + dropCardTexts + "</size>", indent: 2 + indent));
            texts.Add(FormatBasicEntry($"{report.DropsInfo[i].FinalWeight}/{report.TotalValue}",
                new LocalizedString
                    { LocalizationKey = "CstiDetailedCardProgress.Action.Weight", DefaultText = "Weight" }.ToString(),
                indent: 2 + indent));
            if (report.DropsInfo[i].BaseWeight != 0)
                texts.Add(FormatTooltipEntry(report.DropsInfo[i].BaseWeight,
                    new LocalizedString
                        { LocalizationKey = "CstiDetailedCardProgress.Action.Base", DefaultText = "Base" }.ToString(),
                    4 + indent));
            if (withStat && report.DropsInfo[i].StatWeightMods != null)
                foreach(StatDropWeightModReport statmod in report.DropsInfo[i].StatWeightMods)
                {
                    if (statmod.BonusWeight != 0)
                        texts.Add(FormatTooltipEntry(statmod.BonusWeight, $"{statmod.Stat.GameName}", 4 + indent));
                };
            if (withCard && report.DropsInfo[i].CardWeightMods != null)
                foreach(var cardmod in report.DropsInfo[i].CardWeightMods)
                {
                    if (cardmod.BonusWeight != 0)
                        texts.Add(FormatTooltipEntry(cardmod.BonusWeight, $"{cardmod.Card.CardModel.CardName.ToString()}",
                            4 + indent));
                };
            if (withDuability)
            {
                if (report.DropsInfo[i].DurabilitiesWeightMods.SpoilageWeight != 0)
                    texts.Add(FormatTooltipEntry(report.DropsInfo[i].DurabilitiesWeightMods.SpoilageWeight,
                        $"{fromCard.CardModel.SpoilageTime.CardStatName.ToString()}", 4 + indent));
                if (report.DropsInfo[i].DurabilitiesWeightMods.UsageWeight != 0)
                    texts.Add(FormatTooltipEntry(report.DropsInfo[i].DurabilitiesWeightMods.UsageWeight,
                        $"{fromCard.CardModel.UsageDurability.CardStatName.ToString()}", 4 + indent));
                if (report.DropsInfo[i].DurabilitiesWeightMods.FuelWeight != 0)
                    texts.Add(FormatTooltipEntry(report.DropsInfo[i].DurabilitiesWeightMods.FuelWeight,
                        $"{fromCard.CardModel.FuelCapacity.CardStatName.ToString()}", 4 + indent));
                if (report.DropsInfo[i].DurabilitiesWeightMods.ProgressWeight != 0)
                    texts.Add(FormatTooltipEntry(report.DropsInfo[i].DurabilitiesWeightMods.ProgressWeight,
                        $"{fromCard.CardModel.Progress.CardStatName.ToString()}", 4 + indent));
                if (report.DropsInfo[i].DurabilitiesWeightMods.Special1Weight != 0)
                    texts.Add(FormatTooltipEntry(report.DropsInfo[i].DurabilitiesWeightMods.Special1Weight,
                        $"{fromCard.CardModel.SpecialDurability1.CardStatName.ToString()}", 4 + indent));
                if (report.DropsInfo[i].DurabilitiesWeightMods.Special2Weight != 0)
                    texts.Add(FormatTooltipEntry(report.DropsInfo[i].DurabilitiesWeightMods.Special2Weight,
                        $"{fromCard.CardModel.SpecialDurability2.CardStatName.ToString()}", 4 + indent));
                if (report.DropsInfo[i].DurabilitiesWeightMods.Special3Weight != 0)
                    texts.Add(FormatTooltipEntry(report.DropsInfo[i].DurabilitiesWeightMods.Special3Weight,
                        $"{fromCard.CardModel.SpecialDurability3.CardStatName.ToString()}", 4 + indent));
                if (report.DropsInfo[i].DurabilitiesWeightMods.Special4Weight != 0)
                    texts.Add(FormatTooltipEntry(report.DropsInfo[i].DurabilitiesWeightMods.Special4Weight,
                        $"{fromCard.CardModel.SpecialDurability4.CardStatName.ToString()}", 4 + indent));
            }

            if (report.DropsInfo[i].StatMods != null)
            {
                List<string> stateModTexts = new();
                foreach (StatModifier statModifier in report.DropsInfo[i].StatMods)
                    stateModTexts.Add(FormatStatModifier(statModifier, 4 + indent));
                if (stateModTexts.Count > 0)
                {
                    texts.Add(FormatBasicEntry(
                        new LocalizedString
                        {
                            LocalizationKey = "CstiDetailedCardProgress.StatModifier", DefaultText = "Stat Modifier"
                        }, "", indent: 2 + indent));
                    texts.Add(stateModTexts.Join(delimiter: "\n"));
                }
            }
        }

        return $"{texts.Join(delimiter: "\n")}";
    }
}