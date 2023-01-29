using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;

namespace CstiDetailedCardProgress
{
    class Utils
    {
        public static string FormatDismantleCardAction(DismantleCardAction action, InGameCardBase fromCard, int indent = 0)
        {
            List<string> texts = new();
            List<string> stateModTexts = new();
            
            foreach (StatModifier statModifier in action.StatModifications)
            {
                stateModTexts.Add(FormatStatModifier(statModifier, indent: 2));
            }
            if (stateModTexts.Count > 0)
            {
                texts.Add(FormatBasicEntry(new LocalizedString { LocalizationKey = "CstiDetailedCardProgress.StatModifier", DefaultText = "Stat Modifier" }, ""));
                texts.Add(stateModTexts.Join(delimiter: "\n"));
            }

            List<string> cardModTexts = new();
            CardStateChange stateChange = action.ReceivingCardChanges;
            if (stateChange.ModType == CardModifications.DurabilityChanges)
            {
                if (stateChange.SpoilageChange.magnitude != 0)
                    cardModTexts.Add(FormatBasicEntry(FormatMinMaxValue(stateChange.SpoilageChange), string.IsNullOrEmpty(fromCard.CardModel.SpoilageTime.CardStatName)
                        ? new LocalizedString { LocalizationKey = "CstiDetailedCardProgress.Spoilage", DefaultText = "Spoilage" }
                        : fromCard.CardModel.SpoilageTime.CardStatName, indent: 2));
                if (stateChange.UsageChange.magnitude != 0)
                    cardModTexts.Add(FormatBasicEntry(FormatMinMaxValue(stateChange.UsageChange), string.IsNullOrEmpty(fromCard.CardModel.UsageDurability.CardStatName)
                        ? new LocalizedString { LocalizationKey = "CstiDetailedCardProgress.Usage", DefaultText = "Usage" }
                        : fromCard.CardModel.UsageDurability.CardStatName, indent: 2));
                if (stateChange.FuelChange.magnitude != 0)
                    cardModTexts.Add(FormatBasicEntry(FormatMinMaxValue(stateChange.FuelChange), string.IsNullOrEmpty(fromCard.CardModel.FuelCapacity.CardStatName)
                        ? new LocalizedString { LocalizationKey = "CstiDetailedCardProgress.Fuel", DefaultText = "Fuel" }
                        : fromCard.CardModel.FuelCapacity.CardStatName, indent: 2));
                if (stateChange.ChargesChange.magnitude != 0)
                    cardModTexts.Add(FormatBasicEntry(FormatMinMaxValue(stateChange.ChargesChange), string.IsNullOrEmpty(fromCard.CardModel.Progress.CardStatName)
                        ? new LocalizedString { LocalizationKey = "CstiDetailedCardProgress.Progress", DefaultText = "Progress" }
                        : fromCard.CardModel.Progress.CardStatName, indent: 2));
                if (stateChange.LiquidQuantityChange.magnitude != 0)
                    cardModTexts.Add(FormatBasicEntry(FormatMinMaxValue(stateChange.LiquidQuantityChange), new LocalizedString { LocalizationKey = "CstiDetailedCardProgress.LiquidQuantityChange", DefaultText = "Liquid Quantity" }, indent: 2));
                if (stateChange.Special1Change.magnitude != 0)
                    cardModTexts.Add(FormatBasicEntry(FormatMinMaxValue(stateChange.Special1Change), string.IsNullOrEmpty(fromCard.CardModel.SpecialDurability1.CardStatName) ? "SpecialDurability1" : fromCard.CardModel.SpecialDurability1.CardStatName, indent: 2));
                if (stateChange.Special2Change.magnitude != 0)
                    cardModTexts.Add(FormatBasicEntry(FormatMinMaxValue(stateChange.Special2Change), string.IsNullOrEmpty(fromCard.CardModel.SpecialDurability2.CardStatName) ? "SpecialDurability2" : fromCard.CardModel.SpecialDurability2.CardStatName, indent: 2));
                if (stateChange.Special3Change.magnitude != 0)
                    cardModTexts.Add(FormatBasicEntry(FormatMinMaxValue(stateChange.Special3Change), string.IsNullOrEmpty(fromCard.CardModel.SpecialDurability3.CardStatName) ? "SpecialDurability3" : fromCard.CardModel.SpecialDurability3.CardStatName, indent: 2));
                if (stateChange.Special4Change.magnitude != 0)
                    cardModTexts.Add(FormatBasicEntry(FormatMinMaxValue(stateChange.Special4Change), string.IsNullOrEmpty(fromCard.CardModel.SpecialDurability4.CardStatName) ? "SpecialDurability4" : fromCard.CardModel.SpecialDurability4.CardStatName, indent: 2));
            }
            if (cardModTexts.Count > 0)
            {
                texts.Add(FormatBasicEntry(new LocalizedString { LocalizationKey = "CstiDetailedCardProgress.CardStateChange", DefaultText = "Card State Change" }, ""));
                texts.Add(cardModTexts.Join(delimiter: "\n"));
            }
            return texts.Join(delimiter: "\n");
        }

        public static string FormatStatModifier(StatModifier statModifier, int indent = 0)
        {
            List<string> texts = new();
            if (statModifier.ValueModifier.magnitude != 0)
            {
                texts.Add(FormatBasicEntry($"{FormatMinMaxValue(statModifier.ValueModifier)}", $"{statModifier.Stat.GameName}", indent: indent));
            }
            if (statModifier.RateModifier.magnitude != 0)
            {
                texts.Add(FormatBasicEntry($"{FormatMinMaxValue(statModifier.RateModifier)}", $"{statModifier.Stat.GameName} {new LocalizedString { LocalizationKey = "CstiDetailedCardProgress.Rate", DefaultText = "Rate" }}", indent: indent));
            }
            return texts.Join(delimiter: "\n");
        }

        public static string FormatMinMaxValue(Vector2 minMax)
        {
            if (Mathf.Approximately(minMax.x, minMax.y))
            {
                return $"{ColorFloat(minMax.x)}";
            }
            return $"[{ColorFloat(minMax.x)}, {ColorFloat(minMax.y)}]";
        }

        public static string ColorTagFromFloat(float num)
        {
            if (num > 0f)
            {
                return "<color=\"green\">";
            }
            if (num < 0f)
            {
                return "<color=\"red\">";
            }
            return "<color=\"yellow\">";
        }

        public static string ColorFloat(float num)
        {
            return $"{ColorTagFromFloat(num)}{num,-3:+0.##;-0.##;+0}</color>";
        }

        public static string FormatWeight(float weight)
        {
            return $"<color=\"yellow\">{weight:0.#}</color> {new LocalizedString { LocalizationKey = "CstiDetailedCardProgress.FormatWeight.Weight", DefaultText = "Weight" }}";
        }

        public static string FormatProgressAndRate(float current, float max, string name, float rate, int indent = 0)
        {
            return $"{FormatProgress(current, max, name, indent)}\n{FormatRate(rate, current, max)}";
        }

        public static string FormatProgress(float current, float max, string name, int indent = 0)
        {
            return $"{new string(' ', indent)}<color=\"yellow\">{current:0.##}/{max:0.##}</color> {name}";
        }
        public static string TimeSpanFormat(TimeSpan ts)
        {
            if (ts.Days >= 1)
            {
                return $"{ts.Days:0}{new LocalizedString { LocalizationKey = "CstiDetailedCardProgress.d", DefaultText = "d" }}{ts.Hours:0}{new LocalizedString { LocalizationKey = "CstiDetailedCardProgress.h", DefaultText = "h" }}";
            }
            else
            {
                return $"{ts.Hours:0}{new LocalizedString { LocalizationKey = "CstiDetailedCardProgress.h", DefaultText = "h" }}";
            }
        }
        public static string FormatRate(float value, float current, float max, float min = 0)
        {
            string est = "";
            if (value > 0 && current < max)
            {
                float time = Math.Abs((max - current) / value);
                TimeSpan timeSpan = new TimeSpan(0, (int)(Math.Ceiling(time) * 15), 0);
                est = $" ({new LocalizedString { LocalizationKey = "CstiDetailedCardProgress.est.", DefaultText = "est." }} {Math.Ceiling(time)}t/{TimeSpanFormat(timeSpan)})";
            }
            else if (value < 0 && current > min)
            {
                float time = Math.Abs((current - min) / value);
                TimeSpan timeSpan = new TimeSpan(0, (int)(Math.Ceiling(time) * 15), 0);
                est = $" ({new LocalizedString { LocalizationKey = "CstiDetailedCardProgress.est.", DefaultText = "est." }} {Math.Ceiling(time)}t/{TimeSpanFormat(timeSpan)})";
            }
            return FormatTooltipEntry(value, $"{new LocalizedString { LocalizationKey = "CstiDetailedCardProgress.Rate",DefaultText = "Rate" }}<size=70%>{est}</size>", 2);
        }
        public static string FormatRateEntry(float value, string name)
        {
            return FormatTooltipEntry(value, name, 4);
        }
        public static string FormatTooltipEntry(float value, string name, int indent = 0)
        {
            return $"<indent={indent / 2.2:0.##}em>{ColorFloat(value)} {name}</indent>";
        }

        public static string FormatTooltipEntry(OptionalFloatValue value, string name, int indent = 0)
        {
            if (!value)
            {
                return null;
            }
            return FormatTooltipEntry(value.FloatValue, name, indent);
        }

        public static string FormatBasicEntry(string s1, string s2, string s1Color = "yellow", int indent = 0)
        {
            return $"<indent={indent / 2.2:0.##}em><color=\"{s1Color}\">{s1}</color> {s2}</indent>";
        }
    }
}
