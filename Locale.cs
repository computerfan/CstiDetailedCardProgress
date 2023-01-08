using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

namespace CstiDetailedCardProgress
{
    class Locale
    {
        [HarmonyPostfix, HarmonyPatch(typeof(LocalizationManager), "LoadLanguage")]
        public static void LoadLanguagePostfix()
        {
            LocalizationManager __instance = LocalizationManager.Instance;
            if (__instance == null || __instance.Languages == null || LocalizationManager.CurrentLanguage >= __instance.Languages.Length) return;
            LanguageSetting langSetting = __instance.Languages[LocalizationManager.CurrentLanguage];
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"CstiDetailedCardProgress.locale.{langSetting.LanguageName}.csv")) {
                if (stream == null || !stream.CanRead) return;
                using (StreamReader reader = new StreamReader(stream))
                {
                    string localizationString = reader.ReadToEnd();
                    Dictionary<string, List<string>> dictionary = CSVParser.LoadFromString(localizationString);
                    
                    Regex regex = new Regex("\\\\n");
                    Dictionary<string, string> CurrentTexts = Traverse.Create(__instance).Field("CurrentTexts").GetValue<Dictionary<string, string>>();
                    foreach (KeyValuePair<string, List<string>> item in dictionary)
                    {
                        if (!CurrentTexts.ContainsKey(item.Key) && item.Value.Count >= 2)
                        {
                            CurrentTexts.Add(item.Key, regex.Replace(item.Value[1], "\n"));
                        }
                    }
                }
            }

            
        }
        
    }
}
