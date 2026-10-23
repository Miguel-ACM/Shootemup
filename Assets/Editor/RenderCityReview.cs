#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

// Isolated architecture sheets use the runtime meshes and haze material without starting a run.
[InitializeOnLoad]
public static class RenderCityReview
{
    [Serializable] sealed class Kit { public MeshData[] meshes; }
    [Serializable] sealed class MeshData { public string name;public float[] vertices,colors;public int[] triangles; }
    const string Request="Temp/render-city.request",Folder="ArtSource/CombatReview/CityArchitecture";
    static readonly int[] Seeds={170,421,9826};
    static RenderCityReview(){EditorApplication.update+=Tick;}
    static void Tick()
    {
        if(EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(Request))return;
        try{File.Delete(Request);}catch(IOException){return;}
        var ground=new Dictionary<int,Mesh>();
        Material material=null;
        try
        {
            Directory.CreateDirectory(Folder);
            var source=JsonUtility.FromJson<Kit>(Resources.Load<TextAsset>("LevelKit").text);
            foreach(var item in source.meshes)
                if(item.name.StartsWith("CityGround"))ground.Add(int.Parse(item.name.Substring("CityGround".Length)),CreateMesh(item));
            material=new Material(Resources.Load<Shader>("LevelScenery"));material.SetFloat("_Haze",.38f);
            Sheet("Sections.png",ground,material,400,500,10,4,false);
            Sheet("CombatScale.png",ground,material,300,500,8.1f,3,true);
            File.WriteAllText(Folder+"/Readme.txt","Rows: seeds 170, 421, 9826. Sections columns: 0, 1, 2, 3. CombatScale columns: section 0 at X=-1.6, section 1 at X=0, section 2 at X=1.6.\nRuntime architecture, ground, scale and 38% aerial haze; isolated editor rendering without the final game filter or moving fog. CombatScale includes actual small City airframes for contrast review.\n");
            File.WriteAllText("Temp/render-city.result","PASS "+DateTime.Now.ToString("O")+"; 12 section views and 9 combat-scale views.");
        }
        catch(Exception ex){File.WriteAllText("Temp/render-city.result","FAIL "+ex);Debug.LogException(ex);}
        finally
        {
            foreach(var mesh in ground.Values)UnityEngine.Object.DestroyImmediate(mesh);
            if(material!=null)UnityEngine.Object.DestroyImmediate(material);
        }
    }
    static Mesh CreateMesh(MeshData item)
    {
        int count=item.vertices.Length/3;var vertices=new Vector3[count];var colors=new Color[count];
        for(int i=0;i<count;i++)
        {
            vertices[i]=new Vector3(item.vertices[i*3],item.vertices[i*3+1],item.vertices[i*3+2]);
            colors[i]=new Color(item.colors[i*4],item.colors[i*4+1],item.colors[i*4+2],item.colors[i*4+3]);
        }
        var mesh=new Mesh{name=item.name,indexFormat=IndexFormat.UInt32};
        mesh.vertices=vertices;mesh.colors=colors;mesh.triangles=item.triangles;mesh.RecalculateBounds();return mesh;
    }
    static void Sheet(string name,Dictionary<int,Mesh> ground,Material material,int width,int height,float view,int columns,bool fleet)
    {
        var sheet=new Texture2D(width*columns,height*Seeds.Length,TextureFormat.RGB24,false);
        try
        {
            for(int row=0;row<Seeds.Length;row++)for(int column=0;column<columns;column++)
            {
                var image=Render(Seeds[row],column,ground[column],material,width,height,view,fleet ? (column-1)*1.6f : 0,fleet);
                try{sheet.SetPixels(column*width,(Seeds.Length-1-row)*height,width,height,image.GetPixels());}
                finally{UnityEngine.Object.DestroyImmediate(image);}
            }
            sheet.Apply();File.WriteAllBytes(Folder+"/"+name,sheet.EncodeToPNG());
        }
        finally{UnityEngine.Object.DestroyImmediate(sheet);}
    }
    static Texture2D Render(int seed,int section,Mesh ground,Material material,int width,int height,float view,float x,bool fleet)
    {
        var scene=EditorSceneManager.NewPreviewScene();
        RenderTexture target=null;UnityEngine.Camera camera=null;Mesh architecture=null;
        try
        {
            var cameraObject=new GameObject("City studio camera");SceneManager.MoveGameObjectToScene(cameraObject,scene);
            camera=cameraObject.AddComponent<UnityEngine.Camera>();camera.scene=scene;
            camera.transform.position=new Vector3(x,0,-20);camera.orthographic=true;camera.orthographicSize=view;camera.aspect=width/(float)height;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.18f,.23f,.26f);
            camera.nearClipPlane=.1f;camera.farClipPlane=50;
            target=new RenderTexture(width,height,24);camera.targetTexture=target;
            var floor=new GameObject("City ground");SceneManager.MoveGameObjectToScene(floor,scene);
            floor.transform.localScale=new Vector3(.8f,1,1);floor.AddComponent<MeshFilter>().sharedMesh=ground;
            var floorRenderer=floor.AddComponent<MeshRenderer>();floorRenderer.sharedMaterial=material;floorRenderer.sortingLayerName="Background";floorRenderer.sortingOrder=-50;
            var blocks=new GameObject("Seeded architecture");SceneManager.MoveGameObjectToScene(blocks,scene);
            blocks.transform.localScale=new Vector3(.8f,1,1);architecture=CityDistrictGeometry.Build(seed,section);
            blocks.AddComponent<MeshFilter>().sharedMesh=architecture;
            var renderer=blocks.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.sortingLayerName="Background";renderer.sortingOrder=-40;
            if(fleet)
            {
                var kinds=new[]{12,13,14,15,13};var sizes=new[]{.45f,.38f,.8f,.34f,.38f};
                for(int i=0;i<5;i++)
                {
                    var ship=new GameObject("City silhouette "+i);SceneManager.MoveGameObjectToScene(ship,scene);
                    ship.transform.position=new Vector3(x+(i%2==0 ? -2.1f : 2.1f),5.4f-i*2.6f,-8);
                    ship.transform.localScale=Vector3.one*sizes[i];
                    ship.AddComponent<MeshFilter>().sharedMesh=IndustrialMeshFactory.CombatHull(kinds[i]);
                    var hull=ship.AddComponent<MeshRenderer>();hull.sharedMaterial=CombatMaterials.Fleet;hull.sortingOrder=10;
                }
            }
            camera.Render();
            var previous=RenderTexture.active;
            try
            {
                RenderTexture.active=target;var pixels=new Texture2D(width,height,TextureFormat.RGB24,false);
                pixels.ReadPixels(new Rect(0,0,width,height),0,0);pixels.Apply();return pixels;
            }
            finally{RenderTexture.active=previous;}
        }
        finally
        {
            if(camera!=null)camera.targetTexture=null;
            if(RenderTexture.active==target)RenderTexture.active=null;
            if(target!=null){target.Release();UnityEngine.Object.DestroyImmediate(target);}
            EditorSceneManager.ClosePreviewScene(scene);
            if(architecture!=null)UnityEngine.Object.DestroyImmediate(architecture);
        }
    }
}
#endif
