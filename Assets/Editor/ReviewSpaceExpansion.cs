#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ReviewSpaceExpansion
{
    public const string Folder="ArtSource/SpaceExpansionReview";
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    public static void Run()
    {
        Directory.CreateDirectory(Folder);
        try
        {
            EditorSceneManager.OpenScene("Assets/Scenes/L1.unity");GameRules.ResetRun();VerifyRunProgression.Run();
            var sequence=DistrictWaves.Sequence();
            Check(sequence.Count(s=>int.TryParse(s,out _) || s=="miniboss")==4 && sequence.Count(s=>s=="encounterChoice")==1,"District length must be four combat waves and one decision");
            Check(Enumerable.Range(Array.IndexOf(sequence,"encounterChoice")+1,3).All(i=>int.TryParse(sequence[i],out _) || sequence[i]=="miniboss"),"No-hit challenge needs three remaining waves");
            var notes=new List<string>{"Four combat waves instead of ten; one decision; no-hit challenge has three remaining waves.","Route checks passed: Desert/Candyland -> Space -> harder Plains, keeping the run seed; third district offers only Desert and Candyland."};
            foreach(int seed in new[]{17381,421,9826})
            {
                LevelRun.Reset(LevelId.Space,seed);
                for(int budget=2;budget<=20;budget++)
                {
                    var wave=DistrictWaves.Create(budget);Check(wave.units.Count>0 && wave.units.All(u=>DistrictWaves.EnemyType(u.enemy)!=null),"Invalid Space composition "+seed+" / "+budget);
                }
                notes.Add("Seed "+seed+": all 19 Space difficulty budgets build with the production swept-spacing scheduler.");
            }
            foreach(var part in WavePartLibrary.Space)
            {
                bool found=false;for(int variant=0;variant<=12 && !found;variant++)foreach(int side in new[]{-1,1})
                    if(WaveComposer.PreviewPart(part,variant,side,out _)!=null){found=true;break;}
                Check(found,"Unplayable orbital motif: "+part.Name);
            }
            foreach(int hull in new[]{41,42,43,44,45})Check(IndustrialMeshFactory.CombatHull(hull).vertices.All(p=>float.IsFinite(p.x+p.y+p.z)),"Invalid orbital model");
            Check(Resources.Load<Shader>("SpaceSky")!=null,"Missing starfield shader");
            Check(Resources.Load<AudioClip>("Music/"+BGMManager.LevelTrack(LevelId.Space))!=null,"Missing Space music mapping");
            Render("Satellites",false,false);Render("Station-Shielded",true,false);Render("Station-Exposed",true,true);
            Check(!ShaderUtil.ShaderHasError(Resources.Load<Shader>("SpaceSky")),"Space sky shader failed native compilation");
            File.WriteAllText(Folder+"/Editor.txt","PASS\n"+string.Join("\n",notes)+"\nAll ten orbital motifs are playable; native satellite and shielded/exposed boss models rendered with the game filter.\n");
            EditorApplication.Exit(0);
        }
        catch(Exception e){File.WriteAllText(Folder+"/Editor.txt","FAIL\n"+e);EditorApplication.Exit(1);}
    }
    static void Render(string name,bool boss,bool exposed)
    {
        var scene=EditorSceneManager.NewPreviewScene();var owned=new List<UnityEngine.Object>();
        int width=boss ? 270 : 720,height=480;
        var target=new RenderTexture(width,height,24);var filtered=new RenderTexture(width,height,0);var previous=RenderTexture.active;
        try
        {
            var go=new GameObject("Orbital asset camera");SceneManager.MoveGameObjectToScene(go,scene);var camera=go.AddComponent<UnityEngine.Camera>();camera.scene=scene;
            camera.orthographic=true;camera.orthographicSize=boss ? 8.850766f : 4.4f;camera.aspect=width/(float)height;camera.transform.position=new Vector3(0,0,-40);camera.farClipPlane=300;camera.targetTexture=target;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
            var skyMesh=new Mesh();skyMesh.vertices=new[]{new Vector3(-15,-24,125),new Vector3(-15,24,125),new Vector3(15,24,125),new Vector3(15,-24,125)};skyMesh.triangles=new[]{0,1,2,0,2,3};skyMesh.RecalculateBounds();owned.Add(skyMesh);
            var sky=new Material(Resources.Load<Shader>("SpaceSky"));owned.Add(sky);sky.SetFloat("_Seed",17381%997);sky.SetVector("_Drift",new Vector4(0,1,4,0));Add(skyMesh,sky,Vector3.zero,1,-80);
            if(boss)
            {
                var station=(SpaceCitadelBoss)DistrictEnemy.Spawn(typeof(SpaceCitadelBoss),new Vector3(0,3.6f,1));SceneManager.MoveGameObjectToScene(station.gameObject,scene);
                typeof(SpaceCitadelBoss).GetMethod("Start",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(station,null);
                if(exposed)
                {
                    for(int i=0;i<2;i++){station.Parts[i].health=0;foreach(var renderer in station.Parts[i].GetComponentsInChildren<MeshRenderer>())renderer.enabled=false;}
                    station.PartChanged();
                    foreach(var shutter in station.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("Retracting reactor shutter")))
                        shutter.localPosition=new Vector3(shutter.localScale.x>0 ? -.72f : .72f,.12f,0);
                }
            }
            else
            {
                for(int i=0;i<4;i++)Add(IndustrialMeshFactory.CombatHull(41+i),SpaceMaterials.Fleet,new Vector3((i-1.5f)*2.7f,.3f,1),.86f,0);
            }
            var composite=new Material(Shader.Find("Shootemup/PSX Composite"));owned.Add(composite);
            Shader.SetGlobalVector("_PSXSnapResolution",new Vector4(width,height,0,0));camera.Render();Graphics.Blit(target,filtered,composite);RenderTexture.active=filtered;
            var image=new Texture2D(width,height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();File.WriteAllBytes(Folder+"/"+name+".png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);
            void Add(Mesh mesh,Material material,Vector3 position,float scale,int order)
            {
                var obj=new GameObject(mesh.name);SceneManager.MoveGameObjectToScene(obj,scene);obj.transform.position=position;obj.transform.localScale=Vector3.one*scale;
                obj.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=obj.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.sortingLayerName="Background";renderer.sortingOrder=order;
            }
        }
        finally
        {
            RenderTexture.active=previous;EditorSceneManager.ClosePreviewScene(scene);target.Release();filtered.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(filtered);
            foreach(var asset in owned)UnityEngine.Object.DestroyImmediate(asset);
        }
    }
}
#endif
