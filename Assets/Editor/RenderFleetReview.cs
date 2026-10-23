#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// An isolated studio scene renders the real runtime meshes without opening a live run.
[InitializeOnLoad]
public static class RenderFleetReview
{
    const string Request="Temp/render-fleet.request";
    static RenderFleetReview(){EditorApplication.update+=Tick;}
    static void Tick()
    {
        if(EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(Request))return;
        File.Delete(Request);
        try
        {
            string folder="ArtSource/CombatReview/ModelPolish";Directory.CreateDirectory(folder);
            var kinds=new[]{0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,17,18,20,21,22,23,24,25,26,27,28};
            Sheet(folder+"/FleetFront.png",kinds,Vector3.zero,6);
            Sheet(folder+"/FleetAngle.png",kinds,new Vector3(-22,24,0),6);
            Sheet(folder+"/SnowClose.png",new[]{6,7,8,17,21,26},new Vector3(-18,22,0),3);
            Sheet(folder+"/ThunderClose.png",new[]{9,10,11,18,22,27},new Vector3(-18,22,0),3);
            File.WriteAllText(folder+"/HullIds.txt","Row order: "+string.Join(", ",kinds)+"\nSnow: 6,7,8,17,21,26\nThunder: 9,10,11,18,22,27\n");
            File.WriteAllText("Temp/render-fleet.result","PASS "+DateTime.Now.ToString("O"));
        }
        catch(Exception ex){File.WriteAllText("Temp/render-fleet.result","FAIL "+ex);Debug.LogException(ex);}
    }
    static void Sheet(string path,int[] kinds,Vector3 angle,int columns)
    {
        var scene=EditorSceneManager.NewPreviewScene();
        RenderTexture target=null;Texture2D pixels=null;
        try
        {
            int rows=Mathf.CeilToInt(kinds.Length/(float)columns);
            var cameraObject=new GameObject("Fleet studio camera");SceneManager.MoveGameObjectToScene(cameraObject,scene);
            var camera=cameraObject.AddComponent<UnityEngine.Camera>();camera.scene=scene;
            camera.transform.position=new Vector3(0,0,-20);camera.orthographic=true;
            camera.orthographicSize=rows*1.9f;camera.aspect=columns/(float)rows;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.025f,.035f,.045f);
            camera.nearClipPlane=.1f;camera.farClipPlane=40;
            target=new RenderTexture(columns*320,rows*320,24);camera.targetTexture=target;
            for(int i=0;i<kinds.Length;i++)
            {
                var model=new GameObject("Hull "+kinds[i]);SceneManager.MoveGameObjectToScene(model,scene);
                model.transform.position=new Vector3((i%columns-(columns-1)*.5f)*3.8f,((rows-1)*.5f-i/columns)*3.8f,0);
                model.transform.rotation=Quaternion.Euler(angle);
                model.AddComponent<MeshFilter>().sharedMesh=IndustrialMeshFactory.CombatHull(kinds[i]);
                model.AddComponent<MeshRenderer>().sharedMaterial=CombatMaterials.Fleet;
                var label=new GameObject("Hull ID");SceneManager.MoveGameObjectToScene(label,scene);
                label.transform.position=model.transform.position+new Vector3(0,-1.62f,-1);
                var text=label.AddComponent<TMPro.TextMeshPro>();text.text=kinds[i].ToString("00");text.fontSize=2.5f;
                text.alignment=TMPro.TextAlignmentOptions.Center;text.color=new Color(.65f,.75f,.8f);text.rectTransform.sizeDelta=new Vector2(2,.45f);
            }
            camera.Render();
            var previous=RenderTexture.active;RenderTexture.active=target;
            pixels=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
            pixels.ReadPixels(new Rect(0,0,target.width,target.height),0,0);pixels.Apply();RenderTexture.active=previous;
            File.WriteAllBytes(path,pixels.EncodeToPNG());
        }
        finally
        {
            if(pixels!=null)UnityEngine.Object.DestroyImmediate(pixels);
            if(target!=null){target.Release();UnityEngine.Object.DestroyImmediate(target);}
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }
}
#endif
