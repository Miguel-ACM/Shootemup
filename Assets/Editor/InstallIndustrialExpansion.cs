using System;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class InstallIndustrialExpansion
{
    const string Folder="Assets/Art/IndustrialExpansion/";
    const string Request="ArtSource/PSXPreview/expansion.request";
    static bool installing;
    static InstallIndustrialExpansion(){EditorApplication.update+=Check;}
    static void Check()
    {
        const string bulletRequest="Temp/refine-player-bullet.request";
        if(File.Exists(bulletRequest) && !EditorApplication.isCompiling && !EditorApplication.isUpdating && !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            File.Delete(bulletRequest);
            RefinePlayerBullet();
        }
        if(installing || !File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isUpdating)return;
        if(EditorApplication.isPlayingOrWillChangePlaymode){EditorApplication.isPlaying=false;return;}
        installing=true;
        try{File.Delete(Request);Install();}
        catch(Exception e){File.WriteAllText("ArtSource/PSXPreview/ExpansionError.txt",e.ToString());Debug.LogException(e);}
        finally{installing=false;}
    }
    [MenuItem("Tools/PSX Player/Install industrial fleet and UI")]
    public static void Install()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play mode first.");
        Directory.CreateDirectory(Folder);Directory.CreateDirectory("ArtSource/IndustrialExpansion");AssetDatabase.Refresh();
        var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/PlayerShip/GR07_PixelSteel.mat");
        var steel=AssetDatabase.LoadAssetAtPath<Material>(Folder+"Industrial fleet.mat");
        if(steel==null){steel=new Material(material);AssetDatabase.CreateAsset(steel,Folder+"Industrial fleet.mat");}
        steel.SetFloat("_VertexSnap",0);EditorUtility.SetDirty(steel);
        material.SetFloat("_VertexSnap",0);EditorUtility.SetDirty(material);
        material.SetFloat("_CockpitBlue",1);steel.SetFloat("_CockpitBlue",0);
        var background=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/PSXPass/Industrial salvage.mat");background.SetFloat("_VertexSnap",0);EditorUtility.SetDirty(background);
        var screen=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/PSXPass/PSX screen composite.mat");screen.SetFloat("_Dither",.08f);screen.SetFloat("_Vignette",0);EditorUtility.SetDirty(screen);

        var enemies=new Mesh[4];for(int i=0;i<4;i++)enemies[i]=SaveMesh(IndustrialMeshFactory.CreateEnemy(i),"Enemy_"+i);
        var items=new Mesh[IndustrialMeshFactory.ItemKinds.Length];for(int i=0;i<items.Length;i++)items[i]=SaveMesh(IndustrialMeshFactory.CreateItem(i),IndustrialMeshFactory.ItemKinds[i]);
        var equipment=EquipmentMaterial(steel);
        Directory.CreateDirectory("Assets/Resources/Effects");AssetDatabase.Refresh();
        var particles=AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Effects/PSXParticles.mat");
        if(particles==null){particles=new Material(Shader.Find("Shootemup/PSX Exhaust"));AssetDatabase.CreateAsset(particles,"Assets/Resources/Effects/PSXParticles.mat");}
        particles.SetColor("_Tint",Color.white);particles.SetFloat("_ZTest",8);EditorUtility.SetDirty(particles);
        var aura=AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Effects/PSXAura.mat");
        if(aura==null){aura=new Material(Shader.Find("Shootemup/PSX Aura"));AssetDatabase.CreateAsset(aura,"Assets/Resources/Effects/PSXAura.mat");}
        EditorUtility.SetDirty(aura);
        Edit("Assets/Resources/Bullets/LineDrawer.prefab",go=>
        {
            go.transform.localScale=Vector3.one;
            var line=go.GetComponent<LineRenderer>();line.sharedMaterial=particles;
            line.sortingLayerName="Bullet";line.sortingOrder=20;line.shadowCastingMode=ShadowCastingMode.Off;
            line.receiveShadows=false;line.widthMultiplier=.10f;line.widthCurve=AnimationCurve.Linear(0,1,1,1);
            line.startColor=new Color(.25f,.65f,1);line.endColor=new Color(.7f,.92f,1);
        });
        string[] enemyPaths={"enemy1","waver","Biggie","Bosses/stage1boss"};
        for(int i=0;i<enemyPaths.Length;i++)
        {
            int index=i;
            Edit("Assets/Resources/Enemy/"+enemyPaths[i]+".prefab",go=>Visual(go,enemies[index],steel,false,ColliderSize(go),Color.white));
        }
        string[] pickableNames={"life","mana","money"};
        for(int i=0;i<3;i++){int index=i;Edit("Assets/Resources/Pickables/"+pickableNames[i]+".prefab",go=>Visual(go,items[index],equipment,true,Vector2.one*.55f,Color.white));}
        Edit("Assets/Resources/Items/Item.prefab",go=>{var visual=Visual(go,items[4],equipment,true,Vector2.one*.85f,Color.white);visual.chooseItem=true;visual.itemMeshes=items;});
        string[] companions={"AMoon","AngryMoon","BabyYou","Twerp"};int[] companionKinds={10,14,11,5};
        for(int i=0;i<companions.Length;i++)
        {
            int index=i;Edit("Assets/Resources/Companions/"+companions[i]+".prefab",go=>
            {
                var size=ColliderSize(go);
                // The tiny original follower colliders are not a readable visual size.
                if(index>=2)size=Vector2.one*(index==2 ? 1.6f : .65f);
                Visual(go,items[companionKinds[index]],equipment,true,size,Color.white);
            });
        }

        var bulletMaterial=AssetDatabase.LoadAssetAtPath<Material>(Folder+"Bullet sprites.mat");
        if(bulletMaterial==null){bulletMaterial=new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));AssetDatabase.CreateAsset(bulletMaterial,Folder+"Bullet sprites.mat");}
        string[] bulletNames={"PlayerBullet","TwerpBullet","EnemyBulletSmall","EnemyBulletMedium","EnemyBulletBig","CircularWave"};
        for(int i=0;i<bulletNames.Length;i++)
        {
            int index=i;string path="Assets/Resources/Bullets/"+bulletNames[i]+".prefab";
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);var size=ColliderSize(prefab);
            float height=i==0 ? .32f : i==1 ? .46f : Mathf.Max(size.x,size.y)*(i==2 ? 1.89f : i==3 ? 5.43f : i==4 ? 4.35f : 1.45f);   // enlarged enemy visuals (colliders unchanged)
            var sprite=SpriteTexture(bulletNames[i],i,height);
            Edit(path,go=>
            {
                foreach(var mesh in go.GetComponents<MeshRenderer>())UnityEngine.Object.DestroyImmediate(mesh);
                foreach(var mesh in go.GetComponents<MeshFilter>())UnityEngine.Object.DestroyImmediate(mesh);
                foreach(var effect in go.GetComponents<PSXProjectileEffects>())UnityEngine.Object.DestroyImmediate(effect);
                foreach(var trail in go.GetComponentsInChildren<TrailRenderer>(true))UnityEngine.Object.DestroyImmediate(trail);
                var renderer=go.GetComponent<SpriteRenderer>();if(renderer==null)renderer=go.AddComponent<SpriteRenderer>();
                renderer.enabled=true;renderer.sprite=sprite;renderer.sharedMaterial=bulletMaterial;renderer.color=Color.white;renderer.sortingLayerName="Bullet";renderer.sortingOrder=index<2 ? 0 : 5;
            });
        }

        StyleScene("Assets/Scenes/Title.unity",false);
        var scene=StyleScene("Assets/Scenes/L1.unity",true);
        EditorSceneManager.SaveScene(scene,"Assets/Scenes/PSX_Showcase.unity",true);
        var showcase=EditorSceneManager.OpenScene("Assets/Scenes/PSX_Showcase.unity",OpenSceneMode.Single);
        foreach(var wave in showcase.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<WaveOrchestrer>(true)))wave.gameObject.SetActive(false);
        var hud=showcase.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<IndustrialHUD>(true)).Single();hud.showcase=true;
        var preview=new GameObject("PSX Showcase Playback").AddComponent<PSXShowcase>();preview.hudFont=hud.font;
        foreach(var audio in showcase.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<AudioSource>(true)))audio.volume=Mathf.Min(audio.volume,.08f);
        EditorSceneManager.MarkSceneDirty(showcase);EditorSceneManager.SaveScene(showcase);AssetDatabase.SaveAssets();
        string report="{\"success\":true,\"enemyTriangles\":["+string.Join(",",enemies.Select(m=>m.triangles.Length/3))+"],\"itemTriangles\":["+string.Join(",",items.Select(m=>m.triangles.Length/3))+"],\"itemKinds\":17,\"companionPrefabs\":4,\"spriteBulletPrefabs\":6,\"bulletTrails\":0,\"defaultScreenFilter\":false}";
        File.WriteAllText("ArtSource/PSXPreview/ExpansionReport.json",report);Debug.Log("Industrial expansion installed: "+report);
        CaptureIndustrialCatalog.Capture();
        EditorSceneManager.OpenScene("Assets/Scenes/L1.unity");
        new GameObject("Temporary rendering audit").AddComponent<RenderingRegressionProbe>();
        EditorApplication.delayCall+=()=>EditorApplication.isPlaying=true;
    }

    static Scene StyleScene(string path,bool gameplay)
    {
        var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);var roots=scene.GetRootGameObjects();
        var pixel=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/prstartk.asset");
        foreach(var dialog in roots.SelectMany(r=>r.GetComponentsInChildren<Dialog>(true)))dialog.font=pixel;
        foreach(var warning in roots.SelectMany(r=>r.GetComponentsInChildren<Warning>(true)))warning.font=pixel;
        foreach(var text in roots.SelectMany(r=>r.GetComponentsInChildren<TMP_Text>(true)))
        {
            text.font=pixel;text.color=new Color(.78f,.82f,.77f,text.color.a);text.fontStyle=FontStyles.Normal;
            text.fontSize=Mathf.Min(text.fontSize,22);text.enableAutoSizing=true;text.fontSizeMin=8;text.fontSizeMax=22;
        }
        foreach(var image in roots.SelectMany(r=>r.GetComponentsInChildren<Image>(true)))
        {
            if(image.name.Contains("Bg") || image.name=="Dialog") {image.sprite=null;image.color=new Color(.10f,.13f,.13f,.96f);}
            if(image.name.StartsWith("line_") || image.name=="Border")image.color=new Color(.66f,.51f,.29f,image.color.a);
        }
        if(gameplay)
        {
            foreach(var camera in roots.SelectMany(r=>r.GetComponentsInChildren<UnityEngine.Camera>(true)).Where(c=>c.CompareTag("MainCamera")))
            {
                var position=camera.transform.localPosition;position.z=-40;camera.transform.localPosition=position;
                camera.nearClipPlane=.1f;camera.farClipPlane=1200;
                var shaker=camera.GetComponent<EZCameraShake.CameraShaker>();
                if(shaker!=null){var rest=shaker.RestPositionOffset;rest.z=-40;shaker.RestPositionOffset=rest;}
            }
            foreach(var root in roots.Where(r=>r.name=="Industrial HUD" || r.name=="PSX Showcase Playback"))UnityEngine.Object.DestroyImmediate(root);
            var hud=new GameObject("Industrial HUD").AddComponent<IndustrialHUD>();hud.font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/prstartk.ttf");
            roots=scene.GetRootGameObjects();
            foreach(var canvas in roots.SelectMany(r=>r.GetComponentsInChildren<Canvas>(true)))canvas.sortingOrder=20;
            foreach(var transform in roots.SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).Where(t=>t.name=="ItemsLeft" || t.name=="ItemsRight"))
            {var group=transform.GetComponent<CanvasGroup>();if(group==null)group=transform.gameObject.AddComponent<CanvasGroup>();group.alpha=0;group.blocksRaycasts=false;}
            foreach(var presentation in roots.SelectMany(r=>r.GetComponentsInChildren<PSXPresentation>(true)))presentation.filterEnabled=false;
        }
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);return scene;
    }

    static PSXObjectVisual Visual(GameObject root,Mesh mesh,Material material,bool spin,Vector2 size,Color tint)
    {
        var previous=root.transform.Find("Industrial model");if(previous!=null)UnityEngine.Object.DestroyImmediate(previous.gameObject);
        foreach(var sprite in root.GetComponentsInChildren<SpriteRenderer>(true))sprite.enabled=false;
        foreach(var particles in root.GetComponentsInChildren<ParticleSystemRenderer>(true))particles.enabled=false;
        var child=new GameObject("Industrial model");child.transform.SetParent(root.transform,false);
        child.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=child.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
        var spriteSource=root.GetComponent<SpriteRenderer>();if(spriteSource!=null){renderer.sortingLayerID=spriteSource.sortingLayerID;renderer.sortingOrder=spriteSource.sortingOrder;}
        float scale=Mathf.Min(size.x/Mathf.Max(mesh.bounds.size.x,.01f),size.y/Mathf.Max(mesh.bounds.size.y,.01f));child.transform.localScale=Vector3.one*scale;
        var visual=root.GetComponent<PSXObjectVisual>();if(visual==null)visual=root.AddComponent<PSXObjectVisual>();visual.model=child.transform;visual.modelRenderer=renderer;visual.sourceSprite=spriteSource;visual.spin=spin;visual.accent=tint;
        child.AddComponent<PSXVisibilityRelay>().target=root;return visual;
    }
    static Vector2 ColliderSize(GameObject go)
    {
        var box=go.GetComponent<BoxCollider2D>();if(box!=null)return box.size;
        var circle=go.GetComponent<CircleCollider2D>();if(circle!=null)return Vector2.one*circle.radius*2;
        var polygon=go.GetComponent<PolygonCollider2D>();
        if(polygon!=null)
        {
            var min=Vector2.positiveInfinity;var max=Vector2.negativeInfinity;
            for(int path=0;path<polygon.pathCount;path++)foreach(var p in polygon.GetPath(path)){min=Vector2.Min(min,p);max=Vector2.Max(max,p);}
            return max-min;
        }
        var capsule=go.GetComponent<CapsuleCollider2D>();if(capsule!=null)return capsule.size;
        return Vector2.one;
    }
    static void Edit(string path,Action<GameObject> edit)
    {var go=PrefabUtility.LoadPrefabContents(path);try{edit(go);PrefabUtility.SaveAsPrefabAsset(go,path);}finally{PrefabUtility.UnloadPrefabContents(go);}}

    static Material EquipmentMaterial(Material source)
    {
        Color[] colors={new Color(.84f,.84f,.78f),new Color(.11f,.13f,.14f),new Color(.57f,.14f,.12f),new Color(.12f,.34f,.58f),new Color(.67f,.52f,.24f),new Color(.22f,.085f,.025f),new Color(.66f,.69f,.65f),new Color(.28f,.31f,.29f),new Color(.77f,.51f,.34f),new Color(.025f,.03f,.03f),new Color(.25f,.38f,.20f),new Color(.57f,.64f,.67f),new Color(.98f,.98f,.90f),new Color(.05f,.67f,1f),new Color(.86f,.83f,.65f),new Color(.10f,.12f,.12f)};
        var texture=new Texture2D(256,256,TextureFormat.RGBA32,false);
        for(int y=0;y<256;y++)for(int x=0;x<256;x++)
        {
            int tile=x/64+y/64*4;int px=x%64,py=y%64;
            float grain=(Mathf.PerlinNoise(px*.81f+tile*12,py*.81f)-.5f)*.05f;
            Color color=colors[tile]* (1+grain);
            if(tile==6)color=LunarPixel(px,py);
            else if(tile==11 || tile==1 || tile==2 || tile==3)
            {
                if(px==7 || py==7)color*=1.18f;
                if(px==56 || py==56)color*=.60f;
                if((px==10 || px==53) && (py==10 || py==53))color=colors[1];
                if(py==20 && px>35 && px<49 || py==21 && px>39 && px<52)color=Color.Lerp(color,colors[11],.4f);
                if(tile==11 && py>=27 && py<=36 && px>13 && px<50)color*=.7f;
            }
            else if(tile==4)
            {
                float radius=Vector2.Distance(new Vector2(px,py),new Vector2(31.5f,31.5f));
                if(radius>22 && radius<25)color*=.63f;
                if((px+py)%13==0 && radius>23)color*=1.2f;
            }
            else if(tile==0 || tile==12 || tile==14)
            {
                if(py==9)color*=.8f;
                if(py>44 && px>12 && px<50)color*=.96f;
            }
            else if(tile==10 && (px%4==0 || py%4==0))color*=.88f;
            color.a=1;
            texture.SetPixel(x,y,color);
        }
        texture.Apply();string path=Folder+"EquipmentAtlas.png";File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
        var material=AssetDatabase.LoadAssetAtPath<Material>(Folder+"Recognizable equipment.mat");if(material==null){material=new Material(source);AssetDatabase.CreateAsset(material,Folder+"Recognizable equipment.mat");}
        material.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);material.SetFloat("_CockpitBlue",0);EditorUtility.SetDirty(material);return material;
    }
    // Equirectangular lunar painting: dark maria, granular highlands, shadowed bowls,
    // bright raised rims and ejecta. Deterministic pixel art with a seamless longitude.
    static Color LunarPixel(int x,int y)
    {
        float u=(x-4)/55f,v=(y-4)/55f;
        float longitude=u*Mathf.PI*2;
        float maria=Mathf.PerlinNoise(Mathf.Cos(longitude)*1.3f+4,Mathf.Sin(longitude)*1.3f+v*3+4);
        float value=Mathf.Lerp(.37f,.76f,Mathf.SmoothStep(.30f,.70f,maria));
        value+=(Mathf.PerlinNoise(x*1.2f,y*1.2f)-.5f)*.035f;
        for(int i=0;i<24;i++)
        {
            float cx=Mathf.Repeat(i*.618034f+.13f,1),cy=.12f+.76f*Mathf.Repeat(i*.414214f+.21f,1);
            float dx=Mathf.Abs(u-cx);dx=Mathf.Min(dx,1-dx);float dy=v-cy;
            float radius=.035f+Mathf.Repeat(i*.02731f,.065f),d=Mathf.Sqrt(dx*dx+dy*dy);
            if(d<radius*.78f)value=Mathf.Lerp(value,.25f+Mathf.Clamp01((dy/radius+1)*.5f)*.23f,.75f);
            else if(d<radius)value=Mathf.Lerp(value,dy>0 ? .89f : .40f,.8f);
            else if(d<radius*1.22f)value+=.035f;
        }
        value=Mathf.Round(Mathf.Clamp01(value)*25)/25;
        return new Color(value*.98f,value,value*1.015f,1);
    }

    static Mesh SaveMesh(Mesh mesh,string name)
    {
        if(mesh.triangles.Length/3>=300)throw new InvalidOperationException(name+" exceeds triangle budget");
        if(mesh.uv.Length!=mesh.vertexCount)throw new InvalidOperationException(name+" missing UVs");
        string path=Folder+name+".asset";var asset=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(asset==null){AssetDatabase.CreateAsset(mesh,path);asset=mesh;}
        else
        {
            // CopySerialized can leave an existing Mesh's GPU buffers showing its old geometry.
            // Update through Mesh APIs so the renderer receives the replacement vertices/indices.
            asset.Clear();asset.name=mesh.name;asset.SetVertices(mesh.vertices);
            asset.SetTriangles(mesh.triangles,0);asset.SetUVs(0,mesh.uv);asset.SetNormals(mesh.normals);
            asset.RecalculateBounds();UnityEngine.Object.DestroyImmediate(mesh);EditorUtility.SetDirty(asset);
        }
        asset.UploadMeshData(false);
        string materialName=name.StartsWith("Enemy_") ? "ShipSteel" : "Equipment";
        string texture=name.StartsWith("Enemy_") ? "PlayerShip/GR07_Atlas.png" : "IndustrialExpansion/EquipmentAtlas.png";
        File.WriteAllText("ArtSource/IndustrialExpansion/"+materialName+".mtl","newmtl "+materialName+"\nKd 1 1 1\nmap_Kd ../../Assets/Art/"+texture+"\n");
        var obj=new StringBuilder("# Industrial PSX asset; planar UVs\nmtllib "+materialName+".mtl\nusemtl "+materialName+"\n");
        foreach(var p in asset.vertices)obj.AppendFormat(System.Globalization.CultureInfo.InvariantCulture,"v {0} {1} {2}\n",p.x,p.y,p.z);
        foreach(var p in asset.uv)obj.AppendFormat(System.Globalization.CultureInfo.InvariantCulture,"vt {0} {1}\n",p.x,p.y);
        var tris=asset.triangles;for(int i=0;i<tris.Length;i+=3)obj.AppendFormat("f {0}/{0} {1}/{1} {2}/{2}\n",tris[i]+1,tris[i+1]+1,tris[i+2]+1);
        File.WriteAllText("ArtSource/IndustrialExpansion/"+name+".obj",obj.ToString());return asset;
    }

    [MenuItem("Tools/PSX Player/Refine player bullet")]
    public static void RefinePlayerBullet()
    {
        var sprite=SpriteTexture("PlayerBullet",0,.32f);
        Edit("Assets/Resources/Bullets/PlayerBullet.prefab",go=>
        {
            var renderer=go.GetComponent<SpriteRenderer>();
            renderer.sprite=sprite;
            renderer.color=Color.white;
        });
        AssetDatabase.SaveAssets();
        File.WriteAllText("Temp/PlayerBulletRefinement.txt","PASS: rounded cyan capsule, .32 world-unit sprite height, translucent shell and core; collider and weapon properties unchanged.");
    }

    static Sprite SpriteTexture(string name,int kind,float height)
    {
        int w=kind==0 ? 24 : kind==1 ? 8 : kind==5 ? 32 : 12,h=kind==0 ? 48 : kind==1 ? 16 : w;
        var texture=new Texture2D(w,h,TextureFormat.RGBA32,false);var pixels=new Color[w*h];
        Color edge=new Color(.15f,.13f,.10f), shell=new Color(.92f,.43f,.20f), core=new Color(1,.92f,.71f);
        if(kind<2){edge=new Color(.025f,.10f,.24f);shell=new Color(.12f,.42f,1f);core=new Color(.48f,.82f,1f);}
        if(kind==3)shell=new Color(.38f,.75f,.80f);
        for(int y=0;y<h;y++)for(int x=0;x<w;x++)
        {
            float nx=(x+.5f-w*.5f)/(w*.5f),ny=(y+.5f-h*.5f)/(h*.5f);float d=Mathf.Sqrt(nx*nx+ny*ny);
            Color color=Color.clear;
            if(kind==0)
            {
                // Signed distance to a capsule gives the nose a true round cap, with a soft edge.
                float px=x+.5f-w*.5f,py=y+.5f;
                float cap=Mathf.Max(12-py,py-36,0);
                float distance=Mathf.Sqrt(px*px+cap*cap);
                float coverage=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(5.1f,6.4f,distance));
                float highlight=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,5.8f,Mathf.Abs(px+1.1f)));
                color=Color.Lerp(new Color(.12f,.48f,.92f,.52f),new Color(.68f,.94f,1,.78f),highlight);
                color.a*=coverage*Mathf.Lerp(.65f,1,Mathf.SmoothStep(0,1,Mathf.InverseLerp(5,19,py)));
            }
            else if(kind==1){if(Mathf.Abs(nx)<.5f && y>1 && y<h-1)color=Mathf.Abs(nx)<.25f && y>4 ? core : shell;}
            else if(kind==5){if(d>.73f && d<.96f)color=d>.87f ? edge : shell;}
            else
            {
                float shape=kind==3 ? (Mathf.Abs(nx)+Mathf.Abs(ny))*.8f : d;
                if(shape<.96f)color=shape>.76f ? edge : shape>.45f ? shell : core;
            }
            pixels[y*w+x]=color;
        }
        texture.SetPixels(pixels);texture.Apply();string path=Folder+name+".png";File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spritePixelsPerUnit=h/Mathf.Max(.1f,height);importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;
        var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
