using UnityEngine;

// Authored, multi-frame phrases. Eight fixed jobs, the existing projectile pool,
// real moving weapon transforms, and one shared charge sprite at each emitter.
public sealed class BossWeave
{
    enum Kind { Spiral, Web }
    struct Job
    {
        public bool active;
        public Kind kind;
        public Transform anchor;
        public AEnemy battery;
        public Vector3 port;
        public int arms, steps, step;
        public float wait, every, angle, turn, speed, radius;
    }
    readonly Job[] jobs = new Job[8];
    readonly DistrictBoss owner;
    public int Released { get; private set; }
    public int Active { get { int count = 0; foreach (var j in jobs) if (j.active) count++; return count; } }
    public BossWeave(DistrictBoss boss) { owner = boss; }
    public void Cancel() { for (int i = 0; i < jobs.Length; i++) jobs[i].active = false; }
    void Add(Job job)
    {
        for (int i = 0; i < jobs.Length; i++) if (!jobs[i].active)
        { job.active = true; job.wait = .5f; jobs[i] = job; return; }
    }
    public void Spiral(Transform anchor, Vector3 port, int arms, int steps, float turn, float interval,
        float speed = 2.7f, float radius = .17f, float angle = 0, AEnemy battery = null)
    {
        Add(new Job { kind = Kind.Spiral, anchor = anchor, port = port, arms = arms, steps = steps,
            turn = turn, every = interval, speed = speed, radius = radius, angle = angle, battery = battery });
    }
    public void Web(Transform anchor, Vector3 port, int spokes = 8, int rings = 4,
        float speed = 2.2f, float angle = 0, AEnemy battery = null)
    {
        Add(new Job { kind = Kind.Web, anchor = anchor, port = port, arms = spokes, steps = rings * 3,
            every = .24f, speed = speed, radius = .18f, angle = angle, battery = battery });
    }
    public void Tick(float dt)
    {
        for (int i = 0; i < jobs.Length; i++)
        {
            var j = jobs[i];
            if (!j.active) continue;
            if (j.anchor == null || j.battery != null && j.battery.health <= 0)
            { jobs[i].active = false; continue; }
            j.wait -= dt;
            Vector3 port = j.anchor.TransformPoint(j.port);
            float angle = j.angle + (j.kind == Kind.Spiral ? j.step * j.turn : 0);
            owner.PreparePattern(port, j.wait, CombatVisuals.Rotate(Vector2.down, angle));
            if (j.wait <= 0)
            {
                bool ring = j.kind == Kind.Web && j.step % 3 == 0;
                int count = j.kind == Kind.Spiral ? j.arms : ring ? j.arms * 3 : j.arms;
                int gap = j.kind == Kind.Spiral ? 0 : ring ? 3 : 1;
                var player = CombatVisuals.Player;
                bool clear = player == null || Vector2.Distance(port, player.position) > 2.15f;
                if (clear && CombatRound.Live.Count + count - gap <= CombatRound.Limit)
                {
                    for (int n = gap; n < count; n++)
                    {
                        Vector2 direction = CombatVisuals.Rotate(Vector2.down, angle + n * 360f / count);
                        float speed = j.speed * (j.kind == Kind.Web ? ring ? 1 + (j.step / 3) * .08f : .87f : 1);
                        owner.PatternShot(port, direction, speed, ring ? j.radius : j.kind == Kind.Web ? .125f : j.radius);
                        Released++;
                    }
                    j.step++;
                    j.wait += j.every;
                    if (j.step >= j.steps) j.active = false;
                }
                else j.wait = .06f; // Wait for a complete geometric release, never clip a sector.
            }
            jobs[i] = j;
        }
    }
}
