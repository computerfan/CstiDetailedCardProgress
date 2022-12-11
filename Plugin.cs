using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using static CstiDetailedCardProgress.Utils;


namespace CstiDetailedCardProgress
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static TooltipText MyTooltip = new();
        public static InGameStat inGamePlayerWeight;
        public static bool Enabled;
        public static KeyCode HotKey;

        private void Awake()
        {
            Enabled = Config.Bind("General", nameof(Enabled), true, "If true, will show the tool tips.").Value;
            HotKey = Config.Bind("General", nameof(HotKey), KeyCode.F2, "The key to enable and disable the tool tips").Value;

            // Plugin startup logic
            Harmony.CreateAndPatchAll(typeof(Plugin));
            Harmony.CreateAndPatchAll(typeof(Stat));

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }



        [HarmonyPostfix, HarmonyPatch(typeof(GameManager), "Update")]
        public static void GameMangerUpdatePatch()
        {
            if (Input.GetKeyDown(HotKey))
            {
                Enabled = !Enabled;
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(InGameCardBase), "OnHoverEnter")]
        public static void OnHoverEnterPatch(InGameCardBase __instance)
        {
            if (Enabled && !(bool)GameManager.DraggedCard )
            {
                CardData CardModel = __instance.CardModel;
                GraphicsManager GraphicsM = Traverse.Create(__instance).Field("GraphicsM").GetValue<GraphicsManager>();
                List<string> BaseSpoilageRate = new();
                List<string> BaseUsageRate = new();
                List<string> BaseFuelRate = new();
                List<string> BaseConsumableRate = new();
                List<string> BaseSpecial1Rate = new();
                List<string> BaseSpecial2Rate = new();
                List<string> BaseSpecial3Rate = new();
                List<string> BaseSpecial4Rate = new();
                List<string> BaseEvaporationRate = new();
                List<string> texts = new();

                if (CardModel)
                {
                    bool isShowWeightType = Array.IndexOf(new CardTypes[] { CardTypes.Hand, CardTypes.Item }, CardModel.CardType) > -1;
                    if (isShowWeightType && (__instance.CurrentWeight != 0 || CardModel.WeightReductionWhenEquipped != 0 || __instance.CardsInInventory != null && __instance.CardsInInventory.Count > 0)) {
                        texts.Add(FormatWeight(__instance.CurrentWeight));


                        if (CardModel.CardType == CardTypes.Blueprint)
                        {
                            texts.Add(FormatTooltipEntry(CardModel.BlueprintResultWeight, $"BlueprintResultWeight", 2));

                        }
                        else
                        {
                            texts.Add(FormatTooltipEntry(CardModel.ObjectWeight, CardModel.CardName, 2));
                            if ((bool)GraphicsM && GraphicsM.CharacterWindow.HasCardEquipped(__instance))
                            {
                                texts.Add(FormatTooltipEntry(CardModel.WeightReductionWhenEquipped, "Equipped Reduction", 2));
                            }
                        }

                        if (!__instance.DontCountInventoryWeight && (__instance.CardsInInventory != null && __instance.CardsInInventory.Count > 0 || CardModel.CanContainLiquid && __instance.ContainedLiquid))
                        {
                            texts.Add(FormatTooltipEntry(__instance.InventoryWeight(), "Inventory Weight", 2));
                            if (__instance.ContainedLiquid)
                            {
                                texts.Add(FormatTooltipEntry(__instance.ContainedLiquid.CurrentWeight, __instance.ContainedLiquid.CardModel.CardName, 4));
                            }
                            if (__instance.CardsInInventory != null)
                            {
                                if (__instance.MaxWeightCapacity > 0) { texts.Add(FormatBasicEntry($"{__instance.InventoryWeight(_IgnoreBonus: true)}/{__instance.MaxWeightCapacity}", "Capacity", indent: 4)); }
                                for (int i = 0; i < __instance.CardsInInventory.Count; i++)
                                {
                                    if (__instance.CardsInInventory[i] != null && !__instance.CardsInInventory[i].IsFree)
                                    {
                                        texts.Add(FormatTooltipEntry(__instance.CardsInInventory[i].CurrentWeight, $"{__instance.CardsInInventory[i].CardAmt}x {__instance.CardsInInventory[i].MainCard.CardModel.CardName}", 4));
                                    }
                                }
                            }
                            if (CardModel.CardType == CardTypes.Blueprint)
                            {
                                texts.Add(FormatTooltipEntry((-CardModel.BlueprintResultWeight), "Weight Reduction", 4));
                            }
                            else if (CardModel.ContentWeightReduction != 0)
                            {
                                texts.Add(FormatTooltipEntry(CardModel.ContentWeightReduction, $"{CardModel.CardName} Reduction", 4));
                            }
                            
                        }
                    }

                    foreach (PassiveEffect _Effect in __instance.PassiveEffects.Values)
                    {
                        if (string.IsNullOrWhiteSpace(_Effect.EffectName))
                        {
                            continue;
                        }
                        if ((bool)CardModel.SpoilageTime && (bool)_Effect.SpoilageRateModifier)
                        {
                            BaseSpoilageRate.Add(FormatRateEntry(_Effect.SpoilageRateModifier.FloatValue, _Effect.EffectName));
                        }
                        if ((bool)CardModel.UsageDurability && (bool)_Effect.UsageRateModifier)
                        {
                            BaseUsageRate.Add(FormatRateEntry(_Effect.UsageRateModifier.FloatValue, _Effect.EffectName));
                        }
                        if ((bool)CardModel.FuelCapacity && (bool)_Effect.FuelRateModifier)
                        {
                            BaseFuelRate.Add(FormatRateEntry(_Effect.FuelRateModifier.FloatValue, _Effect.EffectName));
                        }
                        if ((bool)CardModel.Progress && (bool)_Effect.ConsumableChargesModifier)
                        {
                            BaseConsumableRate.Add(FormatRateEntry(_Effect.ConsumableChargesModifier.FloatValue, _Effect.EffectName));
                        }
                        if (__instance.IsLiquidContainer && __instance.ContainedLiquid && _Effect.LiquidRateModifier != 0)
                        {
                            BaseEvaporationRate.Add(FormatRateEntry(_Effect.LiquidRateModifier, _Effect.EffectName));
                        }
                        if ((bool)CardModel.SpecialDurability1 && (bool)_Effect.Special1RateModifier)
                        {
                            BaseSpecial1Rate.Add(FormatRateEntry(_Effect.Special1RateModifier.FloatValue, _Effect.EffectName));
                        }
                        if ((bool)CardModel.SpecialDurability2 && (bool)_Effect.Special2RateModifier)
                        {
                            BaseSpecial2Rate.Add(FormatRateEntry(_Effect.Special2RateModifier.FloatValue, _Effect.EffectName));
                        }
                        if ((bool)CardModel.SpecialDurability3 && (bool)_Effect.Special3RateModifier)
                        {
                            BaseSpecial3Rate.Add(FormatRateEntry(_Effect.Special3RateModifier.FloatValue, _Effect.EffectName));
                        }
                        if ((bool)CardModel.SpecialDurability4 && (bool)_Effect.Special4RateModifier)
                        {
                            BaseSpecial4Rate.Add(FormatRateEntry(_Effect.Special4RateModifier.FloatValue, _Effect.EffectName));
                        }
                    }

                    if (__instance.IsLiquidContainer && __instance.ContainedLiquid)
                    {
                        foreach (PassiveEffect _Effect in __instance.ContainedLiquid.PassiveEffects.Values)
                        {
                            BaseEvaporationRate.Add(FormatRateEntry(_Effect.LiquidRateModifier, _Effect.EffectName));
                        }
                    }

                    CookingRecipe changeRecipe = GetRecipeStateChange(__instance);
                    CardStateChange? recipeStateChange = changeRecipe?.IngredientChanges;

                    if (CardModel.SpoilageTime && CardModel.SpoilageTime.Show(__instance.ContainedLiquid, __instance.CurrentSpoilage))
                    {
                        texts.Add(FormatProgressAndRate(__instance.CurrentSpoilage, (CardModel.SpoilageTime.MaxValue == 0 ? CardModel.SpoilageTime.FloatValue 
                            :CardModel.SpoilageTime.MaxValue), (string.IsNullOrEmpty(CardModel.SpoilageTime.CardStatName) ? "Spoilage" : __instance.CardModel.SpoilageTime.CardStatName), __instance.CurrentSpoilageRate + (recipeStateChange?.SpoilageChange.x ?? 0)));
                        if(CardModel.SpoilageTime.RatePerDaytimePoint != 0){texts.Add(FormatRateEntry(CardModel.SpoilageTime.RatePerDaytimePoint, "Base"));}
                        if (BaseSpoilageRate.Count > 0)
                            texts.Add(BaseSpoilageRate.Join(delimiter: "\n"));
                        if (__instance.IsCooking())
                        {
                            texts.Add(FormatRateEntry(CardModel.CookingConditions.ExtraSpoilageRate, "Cooking"));
                        }
                        if (CardModel.LocalCounterEffects != null)
                        {
                            for (int i = 0; i < CardModel.LocalCounterEffects.Length; i++)
                            {
                                if (CardModel.LocalCounterEffects[i].IsActive(__instance))
                                {
                                    texts.Add(FormatRateEntry(CardModel.LocalCounterEffects[i].SpoilageRateModifier.FloatValue, CardModel.LocalCounterEffects[i].Counter.name));
                                }
                            }
                        }
                        if (CardModel.SpoilageTime.ExtraRateWhenEquipped != 0 && GraphicsM && GraphicsM.CharacterWindow.HasCardEquipped(__instance))
                        {
                            texts.Add(FormatRateEntry(CardModel.SpoilageTime.ExtraRateWhenEquipped, "Equipped"));
                        }
                        if ((recipeStateChange?.SpoilageChange.x ?? 0) != 0)
                        {
                            texts.Add(FormatRateEntry(recipeStateChange.Value.SpoilageChange.x, $"Recipe {changeRecipe.ActionName}"));
                        }
                    }
                    if (CardModel.UsageDurability && CardModel.UsageDurability.Show(__instance.ContainedLiquid, __instance.CurrentUsageDurability))
                    {
                        texts.Add(FormatProgressAndRate(__instance.CurrentUsageDurability, (CardModel.UsageDurability.MaxValue == 0 ? CardModel.UsageDurability.FloatValue : CardModel.UsageDurability.MaxValue), (string.IsNullOrEmpty(CardModel.UsageDurability.CardStatName) ? "Usage" 
                            : __instance.CardModel.UsageDurability.CardStatName), __instance.CurrentUsageRate + (recipeStateChange?.UsageChange.x ?? 0)));
                        if(CardModel.UsageDurability.RatePerDaytimePoint != 0){texts.Add(FormatRateEntry(CardModel.UsageDurability.RatePerDaytimePoint, "Base"));}
                        if (BaseUsageRate.Count > 0)
                            texts.Add(BaseUsageRate.Join(delimiter: "\n"));
                        if (__instance.IsCooking())
                        {
                            texts.Add(FormatRateEntry(CardModel.CookingConditions.ExtraUsageRate, "Cooking"));
                        }
                        if (CardModel.LocalCounterEffects != null)
                        {
                            for (int i = 0; i < CardModel.LocalCounterEffects.Length; i++)
                            {
                                if (CardModel.LocalCounterEffects[i].IsActive(__instance))
                                {
                                    texts.Add(FormatRateEntry(CardModel.LocalCounterEffects[i].UsageRateModifier.FloatValue, CardModel.LocalCounterEffects[i].Counter.name));
                                }
                            }
                        }
                        if (CardModel.UsageDurability.ExtraRateWhenEquipped != 0 && GraphicsM && GraphicsM.CharacterWindow.HasCardEquipped(__instance))
                        {
                            texts.Add(FormatRateEntry(CardModel.UsageDurability.ExtraRateWhenEquipped, "Equipped"));
                        }
                        if ((recipeStateChange?.UsageChange.x ?? 0) != 0)
                        {
                            texts.Add(FormatRateEntry(recipeStateChange.Value.UsageChange.x, $"Recipe {changeRecipe.ActionName}"));
                        }
                    }
                    if (CardModel.FuelCapacity && CardModel.FuelCapacity.Show(__instance.ContainedLiquid, __instance.CurrentFuel))
                    {
                        texts.Add(FormatProgressAndRate(__instance.CurrentFuel, CardModel.FuelCapacity.MaxValue, (string.IsNullOrEmpty(CardModel.FuelCapacity.CardStatName) ? "Fuel" 
                            : __instance.CardModel.FuelCapacity.CardStatName), __instance.CurrentFuelRate + (recipeStateChange?.FuelChange.x ?? 0)));
                        if(CardModel.FuelCapacity.RatePerDaytimePoint != 0){texts.Add(FormatRateEntry(CardModel.FuelCapacity.RatePerDaytimePoint, "Base"));}
                        if (BaseFuelRate.Count > 0)
                            texts.Add(BaseFuelRate.Join(delimiter: "\n"));
                        if (__instance.IsCooking())
                        {
                            texts.Add(FormatRateEntry(CardModel.CookingConditions.ExtraFuelRate, "Cooking"));
                        }
                        if (CardModel.LocalCounterEffects != null)
                        {
                            for (int i = 0; i < CardModel.LocalCounterEffects.Length; i++)
                            {
                                if (CardModel.LocalCounterEffects[i].IsActive(__instance))
                                {
                                    texts.Add(FormatRateEntry(CardModel.LocalCounterEffects[i].FuelRateModifier.FloatValue, CardModel.LocalCounterEffects[i].Counter.name));
                                }
                            }
                        }
                        if (CardModel.FuelCapacity.ExtraRateWhenEquipped != 0 && GraphicsM && GraphicsM.CharacterWindow.HasCardEquipped(__instance))
                        {
                            texts.Add(FormatRateEntry(CardModel.FuelCapacity.ExtraRateWhenEquipped, "Equipped"));
                        }
                        if ((recipeStateChange?.FuelChange.x ?? 0) != 0)
                        {
                            texts.Add(FormatRateEntry(recipeStateChange.Value.FuelChange.x, $"Recipe {changeRecipe.ActionName}"));
                        }
                    }
                    if (CardModel.Progress && CardModel.Progress.Show(__instance.ContainedLiquid, __instance.CurrentProgress))
                    {
                        texts.Add(FormatProgressAndRate(__instance.CurrentProgress, CardModel.Progress.MaxValue, (string.IsNullOrEmpty(CardModel.Progress.CardStatName) ? "Progress" 
                            : __instance.CardModel.Progress.CardStatName), __instance.CurrentConsumableRate + (recipeStateChange?.ChargesChange.x ?? 0)));
                        if(CardModel.Progress.RatePerDaytimePoint != 0){texts.Add(FormatRateEntry(CardModel.Progress.RatePerDaytimePoint, "Base"));}
                        if (BaseConsumableRate.Count > 0)
                            texts.Add(BaseConsumableRate.Join(delimiter: "\n"));
                        if (__instance.IsCooking())
                        {
                            texts.Add(FormatRateEntry(CardModel.CookingConditions.ExtraProgressRate, "Cooking"));
                        }
                        if (CardModel.LocalCounterEffects != null)
                        {
                            for (int i = 0; i < CardModel.LocalCounterEffects.Length; i++)
                            {
                                if (CardModel.LocalCounterEffects[i].IsActive(__instance))
                                {
                                    texts.Add(FormatRateEntry(CardModel.LocalCounterEffects[i].ConsumableChargesModifier.FloatValue, CardModel.LocalCounterEffects[i].Counter.name));
                                }
                            }
                        }
                        if (CardModel.Progress.ExtraRateWhenEquipped != 0 && GraphicsM && GraphicsM.CharacterWindow.HasCardEquipped(__instance))
                        {
                            texts.Add(FormatRateEntry(CardModel.Progress.ExtraRateWhenEquipped, "Equipped"));
                        }
                        if ((recipeStateChange?.ChargesChange.x ?? 0) != 0)
                        {
                            texts.Add(FormatRateEntry(recipeStateChange.Value.ChargesChange.x, $"Recipe {changeRecipe.ActionName}"));
                        }
                    }

                    if (__instance.IsLiquidContainer && __instance.ContainedLiquid)
                    {
                        texts.Add(FormatProgressAndRate(__instance.ContainedLiquid.CurrentLiquidQuantity, CardModel.MaxLiquidCapacity, __instance.ContainedLiquidModel.CardName
                            , (recipeStateChange?.ModifyLiquid ?? false) ? __instance.ContainedLiquid.CurrentEvaporationRate + (recipeStateChange?.LiquidQuantityChange.x ?? 0) : __instance.ContainedLiquid.CurrentEvaporationRate));
                        if (CardModel.LiquidEvaporationRate != 0) { texts.Add(FormatRateEntry(CardModel.LiquidEvaporationRate, "Base")); };
                        if (BaseEvaporationRate.Count > 0)
                            texts.Add(BaseEvaporationRate.Join(delimiter: "\n"));
                        if (__instance.CurrentProducedLiquids != null)
                        {
                            for (int i = 0; i < __instance.CurrentProducedLiquids.Count; i++)
                            {
                                if (!__instance.CurrentProducedLiquids[i].IsEmpty && !(__instance.CurrentProducedLiquids[i].LiquidCard != __instance.ContainedLiquidModel))
                                {
                                    texts.Add(FormatRateEntry(__instance.CurrentProducedLiquids[i].Quantity.x, $"Producing {__instance.CurrentProducedLiquids[i].LiquidCard.CardName}"));
                                }
                            }
                        }
                        if ((recipeStateChange?.ModifyLiquid ?? false) && (recipeStateChange?.LiquidQuantityChange.x ?? 0) != 0)
                        {
                            texts.Add(FormatRateEntry(recipeStateChange.Value.LiquidQuantityChange.x, $"Recipe {changeRecipe.ActionName}"));
                        }
                    }

                    if (CardModel.SpecialDurability1 && CardModel.SpecialDurability1.Show(__instance.ContainedLiquid, __instance.CurrentSpecial1))
                    {
                        texts.Add(FormatProgressAndRate(__instance.CurrentSpecial1, CardModel.SpecialDurability1.MaxValue, (string.IsNullOrEmpty(CardModel.SpecialDurability1.CardStatName) ? "SpecialDurability1"
                            : __instance.CardModel.SpecialDurability1.CardStatName), __instance.CurrentSpecial1Rate + (recipeStateChange?.Special1Change.x ?? 0)));
                        if(CardModel.SpecialDurability1.RatePerDaytimePoint != 0){texts.Add(FormatRateEntry(CardModel.SpecialDurability1.RatePerDaytimePoint, "Base"));}
                        if (BaseSpecial1Rate.Count > 0)
                            texts.Add(BaseSpecial1Rate.Join(delimiter: "\n"));
                        if (__instance.IsCooking())
                        {
                            texts.Add(FormatRateEntry(CardModel.CookingConditions.ExtraSpecial1Rate, "Cooking"));
                        }
                        if (CardModel.LocalCounterEffects != null)
                        {
                            for (int i = 0; i < CardModel.LocalCounterEffects.Length; i++)
                            {
                                if (CardModel.LocalCounterEffects[i].IsActive(__instance))
                                {
                                    texts.Add(FormatRateEntry(CardModel.LocalCounterEffects[i].Special1RateModifier.FloatValue, CardModel.LocalCounterEffects[i].Counter.name));
                                }
                            }
                        }
                        if (CardModel.SpecialDurability1.ExtraRateWhenEquipped != 0 && GraphicsM && GraphicsM.CharacterWindow.HasCardEquipped(__instance))
                        {
                            texts.Add(FormatRateEntry(CardModel.SpecialDurability1.ExtraRateWhenEquipped, "Equipped"));
                        }
                        if ((recipeStateChange?.Special1Change.x ?? 0) != 0)
                        {
                            texts.Add(FormatRateEntry(recipeStateChange.Value.Special1Change.x, $"Recipe {changeRecipe.ActionName}"));
                        }
                    }

                    if (CardModel.SpecialDurability2 && CardModel.SpecialDurability2.Show(__instance.ContainedLiquid, __instance.CurrentSpecial2))
                    {
                        texts.Add(FormatProgressAndRate(__instance.CurrentSpecial2, CardModel.SpecialDurability2.MaxValue, (string.IsNullOrEmpty(CardModel.SpecialDurability2.CardStatName) ? "SpecialDurability2" 
                            : __instance.CardModel.SpecialDurability2.CardStatName), __instance.CurrentSpecial2Rate + (recipeStateChange?.Special2Change.x ?? 0)));
                        if(CardModel.SpecialDurability2.RatePerDaytimePoint != 0){texts.Add(FormatRateEntry(CardModel.SpecialDurability2.RatePerDaytimePoint, "Base"));}
                        if (BaseSpecial2Rate.Count > 0)
                            texts.Add(BaseSpecial2Rate.Join(delimiter: "\n"));
                        if (__instance.IsCooking())
                        {
                            texts.Add(FormatRateEntry(CardModel.CookingConditions.ExtraSpecial2Rate, "Cooking"));
                        }
                        if (CardModel.LocalCounterEffects != null)
                        {
                            for (int i = 0; i < CardModel.LocalCounterEffects.Length; i++)
                            {
                                if (CardModel.LocalCounterEffects[i].IsActive(__instance))
                                {
                                    texts.Add(FormatRateEntry(CardModel.LocalCounterEffects[i].Special2RateModifier.FloatValue, CardModel.LocalCounterEffects[i].Counter.name));
                                }
                            }
                        }
                        if (CardModel.SpecialDurability2.ExtraRateWhenEquipped != 0 && GraphicsM && GraphicsM.CharacterWindow.HasCardEquipped(__instance))
                        {
                            texts.Add(FormatRateEntry(CardModel.SpecialDurability2.ExtraRateWhenEquipped, "Equipped"));
                        }
                        if ((recipeStateChange?.Special2Change.x ?? 0) != 0)
                        {
                            texts.Add(FormatRateEntry(recipeStateChange.Value.Special2Change.x, $"Recipe {changeRecipe.ActionName}"));
                        }
                    }

                    if (CardModel.SpecialDurability3 && CardModel.SpecialDurability3.Show(__instance.ContainedLiquid, __instance.CurrentSpecial3))
                    {
                        texts.Add(FormatProgressAndRate(__instance.CurrentSpecial3, CardModel.SpecialDurability3.MaxValue, (string.IsNullOrEmpty(CardModel.SpecialDurability3.CardStatName) ? "SpecialDurability3"
                            : __instance.CardModel.SpecialDurability3.CardStatName), __instance.CurrentSpecial3Rate + (recipeStateChange?.Special3Change.x ?? 0)));
                        if(CardModel.SpecialDurability3.RatePerDaytimePoint != 0){texts.Add(FormatRateEntry(CardModel.SpecialDurability3.RatePerDaytimePoint, "Base"));}
                        if (BaseSpecial3Rate.Count > 0)
                            texts.Add(BaseSpecial3Rate.Join(delimiter: "\n"));
                        if (__instance.IsCooking())
                        {
                            texts.Add(FormatRateEntry(CardModel.CookingConditions.ExtraSpecial3Rate, "Cooking"));
                        }
                        if (CardModel.LocalCounterEffects != null)
                        {
                            for (int i = 0; i < CardModel.LocalCounterEffects.Length; i++)
                            {
                                if (CardModel.LocalCounterEffects[i].IsActive(__instance))
                                {
                                    texts.Add(FormatRateEntry(CardModel.LocalCounterEffects[i].Special3RateModifier.FloatValue, CardModel.LocalCounterEffects[i].Counter.name));
                                }
                            }
                        }
                        if (CardModel.SpecialDurability3.ExtraRateWhenEquipped != 0 && GraphicsM && GraphicsM.CharacterWindow.HasCardEquipped(__instance))
                        {
                            texts.Add(FormatRateEntry(CardModel.SpecialDurability3.ExtraRateWhenEquipped, "Equipped"));
                        }
                        if ((recipeStateChange?.Special3Change.x ?? 0) != 0)
                        {
                            texts.Add(FormatRateEntry(recipeStateChange.Value.Special3Change.x, $"Recipe {changeRecipe.ActionName}"));
                        }
                    }

                    if (CardModel.SpecialDurability4 && CardModel.SpecialDurability4.Show(__instance.ContainedLiquid, __instance.CurrentSpecial4))
                    {
                        texts.Add(FormatProgressAndRate(__instance.CurrentSpecial4, CardModel.SpecialDurability4.MaxValue, (string.IsNullOrEmpty(CardModel.SpecialDurability4.CardStatName) ? "SpecialDurability4"
                            : __instance.CardModel.SpecialDurability4.CardStatName), __instance.CurrentSpecial4Rate + (recipeStateChange?.Special4Change.x ?? 0)));
                        if(CardModel.SpecialDurability4.RatePerDaytimePoint != 0){texts.Add(FormatRateEntry(CardModel.SpecialDurability4.RatePerDaytimePoint, "Base"));}
                        if (BaseSpecial4Rate.Count > 0)
                            texts.Add(BaseSpecial4Rate.Join(delimiter: "\n"));
                        if (__instance.IsCooking())
                        {
                            texts.Add(FormatRateEntry(CardModel.CookingConditions.ExtraSpecial4Rate, "Cooking"));
                        }
                        if (CardModel.LocalCounterEffects != null)
                        {
                            for (int i = 0; i < CardModel.LocalCounterEffects.Length; i++)
                            {
                                if (CardModel.LocalCounterEffects[i].IsActive(__instance))
                                {
                                    texts.Add(FormatRateEntry(CardModel.LocalCounterEffects[i].Special4RateModifier.FloatValue, CardModel.LocalCounterEffects[i].Counter.name));
                                }
                            }
                        }
                        if (CardModel.SpecialDurability4.ExtraRateWhenEquipped != 0 && GraphicsM && GraphicsM.CharacterWindow.HasCardEquipped(__instance))
                        {
                            texts.Add(FormatRateEntry(CardModel.SpecialDurability4.ExtraRateWhenEquipped, "Equipped"));
                        }
                        if ((recipeStateChange?.Special4Change.x ?? 0) != 0)
                        {
                            texts.Add(FormatRateEntry(recipeStateChange.Value.Special4Change.x, $"Recipe {changeRecipe.ActionName}"));
                        }
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
            }

        }

        [HarmonyPrefix, HarmonyPatch(typeof(InGameCardBase), "OnHoverExit")]
        public static void InGameCardBaseOnHoverExitPatch(InGameCardBase __instance)
        {
            Tooltip.RemoveTooltip(MyTooltip);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(EquipmentButton), "Update")]
        public static void EquipmentButtonUpdatePatch(EquipmentButton __instance)
        {
            if (!Enabled)
            {
                __instance.SetTooltip(LocalizedString.Equipment, null, null);
            }
            else
            {
                if (inGamePlayerWeight == null)
                {
                    inGamePlayerWeight = MBSingleton<GameManager>.Instance.InGamePlayerWeight;
                }
                else if (!(bool)GameManager.DraggedCard)
                {
                    __instance.SetTooltip(__instance.Title, FormatBasicEntry($"{inGamePlayerWeight.SimpleCurrentValue}/{inGamePlayerWeight.StatModel.MinMaxValue.y}", "Weight"), null);
                }
            }

        }

        [HarmonyPostfix, HarmonyPatch(typeof(EquipmentButton), "OnDisable")]
        public static void EquipmentButtonOnDisablePatch()
        {
            inGamePlayerWeight = null;
        }

        public static CookingRecipe GetRecipeStateChange(InGameCardBase card)
        {
            CookingRecipe recipeForCard;
            if (card.ContainedLiquid != null)
            {
                recipeForCard = card.CurrentContainer?.CardModel?.GetRecipeForCard(card.ContainedLiquid.CardModel, card.ContainedLiquid, card.CurrentContainer);
            }
            else {
                recipeForCard = card.CurrentContainer?.CardModel?.GetRecipeForCard(card.CardModel, card, card.CurrentContainer);
            }
            if (recipeForCard != null && (recipeForCard.IngredientChanges.ModType == CardModifications.DurabilityChanges || card.ContainedLiquid && recipeForCard.IngredientChanges.ModifyLiquid))
            {
                    return recipeForCard;
            }
            return null;
        }
    }
}
