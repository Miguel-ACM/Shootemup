using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[InitializeOnLoad]
public static class CaptureIndustrialCatalog
{
    static CaptureIndustrialCatalog(){EditorApplication.delayCall+=Check;}
    static void Check()
    {
        const string request="ArtSource/IndustrialExpansion/catalog.request";
        if(!File.Exists(request))return;
        try{Capture();File.Delete(request);}catch(Exception e){Debug.LogException(e);File.WriteAllText("ArtSource/IndustrialExpansion/CatalogError.txt",e.ToString());}
    }
    [MenuItem("Tools/PSX Player/Capture industrial model catalog")]
    public static void Capture()
    {
        const int cell=256,columns=5,rows=5;
        var sheet=new Texture2D(cell*columns,cell*rows,TextureFormat.RGB24,false);
        var fill=Enumerable.Repeat(new Color(.055f,.07f,.075f),cell*columns*cell*rows).ToArray();sheet.SetPixels(fill);
        var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/IndustrialExpansion/Industrial fleet.mat");
        var names=new[]{"Enemy_0","Enemy_1","Enemy_2","Enemy_3"}.Concat(IndustrialMeshFactory.ItemKinds).ToArray();
        var cameraObject=new GameObject("Catalog camera"){hideFlags=HideFlags.HideAndDontSave};var camera=cameraObject.AddComponent<UnityEngine.Camera>();
        camera.transform.position=new Vector3(1000,1000,-10);camera.orthographic=true;camera.orthographicSize=.85f;camera.aspect=1;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.075f,.09f,.095f);camera.cullingMask=1<<31;camera.nearClipPlane=.1f;camera.farClipPlane=30;camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
        var model=new GameObject("Catalog model"){hideFlags=HideFlags.HideAndDontSave,layer=31};model.transform.position=new Vector3(1000,1000,0);model.transform.rotation=Quaternion.Euler(-18,-25,0);
        var filter=model.AddComponent<MeshFilter>();model.AddComponent<MeshRenderer>().sharedMaterial=material;
        var target=new RenderTexture(cell,cell,24,RenderTextureFormat.ARGB32);var old=RenderTexture.active;
        int invalidUV=0,invalidGeometry=0;
        try
        {
            for(int index=0;index<names.Length;index++)
            {
                var mesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Art/IndustrialExpansion/"+names[index]+".asset");filter.sharedMesh=mesh;
                model.GetComponent<MeshRenderer>().sharedMaterial=index<4 ? material : AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/IndustrialExpansion/Recognizable equipment.mat");
                model.transform.localScale=Vector3.one*(1.25f/Mathf.Max(mesh.bounds.size.x,mesh.bounds.size.y));
                RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});
                RenderTexture.active=target;var tile=new Texture2D(cell,cell,TextureFormat.RGB24,false);tile.ReadPixels(new Rect(0,0,cell,cell),0,0);tile.Apply();
                File.WriteAllBytes("ArtSource/IndustrialExpansion/"+names[index]+".png",tile.EncodeToPNG());sheet.SetPixels(index%columns*cell,(rows-1-index/columns)*cell,cell,cell,tile.GetPixels());UnityEngine.Object.DestroyImmediate(tile);
                var vertices=mesh.vertices;var uv=mesh.uv;var triangles=mesh.triangles;
                for(int i=0;i<triangles.Length;i+=3)
                {
                    int a=triangles[i],b=triangles[i+1],c=triangles[i+2];
                    if(Vector3.Cross(vertices[b]-vertices[a],vertices[c]-vertices[a]).sqrMagnitude<1e-12f)invalidGeometry++;
                    var d=uv[b]-uv[a];var e=uv[c]-uv[a];if(Mathf.Abs(d.x*e.y-d.y*e.x)<1e-9f)invalidUV++;
                }
            }
            sheet.Apply();File.WriteAllBytes("ArtSource/IndustrialExpansion/Catalog.png",sheet.EncodeToPNG());
            File.WriteAllText("ArtSource/IndustrialExpansion/GeometryAudit.json","{\"models\":"+names.Length+",\"collapsedTriangles\":"+invalidGeometry+",\"collapsedUVTriangles\":"+invalidUV+"}");
            Debug.Log("Industrial catalog captured; models="+names.Length+", collapsed geometry="+invalidGeometry+", collapsed UV="+invalidUV);
        }
        finally{RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(sheet);UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(model);UnityEngine.Object.DestroyImmediate(cameraObject);}
    }
}
