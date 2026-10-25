#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class VerifyProjectileContacts
{
    public const string Folder="ArtSource/CombatBalanceReview";
    public static void RunEditorOnly()
    {
        try{RunChecks();EditorApplication.Exit(0);}catch(Exception e){File.WriteAllText(Folder+"/ContactFailure.txt",e.ToString());EditorApplication.Exit(1);}
    }
    public static void RunChecks()
    {
        Directory.CreateDirectory(Folder);EditorSceneManager.OpenScene("Assets/Scenes/L1.unity");GameRules.ResetRun();VerifyRunProgression.Run();
        var ship=UnityEngine.Object.FindAnyObjectByType<Shipcontroller>();var polygon=ship.GetComponent<PolygonCollider2D>();
        var circleObject=new GameObject("Blocking companion reference");var circle=circleObject.AddComponent<CircleCollider2D>();circle.radius=.1f;
        var random=new System.Random(29117);int checkedCases=0,mismatches=0;float mismatchDepth=0;
        var kinds=new[]{typeof(SimpleRound),typeof(NeedleRound),typeof(BurstShell),typeof(RicochetBolt),typeof(SuspendedRound),typeof(GuidedMissile),typeof(PlasmaOrb),typeof(ProximityMine),typeof(ReturningDisc),typeof(ScatterCarrier),typeof(LaserSideRound),typeof(HelixRound)};
        foreach(var kind in kinds)
        {
            var obj=new GameObject("Exact authored pixel reference");var body=obj.AddComponent<Rigidbody2D>();body.bodyType=RigidbodyType2D.Kinematic;
            var exact=obj.AddComponent<CustomCollider2D>();exact.isTrigger=true;var sprite=HostileProjectileArt.Sprite(kind);ProjectileSprites.Fit(exact,sprite);
            for(int i=0;i<400;i++)
            {
                obj.transform.SetPositionAndRotation(new Vector3((float)random.NextDouble()*.5f-.25f,(float)random.NextDouble()*.5f-.25f,0),Quaternion.Euler(0,0,(float)random.NextDouble()*360));
                obj.transform.localScale=Vector3.one*(.22f+(float)random.NextDouble()*.9f);
                Collider2D target=i%2==0 ? polygon : circle;
                target.transform.position=new Vector3((float)random.NextDouble()*1.4f-.7f,(float)random.NextDouble()*1.4f-.7f,0);
                Physics2D.SyncTransforms();var distance=Physics2D.Distance(exact,target);
                bool native=distance.isOverlapped,analytic=ProjectilePixelContact.Overlap(obj.transform,ProjectileSprites.Boxes(sprite),target);
                if(native!=analytic && Mathf.Abs(distance.distance)>.0001f)
                {
                    mismatches++;mismatchDepth=Mathf.Max(mismatchDepth,Mathf.Abs(distance.distance));
                    if(mismatches==1)
                    {
                    var bulletShapes=new PhysicsShapeGroup2D();var targetShapes=new PhysicsShapeGroup2D();exact.GetShapes(bulletShapes);target.GetShapes(targetShapes);
                    File.WriteAllText(Folder+"/ContactPose.txt","bullet="+obj.transform.position+" scale="+obj.transform.localScale+" rotation="+obj.transform.eulerAngles+" bounds="+exact.bounds+
                        "\ntarget="+target.transform.position+" scale="+target.transform.localScale+" offset="+target.offset+" bounds="+target.bounds+
                        "\nbody position="+body.position+" rotation="+body.rotation+" distance points="+distance.pointA+" / "+distance.pointB+
                        "\nNative radii: "+bulletShapes.GetShape(0).radius+" / "+targetShapes.GetShape(0).radius+
                        "\nPolygon local="+string.Join(";",polygon.GetPath(0)));
                    }
                }
                checkedCases++;
            }
            UnityEngine.Object.DestroyImmediate(obj);
        }
        UnityEngine.Object.DestroyImmediate(circleObject);
        if(mismatches>0)throw new Exception(mismatches+" native/analytic mismatches; maximum separation/penetration="+mismatchDepth);
        File.WriteAllText(Folder+"/Contacts.txt","PASS\n"+checkedCases+" rotated/scaled cases against the original native pixel fixtures, using the production ship hexagon and a blocking companion circle.\nAll 400 complete five-district route loops passed; Plains is rejected after either third district.\n");
        RenderCoatings();
    }
    static void RenderCoatings()
    {
        var scene=EditorSceneManager.NewPreviewScene();var previous=RenderTexture.active;
        var target=new RenderTexture(720,480,24,RenderTextureFormat.ARGBHalf);target.Create();var output=new RenderTexture(720,480,0);output.Create();
        var composite=new Material(Shader.Find("Shootemup/PSX Composite"));var glow=new ProjectileGlow(720,480);
        try
        {
            var go=new GameObject("Coating review camera");SceneManager.MoveGameObjectToScene(go,scene);var camera=go.AddComponent<UnityEngine.Camera>();camera.scene=scene;
            camera.orthographic=true;camera.orthographicSize=4;camera.transform.position=new Vector3(0,0,-40);camera.targetTexture=target;camera.allowHDR=true;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.025f,.035f,.055f);
            for(int row=0;row<4;row++)
            {
                bool fire=(row&1)!=0,poison=(row&2)!=0;float y=2.9f-row*1.8f;
                var line=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Bullets/LineDrawer"));SceneManager.MoveGameObjectToScene(line,scene);
                var drawer=line.GetComponent<LineDrawer>();
                typeof(LineDrawer).GetMethod("Awake",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(drawer,null);
                drawer.SetCoatings(fire,poison);
                drawer.DrawLine(new[]{new Vector2(-4.7f,y),new Vector2(-3.5f,y+.35f),new Vector2(-2.2f,y-.2f),new Vector2(-.8f,y+.2f)},0,100,true);
                for(int col=0;col<5;col++)
                {
                    var obj=new GameObject("Coated player round");SceneManager.MoveGameObjectToScene(obj,scene);obj.transform.position=new Vector3(.5f+col*.85f,y,0);obj.transform.localScale=Vector3.one*.55f;
                    var sr=obj.AddComponent<SpriteRenderer>();sr.sprite=PlayerProjectileArt.For((PlayerDelivery)col);sr.sharedMaterial=CombatVisuals.ProjectileMaterial;sr.sortingLayerName="Bullet";
                    var properties=new MaterialPropertyBlock();properties.SetColor("_SpriteTint",new Color(.32f,.72f,.85f));properties.SetFloat("_Neutralize",1);properties.SetFloat("_Burn",fire ? 1 : 0);properties.SetFloat("_Poison",poison ? 1 : 0);properties.SetColor("_PoisonTint",PlayerShotPalette.Poison);properties.SetFloat("_Emission",1.15f);sr.SetPropertyBlock(properties);
                }
            }
            camera.Render();glow.Render(target);composite.SetTexture("_GlowTex",glow.Texture);Graphics.Blit(target,output,composite);RenderTexture.active=output;
            var image=new Texture2D(720,480,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,720,480),0,0);image.Apply();File.WriteAllBytes(Folder+"/Coatings.png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);
            if(ShaderUtil.ShaderHasError(CombatVisuals.ProjectileMaterial.shader))throw new Exception("Projectile shader compilation failed");
        }
        finally
        {
            RenderTexture.active=previous;EditorSceneManager.ClosePreviewScene(scene);glow.Dispose();target.Release();output.Release();
            UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(output);UnityEngine.Object.DestroyImmediate(composite);
        }
    }
}
#endif
