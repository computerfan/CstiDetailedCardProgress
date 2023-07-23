using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using static CstiDetailedCardProgress.Utils;

namespace CstiDetailedCardProgress;

internal class Encounter
{
    public static TooltipText EncounterTooltip = new();

    [HarmonyPostfix]
    [HarmonyPatch(typeof(TooltipProvider), "OnHoverEnter")]
    public static void OnHoverEnter(EncounterOptionButton __instance)
    {
        if (!Plugin.Enabled) return;
        EncounterPopup popup = __instance.GetComponentInParent<EncounterPopup>();
        if (popup == null) return;
        int actionIndex = __instance.Index;
        if (actionIndex < 0 || actionIndex > popup.EncounterPlayerActions.Count - 1) return;
        List<string> texts = new();
        texts.Add(FormatEncounterPlayerAction(popup.EncounterPlayerActions.get_Item(actionIndex), popup, actionIndex));

        string newContent = texts.Join(delimiter: "\n");
        if (!string.IsNullOrWhiteSpace(newContent))
        {
            EncounterTooltip.TooltipTitle = __instance.Title;
            string orgContent = __instance.MyTooltip == null ? "" : __instance.MyTooltip.TooltipContent;
            EncounterTooltip.TooltipContent = orgContent + (string.IsNullOrEmpty(orgContent) ? "" : "\n") +
                                              "<size=70%>" + newContent + "</size>";
            EncounterTooltip.HoldText = __instance.MyTooltip == null ? "" : __instance.MyTooltip.HoldText;
            Tooltip.AddTooltip(EncounterTooltip);
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(EncounterPopup), "DisplayPlayerActions")]
    public static void OnEncounterDisplayPlayerActionsPatch(EncounterPopup __instance)
    {
        InGameEncounter encounter = __instance.CurrentEncounter;
        IEnumerable<string> actionTexts = encounter.EncounterModel.EnemyActions
            .Where(a => a != null && !a.DoesNotAttack).Select(a => FormatEnemyHitResult(encounter, a, __instance, 1));
        if (actionTexts.Count() > 0 && !actionTexts.All(string.IsNullOrEmpty))
        {
            __instance.AddToLog(new EncounterLogMessage("如果我被敌人击中，我可能会受伤:（平均情况下）"));
            __instance.AddToLog(new EncounterLogMessage(string.Join("\n", actionTexts)));
        }
        else
        {
            __instance.AddToLog(new EncounterLogMessage("我自信它伤不到我！（平均情况下）"));
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(EncounterPopup), "GenerateEnemyWound")]
    public static void PostGenerateEnemyWoundPatch(EncounterPopup __instance)
    {
        static string SeverityText(WoundSeverity s)
        {
            return s switch
            {
                WoundSeverity.Minor => "本轮伤害: 轻微",
                WoundSeverity.Medium => "本轮伤害: 中等",
                WoundSeverity.Serious => "本轮伤害: 沉重",
                _ => ""
            };
        }

        EncounterPlayerDamageReport report = __instance.CurrentRoundPlayerDamageReport;
        if (report.AttackSeverity > WoundSeverity.NoWound)
            __instance.AddToLog(new EncounterLogMessage(SeverityText(report.AttackSeverity)));
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(TooltipProvider), "OnHoverExit")]
    public static void EncounterOptionButtonOnHoverExitPatch(EncounterOptionButton __instance)
    {
        Tooltip.RemoveTooltip(EncounterTooltip);
        Tooltip.Instance.TooltipContent.pageToDisplay = 1;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(TooltipProvider), "OnDisable")]
    public static void EncounterOptionButtonOnDisablePatch(EncounterOptionButton __instance)
    {
        Tooltip.RemoveTooltip(EncounterTooltip);
        Tooltip.Instance.TooltipContent.pageToDisplay = 1;
    }
}