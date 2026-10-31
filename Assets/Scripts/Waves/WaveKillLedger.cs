using System;
using System.Collections.Generic;

// Flight handoff and kill accounting have different lifetimes when waves overlap.
public sealed class WaveKillLedger
{
    sealed class Entry
    {
        public int id, total, resolved, killed;
        public bool handedOff;
    }
    readonly Queue<Entry> order = new Queue<Entry>();
    readonly Dictionary<int, Entry> entries = new Dictionary<int, Entry>();
    public void Begin(int id, int total)
    {
        var entry = new Entry { id = id, total = total };
        entries.Add(id, entry);
        order.Enqueue(entry);
    }
    public void Resolve(int id, bool killed)
    {
        if (!entries.TryGetValue(id, out var entry))
            return;
        entry.resolved++;
        if (killed)
            entry.killed++;
    }
    public void HandOff(int id)
    {
        if (entries.TryGetValue(id, out var entry))
            entry.handedOff = true;
    }
    public void Drain(Action<int, int> finished)
    {
        while (order.Count > 0 && order.Peek().handedOff && order.Peek().resolved >= order.Peek().total)
        {
            var entry = order.Dequeue();
            entries.Remove(entry.id);
            finished?.Invoke(entry.total, entry.killed);
        }
    }
    public void Clear()
    {
        order.Clear();
        entries.Clear();
    }
}
