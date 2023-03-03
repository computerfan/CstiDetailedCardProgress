using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using BepInEx.Logging;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CstiDetailedCardProgress
{
    class TooltipMod
    {
        public static ContentSizeFitter fitter;
        [HarmonyPostfix, HarmonyPatch(typeof(Tooltip), "Awake")]
        public static void TooltipAwakePatch(Tooltip __instance)
        {
            fitter = __instance.GetComponentInParent<ContentSizeFitter>();
            if (Plugin.Enabled) fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
            TextMeshProUGUI content = __instance.TooltipContent;
            content.overflowMode = TextOverflowModes.Page;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Tooltip), "LateUpdate")]
        public static void TooltipLateUpdatePatch(Tooltip __instance)
        {
            if(fitter == null || fitter.IsDestroyed())
            {
                fitter = __instance.GetComponentInParent<ContentSizeFitter>();
            }
            if (Plugin.Enabled && __instance.TooltipCount > 0)
            {
                RectTransform ParentRect = __instance.ScreenRect;
                VerticalLayoutGroup lg = __instance.GetComponent<VerticalLayoutGroup>();
                RectTransform rect = __instance.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(rect.sizeDelta.x, Mathf.Min(ParentRect.rect.height * 0.95f, lg.preferredHeight));
                TextMeshProUGUI title = __instance.TooltipTitle;
                Traverse.Create(title).Field("m_minHeight").SetValue(title.preferredHeight);
                TextMeshProUGUI content = __instance.TooltipContent;
                int totalpages = content.textInfo.pageCount;
                if (Input.GetKeyDown(Plugin.TooltipNextPageHotKey) && content.pageToDisplay < totalpages){
                    content.pageToDisplay++;
                }
                else if (Input.GetKeyDown(Plugin.TooltipPreviousPageHotKey) && content.pageToDisplay > 1) {
                    content.pageToDisplay--; 
                }
            }
        }
    }
}
