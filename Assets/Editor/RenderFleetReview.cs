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
    public static void PlainsBoss()
    {
        const string folder="ArtSource/CombatReview/PlayerFeedback";
        Directory.CreateDirectory(folder);
        Sheet(folder+"/BossFront.png",new[]{23},Vector3.zero,1,640,4.1f);
        Sheet(folder+"/BossAngle.png",new[]{23},new Vector3(-22,24,0),1,640,4.1f);
        Sheet(folder+"/BossGameplayScale.png",new[]{23},Vector3.zero,1,320,11.34f,true);
    }
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
            Sheet(folder+"/FleetGameplayScale.png",kinds,new Vector3(0,12,0),6,160,5.67f,true);
            Sheet(folder+"/SnowClose.png",new[]{6,7,8,17,21,26},new Vector3(-18,22,0),3);
            Sheet(folder+"/ThunderClose.png",new[]{9,10,11,18,22,27},new Vector3(-18,22,0),3);
            File.WriteAllText(folder+"/HullIds.txt","Row order: "+string.Join(", ",kinds)+"\nSnow: 6,7,8,17,21,26\nThunder: 9,10,11,18,22,27\n");
            File.WriteAllText("Temp/render-fleet.result","PASS "+DateTime.Now.ToString("O"));
        }
        catch(Exception ex){File.WriteAllText("Temp/render-fleet.result","FAIL "+ex);Debug.LogException(ex);}
    }
    static float GameplayScale(int kind)
    {
        switch(kind)
        {
            case 0:return .48f;case 1:return .7f;case 2:return .9f;case 3:return .55f;case 4:return .5f;
            case 5:return .9f;case 6:return .62f;case 7:return .9f;case 8:return .95f;case 9:return .55f;
            case 10:return .55f;case 11:return .55f;case 12:return .45f;case 13:return .38f;case 14:return .8f;
            case 15:return .34f;case 17:return .85f;case 18:return .62f;case 20:return 1.25f;case 21:return 1.6f;
            case 22:return 1.25f;case 23:return 1.4f;default:return 1.1f;
        }
    }
    static void Sheet(string path,int[] kinds,Vector3 angle,int columns,int cellPixels=320,float cellWorld=3.8f,bool gameplayScale=false)
    {
        var scene=EditorSceneManager.NewPreviewScene();
        RenderTexture target=null;Texture2D pixels=null;UnityEngine.Camera camera=null;
        try
        {
            int rows=Mathf.CeilToInt(kinds.Length/(float)columns);
            var cameraObject=new GameObject("Fleet studio camera");SceneManager.MoveGameObjectToScene(cameraObject,scene);
            camera=cameraObject.AddComponent<UnityEngine.Camera>();camera.scene=scene;
            camera.transform.position=new Vector3(0,0,-20);camera.orthographic=true;
            camera.orthographicSize=rows*cellWorld*.5f;camera.aspect=columns/(float)rows;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.025f,.035f,.045f);
            camera.nearClipPlane=.1f;camera.farClipPlane=40;
            target=new RenderTexture(columns*cellPixels,rows*cellPixels,24);camera.targetTexture=target;
            for(int i=0;i<kinds.Length;i++)
            {
                var model=new GameObject("Hull "+kinds[i]);SceneManager.MoveGameObjectToScene(model,scene);
                model.transform.position=new Vector3((i%columns-(columns-1)*.5f)*cellWorld,((rows-1)*.5f-i/columns)*cellWorld,0);
                model.transform.rotation=Quaternion.Euler(angle);
                if(gameplayScale)model.transform.localScale=Vector3.one*GameplayScale(kinds[i]);
                model.AddComponent<MeshFilter>().sharedMesh=IndustrialMeshFactory.CombatHull(kinds[i]);
                model.AddComponent<MeshRenderer>().sharedMaterial=kinds[i]==23 ? CombatMaterials.Fortress : CombatMaterials.Fleet;
                var label=new GameObject("Hull ID");SceneManager.MoveGameObjectToScene(label,scene);
                label.transform.position=model.transform.position+new Vector3(0,-cellWorld*.43f,-1);
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
            if(camera!=null)camera.targetTexture=null;
            if(RenderTexture.active==target)RenderTexture.active=null;
            if(pixels!=null)UnityEngine.Object.DestroyImmediate(pixels);
            if(target!=null){target.Release();UnityEngine.Object.DestroyImmediate(target);}
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }
}
#endif
