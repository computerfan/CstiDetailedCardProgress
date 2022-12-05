using BepInEx;
using HarmonyLib;
using System.Collections.Generic;

namespace CstiDetailedCardProgress
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        private void Awake()
        {
            // Plugin startup logic
            Harmony.CreateAndPatchAll(typeof(Plugin));
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        [HarmonyPrefix, HarmonyPatch(typeof(InGameCardBase), "OnHoverEnter")]
        public static void OnHoverEnterPatch(InGameCardBase __instance)
        {
            //if (Traverse.Create(__instance).Field("MyTooltip").GetValue() != null)
            //{
            //__instance.SetTooltip($"{__instance.Title}\nsfaf", $"{__instance.Content}sdfsfd", $"{(Traverse.Create(__instance).Field("MyTooltip").GetValue()==null?"":Traverse.Create(__instance).Field("MyTooltip").Field("HoldText").GetValue())}");
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
                        BaseSpoilageRate.Add($"{_Effect.SpoilageRateModifier.FloatValue} {_Effect.EffectName}");
                    }
                    if ((bool)CardModel.UsageDurability && (bool)_Effect.UsageRateModifier)
                    {
                        BaseUsageRate.Add($"{ _Effect.UsageRateModifier.FloatValue} {_Effect.EffectName}");
                    }
                    if ((bool)CardModel.FuelCapacity && (bool)_Effect.FuelRateModifier)
                    {
                        BaseFuelRate.Add($"{_Effect.FuelRateModifier.FloatValue} {_Effect.EffectName}");
                    }
                    if ((bool)CardModel.Progress && (bool)_Effect.ConsumableChargesModifier)
                    {
                        BaseConsumableRate.Add($"{_Effect.ConsumableChargesModifier.FloatValue} {_Effect.EffectName}");
                    }
                    if ((bool)CardModel.SpecialDurability1 && (bool)_Effect.Special1RateModifier)
                    {
                        BaseSpecial1Rate.Add($"{_Effect.Special1RateModifier.FloatValue} {_Effect.EffectName}");
                    }
                    if ((bool)CardModel.SpecialDurability2 && (bool)_Effect.Special2RateModifier)
                    {
                        BaseSpecial2Rate.Add($"{_Effect.Special2RateModifier.FloatValue} {_Effect.EffectName}");
                    }
                    if ((bool)CardModel.SpecialDurability3 && (bool)_Effect.Special3RateModifier)
                    {
                        BaseSpecial3Rate.Add($"{_Effect.Special3RateModifier.FloatValue} {_Effect.EffectName}");
                    }
                    if ((bool)CardModel.SpecialDurability4 && (bool)_Effect.Special4RateModifier)
                    {
                        BaseSpecial4Rate.Add($"{_Effect.Special4RateModifier.FloatValue} {_Effect.EffectName}");
                    }

                }

                if (__instance.IsLiquidContainer && __instance.ContainedLiquid)
                {
                    foreach (PassiveEffect _Effect in __instance.ContainedLiquid.PassiveEffects.Values)
                    {
                        BaseEvaporationRate.Add($"{_Effect.LiquidRateModifier} {_Effect.EffectName}");
                    }
                }

                if (CardModel.SpoilageTime && CardModel.SpoilageTime.Show(__instance.ContainedLiquid, __instance.CurrentSpoilage))
                {
                    texts.Add($"{__instance.CurrentSpoilage}/{(CardModel.SpoilageTime.MaxValue == 0 ? CardModel.SpoilageTime.FloatValue : CardModel.SpoilageTime.MaxValue)} {(string.IsNullOrEmpty(CardModel.SpoilageTime.CardStatName) ? "Spoilage" : __instance.CardModel.SpoilageTime.CardStatName)}");
                    texts.Add($"  {__instance.CurrentSpoilageRate} Rate");
                    texts.Add($"    {CardModel.SpoilageTime.RatePerDaytimePoint} Base");
                    if (BaseSpoilageRate.Count > 0)
                        texts.Add(BaseSpoilageRate.Join(x => "    " + x + "\n", ""));
                    if (__instance.IsCooking())
                    {
                        texts.Add($"{CardModel.CookingConditions.ExtraSpoilageRate} Cooking");
                    }
                    if (CardModel.LocalCounterEffects != null)
                    {
                        for (int i = 0; i < CardModel.LocalCounterEffects.Length; i++)
                        {
                            if (CardModel.LocalCounterEffects[i].IsActive(__instance))
                            {
                                texts.Add($"    {CardModel.LocalCounterEffects[i].SpoilageRateModifier.FloatValue} {CardModel.LocalCounterEffects[i].Counter.name}");
                            }
                        }
                    }
                    if (CardModel.SpoilageTime.ExtraRateWhenEquipped != 0 && GraphicsM && GraphicsM.CharacterWindow.HasCardEquipped(__instance))
                    {
                        texts.Add($"    {CardModel.SpoilageTime.ExtraRateWhenEquipped} Equipped");
                    }
                }
                if (CardModel.UsageDurability && CardModel.UsageDurability.Show(__instance.ContainedLiquid, __instance.CurrentUsageDurability))
                {
                    texts.Add($"{__instance.CurrentUsageDurability}/{(CardModel.UsageDurability.MaxValue == 0 ? CardModel.UsageDurability.FloatValue : CardModel.UsageDurability.MaxValue)} {(string.IsNullOrEmpty(CardModel.UsageDurability.CardStatName) ? "Usage" : __instance.CardModel.UsageDurability.CardStatName)}");
                    texts.Add($"  {__instance.CurrentUsageRate} Rate");
                    texts.Add($"    {CardModel.UsageDurability.RatePerDaytimePoint} Base");
                    if (BaseUsageRate.Count > 0)
                        texts.Add(BaseUsageRate.Join(x => "    " + x + "\n", ""));
                    if (__instance.IsCooking())
                    {
                        texts.Add($"{CardModel.CookingConditions.ExtraUsageRate} Cooking");
                    }
                    if (CardModel.LocalCounterEffects != null)
                    {
                        for (int i = 0; i < CardModel.LocalCounterEffects.Length; i++)
                        {
                            if (CardModel.LocalCounterEffects[i].IsActive(__instance))
                            {
                                texts.Add($"    {CardModel.LocalCounterEffects[i].UsageRateModifier.FloatValue} {CardModel.LocalCounterEffects[i].Counter.name}");
                            }
                        }
                    }
                    if (CardModel.UsageDurability.ExtraRateWhenEquipped != 0 && GraphicsM && GraphicsM.CharacterWindow.HasCardEquipped(__instance))
                    {
                        texts.Add($"    {CardModel.UsageDurability.ExtraRateWhenEquipped} Equipped");
                    }
                }
                if (CardModel.FuelCapacity && CardModel.FuelCapacity.Show(__instance.ContainedLiquid, __instance.CurrentFuel))
                {
                    texts.Add($"{__instance.CurrentFuel}/{CardModel.FuelCapacity.MaxValue} {(string.IsNullOrEmpty(CardModel.FuelCapacity.CardStatName) ? "Fuel" : __instance.CardModel.FuelCapacity.CardStatName)}");
                    texts.Add($"  {__instance.CurrentFuelRate} Rate");
                    texts.Add($"    {CardModel.FuelCapacity.RatePerDaytimePoint} Base");
                    if (BaseFuelRate.Count > 0)
                        texts.Add(BaseFuelRate.Join(x => "    " + x + "\n", ""));
                    if (__instance.IsCooking())
                    {
                        texts.Add($"{CardModel.CookingConditions.ExtraFuelRate} Cooking");
                    }
                    if (CardModel.LocalCounterEffects != null)
                    {
                        for (int i = 0; i < CardModel.LocalCounterEffects.Length; i++)
                        {
                            if (CardModel.LocalCounterEffects[i].IsActive(__instance))
                            {
                                texts.Add($"    {CardModel.LocalCounterEffects[i].FuelRateModifier.FloatValue} {CardModel.LocalCounterEffects[i].Counter.name}");
                            }
                        }
                    }
                    if (CardModel.FuelCapacity.ExtraRateWhenEquipped != 0 && GraphicsM && GraphicsM.CharacterWindow.HasCardEquipped(__instance))
                    {
                        texts.Add($"    {CardModel.FuelCapacity.ExtraRateWhenEquipped} Equipped");
                    }
                }
                if (CardModel.Progress && CardModel.Progress.Show(__instance.ContainedLiquid, __instance.CurrentProgress))
                {
                    texts.Add($"{__instance.CurrentProgress}/{CardModel.Progress.MaxValue} {(string.IsNullOrEmpty(CardModel.Progress.CardStatName) ? "Progress" : __instance.CardModel.Progress.CardStatName)}");
                    texts.Add($"  {__instance.CurrentConsumableRate} Rate");
                    texts.Add($"    {CardModel.Progress.RatePerDaytimePoint} Base");
                    if (BaseConsumableRate.Count > 0)
                        texts.Add(BaseConsumableRate.Join(x => "    " + x + "\n", ""));
                    if (__instance.IsCooking())
                    {
                        texts.Add($"{CardModel.CookingConditions.ExtraProgressRate} Cooking");
                    }
                    if (CardModel.LocalCounterEffects != null)
                    {
                        for (int i = 0; i < CardModel.LocalCounterEffects.Length; i++)
                        {
                            if (CardModel.LocalCounterEffects[i].IsActive(__instance))
                            {
                                texts.Add($"    {CardModel.LocalCounterEffects[i].ConsumableChargesModifier.FloatValue} {CardModel.LocalCounterEffects[i].Counter.name}");
                            }
                        }
                    }
                    if (CardModel.Progress.ExtraRateWhenEquipped != 0 && GraphicsM && GraphicsM.CharacterWindow.HasCardEquipped(__instance))
                    {
                        texts.Add($"    {CardModel.Progress.ExtraRateWhenEquipped} Equipped");
                    }
                }

                if (__instance.IsLiquidContainer && __instance.ContainedLiquid)
                {
                    texts.Add($"{__instance.ContainedLiquid.CurrentLiquidQuantity}/{CardModel.MaxLiquidCapacity} {__instance.ContainedLiquidModel.CardName}");
                    texts.Add($"  {__instance.ContainedLiquid.CurrentEvaporationRate} Rate");
                    texts.Add($"    {CardModel.LiquidEvaporationRate} Base");
                    if (BaseEvaporationRate.Count > 0)
                        texts.Add(BaseEvaporationRate.Join(x => "    " + x + "\n", ""));
                    if (__instance.CurrentProducedLiquids != null)
                    {
                        for (int i = 0; i < __instance.CurrentProducedLiquids.Count; i++)
                        {
                            if (!__instance.CurrentProducedLiquids[i].IsEmpty && !(__instance.CurrentProducedLiquids[i].LiquidCard != __instance.ContainedLiquidModel))
                            {
                                texts.Add($"    {__instance.CurrentProducedLiquids[i].Quantity.x} Producing {__instance.CurrentProducedLiquids[i].LiquidCard.CardName}");
                            }
                        }
                    }

                }

                if (CardModel.SpecialDurability1 && CardModel.SpecialDurability1.Show(__instance.ContainedLiquid, __instance.CurrentSpecial1))
                {
                    texts.Add($"{__instance.CurrentSpecial1}/{CardModel.SpecialDurability1.MaxValue} {(string.IsNullOrEmpty(CardModel.SpecialDurability1.CardStatName) ? "SpecialDurability1" : __instance.CardModel.SpecialDurability1.CardStatName)}");
                    texts.Add($"  {__instance.CurrentSpecial1Rate} Rate");
                    texts.Add($"    {CardModel.SpecialDurability1.RatePerDaytimePoint} Base");
                    if (BaseSpecial1Rate.Count > 0)
                        texts.Add(BaseSpecial1Rate.Join(x => "    " + x + "\n", ""));
                    if (__instance.IsCooking())
                    {
                        texts.Add($"{CardModel.CookingConditions.ExtraSpecial1Rate} Cooking");
                    }
                    if (CardModel.LocalCounterEffects != null)
                    {
                        for (int i = 0; i < CardModel.LocalCounterEffects.Length; i++)
                        {
                            if (CardModel.LocalCounterEffects[i].IsActive(__instance))
                            {
                                texts.Add($"    {CardModel.LocalCounterEffects[i].Special1RateModifier.FloatValue} {CardModel.LocalCounterEffects[i].Counter.name}");
                            }
                        }
                    }
                    if (CardModel.SpecialDurability1.ExtraRateWhenEquipped != 0 && GraphicsM && GraphicsM.CharacterWindow.HasCardEquipped(__instance))
                    {
                        texts.Add($"    {CardModel.SpecialDurability1.ExtraRateWhenEquipped} Equipped");
                    }
                }

                if (CardModel.SpecialDurability2 && CardModel.SpecialDurability2.Show(__instance.ContainedLiquid, __instance.CurrentSpecial2))
                {
                    texts.Add($"{__instance.CurrentSpecial2}/{CardModel.SpecialDurability2.MaxValue} {(string.IsNullOrEmpty(CardModel.SpecialDurability2.CardStatName) ? "SpecialDurability2" : __instance.CardModel.SpecialDurability2.CardStatName)}");
                    texts.Add($"  {__instance.CurrentSpecial2Rate} Rate");
                    texts.Add($"    {CardModel.SpecialDurability2.RatePerDaytimePoint} Base");
                    if (BaseSpecial2Rate.Count > 0)
                        texts.Add(BaseSpecial2Rate.Join(x => "    " + x + "\n", ""));
                    if (__instance.IsCooking())
                    {
                        texts.Add($"{CardModel.CookingConditions.ExtraSpecial2Rate} Cooking");
                    }
                    if (CardModel.LocalCounterEffects != null)
                    {
                        for (int i = 0; i < CardModel.LocalCounterEffects.Length; i++)
                        {
                            if (CardModel.LocalCounterEffects[i].IsActive(__instance))
                            {
                                texts.Add($"    {CardModel.LocalCounterEffects[i].Special2RateModifier.FloatValue} {CardModel.LocalCounterEffects[i].Counter.name}");
                            }
                        }
                    }
                    if (CardModel.SpecialDurability2.ExtraRateWhenEquipped != 0 && GraphicsM && GraphicsM.CharacterWindow.HasCardEquipped(__instance))
                    {
                        texts.Add($"    {CardModel.SpecialDurability2.ExtraRateWhenEquipped} Equipped");
                    }
                }

                if (CardModel.SpecialDurability3 && CardModel.SpecialDurability3.Show(__instance.ContainedLiquid, __instance.CurrentSpecial3))
                {
                    texts.Add($"{__instance.CurrentSpecial3}/{CardModel.SpecialDurability3.MaxValue} {(string.IsNullOrEmpty(CardModel.SpecialDurability3.CardStatName) ? "SpecialDurability3" : __instance.CardModel.SpecialDurability3.CardStatName)}");
                    texts.Add($"  {__instance.CurrentSpecial3Rate} Rate");
                    texts.Add($"    {CardModel.SpecialDurability3.RatePerDaytimePoint} Base");
                    if (BaseSpecial3Rate.Count > 0)
                        texts.Add(BaseSpecial3Rate.Join(x => "    " + x + "\n", ""));
                    if (__instance.IsCooking())
                    {
                        texts.Add($"{CardModel.CookingConditions.ExtraSpecial3Rate} Cooking");
                    }
                    if (CardModel.LocalCounterEffects != null)
                    {
                        for (int i = 0; i < CardModel.LocalCounterEffects.Length; i++)
                        {
                            if (CardModel.LocalCounterEffects[i].IsActive(__instance))
                            {
                                texts.Add($"    {CardModel.LocalCounterEffects[i].Special3RateModifier.FloatValue} {CardModel.LocalCounterEffects[i].Counter.name}");
                            }
                        }
                    }
                    if (CardModel.SpecialDurability3.ExtraRateWhenEquipped != 0 && GraphicsM && GraphicsM.CharacterWindow.HasCardEquipped(__instance))
                    {
                        texts.Add($"    {CardModel.SpecialDurability3.ExtraRateWhenEquipped} Equipped");
                    }
                }

                if (CardModel.SpecialDurability4 && CardModel.SpecialDurability4.Show(__instance.ContainedLiquid, __instance.CurrentSpecial4))
                {
                    texts.Add($"{__instance.CurrentSpecial4}/{CardModel.SpecialDurability4.MaxValue} {(string.IsNullOrEmpty(CardModel.SpecialDurability4.CardStatName) ? "SpecialDurability4" : __instance.CardModel.SpecialDurability4.CardStatName)}");
                    texts.Add($"  {__instance.CurrentSpecial4Rate} Rate");
                    texts.Add($"    {CardModel.SpecialDurability4.RatePerDaytimePoint} Base");
                    if (BaseSpecial4Rate.Count > 0)
                        texts.Add(BaseSpecial4Rate.Join(x => "    " + x + "\n", ""));
                    if (__instance.IsCooking())
                    {
                        texts.Add($"{CardModel.CookingConditions.ExtraSpecial4Rate} Cooking");
                    }
                    if (CardModel.LocalCounterEffects != null)
                    {
                        for (int i = 0; i < CardModel.LocalCounterEffects.Length; i++)
                        {
                            if (CardModel.LocalCounterEffects[i].IsActive(__instance))
                            {
                                texts.Add($"    {CardModel.LocalCounterEffects[i].Special4RateModifier.FloatValue} {CardModel.LocalCounterEffects[i].Counter.name}");
                            }
                        }
                    }
                    if (CardModel.SpecialDurability4.ExtraRateWhenEquipped != 0 && GraphicsM && GraphicsM.CharacterWindow.HasCardEquipped(__instance))
                    {
                        texts.Add($"    {CardModel.SpecialDurability4.ExtraRateWhenEquipped} Equipped");
                    }
                }

                if (texts.Count > 0)
                {
                    __instance.SetTooltip("", "<size=80%>" + texts.Join(delimiter: "\n") + "</size>", "");
                }

            }

        }
    }
}
