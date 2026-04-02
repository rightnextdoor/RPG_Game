using System.Collections.Generic;
using UnityEngine;
public class CooldownSystem
{
    #region Ability settings
    private readonly List<AbilityEntry> attackAbilities = new();
    private readonly List<AbilityEntry> evadeAbilities = new();
    private readonly List<AbilityEntry> jumpAbilities = new();
    private readonly List<AbilityEntry> teleportAbilities = new();
    #endregion

    #region Battle settings
    private float battleMinCooldown;
    private float battleMaxCooldown;

    private float battleTempMinCooldown;
    private float battleTempMaxCooldown;
    private float battleTempEndTime;

    private float battleCooldown;
    private float battleLastTimeUsed;

    private bool battleCooldownPaused;
    #endregion

    public void Setup(IDictionary<string, AbilityEntry> abilityMap, float minCooldown, float maxCooldown)
    {
        BuildAbilityLists(abilityMap);
        SetupBattle(minCooldown, maxCooldown);
    }

    #region Ability
    private void BuildAbilityLists(IDictionary<string, AbilityEntry> abilityMap)
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
            return;
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

    public void ApplyStartCooldowns()
    {
        ApplyStartCooldownsInList(attackAbilities);
        ApplyStartCooldownsInList(evadeAbilities);
        ApplyStartCooldownsInList(jumpAbilities);
        ApplyStartCooldownsInList(teleportAbilities);
    }

    private void ApplyStartCooldownsInList(List<AbilityEntry> abilities)
    {
        if (abilities == null || abilities.Count == 0)
            return;

        foreach (var entry in abilities)
        {
            if (entry == null)
                continue;

            if (entry.startCooldown <= 0f)
                continue;

            entry.cooldown = entry.startCooldown;
            entry.lastTimeUsed = Time.time;
        }
    }
    #endregion

    #region Battle

    #region Setup
    private void SetupBattle(float minCooldown, float maxCooldown)
    {
        battleMinCooldown = Mathf.Min(minCooldown, maxCooldown);
        battleMaxCooldown = Mathf.Max(minCooldown, maxCooldown);

        battleTempMinCooldown = 0f;
        battleTempMaxCooldown = 0f;
        battleTempEndTime = 0f;

        battleCooldownPaused = false;

        battleCooldown = 0f;
        battleLastTimeUsed = 0f;
    }
    #endregion

    #region Controls
    public void ChangeBattleCooldownRange(float minCooldown, float maxCooldown)
    {
        battleMinCooldown = Mathf.Min(minCooldown, maxCooldown);
        battleMaxCooldown = Mathf.Max(minCooldown, maxCooldown);
    }

    public void TempChangeBattleCooldownRange(float minCooldown, float maxCooldown, float duration)
    {
        battleTempMinCooldown = Mathf.Min(minCooldown, maxCooldown);
        battleTempMaxCooldown = Mathf.Max(minCooldown, maxCooldown);
        battleTempEndTime = Time.time + duration;
    }

    public void PauseBattleCooldown(bool pause)
    {
        battleCooldownPaused = pause;
    }
    #endregion

    #region Check for ready
    public bool IsBattleCooldownReady()
    {
        if (battleCooldownPaused)
            return false;

        return Time.time >= battleLastTimeUsed + battleCooldown;
    }
    #endregion

    #region Set cooldown
    public void SetBattleCooldown()
    {
        float min = battleMinCooldown;
        float max = battleMaxCooldown;

        if (IsTempBattleCooldownRangeActive())
        {
            min = battleTempMinCooldown;
            max = battleTempMaxCooldown;
        }

        battleCooldown = Random.Range(min, max);
        battleLastTimeUsed = Time.time;
    }
    #endregion

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

    private bool IsTempBattleCooldownRangeActive()
    {
        return Time.time < battleTempEndTime;
    }
    #endregion

    #endregion
}