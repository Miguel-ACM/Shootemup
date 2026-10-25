using System.Collections.Generic;
using UnityEngine;

public class Reform : PatternMover
{
    [Tooltip("Assign the same anchor to every member of one formation.")]
    [SerializeField] private Transform formationAnchor;
    [SerializeField] private int slotOrder;
    [SerializeField, Min(.1f)] private float spacing = 1.4f;
    [SerializeField, Min(0)] private float reformDelay = .6f;
    private static readonly List<Reform> members = new List<Reform>();
    private readonly List<Reform> peers = new List<Reform>();
    private int lastRank = -1, lastCount = -1;
    private float delay;
    private Vector3 fixedCentre, offset;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetMembers() { members.Clear(); }

    protected override void InitializePattern()
    {
        if (!members.Contains(this)) members.Add(this);
        fixedCentre = origin;
        offset = formationAnchor != null ? origin - formationAnchor.position : Vector3.zero;
    }

    protected override void StepPattern(float dt)
    {
        members.RemoveAll(member => member == null);
        peers.Clear();
        foreach (var member in members)
            if (member.isActiveAndEnabled && !member.IsLeaving &&
                (formationAnchor != null ? member.formationAnchor == formationAnchor : member == this)) peers.Add(member);
        peers.Sort(CompareSlots);
        int rank = peers.IndexOf(this);
        if (rank != lastRank || peers.Count != lastCount)
        {
            lastRank = rank;
            lastCount = peers.Count;
            delay = reformDelay;
            SetFiring(false);
        }
        if (delay > 0) { delay -= dt; return; }
        Vector3 centre = formationAnchor != null ? formationAnchor.position : fixedCentre;
        var target = centre + new Vector3((rank - (peers.Count - 1) * .5f) * spacing, offset.y, 0);
        bool reached = Travel(InBounds(target), dt);
        SetFiring(reached);
        if (reached) Arrive();
    }

    private static int CompareSlots(Reform a, Reform b)
    {
        int order = a.slotOrder.CompareTo(b.slotOrder);
        return order != 0 ? order : a.GetEntityId().CompareTo(b.GetEntityId());
    }

    protected override void OnDisable() { members.Remove(this); base.OnDisable(); }
}
