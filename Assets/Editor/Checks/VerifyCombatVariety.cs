#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

// Edit-mode simulation in the disposable checkout. No play session or save writes.
public static class VerifyCombatVariety
{
    const string Folder = "ArtSource/CombatVarietyReview";
    const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    static readonly List<string> results = new List<string>();
    static readonly List<string> errors = new List<string>();
    static readonly HashSet<string> retained = new HashSet<string>();
    static readonly int[] Budgets = { 2, 3, 4, 6, 8, 10, 12, 14, 16, 20 };
    static readonly MethodInfo Step = typeof(CombatRound).GetMethod("UpdateRound", Hidden);
    static readonly FieldInfo Age = typeof(CombatRound).GetField("age", Hidden);
    static void Check(bool value, string text) { if (!value) throw new Exception(text); }
    static void Case(string name, Action body)
    {
        if (retained.Contains("PASS " + name)) { results.Add("PASS " + name); return; }
        try { body(); results.Add("PASS " + name); }
        catch (Exception error) { errors.Add("FAIL " + name + "\n" + error); }
        File.WriteAllLines(Folder + "/Progress.txt", results.Concat(errors));
    }
    static void Guard()
    {
        if (!Application.dataPath.Replace('\\', '/').Contains("/Build/ReviewCache/Temp/"))
            throw new Exception("Use an isolated review checkout");
    }
    public static void Run()
    {
        Guard(); Directory.CreateDirectory(Folder);
        retained.Clear();
        if (Environment.GetCommandLineArgs().Contains("--variety-retry") && File.Exists(Folder + "/Verification.txt"))
            foreach(var line in File.ReadAllLines(Folder + "/Verification.txt"))
                if(line.StartsWith("PASS ")) retained.Add(line);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        results.Clear(); errors.Clear();
        GameRules.playerDisabled = false;
        Case("City models, footprints and unchanged health classes", CityModels);
        Case("No lifetime/fade endpoint removes, shrinks or disables a visible round; pooled reuse", Exits);
        foreach (RunDifficulty difficulty in Enum.GetValues(typeof(RunDifficulty)))
        {
            Difficulty.Select(difficulty);
            Case(difficulty + " Mushroom petals and spore rings fire every position", MushroomVolleys);
            Case(difficulty + " Space supernovas have uniform spacing around the full circle", SupernovaVolleys);
            Case(difficulty + " City phase-two hairpins finish braking, return and bounce before leaving", Hairpins);
            Case(difficulty + " Hell phase one: complete opposed slow flame banks", Hell);
            foreach (LevelId level in Enum.GetValues(typeof(LevelId)))
            {
                if (level == LevelId.Void) continue;
                LevelRun.Reset(level, 41971);
                Case(difficulty + " " + level + " authored route variants", Routes);
                foreach (int seed in new[] { 41971, 78013 })
                {
                    LevelRun.Reset(level, seed);
                    Case(difficulty + " " + level + " ten-wave composition / " + seed, Compositions);
                }
            }
        }
        Case("Tunnel's six narrow passage layouts retain their eleven corridor-following actors", Passage);
        Case("Void remains boss-only", () => Check(WavePartLibrary.For(LevelId.Void).Length == 0, "Void ordinary waves"));
        Case("Watcher-only budgets cannot change later campaign compositions", PreviewOrder);
        File.WriteAllLines(Folder + "/Verification.txt", new[]{errors.Count == 0 ? "PASS" : "FAIL",
            "Headless edit-mode checks. No visual or player approval implied."}.Concat(results).Concat(errors));
        if (errors.Count > 0) throw new Exception(errors.Count + " combat-variety cases failed; see report");
        Debug.Log("PASS Combat variety: " + results.Count + " cases");
    }
    static void Tick(CombatRound shot, float dt)
    {
        Age.SetValue(shot, (float)Age.GetValue(shot) + dt);
        Step.Invoke(shot, new object[]{dt});
    }
    static void ReleaseAll()
    {
        var release = typeof(CombatRound).GetMethod("Release", Hidden);
        foreach (var shot in CombatRound.Live.ToArray()) release.Invoke(shot, null);
    }
    static void Finish(CombatRound shot)
    {
        var renderer = shot.GetComponent<SpriteRenderer>();
        var scale = shot.transform.localScale;
        float alpha = renderer.color.a;
        bool gone = false;
        for (int i = 0; i < 18000; i++)
        {
            var p = shot.transform.position;
            bool visible = CombatRound.Border.Contains(p);
            Tick(shot, 1f / 60);
            if (!CombatRound.Live.Contains(shot))
            {
                Check(!visible, "Round vanished inside visible border at " + p + " age=" + Age.GetValue(shot));
                gone = true; break;
            }
            Check(shot.transform.localScale == scale && renderer.color.a >= alpha - .001f, "Round faded or shrank");
        }
        Check(gone, "Round never exited after 300 simulated seconds");
    }
    static void Exits()
    {
        LevelRun.Reset(LevelId.City, 41971);
        foreach (bool legacyEndpoint in new[]{false,true})
        {
            var shot = CombatRound.Launch<NeedleRound>(Vector3.zero, Vector2.right, 1, .18f);
            if (legacyEndpoint) shot.FadeAt(.15f); else shot.LivesFor(.15f);
            Tick(shot, .5f); Tick(shot, .5f);
            Check(CombatRound.Live.Contains(shot) && ((Collider2D)typeof(CombatRound).GetField("collision",Hidden).GetValue(shot)).enabled, "Expiry erased/disabled visible bullet");
            Finish(shot);
        }
        var orbit = CombatRound.Launch<SimpleRound>(Vector3.zero, Vector2.down, .1f, .18f).Orbit(2, 0, 60, .8f);
        Finish(orbit);
        var reuse = CombatRound.Launch<NeedleRound>(Vector3.zero, Vector2.down, 2, .18f).Pause(.3f,.8f);
        Tick(reuse, .5f);
        var at = reuse.transform.position; Tick(reuse, .2f);
        Check(Vector3.Distance(at, reuse.transform.position) < .001f, "Pooled exit state leaked into next launch");
        Finish(reuse); ReleaseAll();
    }
    static void Hairpins()
    {
        LevelRun.Reset(LevelId.City, 41971);
        var boss = DistrictEnemy.Spawn(typeof(CityDuoBoss), new Vector3(0, 4, 1), false, false, false);
        try
        {
            foreach (float bend in new[]{125f,155f,180f})
            {
                typeof(CityDuoBoss).GetMethod("HairpinFan", Hidden).Invoke(boss,
                    new object[]{new Vector3(0, 3, 0),Vector2.down,bend,.9f,false});
                Check(CombatRound.Live.Count >= 8, "Hairpin score did not fire");
                foreach (var shot in CombatRound.Live.ToArray()) Finish(shot);
            }
        }
        finally { ReleaseAll(); UnityEngine.Object.DestroyImmediate(boss.gameObject); }
    }
    static void MushroomVolleys()
    {
        LevelRun.Reset(LevelId.Mushroom, 41971);
        var boss = DistrictEnemy.Spawn(typeof(MushroomWarden), new Vector3(0, 4, 1), false, false, false);
        try
        {
            var bloom = (IEnumerator)typeof(MushroomWarden).GetMethod("AmanitaBloom", Hidden).Invoke(boss, null);
            bloom.MoveNext(); // Wing charge.
            for (int layer = 0; layer < 3; layer++)
            {
                bloom.MoveNext();
                int marks = Mathf.Max(layer == 1 ? 9 : 18, Difficulty.Dense(layer == 1 ? 18 : 36, LevelRun.BossPressure));
                var wings = CombatRound.Live.GroupBy(shot => (Vector2)shot.transform.position).ToArray();
                Check(wings.Length == 2 && wings.All(wing => wing.Count() == marks), "Incomplete bloom layer " + layer);
                ReleaseAll();
            }
            var instar = (IEnumerator)typeof(MushroomWarden).GetMethod("LastInstar", Hidden).Invoke(boss, null);
            instar.MoveNext();
            instar.MoveNext();
            CheckRing(CombatRound.Live.OfType<SimpleRound>(), Mathf.Max(10, Difficulty.Dense(20, LevelRun.BossPressure)));
        }
        finally { ReleaseAll(); UnityEngine.Object.DestroyImmediate(boss.gameObject); }
    }

    static void SupernovaVolleys()
    {
        LevelRun.Reset(LevelId.Space, 41971);
        var boss = DistrictEnemy.Spawn(typeof(SpaceCitadelBoss), new Vector3(0, 4, 1), false, false, false);
        try
        {
            int beads = Difficulty.Dense(38, LevelRun.BossPressure);
            typeof(SpaceCitadelBoss).GetMethod("SupernovaRing", Hidden).Invoke(boss, new object[] { beads, 3f, 17f, null });
            CheckRing(CombatRound.Live, beads);
        }
        finally { ReleaseAll(); UnityEngine.Object.DestroyImmediate(boss.gameObject); }
    }

    static void CheckRing(IEnumerable<CombatRound> rounds, int expected)
    {
        var angles = rounds.Select(shot => Vector2.SignedAngle(Vector2.down, shot.Heading)).OrderBy(angle => angle).ToArray();
        Check(angles.Length == expected, $"Expected {expected} rounds, got {angles.Length}");
        for (int i = 0; i < expected; i++)
        {
            float spacing = Mathf.Repeat(angles[(i + 1) % expected] - angles[i], 360f);
            Check(Mathf.Abs(spacing - 360f / expected) < .01f, "Uneven spacing around ring");
        }
    }

    static void Hell()
    {
        LevelRun.Reset(LevelId.Hell, 41971);
        var boss = DistrictEnemy.Spawn(typeof(HellCrabBoss), new Vector3(0, 4, 1), false, false, false);
        try
        {
            var cards = (IEnumerator)typeof(HellCrabBoss).GetMethod("Cards", Hidden).Invoke(boss, null);
            Check(cards.MoveNext() && cards.Current.GetType().Name.Contains("Crosswinds"), "First phase selected other attacks");
            typeof(HellCrabBoss).GetMethod("PrepareSideWarnings",Hidden).Invoke(boss,null);
            var warnings=(SpriteRenderer[])typeof(HellCrabBoss).GetField("sideWarnings",Hidden).GetValue(boss);
            Check(warnings.Length==48 && warnings.All(c=>!c.enabled),"Edge cues must be prepared once and initially hidden");
            var warning=(IEnumerator)typeof(HellCrabBoss).GetMethod("WarnCrosswinds",Hidden).Invoke(boss,new object[]{17,.7f});
            warning.MoveNext();
            Check(warnings.Count(c=>c.enabled)==34 && warnings.Where(c=>c.enabled).All(c=>CombatRound.Border.Contains(c.transform.position)),"Every firing position on both edges must show its charge");
            typeof(HellCrabBoss).GetMethod("OnPhaseTwo",Hidden).Invoke(boss,null);
            Check(warnings.All(c=>!c.enabled),"Phase transition leaves stale edge warnings");
            var claws = (Transform[])typeof(HellCrabBoss).GetField("claws", Hidden).GetValue(boss);
            var arms = (Transform[,])typeof(HellCrabBoss).GetField("arms", Hidden).GetValue(boss);
            for(int side=0;side<2;side++)
            {
                claws[side] = new GameObject("Test pincer").transform; claws[side].SetParent(boss.transform);
                for(int segment=0;segment<4;segment++) { arms[side,segment]=new GameObject("Test arm").transform; arms[side,segment].SetParent(boss.transform); }
            }
            var move = typeof(HellCrabBoss).GetMethod("MoveBoss", Hidden);
            for(int i=0;i<60;i++) move.Invoke(boss, new object[]{1f});
            Check((float)typeof(HellCrabBoss).GetField("clawClock",Hidden).GetValue(boss)==0 && CombatRound.Live.Count==0, "Claw attacks in first phase");
            typeof(HellCrabBoss).GetMethod("ReleaseCrosswinds", Hidden).Invoke(boss,new object[]{17});
            Check(CombatRound.Live.Count==34 && CombatRound.Live.All(s=>s is FlameRound), "Side-bank count/type");
            foreach(var shot in CombatRound.Live.ToArray())
            {
                var p=shot.transform.position;
                Check(p.x < CombatRound.Border.xMin || p.x > CombatRound.Border.xMax,"Flame spawned inside arena");
                Tick(shot,.1f); var d=shot.transform.position-p;
                Check(Mathf.Abs(d.y)<.0001f && Mathf.Abs(Mathf.Abs(d.x)-.115f)<.001f && d.x*p.x<0,"Flame must cross horizontally at 1.15 world units/second");
                Finish(shot);
            }
        }
        finally { ReleaseAll(); UnityEngine.Object.DestroyImmediate(boss.gameObject); }
    }
    static void CityModels()
    {
        var types = new[]{typeof(CourierDiver),typeof(AttackDrone),typeof(PursuitFighter),typeof(CommandGunship),typeof(MetroLancer),typeof(TerraceGunboat)};
        foreach(var type in types)
        {
            var enemy=DistrictEnemy.Spawn(type,Vector3.zero,false,false,false);
            try
            {
                float scale=enemy.hullScale, health=enemy.health;
                type.GetMethod("Configure",Hidden).Invoke(enemy,null);
                Check(Mathf.Approximately(scale,enemy.hullScale*1.25f) && health==enemy.health,"City model/health scale: "+type.Name);
                Check(Mathf.Approximately(enemy.Hull.localScale.x,scale),"Hull transform scale");
                float reach=enemy.Hull.GetComponent<MeshFilter>().sharedMesh.vertices.Max(v=>v.magnitude)*scale;
                Check(WaveGeometry.Radius(type.Name)>=reach,"Footprint too small: "+type.Name);
            }
            finally { UnityEngine.Object.DestroyImmediate(enemy.gameObject); }
        }
    }
    static void Routes()
    {
        var parts=WavePartLibrary.For(LevelRun.Current).Where(p=>p.Motion!=WaveMotion.Flow).ToArray();
        Check(parts.Select(p=>p.Motion).Distinct().Count()>=3,"Fewer than three added behaviour types");
        foreach(var part in parts)
            foreach(int side in new[]{-1,1})
                foreach(int variant in new[]{0,5,10})
                    Check(WaveComposer.PreviewPart(part,variant,side,out _)!=null,"Unplaceable route: "+part.Name+" / "+variant+" / "+side);
    }
    static void Compositions()
    {
        var lookup=WavePartLibrary.For(LevelRun.Current).ToDictionary(p=>p.Name);
        var motions=new HashSet<WaveMotion>(); var names=new HashSet<string>();
        foreach(int budget in Budgets)
        {
            var wave=WaveComposer.Create(budget);
            foreach(var name in wave.parts)
                if(lookup.TryGetValue(name,out var part)) {motions.Add(part.Motion); names.Add(name);}
        }
        Check(motions.Count>=4,"Insufficient behaviour variety: "+string.Join(", ",motions));
        Check(names.Count>=10,"Insufficient formation variety: "+names.Count);
    }
    static void Passage()
    {
        LevelRun.Reset(LevelId.Tunnel,41971);
        for(int layout=1;layout<=TunnelPassage.LayoutCount;layout++)
        {
            var wave=TunnelPassageWaves.Create(6,layout);
            Check(wave.units.Count==11 && wave.units.All(unit=>unit.passageRoute) && wave.units[0].passage==layout,"Passage composition changed");
        }
    }
    static void PreviewOrder()
    {
        foreach(LevelId level in Enum.GetValues(typeof(LevelId)))
        {
            if(level==LevelId.Void) continue;
            LevelRun.Reset(level,93217); var expected=WaveComposer.Create(20);
            LevelRun.Reset(level,93218); WaveComposer.Create(2);
            LevelRun.Reset(level,93217);
            foreach(int budget in new[]{5,7,9,11,13,15,17,19}) WaveComposer.Create(budget);
            var actual=WaveComposer.Create(20);
            Check(expected.parts.SequenceEqual(actual.parts) && expected.units.Count==actual.units.Count &&
                expected.units.Zip(actual.units,(a,b)=>a.enemy==b.enemy && a.time==b.time && a.lane==b.lane && a.side==b.side).All(v=>v),"Preview order changed campaign: "+level);
        }
    }
    public static void Build()
    {
        Guard();
        if(!File.ReadAllText(Folder+"/Verification.txt").StartsWith("PASS")) throw new Exception("Verification must pass first");
        var output=VerifyCombatRework.Argument("--variety-output","");
        if(!Path.IsPathRooted(output) || Path.GetExtension(output)!=".exe") throw new Exception("Supply an absolute executable path");
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{ scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray(),target=BuildTarget.StandaloneWindows64,locationPathName=output,options=BuildOptions.None });
        File.WriteAllText(Folder+"/Build.txt",report.summary.result+"\nErrors="+report.summary.totalErrors+"; warnings="+report.summary.totalWarnings+"\n"+output);
        EditorApplication.Exit(report.summary.result==BuildResult.Succeeded?0:1);
    }
}
#endif
