using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using static CstiDetailedCardProgress.Utils;

namespace CstiDetailedCardProgress
{
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
#if MELON_LOADER
                string orgContent = __instance.MyTooltip == null ? "" : __instance.MyTooltip.TooltipContent;
#else
                string orgContent = Traverse.Create(__instance).Field("MyTooltip").Field("TooltipContent")
                    .GetValue<string>();
#endif
                EncounterTooltip.TooltipContent = orgContent + (string.IsNullOrEmpty(orgContent) ? "" : "\n") +
                                                  "<size=70%>" + newContent + "</size>";
#if MELON_LOADER
                EncounterTooltip.HoldText = __instance.MyTooltip == null ? "" : __instance.MyTooltip.HoldText;
#else
                EncounterTooltip.HoldText =
                    Traverse.Create(__instance).Field("MyTooltip").Field("HoldText").GetValue<string>();
#endif
                // Traverse.Create(__instance).Method("CancelTooltip").GetValue();
                Tooltip.AddTooltip(EncounterTooltip);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(EncounterPopup), "DisplayPlayerActions")]
        public static void OnEncounterDisplayPlayerActionsPatch(EncounterPopup __instance)
        {
            var encounter = __instance.CurrentEncounter;
            var actionTexts = encounter.EncounterModel.EnemyActions.Where(a => a != null && !a.DoesNotAttack).Select(a => FormatEnemyHitResult(encounter, a, __instance, 1));
            if (actionTexts.Count() > 0 && !actionTexts.All(string.IsNullOrEmpty))
            {
                __instance.AddToLog(new($"如果我被敌人击中，我可能会受伤:（平均情况下）"));
                __instance.AddToLog(new(string.Join("\n", actionTexts)));
            }
            else
            {
                __instance.AddToLog(new("我自信它伤不到我！（平均情况下）"));
            }
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
}
