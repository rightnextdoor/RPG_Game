using System.Collections.Generic;
using UnityEngine;

public class AbilityHub
{
    private readonly List<AbilityEntry> buildList = new();
    private readonly List<AbilityEntry> attackAbilities = new();
    private readonly List<AbilityEntry> evadeAbilities = new();
    private readonly List<AbilityEntry> jumpAbilities = new();
    private readonly List<AbilityEntry> teleportAbilities = new();
    private readonly List<AbilityEntry> runIntoPlayerAbilities = new();

    private CooldownSystem cooldownSystem;
    
    private float exactPreferenceWeight = 1.5f;
    private float mixedPreferenceWeight = 1.1f;
    private float oppositePreferenceWeight = 0.6f;

    #region Setup
    public void Setup(CooldownSystem _cooldownSystem)
    {
        cooldownSystem = _cooldownSystem;
    }

    public void BuildAbilityLists(IDictionary<string, AbilityEntry> abilityMap)
    {
        attackAbilities.Clear();
        evadeAbilities.Clear();
        jumpAbilities.Clear();
        teleportAbilities.Clear();
        runIntoPlayerAbilities.Clear();

        if (abilityMap == null || abilityMap.Count == 0)
            return;

        foreach (var kvp in abilityMap)
        {
            AbilityEntry entry = kvp.Value;
            if (entry == null)
                continue;

            switch (entry.action)
            {
                case BattleAction.Attack:
                    attackAbilities.Add(entry);
                    break;

                case BattleAction.Evade:
                    evadeAbilities.Add(entry);
                    break;

                case BattleAction.Jump:
                    jumpAbilities.Add(entry);
                    break;

                case BattleAction.Teleport:
                    teleportAbilities.Add(entry);
                    break;

                case BattleAction.RunIntoPlayer:
                    runIntoPlayerAbilities.Add(entry);
                    break;
            }
        }
    }

    #endregion

    public AbilityEntry GetAbility(BattleAction action, AbilityPreference preference)
    {
        switch (action)
        {
            case BattleAction.Attack:
                return GetAbilityFromList(attackAbilities, preference);

            case BattleAction.Evade:
                return GetAbilityFromList(evadeAbilities, preference);

            case BattleAction.Jump:
                return GetAbilityFromList(jumpAbilities, preference);

            case BattleAction.Teleport:
                return GetAbilityFromList(teleportAbilities, preference);

            case BattleAction.RunIntoPlayer:
                return GetAbilityFromList(runIntoPlayerAbilities, preference);

            default:
                return null;
        }
    }

    private AbilityEntry GetAbilityFromList(List<AbilityEntry> sourceList, AbilityPreference preference)
    {
        FilterAbilities(sourceList);

        if (buildList.Count == 0)
            return null;

        return ChooseAbility(sourceList, preference);
    }

    private void FilterAbilities(List<AbilityEntry> sourceList)
    {
        buildList.Clear();

        if (sourceList == null || sourceList.Count == 0)
            return;

        foreach (var entry in sourceList)
        {
            if (entry == null)
                continue;

            if (!entry.unlocked)
                continue;

            if (!cooldownSystem.IsAbilityReady(entry.action, entry.name))
                continue;

            buildList.Add(entry);
        }
    }

    #region Choose ability
    private AbilityEntry ChooseAbility(List<AbilityEntry> sourceList, AbilityPreference preference)
    {
        float totalWeight = 0f;

        foreach (var entry in buildList)
            totalWeight += GetWeight(entry, preference);

        if (totalWeight <= 0f)
            return null;

        float roll = Random.Range(0f, totalWeight);
        float currentWeight = 0f;

        foreach (var entry in buildList)
        {
            currentWeight += GetWeight(entry, preference);

            if (roll <= currentWeight)
            {
                UpdateRepeatStreak(sourceList, entry);
                return entry;
            }
        }

        AbilityEntry chosenEntry = buildList[buildList.Count - 1];
        UpdateRepeatStreak(sourceList, chosenEntry);
        return chosenEntry;
    }

    private float GetWeight(AbilityEntry entry, AbilityPreference preference)
    {
        if (entry == null)
            return 0f;

        float chanceWeight = entry.chance <= 0f ? 1f : entry.chance;
        float preferenceWeight = GetPreferenceWeight(preference, entry.preference);
        float repeatWeight = GetRepeatWeight(entry.repeatStreak);

        return chanceWeight * preferenceWeight * repeatWeight;
    }

    private float GetPreferenceWeight(AbilityPreference requestedPreference, AbilityPreference abilityPreference)
    {
        if (requestedPreference == abilityPreference)
            return exactPreferenceWeight;

        if (requestedPreference == AbilityPreference.Mixed || abilityPreference == AbilityPreference.Mixed)
            return mixedPreferenceWeight;

        return oppositePreferenceWeight;
    }

    private float GetRepeatWeight(int repeatStreak)
    {
        switch (repeatStreak)
        {
            case 0:
                return 1f;
            case 1:
                return 0.75f;
            case 2:
                return 0.5f;
            default:
                return 0.35f;
        }
    }

    private void UpdateRepeatStreak(List<AbilityEntry> sourceList, AbilityEntry chosenEntry)
    {
        if (sourceList == null || sourceList.Count == 0 || chosenEntry == null)
            return;

        foreach (var entry in sourceList)
        {
            if (entry == null)
                continue;

            if (entry == chosenEntry)
                entry.repeatStreak++;
            else
                entry.repeatStreak = 0;
        }
    }
    #endregion
}