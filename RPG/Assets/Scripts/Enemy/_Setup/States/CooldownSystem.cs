using System.Collections.Generic;
using UnityEngine;
public class CooldownSystem
{
    public readonly List<AbilityEntry> attackAbilities = new();
    public readonly List<AbilityEntry> evadeAbilities = new();
    public readonly List<AbilityEntry> jumpAbilities = new();
    public readonly List<AbilityEntry> teleportAbilities = new();

    #region Ability
    public void BuildAbilityLists(IDictionary<string, AbilityEntry> abilityMap)
    {
        attackAbilities.Clear();
        evadeAbilities.Clear();
        jumpAbilities.Clear();
        teleportAbilities.Clear();

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
            }
        }
    }

    #region Check for ready
    public bool IsAbilityReady(BattleAction action, string abilityName)
    {
        switch (action)
        {
            case BattleAction.Attack:
                return IsAbilityReadyInList(attackAbilities, abilityName);

            case BattleAction.Evade:
                return IsAbilityReadyInList(evadeAbilities, abilityName);

            case BattleAction.Jump:
                return IsAbilityReadyInList(jumpAbilities, abilityName);

            case BattleAction.Teleport:
                return IsAbilityReadyInList(teleportAbilities, abilityName);

            default:
                return false;
        }
    }

    private bool IsAbilityReadyInList(List<AbilityEntry> abilities, string abilityName)
    {
        AbilityEntry entry = GetAbilityEntry(abilities, abilityName);
        if (entry == null)
        {
            Debug.LogWarning($"CooldownSystem could not find ability '{abilityName}' in cooldown list.");
            return false;
        }

        return IsCooldownReady(entry);
    }
    
    private bool IsCooldownReady(AbilityEntry entry)
    {
        return Time.time >= entry.lastTimeUsed + entry.cooldown;
    }
    #endregion

    #region Set cooldown
    public void SetAbilityCooldown(BattleAction action, string abilityName)
    {
        switch (action)
        {
            case BattleAction.Attack:
                SetAbilityCooldownInList(attackAbilities, abilityName);
                break;

            case BattleAction.Evade:
                SetAbilityCooldownInList(evadeAbilities, abilityName);
                break;

            case BattleAction.Jump:
                SetAbilityCooldownInList(jumpAbilities, abilityName);
                break;

            case BattleAction.Teleport:
                SetAbilityCooldownInList(teleportAbilities, abilityName);
                break;
        }
    }

    private void SetAbilityCooldownInList(List<AbilityEntry> abilities, string abilityName)
    {
        AbilityEntry entry = GetAbilityEntry(abilities, abilityName);
        if (entry == null)
        {
            Debug.LogWarning($"CooldownSystem could not find ability '{abilityName}' in cooldown list.");
        }

        SetCooldown(entry);
    }

    private void SetCooldown(AbilityEntry entry)
    {
        float min = Mathf.Min(entry.minCooldown, entry.maxCooldown);
        float max = Mathf.Max(entry.minCooldown, entry.maxCooldown);

        entry.cooldown = Random.Range(min, max);
        entry.lastTimeUsed = Time.time;
    }
    #endregion

    #region Helper
    private AbilityEntry GetAbilityEntry(List<AbilityEntry> abilities, string abilityName)
    {
        if (abilities == null || abilities.Count == 0 || string.IsNullOrWhiteSpace(abilityName))
            return null;

        for (int i = 0; i < abilities.Count; i++)
        {
            AbilityEntry entry = abilities[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.name))
                continue;

            if (entry.name == abilityName)
                return entry;
        }

        return null;
    }
    #endregion

    #endregion
}