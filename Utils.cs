using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CstiDetailedCardProgress
{
    class Utils
    {
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
            string colorTag = "";
            if (value > 0)
            {
                colorTag += "<color=\"green\">";
            }
            else if (value < 0)
            {
                colorTag += "<color=\"red\">";
            }
            else if (value == 0)
            {
                colorTag += "<color=\"yellow\">";
            }
            return $"<indent={indent / 2.2:0.##}em>{colorTag}{value,-3:+0.##;-0.##;+0}</color> {name}</indent>";
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
