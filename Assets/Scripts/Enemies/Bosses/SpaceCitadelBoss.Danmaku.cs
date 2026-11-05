using UnityEngine;
using System.Collections;

// The black hole draws circular fronts across the whole arena into its moving core.
// Phase one (central reactor open)
//   Accretion: arrows appear around the disk and spiral in until the core swallows
//     them. Dark stars (giant orbs) curl in from the same orbit on alternate beats.
//   Supernova: the core throws rings of arrows outward; each ring stops, holds, then
//     collapses back through the core and bursts out the other side. The centre
//     laser cuts the middle at the end.
// Phase two (reactor overload at half health)
//   Expulsion: the core flings mirrored spiral arms of arrows that accelerate away
//     and bend straight as they leave the screen, with full rings and rings of dark
//     stars flung out between them.
//   Binary Collapse: two supernova rings at once, counter-rotating offsets, with the
//     laser between them.
// Phase three (Hard)
//   Big Crunch: the core breathes, swallowing accretion rings from its orbit and
//     flinging spiral arms back out, both always running and trading dominance.
public sealed partial class SpaceCitadelBoss
{
    Vector3 Core => Reactor != null ? Reactor.transform.position : transform.position;
    MaterialPropertyBlock wellInk;
    bool drawingIn;
    readonly LineRenderer[] orbitArcs=new LineRenderer[28];
    float orbitStarted;
    const float OrbitStep=2.6f;
    protected override void Attack()
    {
        if (Reactor != null)
            OpenCards();
    }

    protected override IEnumerator Cards()
    {
        while (true)
        {
            if (phaseThree)
                yield return FinalSpell();
            else if (!phaseTwo)
            {
                yield return Accretion();
                yield return Wait(.8f);
                yield return Supernova(false);
                yield return Wait(.8f);
            }
            else
            {
                yield return Expulsion();
                yield return Wait(.7f);
                yield return Supernova(true);
                yield return Wait(.7f);
            }
        }
    }

    Transform CoreBody => Reactor != null ? Reactor.transform : transform;

    // Accretion brightens the disk itself; the singularity stays dark.
    void OpenWell(bool inward)
    {
        if (inward)
        {
            drawingIn=true;
            orbitStarted = motionAge;
            UpdateOrbitTell();
        }
        SetAccretion(1);
    }

    void CloseWell()
    {
        drawingIn=false;
        foreach(var arc in orbitArcs)if(arc!=null)arc.enabled=false;
        SetAccretion(0);
    }
    void SetAccretion(float strength)
    {
        if (Hull == null) return;
        if (wellInk == null) wellInk = new MaterialPropertyBlock();
        var renderer = Hull.GetComponent<MeshRenderer>();
        renderer.GetPropertyBlock(wellInk);
        wellInk.SetFloat("_Feeding", strength);
        renderer.SetPropertyBlock(wellInk);
    }

    void BuildOrbitTell()
    {
        var tint=LevelProjectilePalette.Accent(LevelId.Space);tint.a=.19f;
        for(int i=0;i<orbitArcs.Length;i++)
        {
            var arc=CombatVisuals.Line(transform,tint,.018f);arc.useWorldSpace=true;
            arc.sortingLayerName="Enemy";arc.enabled=false;orbitArcs[i]=arc;
        }
    }

    void UpdateOrbitTell()
    {
        var center=Core;Rect r=CombatRound.Border;
        // Each monotone quadrant clips to the flight rectangle. Never draw the
        // offscreen part of a large circle across the HUD margins.
        for(int ring=0;ring<7;ring++)for(int quadrant=0;quadrant<4;quadrant++)
        {
            var line=orbitArcs[ring*4+quadrant];int count=0;
            float radius=OrbitStep*(ring+1);
            for(int i=0;i<=40;i++)
            {
                var p=center+(Vector3)CombatVisuals.Rotate(Vector2.up,quadrant*90+i*2.25f)*radius;
                if(!r.Contains(p))continue;
                count++;
            }
            line.positionCount=count;line.enabled=count>1;int index=0;
            for(int i=0;i<=40;i++)
            {
                var p=center+(Vector3)CombatVisuals.Rotate(Vector2.up,quadrant*90+i*2.25f)*radius;
                if(r.Contains(p)){p.z=.1f;line.SetPosition(index++,p);}
            }
        }
    }

    bool SafeAccretionPoint(Vector3 point,float inset)
    {
        Rect r=CombatRound.Border;
        return point.x>r.xMin+inset&&point.x<r.xMax-inset&&point.y>r.yMin+inset&&point.y<r.yMax-inset&&
            (player==null||Vector2.Distance(point,player.position)>1.6f+inset);
    }

    void AccretionRing(int n, int density)
    {
        var center=Core;
        for(int ring=0;ring<7;ring++)
        {
            float radius=OrbitStep*(ring+1);
            int count=Mathf.Max(8,Mathf.RoundToInt(density*2*radius/3.6f));
            for(int i=0;i<count;i++)
            {
                float angle=(i+.5f)*360f/count+n*7*(ring%2==0?1:-1);
                // All fronts share one slowly turning radial escape seam.
                if(Mathf.Abs(Mathf.DeltaAngle(angle,180+24*Mathf.Sin(n*.22f)))<8)continue;
                var radial=CombatVisuals.Rotate(Vector2.up,angle);
                var point=center+(Vector3)radial*radius;
                if(!SafeAccretionPoint(point,.25f))continue;
                var heading=CombatVisuals.Rotate(-radial,ring%2==0?23:-23);
                Fire<NeedleRound>(point,heading,1.05f+radius*.055f,.19f)
                    .Absorb(CoreBody,5.2f,.6f).LivesFor(14);
            }
        }
    }

    void AccretionStars(int n, int pairs)
    {
        var center = Core;
        for(int ring=1;ring<7;ring+=2)
        {
            float radius=OrbitStep*(ring+1);
            int count=Mathf.Max(8,Mathf.RoundToInt(pairs*2*radius/3.6f));
            for (int i=0;i<count;i++)
            {
                float angle=(i+.5f)*360f/count-n*11;
                if(Mathf.Abs(Mathf.DeltaAngle(angle,180+24*Mathf.Sin(n*.22f)))<15)continue;
                var radial=CombatVisuals.Rotate(Vector2.up,angle);
                var point=center+(Vector3)radial*radius;
                if(!SafeAccretionPoint(point,.7f))continue;
                Fire<GiantOrb>(point,CombatVisuals.Rotate(-radial,ring%4==1?-18:18),.75f,.62f)
                    .Absorb(CoreBody,3.4f,1).LivesFor(14);
            }
        }
    }

    // Mirrored spiral arms flung from the core; they accelerate out and straighten.
    void Expel(int n, int arms, float twist)
    {
        for (int arm = 0; arm < arms; arm++)
            for(int mirror=-1;mirror<=1;mirror+=2)
            {
                var radial = CombatVisuals.Rotate(Vector2.down, mirror * (n * 9 + arm * 360f / arms));
                Fire<NeedleRound>(Core, CombatVisuals.Rotate(radial, mirror * twist), .5f, .19f).Repel(Core, 7).LivesFor(9);
            }
    }

    // A full ring flung out between the arms, offset every time.
    void ExpelRing(int n, int beads)
    {
        for (int i = 0; i < beads; i++)
            Fire<NeedleRound>(Core, CombatVisuals.Rotate(Vector2.down, n * 7.3f + i * 360f / beads), .7f, .19f).Repel(Core, 6).LivesFor(9);
    }

    void ExpelStars(int n, int count)
    {
        for (int i = 0; i < count; i++)
            Fire<GiantOrb>(Core, CombatVisuals.Rotate(Vector2.down, n * 11 + i * 360f / count), .4f, .75f).Repel(Core, 3).LivesFor(13);
    }

    IEnumerator Accretion()
    {
        OpenWell(inward: true);
        yield return Wait(.6f);
        yield return Every(8, .38f, n =>
        {
            AccretionRing(n, Dense(30));
            if (n % 2 == 1)
                AccretionStars(n, Dense(6));
            // The core answers the inward fronts, so camping below their birth
            // line is not a permanent refuge. These rings cross the returning arcs.
            if(n%3==2)ExpelRing(n,Dense(32));
        });
        yield return Wait(1.2f);
        CloseWell();
    }

    // Rings thrown out that stop, hold, then fall back through the core and burst out beyond it.
    void SupernovaRing(int beads, float radius, float offset)
    {
        float travel = .9f;
        float speed = radius / (travel - .175f);
        for (int i = 0; i < beads; i++)
            Fire<NeedleRound>(Core, CombatVisuals.Rotate(Vector2.down, offset + i * 360f / beads), WorldSpeed(speed), .19f)
                .Pause(travel, .7f, CombatRound.Turn.Rotate, 180, 2.6f / speed)
                .LivesFor(9);
    }

    IEnumerator Supernova(bool binary)
    {
        int beads = Dense(binary ? 26 : 30);
        for (int n = 0; n < 5; n++)
        {
            yield return Charge(Hull, WeaponPort(), .35f);
            SupernovaRing(beads, 3.4f + (n % 2) * .8f, n * 6);
            if (binary)
                SupernovaRing(beads, 2.2f, -n * 6 + 180f / beads);
            yield return Wait(Rest(1.1f));
        }
        Laser();
        yield return Wait(2.2f);
    }

    IEnumerator Expulsion()
    {
        OpenWell(inward: false);
        yield return Charge(Hull, WeaponPort(), .5f);
        yield return Every(8, Rest(.09f), n =>
        {
            Expel(n, 4, 55);
            if (n % 13 == 0)
                ExpelRing(n, Dense(20));
            // Dark stars every 0.36 s, alternating between a big and a small ring.
            if (n % 4 == 1)
                ExpelStars(n, Dense(n % 8 == 1 ? 12 : 8));
        });
        CloseWell();
    }

    void Laser()
    {
        centralLaser = Fire<PulseLaser>(Muzzle(), Vector2.down, 0);
        if (centralLaser == null)
            return;
        centralLaser.Bind(Hull, WeaponPort());
        centralLaser.warning = 1.1f;
        centralLaser.activeTime = phaseTwo ? 1.8f : 1.4f;
        centralLaser.width = phaseTwo ? .42f : .34f;
    }

    // Hard final phase, Big Crunch: absorbing and expelling at once. Every 2.5 s the
    // dominant side flips: full accretion rings with a thin pair of arms, then full
    // spiral arms with sparse rings, dark stars travelling both ways.
    IEnumerator FinalSpell()
    {
        OpenWell(inward: true);
        yield return Wait(.5f);
        yield return Every(10, .1f, n =>
        {
            bool inward = n / 25 % 2 == 0;
            if (n % (inward ? 3 : 6) == 0)
                AccretionRing(n / 3, Dense(inward ? 30 : 20));
            if (!inward || n % 3 == 0)
                Expel(n, inward ? 4 : 6, 55);
            if (!inward && n % 15 == 5)
                ExpelRing(n, Dense(30));
            if (n % 8 == 4)
            {
                AccretionStars(n, Dense(inward ? 6 : 4));
                ExpelStars(n, Dense(inward ? 6 : 9));
            }
        });
        CloseWell();
        yield return Wait(.6f);
    }
}
