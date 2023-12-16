using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using UnityEngine;

namespace CstiDetailedCardProgress;

public static class Utils
{
    public static string LcStr(string key, string defaultText = null)
    {
        if (LocalizationManager.CurrentTexts != null && LocalizationManager.CurrentTexts.TryGetValue(key, out string value))
            return value;
        return defaultText ?? key;
    }

#if MELON_LOADER
public static void GetWoundsForSeverity_il2cpp(this PlayerWounds playerWounds, WoundSeverity _WoundSeverity, List<PlayerWound> _List)
  {
    switch (_WoundSeverity)
    {
      case WoundSeverity.Minor:
        if (playerWounds.MinorWounds == null)
          break;
        _List.AddRange(playerWounds.MinorWounds);
        break;
      case WoundSeverity.Medium:
        if (playerWounds.MediumWounds == null)
          break;
        _List.AddRange(playerWounds.MediumWounds);
        break;
      case WoundSeverity.Serious:
        if (playerWounds.SeriousWounds == null)
          break;
        _List.AddRange(playerWounds.SeriousWounds);
        break;
      default:
        if (playerWounds.UnharmedResults == null)
          break;
        _List.AddRange(playerWounds.UnharmedResults);
        break;
    }
  }
#else
    public static T get_Item<T>(this List<T> list, int index)
    {
        return list[index];
    }
#endif

    public static string FormatEncounterPlayerAction(EncounterPlayerAction action, EncounterPopup popup,
        int actionIndex)
    {
        MeleeClashResultsReport backupCurrentRoundMeleeClashResult = popup.CurrentRoundMeleeClashResult;
        RangedClashResultReport backupCurrentRoundRangedClashResult = popup.CurrentRoundRangedClashResult;
        float num = popup.CalculateActionClashChance(action);
        MeleeClashResultsReport currentRoundMeleeClashResult = popup.CurrentRoundMeleeClashResult;
        RangedClashResultReport currentRoundRangedClashResult = popup.CurrentRoundRangedClashResult;
        popup.CurrentRoundMeleeClashResult = backupCurrentRoundMeleeClashResult;
        popup.CurrentRoundRangedClashResult = backupCurrentRoundRangedClashResult;

        ClashResultsReport commonClashResult = action.ActionRange switch
        {
            ActionRange.Melee => currentRoundMeleeClashResult.CommonClashReport,
            ActionRange.Ranged => currentRoundRangedClashResult.CommonClashReport,
            _ => currentRoundMeleeClashResult.CommonClashReport
        };

        InGameEncounter encounter = popup.CurrentEncounter;

        StringBuilder summary = new($"<size=85%><color=yellow>{LcStr("CstiDetailedCardProgress.Encounter.ActionPreview", "Action Preview")} {encounter.EnemyName}</color></size>\n");
        summary.AppendLine($"{LcStr("CstiDetailedCardProgress.Encounter.PlayerAction", "Player Action")}: {action.ActionName}");
        if (action.ActionRange == ActionRange.Melee)
        {
            float playerSuccess = currentRoundMeleeClashResult.PlayerPercentChance;
            float enemySuccess = currentRoundMeleeClashResult.EnemyPercentChance;
            summary.AppendLine($" {LcStr("CstiDetailedCardProgress.Encounter.PowerComparison", "Power Comparison")}: {currentRoundMeleeClashResult.CommonClashReport.PlayerClashValue:0.#} : {currentRoundMeleeClashResult.CommonClashReport.EnemyClashValue:0.#}");
            summary.AppendLine($" {LcStr("CstiDetailedCardProgress.Encounter.PlayerAttackHitRate", "Player Attack Hit Rate")}: {playerSuccess * 100f:0.##}%{(commonClashResult.PlayerCannotFail ? $" <color=green>({LcStr("CstiDetailedCardProgress.Encounter.GuaranteedHit", "Guaranteed Hit")})</color>" : "")}");
            summary.AppendLine($" {LcStr("CstiDetailedCardProgress.Encounter.EnemyAttackHitRate", "Enemy Attack Hit Rate")}: {enemySuccess * 100f:0.##}%{(commonClashResult.EnemyCannotFail ? $" <color=red>({LcStr("CstiDetailedCardProgress.Encounter.GuaranteedHit", "Guaranteed Hit")})</color>" : "")}");
            if (action.AssociatedCard)
                foreach (PlayerEncounterVariable stat in action.AssociatedCard.CardModel.WeaponClashStatInfluences)
                    summary.AppendLine($"  {stat.Stat.GameName.ToString()}: {FormatMinMaxValue(stat.GenerateRandomRange())}");
            if (!action.DoesNotAttack)
            {
                Vector2 damage = action.Damage;
                Vector2 damageStatSum = action.DamageStatSum;
                Vector2 sizeDamage = new(popup.PlayerSize, popup.PlayerSize);

                summary.AppendLine($" {LcStr("CstiDetailedCardProgress.Encounter.DamagePower", "Damage Power")}: {FormatMinMaxValue(damage)}");
                summary.AppendLine($" {LcStr("CstiDetailedCardProgress.Encounter.StatusDamageBonus", "Status Damage Bonus")}: {FormatMinMaxValue(damageStatSum)}");
                if (action.AssociatedCard)
                    foreach (PlayerEncounterVariable stat in action.AssociatedCard.CardModel.WeaponDamageStatInfluences)
                        summary.AppendLine($"  {stat.Stat.GameName.ToString()}: {FormatMinMaxValue(stat.GenerateRandomRange())}");
                summary.AppendLine($" {LcStr("CstiDetailedCardProgress.Encounter.MeleeSizeDamageBonus", "Melee Size Damage Bonus")}: {ColorFloat(popup.PlayerSize)}");
                summary.AppendLine($" {LcStr("CstiDetailedCardProgress.Encounter.DamageType", "Damage Type")}: {action.DamageTypes.Select(t => t.Name.ToString()).Join()}");
                EncounterPlayerDamageReport damageReport = new()
                {
                    SizeDefense = encounter.CurrentEnemySize
                };

                summary.AppendLine($" {LcStr("CstiDetailedCardProgress.Encounter.HitableParts", "Hitable Parts")}:")
                    .AppendLine(
                        $"{FormatPlayerHitResult(encounter, action, popup, damage + damageStatSum + sizeDamage)}");
            }

            summary.AppendLine($" {LcStr("CstiDetailedCardProgress.Encounter.DistanceChange", "Distance Change")}: {GetDistanceChangeText(action.DistanceChange)}");
        }
        else if (action.ActionRange == ActionRange.Ranged)
        {
            if (encounter.Distant)
            {
                summary.AppendLine(
                    $" {LcStr("CstiDetailedCardProgress.Encounter.PowerComparison", "Power Comparison")}: {currentRoundRangedClashResult.PlayerClashValue:0.#} : {currentRoundRangedClashResult.EnemyClashValue:0.#}");

                float playerSuccess = currentRoundRangedClashResult.PlayerSuccessChance;
                float enemySuccess = currentRoundRangedClashResult.EnemySuccessChance;
                // Debug.Log($"Inaccuracy: {action.ClashInaccuracy}");
                summary.AppendLine($"{LcStr("CstiDetailedCardProgress.Encounter.PlayerHitRate", "Player Hit Rate")}: {playerSuccess * 100f:0.##}%");
                summary.AppendLine($"{LcStr("CstiDetailedCardProgress.Encounter.EnemyHitRate", "Enemy Hit Rate")}: {enemySuccess * 100f:0.##}%");
            }
            else
            {
                summary.AppendLine(
                    $" {LcStr("CstiDetailedCardProgress.Encounter.PowerComparison", "Power Comparison")}: {currentRoundMeleeClashResult.CommonClashReport.PlayerClashValue:0.#} : {currentRoundMeleeClashResult.CommonClashReport.EnemyClashValue:0.#}");

                float playerSuccess = currentRoundMeleeClashResult.PlayerPercentChance;
                float enemySuccess = currentRoundMeleeClashResult.EnemyPercentChance;

                summary.AppendLine($"{LcStr("CstiDetailedCardProgress.Encounter.PlayerHitRate", "Player Hit Rate")}: {playerSuccess * 100f:0.##}%");
                summary.AppendLine($"{LcStr("CstiDetailedCardProgress.Encounter.EnemyHitRate", "Enemy Hit Rate")}: {enemySuccess * 100f:0.##}%");
            }

            Vector2 damage = action.Damage;
            Vector2 damageStatSum = action.DamageStatSum;
            if (!action.DoesNotAttack)
            {
                summary.AppendLine($" {LcStr("CstiDetailedCardProgress.Encounter.BasicDamagePower", "Basic Damage Power")}: {FormatMinMaxValue(damage)}");
                summary.AppendLine($" {LcStr("CstiDetailedCardProgress.Encounter.StatusDamageBonus", "Status Damage Bonus")}: {FormatMinMaxValue(damageStatSum)}");
                if (action.AssociatedCard)
                    foreach (PlayerEncounterVariable stat in action.AssociatedCard.CardModel.WeaponDamageStatInfluences)
                        summary.AppendLine($"  {stat.Stat.GameName.ToString()}: {FormatMinMaxValue(stat.GenerateRandomRange())}");
                if (action.AmmoCard)
                    foreach (PlayerEncounterVariable stat in action.AmmoCard.CardModel.WeaponDamageStatInfluences)
                        summary.AppendLine($"  {stat.Stat.GameName.ToString()}: {FormatMinMaxValue(stat.GenerateRandomRange())}");
                summary.AppendLine($" {LcStr("CstiDetailedCardProgress.Encounter.DamageTypes", "Damage Types")}: {action.DamageTypes.Select(t => t.Name.ToString()).Join()}");
                summary.AppendLine($" {LcStr("CstiDetailedCardProgress.Encounter.HitableParts", "Hitable Parts")}:")
                    .AppendLine($"{FormatPlayerHitResult(encounter, action, popup, damage + damageStatSum)}");
            }

            summary.AppendLine($" {LcStr("CstiDetailedCardProgress.Encounter.DistanceChange", "Distance Change")}: {GetDistanceChangeText(action.DistanceChange)}");
        }

        summary.AppendLine($"{LcStr("CstiDetailedCardProgress.Encounter.CurrentEnemyStatus", "Current Enemy Status")}:")
            .AppendLine($" {LcStr("CstiDetailedCardProgress.Encounter.Health", "Health")}: {encounter.CurrentEnemyBlood}")
            .AppendLine($" {LcStr("CstiDetailedCardProgress.Encounter.Courage", "Courage")}: {encounter.CurrentEnemyMorale}")
            .AppendLine($" {LcStr("CstiDetailedCardProgress.Encounter.Stamina", "Stamina")}: {encounter.CurrentEnemyStamina}")
            .AppendLine($" {LcStr("CstiDetailedCardProgress.Encounter.MeleeSkill", "Melee Skill")}: {encounter.CurrentEnemyMeleeSkill}")
            .AppendLine($" {LcStr("CstiDetailedCardProgress.Encounter.RangedSkill", "Ranged Skill")}: {encounter.CurrentEnemyRangedSkill}")
            .AppendLine($" {LcStr("CstiDetailedCardProgress.Encounter.Stealth", "Stealth")}: {encounter.CurrentEnemyStealth}");

        summary.AppendLine($"{LcStr("CstiDetailedCardProgress.Encounter.PowerDetailedData", "Power Detailed Data")}:\n{FormatPlayerClashValue(encounter, action, popup)}");
        return summary.ToString();
    }

    public static EnemyActionSelectionReport GenEnemyActionSelection(InGameEncounter _FromEncounter,
        List<EnemyAction> _ActionsList)
    {
        EnemyActionSelectionReport result = default;
        if (!_FromEncounter || !_FromEncounter.EncounterModel || _ActionsList == null || _ActionsList.Count == 0)
        {
            result.Actions = Array.Empty<EnemyActionSelectionInfo>();
            return result;
        }

        int num = 0;
        result.Actions = new EnemyActionSelectionInfo[_ActionsList.Count];
        for (int i = 0; i < _ActionsList.Count; i++)
        {
            result.Actions[i] = default;
            result.Actions[i].ActionName = new LocalizedString
            { LocalizationKey = _ActionsList[i].ActionLog.MainLogKey };
            result.Actions[i].BaseWeight = _ActionsList[i].BaseWeight;
            result.Actions[i].DistanceWeightMod = _FromEncounter.Distant ? _ActionsList[i].DistanceWeightModifier : 0;
            result.Actions[i].CloseWeightMod = _FromEncounter.Distant ? 0 : _ActionsList[i].CloseRangeWeightModifier;
            result.Actions[i].EnemyHiddenWeightMod =
                _FromEncounter.EnemyHidden ? _ActionsList[i].EnemyHiddenWeightModifier : 0;
            result.Actions[i].PlayerHiddenWeightMod =
                _FromEncounter.PlayerHidden ? _ActionsList[i].PlayerHiddenWeightModifier : 0;
            result.Actions[i].StatWeightMods = new();
            _ActionsList[i].GetStatWeightMods(_FromEncounter.EncounterModel, result.Actions[i].StatWeightMods);
            result.Actions[i].CardWeightMods = new();
            _ActionsList[i].GetCardWeightMods(result.Actions[i].CardWeightMods);
            result.Actions[i].ValuesWeightMods =
                new EnemyValuesWeightModReport(_ActionsList[i].ValuesWeightModifiers, _FromEncounter);
            result.Actions[i].WoundsWeightMods =
                new EnemyWoundsWeightModReport(_ActionsList[i].WoundsWeightModifiers, _FromEncounter);
            if (result.Actions[i].FinalWeight <= 0)
            {
                result.Actions[i].RangeUpTo = -1;
            }
            else
            {
                num += result.Actions[i].FinalWeight;
                result.Actions[i].RangeUpTo = num;
            }
        }

        result.TotalWeight = num;
        return result;
    }

    public static string GetDistanceChangeText(EncounterDistanceChange distanceChange)
    {
        return distanceChange switch
        {
            EncounterDistanceChange.DontChangeDistance => $"{LcStr("CstiDetailedCardProgress.Encounter.NoChange", "No Change")}",
            EncounterDistanceChange.AddDistance => $"{LcStr("CstiDetailedCardProgress.Encounter.IncreaseDistance", "Increase Distance")}",
            EncounterDistanceChange.CloseDistance => $"{LcStr("CstiDetailedCardProgress.Encounter.DecreaseDistance", "Decrease Distance")}",
            _ => ""
        };
    }

    public static string FormatPlayerHitResult(InGameEncounter encounter, EncounterPlayerAction action,
        EncounterPopup popup, Vector2 playerActionDamage, int indent = 2)
    {
        bool flag = action.ActionRange == ActionRange.Melee;
        StringBuilder result = new();
        global::Encounter encounterModel = encounter.EncounterModel;
        EnemyBodyLocationSelectionReport resultReport = default;

        // 计算命中权重
        resultReport.Ranged = !flag;
        resultReport.BaseWeights.Head = flag
            ? encounterModel.EnemyBodyTemplate.Head.MeleeHitChanceWeight
            : encounterModel.EnemyBodyTemplate.Head.RangedHitChanceWeight;
        resultReport.BaseWeights.Torso = flag
            ? encounterModel.EnemyBodyTemplate.Torso.MeleeHitChanceWeight
            : encounterModel.EnemyBodyTemplate.Torso.RangedHitChanceWeight;
        resultReport.BaseWeights.LArm = flag
            ? encounterModel.EnemyBodyTemplate.LArm.MeleeHitChanceWeight
            : encounterModel.EnemyBodyTemplate.LArm.RangedHitChanceWeight;
        resultReport.BaseWeights.RArm = flag
            ? encounterModel.EnemyBodyTemplate.RArm.MeleeHitChanceWeight
            : encounterModel.EnemyBodyTemplate.RArm.RangedHitChanceWeight;
        resultReport.BaseWeights.LLeg = flag
            ? encounterModel.EnemyBodyTemplate.LLeg.MeleeHitChanceWeight
            : encounterModel.EnemyBodyTemplate.LLeg.RangedHitChanceWeight;
        resultReport.BaseWeights.RLeg = flag
            ? encounterModel.EnemyBodyTemplate.RLeg.MeleeHitChanceWeight
            : encounterModel.EnemyBodyTemplate.RLeg.RangedHitChanceWeight;
        resultReport.ArmorWeights.Head = encounterModel.EnemyArmor.HeadHitProbabilityModifier;
        resultReport.ArmorWeights.Torso = encounterModel.EnemyArmor.TorsoHitProbabilityModifier;
        resultReport.ArmorWeights.LArm = encounterModel.EnemyArmor.LArmHitProbabilityModifier;
        resultReport.ArmorWeights.RArm = encounterModel.EnemyArmor.RArmHitProbabilityModifier;
        resultReport.ArmorWeights.LLeg = encounterModel.EnemyArmor.LLegHitProbabilityModifier;
        resultReport.ArmorWeights.RLeg = encounterModel.EnemyArmor.RLegHitProbabilityModifier;
        resultReport.TrackingWeights.Head = encounter.CurrentEnemyBodyProbabilities.CurrentHeadProbModifier;
        resultReport.TrackingWeights.Torso = encounter.CurrentEnemyBodyProbabilities.CurrentTorsoProbModifier;
        resultReport.TrackingWeights.LArm = encounter.CurrentEnemyBodyProbabilities.CurrentLArmProbModifier;
        resultReport.TrackingWeights.RArm = encounter.CurrentEnemyBodyProbabilities.CurrentRArmProbModifier;
        resultReport.TrackingWeights.LLeg = encounter.CurrentEnemyBodyProbabilities.CurrentLLegProbModifier;
        resultReport.TrackingWeights.RLeg = encounter.CurrentEnemyBodyProbabilities.CurrentRLegProbModifier;

        // 计算各个部位的基础护甲
        BodyLocations[] bodyParts = new[]
        {
            BodyLocations.Head, BodyLocations.Torso, BodyLocations.LArm, BodyLocations.RArm, BodyLocations.LLeg,
            BodyLocations.RLeg
        };
        float[] bodyPartArmors = new float[bodyParts.Length];
        BodyTemplate body = encounterModel.EnemyBodyTemplate;
        bodyPartArmors[(int)BodyLocations.Head] = body.Head.GetArmor(action.DamageTypes);
        bodyPartArmors[(int)BodyLocations.Torso] = body.Torso.GetArmor(action.DamageTypes);
        bodyPartArmors[(int)BodyLocations.LArm] = body.LArm.GetArmor(action.DamageTypes);
        bodyPartArmors[(int)BodyLocations.RArm] = body.RArm.GetArmor(action.DamageTypes);
        bodyPartArmors[(int)BodyLocations.LLeg] = body.LLeg.GetArmor(action.DamageTypes);
        bodyPartArmors[(int)BodyLocations.RLeg] = body.RLeg.GetArmor(action.DamageTypes);


        // 计算各个部位的实时防御力
        float[] bodyPartArmorDefenses = new float[bodyParts.Length];
        float[] trackingDefenses = new float[bodyParts.Length];
        foreach (BodyLocations part in bodyParts)
        {
            bodyPartArmorDefenses[(int)part] =
                encounterModel.EnemyArmor.CalculateArmorForLocation(action.DamageTypes, part);
            trackingDefenses[(int)part] = encounter.CurrentEnemyBodyProbabilities.GetDefenseModifierForLocation(part);
        }

        // 计算最终的Enemy Defense
        float sizeDefense = encounter.CurrentEnemySize;
        float[] enemyDefenses = new float[bodyParts.Length];
        for (int i = 0; i < bodyParts.Length; i++)
            enemyDefenses[i] = sizeDefense + bodyPartArmors[i] + bodyPartArmorDefenses[i] + trackingDefenses[i];

        List<List<(Vector2, WoundSeverity)>> woundMappings = new();
        // 计算玩家伤害可造成的伤口
        for (int i = 0; i < bodyParts.Length; i++)
        {
            List<WoundSeverityMappings> mappings = popup.WoundSeverityMappings.ToList();
            mappings.Insert(0,
                new WoundSeverityMappings
                {
                    AttackDefenseRatio = new Vector2(0f, mappings[0].AttackDefenseRatio.x),
                    WoundSeverity = WoundSeverity.NoWound
                });
            mappings.Add(new WoundSeverityMappings
            {
                AttackDefenseRatio =
                    new Vector2(mappings[mappings.Count - 1].AttackDefenseRatio.y, float.PositiveInfinity),
                WoundSeverity = WoundSeverity.Serious
            });
            IOrderedEnumerable<(Vector2, WoundSeverity WoundSeverity)> attackRanges =
                mappings.Select(m => (m.AttackDefenseRatio * enemyDefenses[i], m.WoundSeverity)).OrderBy(a =>
                    a.WoundSeverity);
            woundMappings.Add(attackRanges.ToList());
        }


        string spaces = new(' ', indent);
        float deadlyProb = 0f;
        foreach (BodyLocations bodyPart in bodyParts)
            if (resultReport.GetBodyLocationHitWeight(bodyPart) > 0)
            {
                LocalizedString bodyPartName = new()
                { LocalizationKey = $"CstiDetailedCardProgress.BodyParts.{bodyPart}", DefaultText = bodyPart.ToString()};
                List<(Vector2, WoundSeverity)> mapping = woundMappings[(int)bodyPart];
                IEnumerable<(WoundSeverity, float)> woundsProbs = from m in mapping
                                                                  where VectorMath.RangeIntersect(playerActionDamage, m.Item1).RangeLength() > 0
                                                                  select (m.Item2,
                                                                      VectorMath.RangeIntersect(playerActionDamage, m.Item1).RangeLength() /
                                                                      playerActionDamage.RangeLength());
                foreach ((WoundSeverity, float) woundProb in woundsProbs)
                {
                    EnemyWound[] wounds = body.GetBodyLocation(bodyPart)
                        .GetWoundsForSeverityDamageType(woundProb.Item1, action.DamageTypes);
                    foreach (EnemyWound wound in wounds)
                        // Debug.Log($"{wound.CombatLog}: {wound.EnemyValuesModifiers.BloodModifier.RangeMidValue()} / {encounter.CurrentEnemyBlood}");
                        if (-wound.EnemyValuesModifiers.BloodModifier.RangeMidValue() >=
                            encounter.CurrentEnemyBlood - 1e-5)
                            // Debug.Log($"Added prob for {wound.CombatLog} (On {bodyPartName}) {resultReport.GetBodyLocationHitWeight(bodyPart) / resultReport.TotalWeight * 100f:0.##} * {woundProb.Item2:0.##} / {wounds.Length}");
                            deadlyProb += resultReport.GetBodyLocationHitWeight(bodyPart) / resultReport.TotalWeight *
                                woundProb.Item2 / wounds.Length;
                }

                result.AppendLine(
                    $"{spaces}{bodyPartName.ToString()}: {resultReport.GetBodyLocationHitWeight(bodyPart) / resultReport.TotalWeight * 100f:0}% ({LcStr("CstiDetailedCardProgress.Encounter.TotalDefense", "Total Defense")}: {enemyDefenses[(int)bodyPart]:0})");
                result.AppendLine(
                    $"{spaces}| {string.Join(" | ", mapping.Select(m => $"{VectorMath.RangeIntersect(playerActionDamage, m.Item1).RangeLength() / playerActionDamage.RangeLength() * 100f:0}%"))} |");
            }

        result.AppendLine($" {LcStr("CstiDetailedCardProgress.Encounter.LethalityProbabilityOfThisAttack", "Lethality Probability of This Attack")}:{deadlyProb * 100f:0.##}%");
        // 输出结果
        return result.ToString();
    }

    public static string FormatPlayerClashValue(InGameEncounter encounter, EncounterPlayerAction action,
        EncounterPopup popup, int indent = 1)
    {
        bool _WithRandomness = false;
        StringBuilder result = new();
        string spaces = new(' ', indent);
        ClashResultsReport report = new()
        {
            PlayerCannotFail = action.CannotFailClash,
            PlayerActionClashValue = action.GetClash(_WithRandomness),
            PlayerSizeClashValue = popup.PlayerSize,
            PlayerActionReachClashValue = action.Reach,
            PlayerClashStatsAddedValues = action.GetClashStatsAddedValues(_WithRandomness)
        };
        bool ranged = encounter.Distant;
        // Debug.Log(action.GetClash(true));
        result.AppendLine($"{spaces}{LcStr("CstiDetailedCardProgress.Encounter.BaseValue", "Base Value")}: {report.PlayerActionClashValue:0}")
    .AppendLine(
        $"{spaces}{LcStr("CstiDetailedCardProgress.Encounter.SizeBonus", "Size Bonus")}: {(action.ActionRange == ActionRange.Ranged ? 0.0f : report.PlayerSizeClashValue):0}")
    .AppendLine($"{spaces}{LcStr("CstiDetailedCardProgress.Encounter.WeaponLengthBonus", "Weapon Length Bonus")}: {report.PlayerActionReachClashValue}");
        if (report.PlayerClashStatsAddedValues != null && report.PlayerClashStatsAddedValues.Count > 0)
            result.AppendLine(
                $"{spaces}{LcStr("CstiDetailedCardProgress.Encounter.StatusBonus", "Status Bonus")}:\n{string.Join("\n", report.PlayerClashStatsAddedValues.ToArray().Select(v => $"{spaces} {v.Stat.GameName.ToString()}: {ColorFloat(v.Value)}"))}");
        if (encounter.PlayerHidden)
        {
            report.PlayerClashStealthBonus = action.GetClashStealthBonus(_WithRandomness);
            // Debug.Log(action.GetClashStealthBonus(true));
            result.AppendLine($"{spaces}{LcStr("CstiDetailedCardProgress.Encounter.StealthBonus", "Stealth Bonus")}: <color=green>{report.PlayerClashStealthBonus:0}</color>");
        }

        if ((!ranged && action.ActionRange == ActionRange.Ranged) ||
            (ranged && action.ActionRange == ActionRange.Melee))
        {
            report.PlayerClashIneffectiveRangeMalus = action.GetClashIneffectiveRangeMalus(_WithRandomness);
            // Debug.Log(action.GetClashIneffectiveRangeMalus(true));
            result.AppendLine($"{spaces}{LcStr("CstiDetailedCardProgress.Encounter.IneffectiveRangeMalus", "Ineffective Range Malus")}: <color=red>{report.PlayerClashIneffectiveRangeMalus:0}</color>");
        }

        if (action.ActionRange == ActionRange.Ranged)
        {
            result.AppendLine($"{spaces}{LcStr("CstiDetailedCardProgress.Encounter.EnemySizeMalus", "Enemy Size Malus")}: {ColorFloat(-encounter.CurrentEnemySize)}");
            result.AppendLine($"{spaces}{LcStr("CstiDetailedCardProgress.Encounter.InaccuracyMalus", "Inaccuracy Malus")}: {ColorFloat(-action.ClashInaccuracy.y)}");
            result.AppendLine($"{spaces}{LcStr("CstiDetailedCardProgress.Encounter.EnemyCoverMalus", "Enemy Cover Malus")}: {ColorFloat(-encounter.CurrentEnemyCover)}");
        }

        return result.ToString();
    }

    public static string FormatEnemyHitResult(InGameEncounter encounter, EnemyAction action, EncounterPopup popup,
        int indent = 2)
    {
        StringBuilder result = new();
        GameManager gm = GameManager.Instance;
        PlayerBodyLocationSelectionReport playerBodyLocationHit = default;
        EncounterEnemyDamageReport currentRoundEnemyDamageReport = new();

        BodyLocations[] bodyParts = {
            BodyLocations.Head, BodyLocations.Torso, BodyLocations.LArm, BodyLocations.RArm, BodyLocations.LLeg,
            BodyLocations.RLeg
        };
        float[] bodyPartArmors = new float[bodyParts.Length];
        float[] armors = new float[bodyParts.Length];

        if (action.ActionRange == ActionRange.Melee)
        {
            playerBodyLocationHit.Ranged = false;
            playerBodyLocationHit.BaseWeights.Head = popup.Head.MeleeHitChanceWeight;
            playerBodyLocationHit.BaseWeights.Torso = popup.Torso.MeleeHitChanceWeight;
            playerBodyLocationHit.BaseWeights.LArm = popup.LArm.MeleeHitChanceWeight;
            playerBodyLocationHit.BaseWeights.RArm = popup.RArm.MeleeHitChanceWeight;
            playerBodyLocationHit.BaseWeights.LLeg = popup.LLeg.MeleeHitChanceWeight;
            playerBodyLocationHit.BaseWeights.RLeg = popup.RLeg.MeleeHitChanceWeight;
        }
        else
        {
            playerBodyLocationHit.BaseWeights.Head = popup.Head.RangedHitChanceWeight;
            playerBodyLocationHit.BaseWeights.Torso = popup.Torso.RangedHitChanceWeight;
            playerBodyLocationHit.BaseWeights.LArm = popup.LArm.RangedHitChanceWeight;
            playerBodyLocationHit.BaseWeights.RArm = popup.RArm.RangedHitChanceWeight;
            playerBodyLocationHit.BaseWeights.LLeg = popup.LLeg.RangedHitChanceWeight;
            playerBodyLocationHit.BaseWeights.RLeg = popup.RLeg.RangedHitChanceWeight;
            if (popup.CurrentEncounter.Distant && gm && gm.CoverCards != null)
                for (int i = 0; i < gm.CoverCards.Count; i++)
                    if (!gm.CoverCards.get_Item(i).CardModel.AppliesCoverWhenEquipped ||
                        (gm.CoverCards.get_Item(i).CardModel.AppliesCoverWhenEquipped &&
                         GraphicsManager.Instance.CharacterWindow.HasCardEquipped(gm.CoverCards.get_Item(i))))
                    {
                        playerBodyLocationHit.CoverWeights.Head = gm.CoverCards.get_Item(i).CardModel
                            .PlayerCoverHitProbabilityModifiers.HeadHitProbabilityModifier;
                        playerBodyLocationHit.CoverWeights.Torso = gm.CoverCards.get_Item(i).CardModel
                            .PlayerCoverHitProbabilityModifiers.TorsoHitProbabilityModifier;
                        playerBodyLocationHit.CoverWeights.LArm = gm.CoverCards.get_Item(i).CardModel
                            .PlayerCoverHitProbabilityModifiers.LArmHitProbabilityModifier;
                        playerBodyLocationHit.CoverWeights.RArm = gm.CoverCards.get_Item(i).CardModel
                            .PlayerCoverHitProbabilityModifiers.RArmHitProbabilityModifier;
                        playerBodyLocationHit.CoverWeights.LLeg = gm.CoverCards.get_Item(i).CardModel
                            .PlayerCoverHitProbabilityModifiers.LLegHitProbabilityModifier;
                        playerBodyLocationHit.CoverWeights.RLeg = gm.CoverCards.get_Item(i).CardModel
                            .PlayerCoverHitProbabilityModifiers.RLegHitProbabilityModifier;
                    }
        }

        if (gm && gm.ArmorCards != null)
            for (int j = 0; j < gm.ArmorCards.Count; j++)
                if (GraphicsManager.Instance.CharacterWindow.HasCardEquipped(gm.ArmorCards.get_Item(j)))
                {
                    playerBodyLocationHit.ArmorWeights.Head +=
                        gm.ArmorCards.get_Item(j).CardModel.ArmorValues.HeadHitProbabilityModifier;
                    playerBodyLocationHit.ArmorWeights.Torso +=
                        gm.ArmorCards.get_Item(j).CardModel.ArmorValues.TorsoHitProbabilityModifier;
                    playerBodyLocationHit.ArmorWeights.LArm +=
                        gm.ArmorCards.get_Item(j).CardModel.ArmorValues.LArmHitProbabilityModifier;
                    playerBodyLocationHit.ArmorWeights.RArm +=
                        gm.ArmorCards.get_Item(j).CardModel.ArmorValues.RArmHitProbabilityModifier;
                    playerBodyLocationHit.ArmorWeights.LLeg +=
                        gm.ArmorCards.get_Item(j).CardModel.ArmorValues.LLegHitProbabilityModifier;
                    playerBodyLocationHit.ArmorWeights.RLeg +=
                        gm.ArmorCards.get_Item(j).CardModel.ArmorValues.RLegHitProbabilityModifier;
                    for (int k = 0; k < bodyParts.Length; k++)
                        armors[k] += gm.ArmorCards.get_Item(j).CardModel.ArmorValues
                            .CalculateArmorForLocation(action.DamageTypes, bodyParts[k]);
                }

        playerBodyLocationHit.EnemyActionWeights.Head +=
            action.AddedPlayerLocationHitProbabilities.HeadHitProbabilityModifier;
        playerBodyLocationHit.EnemyActionWeights.Torso +=
            action.AddedPlayerLocationHitProbabilities.TorsoHitProbabilityModifier;
        playerBodyLocationHit.EnemyActionWeights.LArm +=
            action.AddedPlayerLocationHitProbabilities.LArmHitProbabilityModifier;
        playerBodyLocationHit.EnemyActionWeights.RArm +=
            action.AddedPlayerLocationHitProbabilities.RArmHitProbabilityModifier;
        playerBodyLocationHit.EnemyActionWeights.LLeg +=
            action.AddedPlayerLocationHitProbabilities.LLegHitProbabilityModifier;
        playerBodyLocationHit.EnemyActionWeights.RLeg +=
            action.AddedPlayerLocationHitProbabilities.RLegHitProbabilityModifier;

        // 计算玩家护甲
        // Debug.Log(string.Join(",", action.DamageTypes.Select(t => t.Name)));
        foreach (BodyLocations bodyPart in bodyParts)
            bodyPartArmors[(int)bodyPart] = popup.GetBodyLocation(bodyPart).GetArmor(action.DamageTypes);

        // 计算玩家体型防御和状态防御加成
        float sizeDefense = popup.PlayerSize;
        Vector2 statsDefense = Vector2.zero;
        if (popup.PlayerExtraDefenseCalculation != null)
            for (int k = 0; k < popup.PlayerExtraDefenseCalculation.Length; k++)
                statsDefense += popup.PlayerExtraDefenseCalculation[k].GenerateRandomRange();
        currentRoundEnemyDamageReport.SizeDefense = sizeDefense;
        currentRoundEnemyDamageReport.StatsDefense = Mathf.Lerp(statsDefense.x, statsDefense.y, 0.5f);
        // 计算敌人行动伤害
        currentRoundEnemyDamageReport.SizeDamage =
            action.ActionRange == ActionRange.Melee ? encounter.CurrentEnemySize : 0f;
        currentRoundEnemyDamageReport.ActionDamage = Mathf.Lerp(action.Damage.x, action.Damage.y, 0.5f);
        currentRoundEnemyDamageReport.ValuesDamage = action.AddedDamageFromEnemyValues(encounter, false);
        currentRoundEnemyDamageReport.WoundsDamage = action.AddedDamageValueFromWounds(encounter, false);
        currentRoundEnemyDamageReport.StatsAddedDamage = action.AddedDamageFromStats(false);

        // 计算敌人可造成的伤口
        List<List<Tuple<Vector2, WoundSeverity>>> woundMappings = new();
        for (int i = 0; i < bodyParts.Length; i++)
        {
            List<WoundSeverityMappings> mappings = popup.WoundSeverityMappings.ToList();
            mappings.Insert(0,
                new WoundSeverityMappings
                {
                    AttackDefenseRatio = new Vector2(0f, mappings[0].AttackDefenseRatio.x),
                    WoundSeverity = WoundSeverity.NoWound
                });
            mappings.Add(new WoundSeverityMappings
            {
                AttackDefenseRatio =
                    new Vector2(mappings[mappings.Count - 1].AttackDefenseRatio.y, float.PositiveInfinity),
                WoundSeverity = WoundSeverity.Serious
            });
            // Debug.Log(string.Join("\n", mappings.Select(m => $"{m.WoundSeverity}: {m.AttackDefenseRatio}")));
            IOrderedEnumerable<Tuple<Vector2, WoundSeverity>> attackRanges =
                (from m in mappings
                 select new Tuple<Vector2, WoundSeverity>(m.AttackDefenseRatio * armors[i], m.WoundSeverity))
                .OrderBy(a => a.Item2);
            woundMappings.Add(attackRanges.ToList());
        }

        foreach (BodyLocations bodyPart in bodyParts)
        {
            // Debug.Log(string.Join(",", armors));
            currentRoundEnemyDamageReport.ArmorDefense = armors[(int)bodyPart];
            WoundSeverity woundSeverity = popup.GenerateWoundSeverity(currentRoundEnemyDamageReport.EnemyDamage,
                currentRoundEnemyDamageReport.PlayerDefense);
            currentRoundEnemyDamageReport.AttackSeverity = woundSeverity;
            List<PlayerWound> wounds = new();
#if MELON_LOADER
            action.PlayerWounds.GetWoundsForSeverity_il2cpp(woundSeverity, wounds);
            if (wounds.Count == 0)
                encounter.EncounterModel.DefaultPlayerWounds.GetWoundsForSeverity_il2cpp(woundSeverity, wounds);
#else
            action.PlayerWounds.GetWoundsForSeverity(woundSeverity, ref wounds);
            if (wounds.Count == 0)
                encounter.EncounterModel.DefaultPlayerWounds.GetWoundsForSeverity(woundSeverity, ref wounds);
#endif
            if (wounds[0].DroppedCards.Length == 0) return "";
            if (playerBodyLocationHit.GetBodyLocationHitWeight(bodyPart) > 0)
                result.AppendLine(
                    $"{new string(' ', indent)}{new LocalizedString { LocalizationKey = $"CstiDetailedCardProgress.BodyParts.{bodyPart}", DefaultText = bodyPart.ToString()}.ToString()}({playerBodyLocationHit.GetBodyLocationHitWeight(bodyPart) / playerBodyLocationHit.TotalWeight * 100f:0.#}%): {wounds.Select(w => w.DroppedCards[0].CardName.ToString()).Join()} ({LcStr("CstiDetailedCardProgress.Encounter.AttackDefenseRatio", "Attack-Defense Ratio")}: {currentRoundEnemyDamageReport.EnemyDamage}:{currentRoundEnemyDamageReport.PlayerDefense})");
        }

        return result.ToString();
    }

    public static BodyLocation GetBodyLocation(this EncounterPopup popup, BodyLocations bodyLocation)
    {
        return bodyLocation switch
        {
            BodyLocations.Head => popup.Head,
            BodyLocations.Torso => popup.Torso,
            BodyLocations.LArm => popup.LArm,
            BodyLocations.RArm => popup.RArm,
            BodyLocations.LLeg => popup.LLeg,
            BodyLocations.RLeg => popup.RLeg,
            _ => null
        };
    }

    public static float GetBodyLocationHitWeight(this PlayerBodyLocationSelectionReport report,
        BodyLocations bodyLocation)
    {
        return bodyLocation switch
        {
            BodyLocations.Head => report.HeadHitWeight,
            BodyLocations.Torso => report.TorsoHitWeight,
            BodyLocations.LArm => report.LArmHitWeight,
            BodyLocations.RArm => report.RArmHitWeight,
            BodyLocations.LLeg => report.LLegHitWeight,
            BodyLocations.RLeg => report.RLegHitWeight,
            _ => 0f
        };
    }

    public static float GetBodyLocationHitWeight(this EnemyBodyLocationSelectionReport report,
        BodyLocations bodyLocation)
    {
        return bodyLocation switch
        {
            BodyLocations.Head => report.HeadHitWeight,
            BodyLocations.Torso => report.TorsoHitWeight,
            BodyLocations.LArm => report.LArmHitWeight,
            BodyLocations.RArm => report.RArmHitWeight,
            BodyLocations.LLeg => report.LLegHitWeight,
            BodyLocations.RLeg => report.RLegHitWeight,
            _ => 0f
        };
    }

    public static string FormatCardOnCardAction(CardOnCardAction action, InGameCardBase recivingCard,
        InGameCardBase givenCard, int indent = 0)
    {
        List<string> texts = new();
        string cardActionText = FormatCardAction(action, recivingCard, indent);
        if (!string.IsNullOrWhiteSpace(cardActionText)) texts.Add(cardActionText);
        CardStateChange stateChange = action.GivenCardChanges;
        string cardModText = FormatStateChange(stateChange, givenCard, indent);
        if (!string.IsNullOrWhiteSpace(cardModText))
        {
            texts.Add(FormatBasicEntry(
                new LocalizedString
                {
                    LocalizationKey = "CstiDetailedCardProgress.GivenCardStateChange",
                    DefaultText = "Given Card State Change"
                }, ""));
            texts.Add(cardModText);
        }

        LiquidDrop currentLiquidDrop = action.CreatedLiquidInGivenCard;
        if (currentLiquidDrop.LiquidCard)
        {
            string liquidDropText =
                $"{FormatMinMaxValue(currentLiquidDrop.Quantity)} ({currentLiquidDrop.LiquidCard.CardType}){currentLiquidDrop.LiquidCard.CardName.ToString()}";
            texts.Add(FormatBasicEntry(
                $"<size=55%>{new LocalizedString { LocalizationKey = "CstiDetailedCardProgress.Action.LiquidDrops", DefaultText = "Liquid Drops" }.ToString()}</size>",
                "<size=55%>" + liquidDropText + "</size>", indent: indent));
        }

        return texts.Join(delimiter: "\n");
    }

    public static string FormatCardAction(CardAction action, InGameCardBase fromCard, int indent = 0)
    {
        List<string> texts = new();
        List<string> stateModTexts = new();

        if (action.UnmodifiedDaytimeCost != action.TotalDaytimeCost)
        {
            string timeModText = FormatTimeCostModifiers(action, fromCard, indent);
            texts.Add(FormatBasicEntry(new LocalizedString()
            {
                LocalizationKey = "CstiDetailedCardProgress.TimeCostModifiers",
                DefaultText = "Time Cost Modifiers"
            }, "", indent: indent));
            texts.Add(timeModText);
        }

        if (action.StatModifications != null)
        {
            foreach (StatModifier statModifier in action.AllStatModifiers)
                stateModTexts.Add(FormatStatModifier(statModifier, indent + 2));
            if (stateModTexts.Count > 0)
            {
                texts.Add(FormatBasicEntry(
                    new LocalizedString
                    { LocalizationKey = "CstiDetailedCardProgress.StatModifier", DefaultText = "Stat Modifier" }
                        .ToString(),
                    "", indent: indent));
                texts.Add(stateModTexts.Join(delimiter: "\n"));
            }
        }

        CardStateChange stateChange = action.ReceivingCardChanges;
        string cardModText = FormatStateChange(stateChange, fromCard, indent);
        if (!string.IsNullOrWhiteSpace(cardModText))
        {
            texts.Add(FormatBasicEntry(
                new LocalizedString
                {
                    LocalizationKey = "CstiDetailedCardProgress.CardStateChange",
                    DefaultText = "Card State Change"
                }
                    .ToString(),
                "", indent: indent));
            texts.Add(cardModText);
        }

        return texts.Join(delimiter: "\n");
    }
    private static string FormatTimeCostModifiers(CardAction action, InGameCardBase fromCard, int indent)
    {
        List<string> texts = new();
        if (action != null)
        {
            var gm = MBSingleton<GameManager>.Instance;
            if (gm.CurrentActionModifiers != null && gm.CurrentActionModifiers.Count > 0)
            {
                for (int i = 0; i < gm.CurrentActionModifiers.Count; i++)
                {
                    var cur = gm.CurrentActionModifiers.get_Item(i);
                    if (cur.AppliesToAction(action, gm.NotInBase, fromCard) && cur.DurationModifier != 0)
                    {
                        texts.Add(FormatBasicEntry($"{ColorFloat(cur.DurationModifier)}", $"{cur.Source}", indent: indent + 2));
                    }
                }
            }
        }
        return texts.Join(delimiter: "\n");
    }
    private static string FormatStateChange(CardStateChange stateChange, InGameCardBase fromCard, int indent = 0)
    {
        List<string> cardModTexts = new();
        if (stateChange.ModType == CardModifications.DurabilityChanges)
        {
            if (stateChange.SpoilageChange.magnitude != 0)
                cardModTexts.Add(FormatBasicEntry(FormatMinMaxValue(stateChange.SpoilageChange),
                    string.IsNullOrEmpty(fromCard.CardModel.SpoilageTime.CardStatName)
                        ? new LocalizedString
                        { LocalizationKey = "CstiDetailedCardProgress.Spoilage", DefaultText = "Spoilage" }
                            .ToString()
                        : fromCard.CardModel.SpoilageTime.CardStatName, indent: indent + 2));
            if (stateChange.UsageChange.magnitude != 0)
                cardModTexts.Add(FormatBasicEntry(FormatMinMaxValue(stateChange.UsageChange),
                    string.IsNullOrEmpty(fromCard.CardModel.UsageDurability.CardStatName)
                        ? new LocalizedString
                        { LocalizationKey = "CstiDetailedCardProgress.Usage", DefaultText = "Usage" }.ToString()
                        : fromCard.CardModel.UsageDurability.CardStatName, indent: indent + 2));
            if (stateChange.FuelChange.magnitude != 0)
                cardModTexts.Add(FormatBasicEntry(FormatMinMaxValue(stateChange.FuelChange),
                    string.IsNullOrEmpty(fromCard.CardModel.FuelCapacity.CardStatName)
                        ? new LocalizedString
                        { LocalizationKey = "CstiDetailedCardProgress.Fuel", DefaultText = "Fuel" }.ToString()
                        : fromCard.CardModel.FuelCapacity.CardStatName, indent: indent + 2));
            if (stateChange.ChargesChange.magnitude != 0)
                cardModTexts.Add(FormatBasicEntry(FormatMinMaxValue(stateChange.ChargesChange),
                    string.IsNullOrEmpty(fromCard.CardModel.Progress.CardStatName)
                        ? new LocalizedString
                        { LocalizationKey = "CstiDetailedCardProgress.Progress", DefaultText = "Progress" }
                            .ToString()
                        : fromCard.CardModel.Progress.CardStatName, indent: indent + 2));
            if (stateChange.LiquidQuantityChange.magnitude != 0)
                cardModTexts.Add(FormatBasicEntry(FormatMinMaxValue(stateChange.LiquidQuantityChange),
                    new LocalizedString
                    {
                        LocalizationKey = "CstiDetailedCardProgress.LiquidQuantityChange",
                        DefaultText = "Liquid Quantity"
                    }.ToString(), indent: indent + 2));
            if (stateChange.Special1Change.magnitude != 0)
                cardModTexts.Add(FormatBasicEntry(FormatMinMaxValue(stateChange.Special1Change),
                    string.IsNullOrEmpty(fromCard.CardModel.SpecialDurability1.CardStatName)
                        ? "SpecialDurability1"
                        : fromCard.CardModel.SpecialDurability1.CardStatName, indent: indent + 2));
            if (stateChange.Special2Change.magnitude != 0)
                cardModTexts.Add(FormatBasicEntry(FormatMinMaxValue(stateChange.Special2Change),
                    string.IsNullOrEmpty(fromCard.CardModel.SpecialDurability2.CardStatName)
                        ? "SpecialDurability2"
                        : fromCard.CardModel.SpecialDurability2.CardStatName, indent: indent + 2));
            if (stateChange.Special3Change.magnitude != 0)
                cardModTexts.Add(FormatBasicEntry(FormatMinMaxValue(stateChange.Special3Change),
                    string.IsNullOrEmpty(fromCard.CardModel.SpecialDurability3.CardStatName)
                        ? "SpecialDurability3"
                        : fromCard.CardModel.SpecialDurability3.CardStatName, indent: indent + 2));
            if (stateChange.Special4Change.magnitude != 0)
                cardModTexts.Add(FormatBasicEntry(FormatMinMaxValue(stateChange.Special4Change),
                    string.IsNullOrEmpty(fromCard.CardModel.SpecialDurability4.CardStatName)
                        ? "SpecialDurability4"
                        : fromCard.CardModel.SpecialDurability4.CardStatName, indent: indent + 2));
        }
        else if (stateChange.ModType == CardModifications.Transform && stateChange.TransformInto)
        {
            cardModTexts.Add(FormatBasicEntry(
                new LocalizedString
                { LocalizationKey = "CstiDetailedCardProgress.TransformInto", DefaultText = "Transform into" }
                    .ToString(),
                $"{stateChange.TransformInto.CardName.ToString()}", indent: indent + 2));
        }
        else if (stateChange.ModType == CardModifications.Destroy)
        {
            cardModTexts.Add(FormatBasicEntry(
                new LocalizedString { LocalizationKey = "CstiDetailedCardProgress.Destroy", DefaultText = "Destroy" }
                    .ToString(),
                fromCard.CardModel.CardName.ToString(), "red", indent + 2));
        }

        if (fromCard && fromCard.ContainedLiquid && stateChange.ModifyLiquid)
        {
            cardModTexts.Add(FormatBasicEntry(
                new LocalizedString
                { LocalizationKey = "CstiDetailedCardProgress.ModifyLiquid", DefaultText = "Modify Liquid" }
                    .ToString(), "", indent: indent + 2));
            cardModTexts.Add(FormatBasicEntry(
                FormatMinMaxValue(stateChange.LiquidQuantityChange), fromCard.ContainedLiquidModel.CardName, indent: indent + 4));
        }

        return cardModTexts.Join(delimiter: "\n");
    }
    public static string FormatActionDurationModifiers(ActionModifier modifier, int indent = 0)
    {
        List<string> texts = new();
        if (modifier == null || modifier.AppliesTo == null || modifier.DurationModifier == 0) return "";
        foreach (var tag in modifier.AppliesTo)
        {
            if (tag) texts.Add(FormatBasicEntry(tag.name, ColorFloat(modifier.DurationModifier), indent: indent));
        }
        return texts.Join(delimiter: "\n");
    }
    public static string FormatStatModifier(StatModifier statModifier, int indent = 0)
    {
        List<string> texts = new();
        if (statModifier.Stat != null)
        {
            if (statModifier.ValueModifier.magnitude != 0)
                texts.Add(FormatBasicEntry($"{FormatMinMaxValue(statModifier.ValueModifier)}",
                    $"{statModifier.Stat.GameName.ToString()}", indent: indent));
            if (statModifier.RateModifier.magnitude != 0)
                texts.Add(FormatBasicEntry($"{FormatMinMaxValue(statModifier.RateModifier)}",
                    $"{statModifier.Stat.GameName.ToString()} {new LocalizedString { LocalizationKey = "CstiDetailedCardProgress.Rate", DefaultText = "Rate" }.ToString()}",
                    indent: indent));
        }

        return texts.Join(delimiter: "\n");
    }

    public static string FormatMinMaxValue(Vector2 minMax)
    {
        if (Mathf.Approximately(minMax.x, minMax.y)) return $"{ColorFloat(minMax.x)}";
        return $"[{ColorFloat(minMax.x)}, {ColorFloat(minMax.y)}]";
    }

    public static string ColorTagFromFloat(float num)
    {
        return num switch
        {
            > 0f => "<color=\"green\">",
            < 0f => "<color=\"red\">",
            _ => "<color=\"yellow\">"
        };
    }

    public static string ColorFloat(float num, bool asPercent = false, bool reverseColor = false)
    {
        return asPercent
            ? $"{ColorTagFromFloat(reverseColor ? -num : num)}{num,-3:+0.##%;-0.##%;+0}</color>"
            : $"{ColorTagFromFloat(reverseColor ? -num : num)}{num,-3:+0.##;-0.##;+0}</color>";
    }

    public static string FormatWeight(float weight)
    {
        return
            $"<color=\"yellow\">{weight:0.#}</color> {new LocalizedString { LocalizationKey = "CstiDetailedCardProgress.FormatWeight.Weight", DefaultText = "Weight" }.ToString()}";
    }

    public static string FormatProgressAndRate(float current, float max, string name, float rate,
        InGameCardBase currentCard = null, DurabilityStat stat = null, int indent = 0)
    {
        return
            $"{FormatProgress(current, max, name, indent)}\n{FormatRate(rate, current, max, currentCard: currentCard, stat: stat)}";
    }

    public static string FormatProgress(float current, float max, string name, int indent = 0)
    {
        return $"{new string(' ', indent)}<color=\"yellow\">{current:0.##}/{max:0.##}</color> {name}";
    }

    public static string FormatWeaponStats(Vector2 clash, Vector2 damage, float reach, int indent = 0)
    {
        LocalizedString title = new()
            { LocalizationKey = "CstiDetailedCardProgress.WeaponStats", DefaultText = "Weapon Stats" };
        LocalizedString clashTitle = new()
            { LocalizationKey = "CstiDetailedCardProgress.WeaponStats.Clash", DefaultText = "Clash" };
        LocalizedString damageTitle = new()
            { LocalizationKey = "CstiDetailedCardProgress.WeaponStats.Damage", DefaultText = "Damage" };
        LocalizedString reachTitle = new()
            { LocalizationKey = "CstiDetailedCardProgress.WeaponStats.Reach", DefaultText = "Reach" };
        return $"{FormatBasicEntry(title, "", indent: indent)}\n" +
               $"  <size=75%>{FormatBasicEntry(FormatMinMaxValue(clash),clashTitle, indent: indent + 2)}\n" +
               $"  {FormatBasicEntry(FormatMinMaxValue(damage),damageTitle, indent: indent + 2)}\n" +
               $"  {FormatBasicEntry(ColorFloat(reach), reachTitle, indent: indent + 2)}</size>";
    }

    public static string TimeSpanFormat(TimeSpan ts)
    {
        return ts.Days >= 1
            ? $"{ts.Days:0}{new LocalizedString { LocalizationKey = "CstiDetailedCardProgress.d", DefaultText = "d" }.ToString()}{ts.Hours:0}{new LocalizedString { LocalizationKey = "CstiDetailedCardProgress.h", DefaultText = "h" }.ToString()}"
            : $"{ts.Hours:0}{new LocalizedString { LocalizationKey = "CstiDetailedCardProgress.h", DefaultText = "h" }.ToString()}";
    }

    public static string FormatRate(float value, float current, float max, float min = 0,
        InGameCardBase currentCard = null, DurabilityStat stat = null)
    {
        string est = "";
        string statOnFullZeroText = "";
        string dropList = "";
        string statOnFullZeroTitle = "";
        if (value > 0 && current < max)
        {
            float time = Math.Abs((max - current) / value);
            TimeSpan timeSpan = new(0, (int)(Math.Ceiling(time) * 15), 0);
            est =
                $" ({new LocalizedString { LocalizationKey = "CstiDetailedCardProgress.est.", DefaultText = "est." }.ToString()} {Math.Ceiling(time)}t/{TimeSpanFormat(timeSpan)})";
            if (stat != null && currentCard != null && stat.HasActionOnFull && stat.OnFull != null)
            {
                statOnFullZeroTitle = FormatBasicEntry(new LocalizedString
                { LocalizationKey = "CstiDetailedCardProgress.statOnFullTitle", DefaultText = "On Full" }
                    .ToString(), "", indent: 4);
                CollectionDropReport collectionDropsReport =
                    GameManager.Instance.GetCollectionDropsReport(stat.OnFull, currentCard, false);
                dropList = Action.FormatCardDropList(
                    collectionDropsReport, currentCard,
                    action: stat.OnFull, indent: 6);
                statOnFullZeroText = FormatCardAction(stat.OnFull, currentCard, 6);
            }
        }
        else if (value < 0 && current > min)
        {
            float time = Math.Abs((current - min) / value);
            TimeSpan timeSpan = new(0, (int)(Math.Ceiling(time) * 15), 0);
            est =
                $" ({new LocalizedString { LocalizationKey = "CstiDetailedCardProgress.est.", DefaultText = "est." }.ToString()} {Math.Ceiling(time)}t/{TimeSpanFormat(timeSpan)})";
            if (stat != null && currentCard != null && stat.HasActionOnZero && stat.OnZero != null)
            {
                statOnFullZeroTitle = FormatBasicEntry(new LocalizedString
                { LocalizationKey = "CstiDetailedCardProgress.statOnZeroTitle", DefaultText = "On Zero" }
                    .ToString(), "", indent: 4);
                bool uniqueOnBoard = currentCard.CardModel.UniqueOnBoard;
                if (currentCard.CardModel.CardType == CardTypes.Weather) currentCard.CardModel.UniqueOnBoard = false;
                CollectionDropReport collectionDropsReport =
                    GameManager.Instance.GetCollectionDropsReport(stat.OnZero, currentCard, false);
                currentCard.CardModel.UniqueOnBoard = uniqueOnBoard;
                dropList = Action.FormatCardDropList(
                    collectionDropsReport, currentCard,
                    action: stat.OnZero, indent: 6);
                statOnFullZeroText = FormatCardAction(stat.OnZero, currentCard, 6);
            }
        }

        List<string> texts = new()
        {
            FormatTooltipEntry(value,
                $"{new LocalizedString { LocalizationKey = "CstiDetailedCardProgress.Rate", DefaultText = "Rate" }.ToString()}<size=70%>{est}</size>",
                2)
        };
        if (!string.IsNullOrWhiteSpace(statOnFullZeroTitle)) texts.Add(statOnFullZeroTitle);
        if (!string.IsNullOrWhiteSpace(dropList)) texts.Add(dropList);
        if (!string.IsNullOrWhiteSpace(statOnFullZeroText)) texts.Add(statOnFullZeroText);
        return texts.Join(delimiter: "\n");
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
        return !value ? null : FormatTooltipEntry(value.FloatValue, name, indent);
    }

    public static string FormatBasicEntry(string s1, string s2, string s1Color = "yellow", int indent = 0)
    {
        return $"<indent={indent / 2.2:0.##}em><color=\"{s1Color}\">{s1}</color> {s2}</indent>";
    }
}