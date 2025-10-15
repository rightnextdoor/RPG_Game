using System.Collections.Generic;
using UnityEngine;

public class AbilityHub
{
    public AbilityEntry TryPick(IDictionary<string, AbilityEntry> abilityMap, float playerDistance)
    {
        if (abilityMap == null || abilityMap.Count == 0) return null;

        var eligible = BuildEligibleList(abilityMap, playerDistance);
        if (eligible.Count == 0) return null;

        while (eligible.Count > 0)
        {
            var chosen = WeightedPickByChance(eligible);
            if (TryConsumeCooldown(chosen))
                return chosen;

            eligible.Remove(chosen);
        }

        return null;
    }

    private List<AbilityEntry> BuildEligibleList(IDictionary<string, AbilityEntry> abilityMap, float playerDistance)
    {
        var list = new List<AbilityEntry>(abilityMap.Count);

        foreach (var kvp in abilityMap)
        {
            var entry = kvp.Value;
            if (entry == null) continue;
            if (!entry.unlocked) continue;

            bool hasMin = entry.rangeMin.HasValue;
            bool hasMax = entry.rangeMax.HasValue;

            bool noRangeByNulls = !hasMin && !hasMax;
            bool noRangeByZeros = hasMin && hasMax &&
                                  Mathf.Approximately(entry.rangeMin.Value, 0f) &&
                                  Mathf.Approximately(entry.rangeMax.Value, 0f);

            if (noRangeByNulls || noRangeByZeros)
            {
                list.Add(entry);
                continue;
            }

            if (hasMin && !hasMax)
            {
                if (playerDistance >= entry.rangeMin.Value) list.Add(entry);
                continue;
            }

            if (!hasMin && hasMax)
            {
                if (playerDistance <= entry.rangeMax.Value) list.Add(entry);
                continue;
            }

            // both set
            if (playerDistance >= entry.rangeMin.Value && playerDistance <= entry.rangeMax.Value)
                list.Add(entry);
        }

        return list;
    }

    private AbilityEntry WeightedPickByChance(List<AbilityEntry> candidates)
    {
        float total = 0f;
        foreach (var candidate in candidates)
            total += 1f + Mathf.Max(0f, candidate.chance); 

        float roll = Random.Range(0f, total);
        float accum = 0f;

        foreach (var candidate in candidates)
        {
            accum += 1f + Mathf.Max(0f, candidate.chance);
            if (roll < accum) return candidate; 
        }

        return candidates[candidates.Count - 1];
    }



    private bool TryConsumeCooldown(AbilityEntry entry)
    {
        if (Time.time >= entry.lastTimeUsed + entry.cooldown)
        {
            entry.cooldown = Random.Range(entry.minCooldown, entry.maxCooldown);
            entry.lastTimeUsed = Time.time;
            return true;
        }
        return false;
    }
}
