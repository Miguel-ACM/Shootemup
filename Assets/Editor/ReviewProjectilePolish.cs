#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// One editor-only review: real sprite shader renders and deterministic production movement.
public static class ReviewProjectilePolish
{
    const string Folder="ArtSource/ProjectilePolishReview";
    const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
    static readonly List<string> checks=new List<string>();
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);checks.Add(message);}
    public static void Run()
    {
        Directory.CreateDirectory(Folder);
        try
        {
            EditorSceneManager.OpenScene("Assets/Scenes/L1.unity");
            GameRules.enemySpeed=GameRules.enemyHealth=GameRules.enemyShootSpeed=1;GameRules.playerDisabled=false;
            var ship=UnityEngine.Object.FindAnyObjectByType<Shipcontroller>();
            Check(ship!=null,"Loaded production ship and health bar");
            ship.maxHealth=120;ship.health=18;GameRules.healMultiplier=2;ship.HealFraction(.5f);
            Check(Mathf.Approximately(ship.health,78),"Health route restores exactly half maximum health (60 of 120), even with healing modifiers");
            ship.HealFraction(.5f);Check(ship.health==120,"Route healing caps at maximum health");
            ship.health=18;ship.HealFlat(2);Check(ship.health==22,"Ordinary health pickups retain their existing healing modifiers");
            Check(Resources.Load<GameObject>("Pickables/life").GetComponent<Life>().maxHealthFraction==0,"Ordinary pickup prefab still uses flat healing");
            foreach(LevelId level in Enum.GetValues(typeof(LevelId)))ReviewMiniboss(level);
            RenderBullets();
            File.WriteAllText(Folder+"/Verification.txt","PASS\nEditor only: no Play mode or gameplay run.\n"+string.Join("\n",checks));
            EditorApplication.Exit(0);
        }
        catch(Exception e){File.WriteAllText(Folder+"/Verification.txt","FAIL\n"+e+"\n"+string.Join("\n",checks));EditorApplication.Exit(1);}
    }
    static DistrictEnemy Spawn(WaveUnit unit)
    {
        var enemy=DistrictEnemy.Spawn(DistrictWaves.EnemyType(unit.enemy),new Vector3(unit.position.x*Camera.RightLimit,unit.position.y*Camera.UpLimit,1));
        // Unity's normal end-of-frame component removal is unavailable outside Play mode.
        if(unit.phrase!=FlightPhrase.None)UnityEngine.Object.DestroyImmediate(enemy.GetComponent<PatternMover>());
        enemy.FlyPhrase(unit);return enemy;
    }
    static float FirstBeat(DistrictEnemy enemy)
    {
        var mover=enemy.GetComponent<PatternMover>();
        for(int i=1;i<=900;i++)
        {
            mover.Step(1f/60);
            if(mover.CanFire && enemy.transform.position.y<=Camera.UpLimit-.5f && enemy.transform.position.y>=Camera.DownLimit+1 &&
                (!(mover is FormationFlight flight) || flight.TakeFireBeat()))return i/60f;
        }
        return float.PositiveInfinity;
    }
    static void ReviewMiniboss(LevelId level)
    {
        LevelRun.Reset(level,17381);var wave=DistrictWaves.Miniboss();
        Check(wave.units.Count>0 && wave.units.All(u=>u.miniboss && ((WaveUnit)u.Clone()).miniboss),level+": every miniboss member and clone is flagged");
        float oldFirst=float.PositiveInfinity,newFirst=float.PositiveInfinity;
        foreach(var unit in wave.units)
        {
            var ordinary=(WaveUnit)unit.Clone();ordinary.miniboss=false;
            var normal=Spawn(ordinary);var mini=Spawn(unit);
            float health=normal.health;float interval=(float)typeof(DistrictEnemy).GetField("attackInterval",Hidden).GetValue(normal);
            Check(Mathf.Abs(mini.health/health-.68f)<.001f &&
                Mathf.Abs((float)typeof(DistrictEnemy).GetField("attackInterval",Hidden).GetValue(mini)/interval-.74f)<.001f,
                level+"/"+unit.enemy+": 32% less health and 26% shorter attack interval");
            if(unit.fireTimes!=null)Check(!ReferenceEquals(mini.GetComponent<FormationFlight>().fireTimes,unit.fireTimes),"Miniboss score leaves the cached ordinary formation untouched");
            oldFirst=Mathf.Min(oldFirst,unit.time+FirstBeat(normal));newFirst=Mathf.Min(newFirst,unit.time+FirstBeat(mini));
            UnityEngine.Object.DestroyImmediate(normal.gameObject);UnityEngine.Object.DestroyImmediate(mini.gameObject);
        }
        Check(newFirst<oldFirst,level+": first visible firing beat "+newFirst.ToString("F2")+"s, previously "+oldFirst.ToString("F2")+"s");
        var one=Spawn(wave.units[0]);one.transform.position=new Vector3(0,4,1);
        var fire=typeof(DistrictEnemy).GetMethod("Fire",Hidden).MakeGenericMethod(typeof(NeedleRound));
        var shot=(CombatRound)fire.Invoke(one,new object[]{one.transform.position,Vector2.down,5f,.14f});
        Check(Mathf.Abs(shot.speed/(5*GameRules.enemyBulletSpeed*LevelRun.EnemyShotSpeedScale)-1.14f)<.001f,level+": miniboss projectiles travel 14% faster");
        Check(shot.GetComponent<MeshRenderer>()==null && shot.GetComponent<CustomCollider2D>()!=null,"Rounds retain sprite rendering and the original pixel collider");
        UnityEngine.Object.DestroyImmediate(shot.gameObject);UnityEngine.Object.DestroyImmediate(one.gameObject);
        if(level==LevelId.Candyland)Check(WaveComposer.Create(16).units.All(u=>!u.miniboss),"Candyland's ordinary waves are not modified through the composition cache");
    }
    static void RenderBullets()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var camera=new GameObject("Sprite review camera").AddComponent<UnityEngine.Camera>();
        camera.orthographic=true;camera.orthographicSize=7.5f;camera.transform.position=new Vector3(0,0,-10);
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.065f,.08f,.12f);
        var target=new RenderTexture(640,480,24);camera.targetTexture=target;
        var kinds=new[]{typeof(SimpleRound),typeof(NeedleRound),typeof(BurstShell),typeof(RicochetBolt),typeof(SuspendedRound),typeof(GuidedMissile),typeof(PlasmaOrb),typeof(ProximityMine),typeof(ReturningDisc),typeof(ScatterCarrier),typeof(LaserSideRound),typeof(HelixRound)};
        var material=new Material(CombatVisuals.ProjectileMaterial);material.SetFloat("_Neutralize",1);
        Check(material.shader.name=="Shootemup/Combat Projectile" && !ShaderUtil.ShaderHasError(material.shader),"Production projectile shader compiles");
        Check(Mathf.Approximately(material.GetFloat("_Gloss"),.7f),"Existing projectile material receives the enamel highlight default");
        var visuals=new List<SpriteRenderer>();
        for(int row=0;row<8;row++)for(int col=0;col<12;col++)
        {
            var sprite=row==7 ? PlayerProjectileArt.For((PlayerDelivery)(col%6)) : HostileProjectileArt.Sprite(kinds[col]);
            var sr=new GameObject("Approved projectile "+row+" / "+col).AddComponent<SpriteRenderer>();
            sr.sprite=sprite;sr.sharedMaterial=material;sr.sortingLayerName="Bullet";
            sr.transform.position=new Vector3((col-5.5f)*1.52f,5.9f-row*1.7f,0);
            sr.transform.localScale=Vector3.one*.8f;
            var block=new MaterialPropertyBlock();block.SetColor("_SpriteTint",row==7 ? new Color(.46f,.9f,1) : LevelProjectilePalette.Accent((LevelId)row));sr.SetPropertyBlock(block);visuals.Add(sr);
        }
        Texture2D Capture(float gloss,string name)
        {
            material.SetFloat("_Gloss",gloss);camera.Render();RenderTexture.active=target;
            var texture=new Texture2D(640,480,TextureFormat.RGBA32,false);texture.ReadPixels(new Rect(0,0,640,480),0,0);texture.Apply();RenderTexture.active=null;
            File.WriteAllBytes(Folder+"/"+name+".png",texture.EncodeToPNG());return texture;
        }
        var before=Capture(0,"Before");var after=Capture(.7f,"After");
        int changed=0;var a=before.GetPixels32();var b=after.GetPixels32();
        for(int i=0;i<a.Length;i++)if(!a[i].Equals(b[i]))changed++;
        Check(changed>100,"Rendered visible highlights on the original sprites across all seven palettes and player fire ("+changed+" affected pixels)");
        Check(material.passCount==1,"Highlights use one texture sample and the existing single render pass");
        UnityEngine.Object.DestroyImmediate(before);UnityEngine.Object.DestroyImmediate(after);
    }
}
#endif
