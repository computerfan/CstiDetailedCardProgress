using HarmonyLib;

namespace CstiDetailedCardProgress;

internal class PrefabMod
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameManager), "Awake")]
    public static void GameManagerAwakePostfix(GameManager __instance)
    {
        // make weather card inspect-able
        if (Plugin.WeatherCardInspectable)
        {
#if MELON_LOADER
            CardGraphics graphics = CardVisualsManager.Instance.WeatherCardVisualsPrefab;
#else
            CardGraphics graphics = Traverse.CreateWithType("CardVisualsManager").Property("Instance")
                .Field("WeatherCardVisualsPrefab")
                .GetValue<CardGraphics>();
#endif

            // pre-1.04 patch
            if (graphics == null)
            {
                graphics = __instance.WeatherCardPrefab.GetComponent<CardGraphics>();
                if (__instance.CurrentWeatherCard != null) __instance.CurrentWeatherCard.BlocksRaycasts = true;
            }

            if (graphics == null) return;
            graphics.DontBlockRaycasts = false;
        }
    }
}