using System;
using System.Collections.Generic;

public enum RunSlotKind { Combat, Miniboss, Boss, EncounterChoice, MinibossChoice, Reward, CoinShop, MultiplierShop, Pact, BossReward, ChallengeReward, End }
public enum RunPhase { Idle, Opening, Combat, BossWarning, Boss, Decision, Routing }

public readonly struct RunSlot
{
    public readonly RunSlotKind Kind;
    public readonly int Budget;
    public RunSlot(RunSlotKind kind, int budget = 0) { Kind = kind; Budget = budget; }
    public bool IsChoice => Kind == RunSlotKind.EncounterChoice || Kind == RunSlotKind.MinibossChoice;
    public bool IsDecision => Kind == RunSlotKind.Reward || Kind == RunSlotKind.CoinShop || Kind == RunSlotKind.MultiplierShop ||
        Kind == RunSlotKind.Pact || Kind == RunSlotKind.BossReward || Kind == RunSlotKind.ChallengeReward;
    public string Key => Kind == RunSlotKind.Combat ? Budget.ToString(System.Globalization.CultureInfo.InvariantCulture) :
        Kind == RunSlotKind.CoinShop ? "coinShop" : Kind == RunSlotKind.MultiplierShop ? "multiplierShop" :
        Kind == RunSlotKind.EncounterChoice ? "encounterChoice" : Kind == RunSlotKind.MinibossChoice ? "minibossChoice" :
        Kind == RunSlotKind.BossReward ? "bossReward" : Kind == RunSlotKind.ChallengeReward ? "challengeReward" : Kind.ToString().ToLowerInvariant();
    public static RunSlot Parse(string key)
    {
        if (int.TryParse(key, out int budget) && budget > 0)
            return new RunSlot(RunSlotKind.Combat, budget);
        if (key == "scoreShop")
            return new RunSlot(RunSlotKind.CoinShop);
        if (Enum.TryParse(key, true, out RunSlotKind kind) && kind != RunSlotKind.Combat)
            return new RunSlot(kind);
        throw new ArgumentException("Unknown run slot: " + key);
    }
}

public sealed class RunPlan
{
    readonly List<RunSlot> slots = new List<RunSlot>();
    public int Count => slots.Count;
    public RunSlot this[int index] => index >= 0 && index < slots.Count ? slots[index] : new RunSlot(RunSlotKind.End);
    public void Load(IEnumerable<string> keys)
    {
        slots.Clear();
        foreach (string key in keys)
            slots.Add(RunSlot.Parse(key));
    }
    public string[] Export() => slots.ConvertAll(slot => slot.Key).ToArray();
    public void Insert(int index, RunSlotKind kind) => slots.Insert(Math.Min(index, slots.Count), new RunSlot(kind));
    public RunSlot Following(int index)
    {
        while (index < slots.Count && slots[index].IsChoice)
            index++;
        return this[index];
    }
    public int RemainingCombat(int index)
    {
        int count = 0;
        for (; index < slots.Count; index++)
            if (slots[index].Kind == RunSlotKind.Combat)
                count++;
        return count;
    }
}

public sealed class ChallengeProgress
{
    public int Remaining { get; set; }
    public bool CurrentWave { get; set; }
    public void BeginWave(bool ordinary) => CurrentWave = ordinary && Remaining > 0;
    public void Start(bool currentOrdinary) { Remaining = 3; CurrentWave = currentOrdinary; }
    public bool FinishWave()
    {
        if (!CurrentWave)
            return false;
        CurrentWave = false;
        return --Remaining == 0;
    }
    public void Reset() { Remaining = 0; CurrentWave = false; }
}
