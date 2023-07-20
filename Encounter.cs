﻿using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
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
                string orgContent = __instance.MyTooltip == null ? "" : __instance.MyTooltip.TooltipContent;
                EncounterTooltip.TooltipContent = orgContent + (string.IsNullOrEmpty(orgContent) ? "" : "\n") +
                                                  "<size=70%>" + newContent + "</size>";
                EncounterTooltip.HoldText = __instance.MyTooltip == null ? "" : __instance.MyTooltip.HoldText;
                Tooltip.AddTooltip(EncounterTooltip);
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
