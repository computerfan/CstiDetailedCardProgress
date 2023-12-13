#if MELON_LOADER
using MelonLoader;
#else
using BepInEx;
#endif
using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using static CstiDetailedCardProgress.Utils;


#if MELON_LOADER
[assembly: MelonInfo(typeof(CstiDetailedCardProgress.Plugin), CstiDetailedCardProgress.PluginInfo.PLUGIN_NAME, CstiDetailedCardProgress.PluginInfo.PLUGIN_VERSION, "computerfan")]
[assembly: MelonGame("WinterSpring Games", "Card Survival - Tropical Island")]
[assembly: MelonGame("WinterSpringGames", "CardSurvivalTropicalIsland")]
[assembly: MelonGame("winterspringgames", "survivaljourney")]
[assembly: MelonGame("winterspringgames", "survivaljourneydemo")]
[assembly: HarmonyDontPatchAll]
[assembly: MelonPlatformDomain(MelonPlatformDomainAttribute.CompatibleDomains.IL2CPP)]
[assembly: MelonPlatform((MelonPlatformAttribute.CompatiblePlatforms)3)] // 3 = Android
#endif

namespace CstiDetailedCardProgress
{
#if MELON_LOADER
    public class Plugin : MelonMod
#else
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
#endif
    {
        public static TooltipText MyTooltip = new();
        public static InGameStat InGamePlayerWeight;
        public static bool Enabled;
        public static KeyCode HotKey;
        public static bool WeatherCardInspectable;
        public static bool RecipesShowTargetDuration;
        public static bool HideImpossibleDropSet;
        public static KeyCode TooltipNextPageHotKey;
        public static KeyCode TooltipPreviousPageHotKey;
        public static bool AdditionalEncounterLogMessage;
        public static bool ForceInspectStatInfos;


        public static InGameCardBase LastDragHoverCard;
        public static string LastDragHoverCardOrgTooltipContent;

#if MELON_LOADER
        private MelonPreferences_Category GeneralPreferencesCategory;
        private MelonPreferences_Category TweakPreferencesCategory;
        private MelonPreferences_Entry<bool> WeatherCardInspectableEntry;
        private MelonPreferences_Entry<bool> EnabledEntry;
        private MelonPreferences_Entry<KeyCode> HotKeyEntry;
        private MelonPreferences_Entry<bool> RecipesShowTargetDurationEntry;
        private MelonPreferences_Entry<bool> HideImpossibleDropSetEntry;
        private MelonPreferences_Entry<bool> AdditionalEncounterLogMessageEntry;
        private MelonPreferences_Entry<bool> ForceInspectStatInfosEntry;
        public override void OnInitializeMelon()
        {
            GeneralPreferencesCategory = MelonPreferences.CreateCategory("General");
            TweakPreferencesCategory = MelonPreferences.CreateCategory("Tweak");
            GeneralPreferencesCategory.SetFilePath("UserData/CstiDetailedCardProgress.cfg");
            TweakPreferencesCategory.SetFilePath("UserData/CstiDetailedCardProgress.cfg");
            EnabledEntry =
 GeneralPreferencesCategory.CreateEntry(nameof(Enabled), true, "If true, will show the tool tips.");
            HotKeyEntry =
 GeneralPreferencesCategory.CreateEntry(nameof(HotKey), KeyCode.F2, "The key to enable and disable the tool tips");
            WeatherCardInspectableEntry =
 GeneralPreferencesCategory.CreateEntry(nameof(WeatherCardInspectable), true, "If true, will make weather card inspect-able");
            RecipesShowTargetDurationEntry =
 TweakPreferencesCategory.CreateEntry(nameof(RecipesShowTargetDuration), false, "If true, will show the target duration of recipes");
            HideImpossibleDropSetEntry =
 TweakPreferencesCategory.CreateEntry(nameof(HideImpossibleDropSet), true, "If true, will hide the impossible drop set");
            AdditionalEncounterLogMessageEntry = TweakPreferencesCategory.CreateEntry(
                nameof(AdditionalEncounterLogMessage), false,
                "If true, shows additional tips in the message log of combat encounter.");
            ForceInspectStatInfosEntry = GeneralPreferencesCategory.CreateEntry(nameof(ForceInspectStatInfosEntry),
                false, "If true, stats like Bacteria Fever are forced to be inspectable.");
            Enabled = EnabledEntry.Value; 
            HotKey = HotKeyEntry.Value;
            WeatherCardInspectable = WeatherCardInspectableEntry.Value;
            RecipesShowTargetDuration = RecipesShowTargetDurationEntry.Value;
            HideImpossibleDropSet = HideImpossibleDropSetEntry.Value;
            AdditionalEncounterLogMessage = AdditionalEncounterLogMessageEntry.Value;
            ForceInspectStatInfos = ForceInspectStatInfosEntry.Value;

            HarmonyLib.Harmony.CreateAndPatchAll(typeof(Plugin));
            HarmonyLib.Harmony.CreateAndPatchAll(typeof(Stat));
            HarmonyLib.Harmony.CreateAndPatchAll(typeof(Action));
            HarmonyLib.Harmony.CreateAndPatchAll(typeof(Locale));
            Locale.LoadLanguagePostfix();
            HarmonyLib.Harmony.CreateAndPatchAll(typeof(TooltipMod));
            HarmonyLib.Harmony.CreateAndPatchAll(typeof(PrefabMod));
            HarmonyLib.Harmony.CreateAndPatchAll(typeof(Encounter));

            GeneralPreferencesCategory.SaveToFile();
            TweakPreferencesCategory.SaveToFile();

            LoggerInstance.Msg($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }
#else
        public static ConfigEntry<bool> AdditionalEncounterLogMessageEntry;

        private void Awake()
        {
            Enabled = Config.Bind("General", nameof(Enabled), true, "If true, will show the tool tips.").Value;
            HotKey = Config.Bind("General", nameof(HotKey), KeyCode.F2, "The key to enable and disable the tool tips")
                .Value;
            WeatherCardInspectable = Config.Bind("General", nameof(WeatherCardInspectable), true,
                    "If true, the weather card on the left side of the clock can be clicked to inspect. True is required for showing tooltip on it.")
                .Value;
            RecipesShowTargetDuration = Config.Bind("Tweak", nameof(RecipesShowTargetDuration), false,
                "If true, cookers like traps will show exact cooking duration instead of a range.").Value;
            HideImpossibleDropSet = Config.Bind("Tweak", nameof(HideImpossibleDropSet), true,
                "If true, impossible drop sets will be hidden.").Value;
            TooltipNextPageHotKey = Config.Bind("Tooltip", nameof(TooltipNextPageHotKey), KeyCode.RightBracket,
                "The key to show next page of the tool tip.").Value;
            TooltipPreviousPageHotKey = Config.Bind("Tooltip", nameof(TooltipPreviousPageHotKey), KeyCode.LeftBracket,
                "The key to show previous page of the tool tip.").Value;
            AdditionalEncounterLogMessageEntry = Config.Bind("General", nameof(AdditionalEncounterLogMessage), false,
                "If true, shows additional tips in the message log of combat encounter.");
            AdditionalEncounterLogMessage = AdditionalEncounterLogMessageEntry.Value;
            ForceInspectStatInfos = Config.Bind("General", nameof(ForceInspectStatInfos), false, "If true, stats like Bacteria Fever are forced to be inspectable.").Value;

            // Plugin startup logic
            Harmony.CreateAndPatchAll(typeof(Plugin));
            Harmony.CreateAndPatchAll(typeof(Stat));
            Harmony.CreateAndPatchAll(typeof(Action));
            Harmony.CreateAndPatchAll(typeof(Locale));
            Harmony.CreateAndPatchAll(typeof(TooltipMod));
            Harmony.CreateAndPatchAll(typeof(PrefabMod));
            Harmony.CreateAndPatchAll(typeof(Encounter));

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }
#endif

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameManager), "Update")]
        public static void GameMangerUpdatePatch()
        {
            if (Input.GetKeyDown(HotKey))
            {
                Enabled = !Enabled;
                TooltipMod.Fitter.verticalFit =
                    ~TooltipMod.Fitter.verticalFit & ContentSizeFitter.FitMode.PreferredSize;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(InGameCardBase), "OnHoverEnter")]
        public static void OnHoverEnterPatch(InGameCardBase __instance)
        {
            if (!Enabled || __instance.IsPinned) return;
            CardData cardModel = __instance.CardModel;
            if (!cardModel) return;
            GraphicsManager graphicsM = GraphicsManager.Instance;
            GameManager gm = GameManager.Instance;
            List<string> baseSpoilageRate = new();
            List<string> baseUsageRate = new();
            List<string> baseFuelRate = new();
            List<string> baseConsumableRate = new();
            List<string> baseSpecial1Rate = new();
            List<string> baseSpecial2Rate = new();
            List<string> baseSpecial3Rate = new();
            List<string> baseSpecial4Rate = new();
            List<string> baseEvaporationRate = new();
            List<string> texts = new();

            if (GameManager.DraggedCard)
            {
                InGameDraggableCard droppedCard = GameManager.DraggedCard;
                if (!droppedCard || !droppedCard.CanBeDragged) return;
                if (LastDragHoverCard == __instance) return;

                if (LastDragHoverCard != null)
                {
                    TooltipText orgTooltip = LastDragHoverCard.MyTooltip;
                    if (orgTooltip != null) orgTooltip.TooltipContent = LastDragHoverCardOrgTooltipContent;
                    LastDragHoverCard = null;
                }

                CardOnCardAction action = __instance.PossibleAction;
                if (action == null) return;
                InGameCardBase currentCard = __instance.ContainedLiquid ?? __instance;
                if (action.ProducedCards != null)
                {
                    CollectionDropReport dropReport = gm.GetCollectionDropsReport(action, currentCard, true);
                    texts.Add(Action.FormatCardDropList(dropReport, currentCard, action: action));
                }

                texts.Add(FormatCardOnCardAction(action, currentCard, droppedCard));
                if (texts.Count > 0)
                {
                    TooltipText orgTooltip = __instance.MyTooltip;
                    LastDragHoverCardOrgTooltipContent = __instance.Content;
                    LastDragHoverCard = __instance;
                    orgTooltip.TooltipContent =
                        (string.IsNullOrEmpty(__instance.Content) ? "" : __instance.Content + "\n") + "<size=75%>" +
                        texts.Join(delimiter: "\n") + "</size>";
                }

                return;
            }

            if (cardModel.CardType == CardTypes.Location && __instance.IsCooking())
            {
                foreach (CookingCardStatus cookingstatus in __instance.CookingCards)
                {
                    if (cookingstatus == null || cookingstatus.Card == null) continue;
                    CookingRecipe recipe =
                        cardModel.GetRecipeForCard(cookingstatus.Card.CardModel, cookingstatus.Card, __instance);
                    if (recipe == null) continue;
                    if (!RecipesShowTargetDuration && recipe.MinDuration != recipe.MaxDuration)
                    {
                        texts.Add(FormatBasicEntry(
                            $"{cookingstatus.CookedDuration}/[{recipe.MinDuration}, {recipe.MaxDuration}]",
                            $"{recipe.ActionName}"));
                        texts.Add(FormatRate(1, cookingstatus.CookedDuration, recipe.MaxDuration));
                    }
                    else
                    {
                        texts.Add(FormatBasicEntry($"{cookingstatus.CookedDuration}/{cookingstatus.TargetDuration}",
                            $"{recipe.ActionName}"));
                        texts.Add(FormatRate(1, cookingstatus.CookedDuration, cookingstatus.TargetDuration));
                    }

                    if (recipe.DropsAsCollection != null && recipe.DropsAsCollection.Length != 0)
                    {
                        CardOnCardAction cardOnCardAction = recipe.GetResult(cookingstatus.Card);
                        CollectionDropReport dropReport =
                            gm.GetCollectionDropsReport(cardOnCardAction, __instance, false);
                        texts.Add("<size=70%>" + Action.FormatCardDropList(dropReport, __instance, indent: 2) +
                                  "</size>");
                    }
                }
            }

            bool isShowWeightType = Array.IndexOf(new[] { CardTypes.Hand, CardTypes.Item, CardTypes.Location },
                cardModel.CardType) > -1;
            if (isShowWeightType && (__instance.CurrentWeight != 0 || cardModel.WeightReductionWhenEquipped != 0 ||
                                     (__instance.CardsInInventory != null && __instance.CardsInInventory.Count > 0)))
            {
                texts.Add(FormatWeight(__instance.CurrentWeight));


                if (cardModel.CardType == CardTypes.Blueprint)
                {
                    texts.Add(FormatTooltipEntry(cardModel.BlueprintResultWeight,
                        new LocalizedString
                        {
                            LocalizationKey = "CstiDetailedCardProgress.BlueprintResultWeight",
                            DefaultText = "BlueprintResultWeight"
                        }, 2));
                }
                else
                {
                    texts.Add(FormatTooltipEntry(cardModel.ObjectWeight, cardModel.CardName.ToString(), 2));
                    if ((bool)graphicsM && graphicsM.CharacterWindow.HasCardEquipped(__instance))
                        texts.Add(FormatTooltipEntry(cardModel.WeightReductionWhenEquipped,
                            new LocalizedString
                            {
                                LocalizationKey = "CstiDetailedCardProgress.EquippedReduction",
                                DefaultText = "Equipped Reduction"
                            }, 2));
                }

                if (!__instance.DontCountInventoryWeight &&
                    ((__instance.CardsInInventory != null && __instance.CardsInInventory.Count > 0) ||
                     (cardModel.CanContainLiquid && __instance.ContainedLiquid)))
                {
                    texts.Add(FormatTooltipEntry(__instance.InventoryWeight(),
                        new LocalizedString
                        {
                            LocalizationKey = "CstiDetailedCardProgress.InventoryWeight",
                            DefaultText = "Inventory Weight"
                        }, 2));
                    if (__instance.ContainedLiquid)
                        texts.Add(FormatTooltipEntry(__instance.ContainedLiquid.CurrentWeight,
                            __instance.ContainedLiquid.CardModel.CardName.ToString(), 4));
                    if (__instance.CardsInInventory != null)
                    {
                        if (__instance.MaxWeightCapacity > 0)
                            texts.Add(FormatBasicEntry(
                                $"{__instance.InventoryWeight(true)}/{__instance.MaxWeightCapacity}",
                                new LocalizedString
                                { LocalizationKey = "CstiDetailedCardProgress.Capacity", DefaultText = "Capacity" },
                                indent: 4));
                        for (int i = 0; i < __instance.CardsInInventory.Count; i++)
                            if (__instance.CardsInInventory.get_Item(i) != null &&
                                !__instance.CardsInInventory.get_Item(i).IsFree)
                                texts.Add(FormatTooltipEntry(__instance.CardsInInventory.get_Item(i).CurrentWeight,
                                    $"{__instance.CardsInInventory.get_Item(i).CardAmt}x {__instance.CardsInInventory.get_Item(i).MainCard.CardModel.CardName.ToString()}",
                                    4));
                    }

                    if (cardModel.CardType == CardTypes.Blueprint)
                        texts.Add(FormatTooltipEntry(-cardModel.BlueprintResultWeight,
                            new LocalizedString
                            {
                                LocalizationKey = "CstiDetailedCardProgress.WeightReduction",
                                DefaultText = "Weight Reduction"
                            }, 4));
                    else if (cardModel.ContentWeightReduction != 0)
                        texts.Add(FormatTooltipEntry(cardModel.ContentWeightReduction,
                            $"{cardModel.CardName.ToString()} {new LocalizedString { LocalizationKey = "CstiDetailedCardProgress.Reduction", DefaultText = "Reduction" }.ToString()}",
                            4));
                }
            }

            foreach (PassiveEffect effect in __instance.PassiveEffects.Values)
            {
                if (string.IsNullOrWhiteSpace(effect.EffectName)) continue;
                int multiplier = effect.EffectStacksWithRequiredCards ? effect.CurrentStack : 1;
                string entryValue = effect.EffectStacksWithRequiredCards
                    ? $"{effect.CurrentStack}x {effect.EffectName}"
                    : effect.EffectName;
                if ((bool)cardModel.SpoilageTime && (bool)effect.SpoilageRateModifier)
                    baseSpoilageRate.Add(FormatRateEntry(multiplier * effect.SpoilageRateModifier.FloatValue,
                        entryValue));
                if ((bool)cardModel.UsageDurability && (bool)effect.UsageRateModifier)
                    baseUsageRate.Add(FormatRateEntry(multiplier * effect.UsageRateModifier.FloatValue, entryValue));
                if ((bool)cardModel.FuelCapacity && (bool)effect.FuelRateModifier)
                    baseFuelRate.Add(FormatRateEntry(multiplier * effect.FuelRateModifier.FloatValue, entryValue));
                if ((bool)cardModel.Progress && (bool)effect.ConsumableChargesModifier)
                    baseConsumableRate.Add(FormatRateEntry(multiplier * effect.ConsumableChargesModifier.FloatValue,
                        entryValue));
                if (__instance.IsLiquidContainer && __instance.ContainedLiquid && effect.LiquidRateModifier != 0)
                    baseEvaporationRate.Add(FormatRateEntry(multiplier * effect.LiquidRateModifier, entryValue));
                if ((bool)cardModel.SpecialDurability1 && (bool)effect.Special1RateModifier)
                    baseSpecial1Rate.Add(FormatRateEntry(multiplier * effect.Special1RateModifier.FloatValue,
                        entryValue));
                if ((bool)cardModel.SpecialDurability2 && (bool)effect.Special2RateModifier)
                    baseSpecial2Rate.Add(FormatRateEntry(multiplier * effect.Special2RateModifier.FloatValue,
                        entryValue));
                if ((bool)cardModel.SpecialDurability3 && (bool)effect.Special3RateModifier)
                    baseSpecial3Rate.Add(FormatRateEntry(multiplier * effect.Special3RateModifier.FloatValue,
                        entryValue));
                if ((bool)cardModel.SpecialDurability4 && (bool)effect.Special4RateModifier)
                    baseSpecial4Rate.Add(FormatRateEntry(multiplier * effect.Special4RateModifier.FloatValue,
                        entryValue));
            }

            if (__instance.IsLiquidContainer && __instance.ContainedLiquid)
                foreach (PassiveEffect effect in __instance.ContainedLiquid.PassiveEffects.Values)
                    baseEvaporationRate.Add(FormatRateEntry(effect.LiquidRateModifier, effect.EffectName));

            CookingRecipe changeRecipe = GetRecipeForCard(__instance);
            CardStateChange? recipeStateChange = changeRecipe?.IngredientChanges;

            if (cardModel.SpoilageTime &&
                cardModel.SpoilageTime.Show(__instance.ContainedLiquid, __instance.CurrentSpoilage))
            {
                texts.Add(FormatProgressAndRate(__instance.CurrentSpoilage, cardModel.SpoilageTime.MaxValue == 0
                        ? cardModel.SpoilageTime.FloatValue
                        : cardModel.SpoilageTime.MaxValue,
                    string.IsNullOrEmpty(cardModel.SpoilageTime.CardStatName)
                        ? new LocalizedString
                        { LocalizationKey = "CstiDetailedCardProgress.Spoilage", DefaultText = "Spoilage" }
                        : __instance.CardModel.SpoilageTime.CardStatName,
                    __instance.CurrentSpoilageRate + (recipeStateChange?.SpoilageChange.x ?? 0), __instance,
                    cardModel.SpoilageTime));
                if (cardModel.SpoilageTime.RatePerDaytimePoint != 0)
                    texts.Add(FormatRateEntry(cardModel.SpoilageTime.RatePerDaytimePoint,
                        new LocalizedString
                        { LocalizationKey = "CstiDetailedCardProgress.Base", DefaultText = "Base" }));
                if (baseSpoilageRate.Count > 0)
                    texts.Add(baseSpoilageRate.Join(delimiter: "\n"));
                if (__instance.IsCooking())
                    texts.Add(FormatRateEntry(cardModel.CookingConditions.ExtraSpoilageRate,
                        new LocalizedString
                        { LocalizationKey = "CstiDetailedCardProgress.Cooking", DefaultText = "Cooking" }));
                if (cardModel.LocalCounterEffects != null)
                    for (int i = 0; i < cardModel.LocalCounterEffects.Length; i++)
                        if (cardModel.LocalCounterEffects[i].IsActive(__instance))
                            texts.Add(FormatRateEntry(cardModel.LocalCounterEffects[i].SpoilageRateModifier.FloatValue,
                                cardModel.LocalCounterEffects[i].Counter.name));
                if (cardModel.SpoilageTime.ExtraRateWhenEquipped != 0 && graphicsM &&
                    graphicsM.CharacterWindow.HasCardEquipped(__instance))
                    texts.Add(FormatRateEntry(cardModel.SpoilageTime.ExtraRateWhenEquipped,
                        new LocalizedString
                        { LocalizationKey = "CstiDetailedCardProgress.Equipped", DefaultText = "Equipped" }));
                if ((recipeStateChange?.SpoilageChange.x ?? 0) != 0)
                    texts.Add(FormatRateEntry(recipeStateChange?.SpoilageChange.x ?? 0,
                        $"{new LocalizedString { LocalizationKey = "CstiDetailedCardProgress.Recipe", DefaultText = "Recipe" }.ToString()} {changeRecipe.ActionName}"));
            }

            // liquid spoilage temp fix
            if (__instance.ContainedLiquid?.CardModel?.SpoilageTime)
            {
                texts.Add(FormatProgressAndRate(__instance.ContainedLiquid.CurrentSpoilage,
                    __instance.ContainedLiquid.CardModel.SpoilageTime.MaxValue == 0
                        ? __instance.ContainedLiquid.CardModel.SpoilageTime.FloatValue
                        : __instance.ContainedLiquid.CardModel.SpoilageTime.MaxValue,
                    string.IsNullOrEmpty(__instance.ContainedLiquid.CardModel.SpoilageTime.CardStatName)
                        ? new LocalizedString
                        { LocalizationKey = "CstiDetailedCardProgress.Spoilage", DefaultText = "Spoilage" }
                        : __instance.ContainedLiquid.CardModel.SpoilageTime.CardStatName,
                    __instance.ContainedLiquid.CurrentSpoilageRate + (recipeStateChange?.SpoilageChange.x ?? 0)));
                if (__instance.ContainedLiquid.CardModel.SpoilageTime.RatePerDaytimePoint != 0)
                    texts.Add(FormatRateEntry(__instance.ContainedLiquid.CardModel.SpoilageTime.RatePerDaytimePoint,
                        new LocalizedString
                        { LocalizationKey = "CstiDetailedCardProgress.Base", DefaultText = "Base" }));
                if (baseSpoilageRate.Count > 0)
                    texts.Add(baseSpoilageRate.Join(delimiter: "\n"));
                if (__instance.ContainedLiquid.IsCooking())
                    texts.Add(FormatRateEntry(__instance.ContainedLiquid.CardModel.CookingConditions.ExtraSpoilageRate,
                        new LocalizedString
                        { LocalizationKey = "CstiDetailedCardProgress.Cooking", DefaultText = "Cooking" }));
                if (__instance.ContainedLiquid.CardModel.LocalCounterEffects != null)
                    for (int i = 0; i < __instance.ContainedLiquid.CardModel.LocalCounterEffects.Length; i++)
                        if (__instance.ContainedLiquid.CardModel.LocalCounterEffects[i]
                            .IsActive(__instance.ContainedLiquid))
                            texts.Add(FormatRateEntry(
                                __instance.ContainedLiquid.CardModel.LocalCounterEffects[i].SpoilageRateModifier
                                    .FloatValue,
                                __instance.ContainedLiquid.CardModel.LocalCounterEffects[i].Counter.name));
                if (__instance.ContainedLiquid.CardModel.SpoilageTime.ExtraRateWhenEquipped != 0 && graphicsM &&
                    graphicsM.CharacterWindow.HasCardEquipped(__instance.ContainedLiquid))
                    texts.Add(FormatRateEntry(__instance.ContainedLiquid.CardModel.SpoilageTime.ExtraRateWhenEquipped,
                        new LocalizedString
                        { LocalizationKey = "CstiDetailedCardProgress.Equipped", DefaultText = "Equipped" }));
                if ((recipeStateChange?.SpoilageChange.x ?? 0) != 0)
                    texts.Add(FormatRateEntry(recipeStateChange?.SpoilageChange.x ?? 0,
                        $"{new LocalizedString { LocalizationKey = "CstiDetailedCardProgress.Recipe", DefaultText = "Recipe" }.ToString()} {changeRecipe.ActionName}"));
            }

            if (cardModel.UsageDurability &&
                cardModel.UsageDurability.Show(__instance.ContainedLiquid, __instance.CurrentUsageDurability))
            {
                texts.Add(FormatProgressAndRate(__instance.CurrentUsageDurability,
                    cardModel.UsageDurability.MaxValue == 0
                        ? cardModel.UsageDurability.FloatValue
                        : cardModel.UsageDurability.MaxValue,
                    string.IsNullOrEmpty(cardModel.UsageDurability.CardStatName)
                        ? new LocalizedString
                        { LocalizationKey = "CstiDetailedCardProgress.Usage", DefaultText = "Usage" }
                        : __instance.CardModel.UsageDurability.CardStatName,
                    __instance.CurrentUsageRate + (recipeStateChange?.UsageChange.x ?? 0), __instance,
                    cardModel.UsageDurability));
                if (cardModel.UsageDurability.RatePerDaytimePoint != 0)
                    texts.Add(FormatRateEntry(cardModel.UsageDurability.RatePerDaytimePoint,
                        new LocalizedString
                        { LocalizationKey = "CstiDetailedCardProgress.Base", DefaultText = "Base" }));
                if (baseUsageRate.Count > 0)
                    texts.Add(baseUsageRate.Join(delimiter: "\n"));
                if (__instance.IsCooking())
                    texts.Add(FormatRateEntry(cardModel.CookingConditions.ExtraUsageRate,
                        new LocalizedString
                        { LocalizationKey = "CstiDetailedCardProgress.Cooking", DefaultText = "Cooking" }));
                if (cardModel.LocalCounterEffects != null)
                    for (int i = 0; i < cardModel.LocalCounterEffects.Length; i++)
                        if (cardModel.LocalCounterEffects[i].IsActive(__instance))
                            texts.Add(FormatRateEntry(cardModel.LocalCounterEffects[i].UsageRateModifier.FloatValue,
                                cardModel.LocalCounterEffects[i].Counter.name));
                if (cardModel.UsageDurability.ExtraRateWhenEquipped != 0 && graphicsM &&
                    graphicsM.CharacterWindow.HasCardEquipped(__instance))
                    texts.Add(FormatRateEntry(cardModel.UsageDurability.ExtraRateWhenEquipped,
                        new LocalizedString
                        { LocalizationKey = "CstiDetailedCardProgress.Equipped", DefaultText = "Equipped" }));
                if ((recipeStateChange?.UsageChange.x ?? 0) != 0)
                    texts.Add(FormatRateEntry(recipeStateChange?.UsageChange.x ?? 0,
                        $"{new LocalizedString { LocalizationKey = "CstiDetailedCardProgress.Recipe", DefaultText = "Recipe" }.ToString()} {changeRecipe.ActionName}"));
            }

            if (cardModel.FuelCapacity &&
                cardModel.FuelCapacity.Show(__instance.ContainedLiquid, __instance.CurrentFuel))
            {
                texts.Add(FormatProgressAndRate(__instance.CurrentFuel, cardModel.FuelCapacity.MaxValue,
                    string.IsNullOrEmpty(cardModel.FuelCapacity.CardStatName)
                        ? new LocalizedString
                        { LocalizationKey = "CstiDetailedCardProgress.Fuel", DefaultText = "Fuel" }
                        : __instance.CardModel.FuelCapacity.CardStatName,
                    __instance.CurrentFuelRate + (recipeStateChange?.FuelChange.x ?? 0), __instance,
                    cardModel.FuelCapacity));
                if (cardModel.FuelCapacity.RatePerDaytimePoint != 0)
                    texts.Add(FormatRateEntry(cardModel.FuelCapacity.RatePerDaytimePoint,
                        new LocalizedString
                        { LocalizationKey = "CstiDetailedCardProgress.Base", DefaultText = "Base" }));
                if (baseFuelRate.Count > 0)
                    texts.Add(baseFuelRate.Join(delimiter: "\n"));
                if (__instance.IsCooking())
                    texts.Add(FormatRateEntry(cardModel.CookingConditions.ExtraFuelRate,
                        new LocalizedString
                        { LocalizationKey = "CstiDetailedCardProgress.Cooking", DefaultText = "Cooking" }));
                if (cardModel.LocalCounterEffects != null)
                    for (int i = 0; i < cardModel.LocalCounterEffects.Length; i++)
                        if (cardModel.LocalCounterEffects[i].IsActive(__instance))
                            texts.Add(FormatRateEntry(cardModel.LocalCounterEffects[i].FuelRateModifier.FloatValue,
                                cardModel.LocalCounterEffects[i].Counter.name));
                if (cardModel.FuelCapacity.ExtraRateWhenEquipped != 0 && graphicsM &&
                    graphicsM.CharacterWindow.HasCardEquipped(__instance))
                    texts.Add(FormatRateEntry(cardModel.FuelCapacity.ExtraRateWhenEquipped,
                        new LocalizedString
                        { LocalizationKey = "CstiDetailedCardProgress.Equipped", DefaultText = "Equipped" }));
                if ((recipeStateChange?.FuelChange.x ?? 0) != 0)
                    texts.Add(FormatRateEntry(recipeStateChange?.FuelChange.x ?? 0,
                        $"{new LocalizedString { LocalizationKey = "CstiDetailedCardProgress.Recipe", DefaultText = "Recipe" }.ToString()} {changeRecipe.ActionName}"));
            }

            if (cardModel.Progress && cardModel.Progress.Show(__instance.ContainedLiquid, __instance.CurrentProgress))
            {
                texts.Add(FormatProgressAndRate(__instance.CurrentProgress, cardModel.Progress.MaxValue,
                    string.IsNullOrEmpty(cardModel.Progress.CardStatName)
                        ? new LocalizedString
                        { LocalizationKey = "CstiDetailedCardProgress.Progress", DefaultText = "Progress" }
                        : __instance.CardModel.Progress.CardStatName,
                    __instance.CurrentConsumableRate + (recipeStateChange?.ChargesChange.x ?? 0), __instance,
                    cardModel.Progress));
                if (cardModel.Progress.RatePerDaytimePoint != 0)
                    texts.Add(FormatRateEntry(cardModel.Progress.RatePerDaytimePoint,
                        new LocalizedString
                        { LocalizationKey = "CstiDetailedCardProgress.Base", DefaultText = "Base" }));
                if (baseConsumableRate.Count > 0)
                    texts.Add(baseConsumableRate.Join(delimiter: "\n"));
                if (__instance.IsCooking())
                    texts.Add(FormatRateEntry(cardModel.CookingConditions.ExtraProgressRate,
                        new LocalizedString
                        { LocalizationKey = "CstiDetailedCardProgress.Cooking", DefaultText = "Cooking" }));
                if (cardModel.LocalCounterEffects != null)
                    for (int i = 0; i < cardModel.LocalCounterEffects.Length; i++)
                        if (cardModel.LocalCounterEffects[i].IsActive(__instance))
                            texts.Add(FormatRateEntry(
                                cardModel.LocalCounterEffects[i].ConsumableChargesModifier.FloatValue,
                                cardModel.LocalCounterEffects[i].Counter.name));
                if (cardModel.Progress.ExtraRateWhenEquipped != 0 && graphicsM &&
                    graphicsM.CharacterWindow.HasCardEquipped(__instance))
                    texts.Add(FormatRateEntry(cardModel.Progress.ExtraRateWhenEquipped,
                        new LocalizedString
                        { LocalizationKey = "CstiDetailedCardProgress.Equipped", DefaultText = "Equipped" }));
                if ((recipeStateChange?.ChargesChange.x ?? 0) != 0)
                    texts.Add(FormatRateEntry(recipeStateChange?.ChargesChange.x ?? 0,
                        $"{new LocalizedString { LocalizationKey = "CstiDetailedCardProgress.Recipe", DefaultText = "Recipe" }.ToString()} {changeRecipe.ActionName}"));
            }

            if (__instance.IsLiquidContainer && __instance.ContainedLiquid)
            {
                texts.Add(FormatProgressAndRate(__instance.ContainedLiquid.CurrentLiquidQuantity,
                    cardModel.MaxLiquidCapacity, __instance.ContainedLiquidModel.CardName.ToString()
                    , recipeStateChange?.ModifyLiquid ?? false ? __instance.ContainedLiquid.CurrentEvaporationRate + (recipeStateChange?.LiquidQuantityChange.x ?? 0) : __instance.ContainedLiquid.CurrentEvaporationRate));
                if (cardModel.LiquidEvaporationRate != 0)
                    texts.Add(FormatRateEntry(cardModel.LiquidEvaporationRate,
                        new LocalizedString
                        { LocalizationKey = "CstiDetailedCardProgress.Base", DefaultText = "Base" }));
                ;
                if (baseEvaporationRate.Count > 0)
                    texts.Add(baseEvaporationRate.Join(delimiter: "\n"));
                if (__instance.CurrentProducedLiquids != null)
                    for (int i = 0; i < __instance.CurrentProducedLiquids.Count; i++)
                        if (!__instance.CurrentProducedLiquids.get_Item(i).IsEmpty &&
                            !(__instance.CurrentProducedLiquids.get_Item(i).LiquidCard !=
                              __instance.ContainedLiquidModel))
                            texts.Add(FormatRateEntry(__instance.CurrentProducedLiquids.get_Item(i).Quantity.x,
                                $"{new LocalizedString { LocalizationKey = "CstiDetailedCardProgress.Producing", DefaultText = "Producing" }.ToString()} {__instance.CurrentProducedLiquids.get_Item(i).LiquidCard.CardName.ToString()}"));
                if ((recipeStateChange?.ModifyLiquid ?? false) && (recipeStateChange?.LiquidQuantityChange.x ?? 0) != 0)
                    texts.Add(FormatRateEntry(recipeStateChange?.LiquidQuantityChange.x ?? 0,
                        $"{new LocalizedString { LocalizationKey = "CstiDetailedCardProgress.Recipe", DefaultText = "Recipe" }.ToString()} {changeRecipe.ActionName}"));
            }

            if (cardModel.SpecialDurability1 &&
                cardModel.SpecialDurability1.Show(__instance.ContainedLiquid, __instance.CurrentSpecial1))
            {
                texts.Add(FormatProgressAndRate(__instance.CurrentSpecial1, cardModel.SpecialDurability1.MaxValue,
                    string.IsNullOrEmpty(cardModel.SpecialDurability1.CardStatName)
                        ? "SpecialDurability1"
                        : __instance.CardModel.SpecialDurability1.CardStatName,
                    __instance.CurrentSpecial1Rate + (recipeStateChange?.Special1Change.x ?? 0), __instance,
                    cardModel.SpecialDurability1));
                if (cardModel.SpecialDurability1.RatePerDaytimePoint != 0)
                    texts.Add(FormatRateEntry(cardModel.SpecialDurability1.RatePerDaytimePoint,
                        new LocalizedString
                        { LocalizationKey = "CstiDetailedCardProgress.Base", DefaultText = "Base" }));
                if (baseSpecial1Rate.Count > 0)
                    texts.Add(baseSpecial1Rate.Join(delimiter: "\n"));
                if (__instance.IsCooking())
                    texts.Add(FormatRateEntry(cardModel.CookingConditions.ExtraSpecial1Rate,
                        new LocalizedString
                        { LocalizationKey = "CstiDetailedCardProgress.Cooking", DefaultText = "Cooking" }));
                if (cardModel.LocalCounterEffects != null)
                    for (int i = 0; i < cardModel.LocalCounterEffects.Length; i++)
                        if (cardModel.LocalCounterEffects[i].IsActive(__instance))
                            texts.Add(FormatRateEntry(cardModel.LocalCounterEffects[i].Special1RateModifier.FloatValue,
                                cardModel.LocalCounterEffects[i].Counter.name));
                if (cardModel.SpecialDurability1.ExtraRateWhenEquipped != 0 && graphicsM &&
                    graphicsM.CharacterWindow.HasCardEquipped(__instance))
                    texts.Add(FormatRateEntry(cardModel.SpecialDurability1.ExtraRateWhenEquipped,
                        new LocalizedString
                        { LocalizationKey = "CstiDetailedCardProgress.Equipped", DefaultText = "Equipped" }));
                if ((recipeStateChange?.Special1Change.x ?? 0) != 0)
                    texts.Add(FormatRateEntry(recipeStateChange?.Special1Change.x ?? 0,
                        $"{new LocalizedString { LocalizationKey = "CstiDetailedCardProgress.Recipe", DefaultText = "Recipe" }.ToString()} {changeRecipe.ActionName}"));
            }

            if (cardModel.SpecialDurability2 &&
                cardModel.SpecialDurability2.Show(__instance.ContainedLiquid, __instance.CurrentSpecial2))
            {
                texts.Add(FormatProgressAndRate(__instance.CurrentSpecial2, cardModel.SpecialDurability2.MaxValue,
                    string.IsNullOrEmpty(cardModel.SpecialDurability2.CardStatName)
                        ? "SpecialDurability2"
                        : __instance.CardModel.SpecialDurability2.CardStatName,
                    __instance.CurrentSpecial2Rate + (recipeStateChange?.Special2Change.x ?? 0), __instance,
                    cardModel.SpecialDurability2));
                if (cardModel.SpecialDurability2.RatePerDaytimePoint != 0)
                    texts.Add(FormatRateEntry(cardModel.SpecialDurability2.RatePerDaytimePoint,
                        new LocalizedString
                        { LocalizationKey = "CstiDetailedCardProgress.Base", DefaultText = "Base" }));
                if (baseSpecial2Rate.Count > 0)
                    texts.Add(baseSpecial2Rate.Join(delimiter: "\n"));
                if (__instance.IsCooking())
                    texts.Add(FormatRateEntry(cardModel.CookingConditions.ExtraSpecial2Rate,
                        new LocalizedString
                        { LocalizationKey = "CstiDetailedCardProgress.Cooking", DefaultText = "Cooking" }));
                if (cardModel.LocalCounterEffects != null)
                    for (int i = 0; i < cardModel.LocalCounterEffects.Length; i++)
                        if (cardModel.LocalCounterEffects[i].IsActive(__instance))
                            texts.Add(FormatRateEntry(cardModel.LocalCounterEffects[i].Special2RateModifier.FloatValue,
                                cardModel.LocalCounterEffects[i].Counter.name));
                if (cardModel.SpecialDurability2.ExtraRateWhenEquipped != 0 && graphicsM &&
                    graphicsM.CharacterWindow.HasCardEquipped(__instance))
                    texts.Add(FormatRateEntry(cardModel.SpecialDurability2.ExtraRateWhenEquipped,
                        new LocalizedString
                        { LocalizationKey = "CstiDetailedCardProgress.Equipped", DefaultText = "Equipped" }));
                if ((recipeStateChange?.Special2Change.x ?? 0) != 0)
                    texts.Add(FormatRateEntry(recipeStateChange?.Special2Change.x ?? 0,
                        $"{new LocalizedString { LocalizationKey = "CstiDetailedCardProgress.Recipe", DefaultText = "Recipe" }.ToString()} {changeRecipe.ActionName}"));
            }

            if (cardModel.SpecialDurability3 &&
                cardModel.SpecialDurability3.Show(__instance.ContainedLiquid, __instance.CurrentSpecial3))
            {
                texts.Add(FormatProgressAndRate(__instance.CurrentSpecial3, cardModel.SpecialDurability3.MaxValue,
                    string.IsNullOrEmpty(cardModel.SpecialDurability3.CardStatName)
                        ? "SpecialDurability3"
                        : __instance.CardModel.SpecialDurability3.CardStatName,
                    __instance.CurrentSpecial3Rate + (recipeStateChange?.Special3Change.x ?? 0), __instance,
                    cardModel.SpecialDurability3));
                if (cardModel.SpecialDurability3.RatePerDaytimePoint != 0)
                    texts.Add(FormatRateEntry(cardModel.SpecialDurability3.RatePerDaytimePoint,
                        new LocalizedString
                        { LocalizationKey = "CstiDetailedCardProgress.Base", DefaultText = "Base" }));
                if (baseSpecial3Rate.Count > 0)
                    texts.Add(baseSpecial3Rate.Join(delimiter: "\n"));
                if (__instance.IsCooking())
                    texts.Add(FormatRateEntry(cardModel.CookingConditions.ExtraSpecial3Rate,
                        new LocalizedString
                        { LocalizationKey = "CstiDetailedCardProgress.Cooking", DefaultText = "Cooking" }));
                if (cardModel.LocalCounterEffects != null)
                    for (int i = 0; i < cardModel.LocalCounterEffects.Length; i++)
                        if (cardModel.LocalCounterEffects[i].IsActive(__instance))
                            texts.Add(FormatRateEntry(cardModel.LocalCounterEffects[i].Special3RateModifier.FloatValue,
                                cardModel.LocalCounterEffects[i].Counter.name));
                if (cardModel.SpecialDurability3.ExtraRateWhenEquipped != 0 && graphicsM &&
                    graphicsM.CharacterWindow.HasCardEquipped(__instance))
                    texts.Add(FormatRateEntry(cardModel.SpecialDurability3.ExtraRateWhenEquipped,
                        new LocalizedString
                        { LocalizationKey = "CstiDetailedCardProgress.Equipped", DefaultText = "Equipped" }));
                if ((recipeStateChange?.Special3Change.x ?? 0) != 0)
                    texts.Add(FormatRateEntry(recipeStateChange?.Special3Change.x ?? 0,
                        $"{new LocalizedString { LocalizationKey = "CstiDetailedCardProgress.Recipe", DefaultText = "Recipe" }.ToString()} {changeRecipe.ActionName}"));
            }

            if (cardModel.SpecialDurability4 &&
                cardModel.SpecialDurability4.Show(__instance.ContainedLiquid, __instance.CurrentSpecial4))
            {
                texts.Add(FormatProgressAndRate(__instance.CurrentSpecial4, cardModel.SpecialDurability4.MaxValue,
                    string.IsNullOrEmpty(cardModel.SpecialDurability4.CardStatName)
                        ? "SpecialDurability4"
                        : __instance.CardModel.SpecialDurability4.CardStatName,
                    __instance.CurrentSpecial4Rate + (recipeStateChange?.Special4Change.x ?? 0), __instance,
                    cardModel.SpecialDurability4));
                if (cardModel.SpecialDurability4.RatePerDaytimePoint != 0)
                    texts.Add(FormatRateEntry(cardModel.SpecialDurability4.RatePerDaytimePoint,
                        new LocalizedString
                        { LocalizationKey = "CstiDetailedCardProgress.Base", DefaultText = "Base" }));
                if (baseSpecial4Rate.Count > 0)
                    texts.Add(baseSpecial4Rate.Join(delimiter: "\n"));
                if (__instance.IsCooking())
                    texts.Add(FormatRateEntry(cardModel.CookingConditions.ExtraSpecial4Rate,
                        new LocalizedString
                        { LocalizationKey = "CstiDetailedCardProgress.Cooking", DefaultText = "Cooking" }));
                if (cardModel.LocalCounterEffects != null)
                    for (int i = 0; i < cardModel.LocalCounterEffects.Length; i++)
                        if (cardModel.LocalCounterEffects[i].IsActive(__instance))
                            texts.Add(FormatRateEntry(cardModel.LocalCounterEffects[i].Special4RateModifier.FloatValue,
                                cardModel.LocalCounterEffects[i].Counter.name));
                if (cardModel.SpecialDurability4.ExtraRateWhenEquipped != 0 && graphicsM &&
                    graphicsM.CharacterWindow.HasCardEquipped(__instance))
                    texts.Add(FormatRateEntry(cardModel.SpecialDurability4.ExtraRateWhenEquipped,
                        new LocalizedString
                        { LocalizationKey = "CstiDetailedCardProgress.Equipped", DefaultText = "Equipped" }));
                if ((recipeStateChange?.Special4Change.x ?? 0) != 0)
                    texts.Add(FormatRateEntry(recipeStateChange?.Special4Change.x ?? 0,
                        $"{new LocalizedString { LocalizationKey = "CstiDetailedCardProgress.Recipe", DefaultText = "Recipe" }.ToString()} {changeRecipe.ActionName}"));
            }

            if (cardModel.IsWeapon)
            {
                texts.Add(FormatWeaponStats(cardModel.BaseClashValue, cardModel.WeaponDamage, cardModel.WeaponReach));
            }

            if (texts.Count > 0)
            {
                MyTooltip.TooltipTitle = "";
                MyTooltip.TooltipContent = "<size=75%>" + texts.Join(delimiter: "\n") + "</size>";
                MyTooltip.HoldText = "";
                MyTooltip.Priority = -1;
                Tooltip.AddTooltip(MyTooltip);
            }
        }


        [HarmonyPrefix]
        [HarmonyPatch(typeof(InGameCardBase), "OnHoverExit")]
        public static void InGameCardBaseOnHoverExitPatch(InGameCardBase __instance)
        {
            Tooltip.RemoveTooltip(MyTooltip);
            Tooltip.Instance.TooltipContent.pageToDisplay = 1;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(InGameDraggableCard), "OnEndDrag")]
        public static void InGameDraggableCardOnEndDragPatch(InGameDraggableCard __instance)
        {
            LastDragHoverCard = null;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(EquipmentButton), "Update")]
        public static void EquipmentButtonUpdatePatch(EquipmentButton __instance)
        {
            if (!Enabled)
            {
                __instance.SetTooltip(LocalizedString.Equipment, null, null);
            }
            else
            {
                if (InGamePlayerWeight == null)
                    InGamePlayerWeight = MBSingleton<GameManager>.Instance.InGamePlayerWeight;
                else if (!(bool)GameManager.DraggedCard)
                    __instance.SetTooltip(__instance.Title,
                        FormatBasicEntry(
                            $"{InGamePlayerWeight.SimpleCurrentValue}/{InGamePlayerWeight.StatModel.MinMaxValue.y}",
                            "Weight"), null);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(EquipmentButton), "OnDisable")]
        public static void EquipmentButtonOnDisablePatch()
        {
            InGamePlayerWeight = null;
        }

        public static CookingRecipe GetRecipeForCard(InGameCardBase card)
        {
            CookingRecipe recipeForCard;
            if (card.ContainedLiquid != null)
                recipeForCard = card.CurrentContainer?.CardModel?.GetRecipeForCard(card.ContainedLiquid.CardModel,
                    card.ContainedLiquid, card.CurrentContainer);
            else
                recipeForCard =
                    card.CurrentContainer?.CardModel?.GetRecipeForCard(card.CardModel, card, card.CurrentContainer);
            if (recipeForCard != null &&
                (recipeForCard.IngredientChanges.ModType == CardModifications.DurabilityChanges ||
                 (card.ContainedLiquid && recipeForCard.IngredientChanges.ModifyLiquid))) return recipeForCard;
            return null;
        }
    }
}