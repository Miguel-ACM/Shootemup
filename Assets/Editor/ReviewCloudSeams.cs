using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Run in an isolated review project: renders production UI without starting gameplay.
public static class ReviewCloudSeams
{
    const string Folder="ArtSource/CloudSeamReview";
    public static void Run()
    {
        Directory.CreateDirectory(Folder);
        try
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var camera=new GameObject("Cloud review camera").AddComponent<UnityEngine.Camera>();
            camera.orthographic=true;camera.orthographicSize=360;camera.nearClipPlane=.01f;camera.farClipPlane=100;
            camera.transform.position=new Vector3(0,0,-10);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.035f,.015f,.08f);
            var root=new GameObject("Cloud canvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));
            var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
            var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=1;
            var mask=new GameObject("Cloud playfield mask",typeof(RectTransform),typeof(RectMask2D));mask.transform.SetParent(root.transform,false);
            var clip=mask.GetComponent<RectTransform>();clip.anchorMin=new Vector2(.5f,0);clip.anchorMax=new Vector2(.5f,1);clip.sizeDelta=new Vector2(405,0);
            var artwork=Resources.Load<Texture2D>("CloudBillow");File.WriteAllBytes(Folder+"/Imported.png",artwork.EncodeToPNG());
            var clouds=new CloudCurtainGraphic[4];
            for(int i=0;i<4;i++)
            {
                var go=new GameObject("Cloud bank "+i,typeof(RectTransform),typeof(CanvasRenderer),typeof(CloudCurtainGraphic));go.transform.SetParent(clip,false);
                var rect=go.GetComponent<RectTransform>();rect.sizeDelta=new Vector2(650,1160);
                clouds[i]=go.GetComponent<CloudCurtainGraphic>();clouds[i].color=Color.Lerp(new Color(.88f,.92f,.95f),Color.white,i/3f);clouds[i].Configure(artwork,i);
            }
            foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(640,480),new Vector2Int(1366,768)})
            {
                var target=new RenderTexture(size.x,size.y,24);camera.targetTexture=target;
                foreach(float y in new[]{500.3f,220.7f,0,-220.3f,-400.25f,-650.5f,-960})
                {
                    for(int i=0;i<4;i++){var p=y+i*36;clouds[i].rectTransform.anchoredPosition=new Vector2(0,p);clouds[i].SetTravel(p,0,777.4f-y*1.11f);}
                    Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;
                    var image=new Texture2D(size.x,size.y,TextureFormat.RGBA32,false);image.ReadPixels(new Rect(0,0,size.x,size.y),0,0);image.Apply();RenderTexture.active=null;
                    File.WriteAllBytes(Folder+"/Cloud-"+size.x+"-"+y.ToString("F2",System.Globalization.CultureInfo.InvariantCulture)+".png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);
                }
                camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(target);
            }
            File.WriteAllText(Folder+"/Verification.txt","PASS: editor-only native cloud renders at three resolutions, including fractional scales and moving cover/reveal poses. No Play mode.");
            EditorApplication.Exit(0);
        }
        catch(Exception e){File.WriteAllText(Folder+"/Verification.txt",e.ToString());EditorApplication.Exit(1);}
    }
}
