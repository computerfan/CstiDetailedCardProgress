using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;

namespace CstiDetailedCardProgress
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static TooltipText MyTooltip = new();
        private void Awake()
        {
            // Plugin startup logic
            Harmony.CreateAndPatchAll(typeof(Plugin));
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        public static string FormatProgressAndRate(float current, float max, string name, float rate, int indent = 0)
        {
            return $"{FormatProgress(current, max, name, indent)}\n{FormatRate(rate, current, max)}";
        }

        public static string FormatProgress(float current, float max, string name, int indent = 0)
        {
            return $"{new string(' ', indent)}<color=\"yellow\">{current}/{max}</color> {name}";
        }
        public static string TimeSpanFormat(TimeSpan ts)
        {
            if(ts.Days >= 1)
            {
                return $"{ts.Days:0}d{ts.Hours:0}h";
            } else
            {
                return $"{ts.Hours:0}h";
            }
        }
        public static string FormatRate(float value, float current, float max)
        {
            string est = "";
            if (value > 0 && current < max)
            {
                float time = Math.Abs((max - current) / value);
                TimeSpan timeSpan = new TimeSpan(0, (int)(Math.Ceiling(time) * 15), 0);
                est = $" (est. {Math.Ceiling(time)}t/{TimeSpanFormat(timeSpan)})";
            } else if(value < 0 && current > 0)
            {
                float time = Math.Abs(current / value);
                TimeSpan timeSpan = new TimeSpan( 0, (int)(Math.Ceiling(time) * 15), 0);
                est = $" (est. {Math.Ceiling(time)}t/{TimeSpanFormat(timeSpan)})";
            }
            return FormatTooltipEntry(value, $"Rate<size=70%>{est}</size>", 2);
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
            } else if (value < 0)
            {
                colorTag += "<color=\"red\">";
            } else if (value == 0)
            {
                colorTag += "<color=\"yellow\">";
            }
            return $"<indent={indent/2.2:0.##}em>{colorTag}{value,-3:+0.##;-0.##;+0}</color> {name}</indent>";
        }

        public static string FormatTooltipEntry(OptionalFloatValue value, string name, int indent = 0)
        {
            if (!value)
            {
                return null;
            }
            return FormatTooltipEntry(value.FloatValue, name, indent);
        }

        [HarmonyPrefix, HarmonyPatch(typeof(InGameCardBase), "OnHoverEnter")]
        public static void OnHoverEnterPatch(InGameCardBase __instance)
        {
            if (!(bool)GameManager.DraggedCard)
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
                        BaseUsageRate.Add(FormatRateEntry( _Effect.UsageRateModifier.FloatValue, _Effect.EffectName));
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

                if (CardModel.SpoilageTime && CardModel.SpoilageTime.Show(__instance.ContainedLiquid, __instance.CurrentSpoilage))
                {
                    texts.Add(FormatProgressAndRate(__instance.CurrentSpoilage, (CardModel.SpoilageTime.MaxValue == 0 ? CardModel.SpoilageTime.FloatValue : CardModel.SpoilageTime.MaxValue), (string.IsNullOrEmpty(CardModel.SpoilageTime.CardStatName) ? "Spoilage" : __instance.CardModel.SpoilageTime.CardStatName), __instance.CurrentSpoilageRate));
                    texts.Add(FormatRateEntry(CardModel.SpoilageTime.RatePerDaytimePoint, "Base"));
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
                }
                if (CardModel.UsageDurability && CardModel.UsageDurability.Show(__instance.ContainedLiquid, __instance.CurrentUsageDurability))
                {
                    texts.Add(FormatProgressAndRate(__instance.CurrentUsageDurability, (CardModel.UsageDurability.MaxValue == 0 ? CardModel.UsageDurability.FloatValue : CardModel.UsageDurability.MaxValue), (string.IsNullOrEmpty(CardModel.UsageDurability.CardStatName) ? "Usage" : __instance.CardModel.UsageDurability.CardStatName), __instance.CurrentUsageRate));
                    texts.Add(FormatRateEntry(CardModel.UsageDurability.RatePerDaytimePoint, "Base"));
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
                }
                if (CardModel.FuelCapacity && CardModel.FuelCapacity.Show(__instance.ContainedLiquid, __instance.CurrentFuel))
                {
                    texts.Add(FormatProgressAndRate(__instance.CurrentFuel, CardModel.FuelCapacity.MaxValue, (string.IsNullOrEmpty(CardModel.FuelCapacity.CardStatName) ? "Fuel" : __instance.CardModel.FuelCapacity.CardStatName), __instance.CurrentFuelRate));
                    texts.Add(FormatRateEntry(CardModel.FuelCapacity.RatePerDaytimePoint, "Base"));
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
                }
                if (CardModel.Progress && CardModel.Progress.Show(__instance.ContainedLiquid, __instance.CurrentProgress))
                {
                    texts.Add(FormatProgressAndRate(__instance.CurrentProgress, CardModel.Progress.MaxValue, (string.IsNullOrEmpty(CardModel.Progress.CardStatName) ? "Progress" : __instance.CardModel.Progress.CardStatName), __instance.CurrentConsumableRate));
                    texts.Add(FormatRateEntry(CardModel.Progress.RatePerDaytimePoint, "Base"));
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
                }

                if (__instance.IsLiquidContainer && __instance.ContainedLiquid)
                {
                    texts.Add(FormatProgressAndRate(__instance.ContainedLiquid.CurrentLiquidQuantity, CardModel.MaxLiquidCapacity, __instance.ContainedLiquidModel.CardName, __instance.ContainedLiquid.CurrentEvaporationRate));
                    texts.Add(FormatRateEntry(CardModel.LiquidEvaporationRate, "Base"));
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

                }

                if (CardModel.SpecialDurability1 && CardModel.SpecialDurability1.Show(__instance.ContainedLiquid, __instance.CurrentSpecial1))
                {
                    texts.Add(FormatProgressAndRate(__instance.CurrentSpecial1, CardModel.SpecialDurability1.MaxValue, (string.IsNullOrEmpty(CardModel.SpecialDurability1.CardStatName) ? "SpecialDurability1" : __instance.CardModel.SpecialDurability1.CardStatName), __instance.CurrentSpecial1Rate));
                    texts.Add(FormatRateEntry(CardModel.SpecialDurability1.RatePerDaytimePoint, "Base"));
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
                }

                if (CardModel.SpecialDurability2 && CardModel.SpecialDurability2.Show(__instance.ContainedLiquid, __instance.CurrentSpecial2))
                {
                    texts.Add(FormatProgressAndRate(__instance.CurrentSpecial2, CardModel.SpecialDurability2.MaxValue, (string.IsNullOrEmpty(CardModel.SpecialDurability2.CardStatName) ? "SpecialDurability2" : __instance.CardModel.SpecialDurability2.CardStatName), __instance.CurrentSpecial2Rate));
                    texts.Add(FormatRateEntry(CardModel.SpecialDurability2.RatePerDaytimePoint, "Base"));
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
                }

                if (CardModel.SpecialDurability3 && CardModel.SpecialDurability3.Show(__instance.ContainedLiquid, __instance.CurrentSpecial3))
                {
                    texts.Add(FormatProgressAndRate(__instance.CurrentSpecial3, CardModel.SpecialDurability3.MaxValue, (string.IsNullOrEmpty(CardModel.SpecialDurability3.CardStatName) ? "SpecialDurability3" : __instance.CardModel.SpecialDurability3.CardStatName), __instance.CurrentSpecial3Rate));
                    texts.Add(FormatRateEntry(CardModel.SpecialDurability3.RatePerDaytimePoint, "Base"));
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
                }

                if (CardModel.SpecialDurability4 && CardModel.SpecialDurability4.Show(__instance.ContainedLiquid, __instance.CurrentSpecial4))
                {
                    texts.Add(FormatProgressAndRate(__instance.CurrentSpecial4, CardModel.SpecialDurability4.MaxValue, (string.IsNullOrEmpty(CardModel.SpecialDurability4.CardStatName) ? "SpecialDurability4" : __instance.CardModel.SpecialDurability4.CardStatName), __instance.CurrentSpecial4Rate));
                    texts.Add(FormatRateEntry(CardModel.SpecialDurability4.RatePerDaytimePoint, "Base"));
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

        [HarmonyPrefix, HarmonyPatch(typeof(InGameCardBase), "OnHoverExit")]
        public static void OnHoverExitPatch(InGameCardBase __instance)
        {
            Tooltip.RemoveTooltip(MyTooltip);
        }
    }
}
