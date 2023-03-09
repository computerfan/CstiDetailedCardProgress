using HarmonyLib;

namespace CstiDetailedCardProgress;

internal class PrefabMod
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameManager), "Awake")]
    public static void GameManagerAwakePostfix()
    {
        // make weather card inspect-able
        if (Plugin.WeatherCardInspectable)
        {
            CardGraphics graphics = Traverse.Create(CardVisualsManager.Instance).Field("WeatherCardVisualsPrefab")
                .GetValue<CardGraphics>();
            if (graphics == null) return;
            graphics.DontBlockRaycasts = false;
        }
    }
}