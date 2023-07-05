#if MELON_LOADER
using Il2CppSystem.Collections.Generic;
using MelonLoader;
#else
using System.Collections.Generic;
#endif
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using HarmonyLib;
using System.Runtime.InteropServices.ComTypes;

namespace CstiDetailedCardProgress;

internal class Locale
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(LocalizationManager), "LoadLanguage")]
    public static void LoadLanguagePostfix()
    {
        LocalizationManager __instance = LocalizationManager.Instance;
        if (__instance == null || __instance.Languages == null ||
            LocalizationManager.CurrentLanguage >= __instance.Languages.Length) return;
        LanguageSetting langSetting = __instance.Languages[LocalizationManager.CurrentLanguage];
        using Stream stream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream($"CstiDetailedCardProgress.locale.{langSetting.LanguageName}.csv");
        if (stream == null || !stream.CanRead) return;
        using StreamReader reader = new StreamReader(stream);
        string localizationString = reader.ReadToEnd();
        var dictionary = CSVParser.LoadFromString(localizationString, Delimiter.Comma);

        Regex regex = new Regex("\\\\n");
#if MELON_LOADER
        Dictionary<string, string> currentTexts = LocalizationManager.CurrentTexts;
#else
        Dictionary<string, string> currentTexts = Traverse.Create(__instance).Field("CurrentTexts")
            .GetValue<Dictionary<string, string>>();
#endif
        foreach (var item in dictionary)
            if (!currentTexts.ContainsKey(item.Key) && item.Value.Count >= 2)
                currentTexts.Add(item.Key, regex.Replace(item.Value.get_Item(1), "\n"));
    }
}