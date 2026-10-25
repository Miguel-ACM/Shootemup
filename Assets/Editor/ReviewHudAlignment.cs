#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Production HUD layout and blink poses rendered without Play mode.
public static class ReviewHudAlignment
{
    const string Folder="ArtSource/HudAlignmentReview";
    const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
    static readonly System.Collections.Generic.List<string> checks=new System.Collections.Generic.List<string>();
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);checks.Add(message);}
    static T Get<T>(IndustrialHUD hud,string field)=>(T)typeof(IndustrialHUD).GetField(field,Hidden).GetValue(hud);
    static void Set(IndustrialHUD hud,string field,object value)=>typeof(IndustrialHUD).GetField(field,Hidden).SetValue(hud,value);
    public static void Run()
    {
        Directory.CreateDirectory(Folder);
        try
        {
            EditorSceneManager.OpenScene("Assets/Scenes/L1.unity");
            foreach(var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include))canvas.enabled=false;
            GameRules.ResetRun();Score.ResetRun();RunCoins.Reset();
            var hud=UnityEngine.Object.FindFirstObjectByType<IndustrialHUD>();
            if(hud==null)hud=new GameObject("HUD alignment preview").AddComponent<IndustrialHUD>();
            hud.font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/prstartk.ttf");
            typeof(IndustrialHUD).GetMethod("Start",Hidden).Invoke(hud,null);Set(hud,"entranceAge",4f);
            foreach(var group in hud.GetComponentsInChildren<CanvasGroup>())group.alpha=1;
            var ship=UnityEngine.Object.FindFirstObjectByType<Shipcontroller>();ship.maxHealth=100;ship.maxMana=100;
            var go=new GameObject("HUD review camera");var camera=go.AddComponent<UnityEngine.Camera>();
            camera.orthographic=true;camera.orthographicSize=360;camera.nearClipPlane=.01f;camera.farClipPlane=100;
            camera.transform.position=new Vector3(0,0,-10);camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
            var hudCanvas=hud.GetComponentInChildren<Canvas>();hudCanvas.renderMode=RenderMode.ScreenSpaceCamera;hudCanvas.worldCamera=camera;hudCanvas.planeDistance=1;
            foreach(var t in hudCanvas.GetComponentsInChildren<Transform>())t.gameObject.layer=31;
            var backdrop=new GameObject("Gameplay reference",typeof(RectTransform),typeof(Canvas));
            var backgroundCanvas=backdrop.GetComponent<Canvas>();backgroundCanvas.renderMode=RenderMode.ScreenSpaceCamera;backgroundCanvas.worldCamera=camera;backgroundCanvas.planeDistance=2;
            var picture=new GameObject("Backdrop",typeof(RectTransform),typeof(RawImage));picture.transform.SetParent(backdrop.transform,false);
            var rect=picture.GetComponent<RectTransform>();rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            var texture=new Texture2D(2,2);texture.LoadImage(File.ReadAllBytes("ArtSource/SnowLandscapeReview/Gameplay-12.png"));texture.filterMode=FilterMode.Point;picture.GetComponent<RawImage>().texture=texture;
            foreach(var t in backdrop.GetComponentsInChildren<Transform>())t.gameObject.layer=31;
            foreach(var size in new[]{new Vector2Int(640,480),new Vector2Int(1280,720),new Vector2Int(1366,768)})
            {
                var target=new RenderTexture(size.x,size.y,24);camera.targetTexture=target;
                foreach(float health in new[]{100f,20f})foreach(float phase in new[]{.1f,.5f})
                {
                    ship.health=health;ship.mana=health==100 ? 100 : 12;
                    RunCoins.Reset();RunCoins.Add(health==100 ? 37 : 1234);Score.ResetRun();Score.IncreaseScore(health==100 ? 94 : 148);
                    Set(hud,"nextUpdate",-1f);typeof(IndustrialHUD).GetMethod("Update",Hidden).Invoke(hud,null);
                    typeof(IndustrialHUD).GetMethod("UpdateMeterWarning",Hidden).Invoke(hud,new object[]{health/100,phase});
                    typeof(IndustrialHUD).GetMethod("LateUpdate",Hidden).Invoke(hud,null);
                    Get<RectTransform>(hud,"bossPanel").gameObject.SetActive(true);
                    CheckMeters(hud);CheckProgress(hud);
                    bool warning=Get<Image[]>(hud,"meterCasing").All(image=>image.color.r>.9f && image.color.g<.3f);
                    Check(warning==(health<=25 && phase<.4f),"Correct low-health blink state at "+health+"% / "+phase);
                    Canvas.ForceUpdateCanvases();camera.Render();var previous=RenderTexture.active;RenderTexture.active=target;
                    var image=new Texture2D(size.x,size.y,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,size.x,size.y),0,0);image.Apply();RenderTexture.active=previous;
                    string name=size.x+"-"+(health==100 ? "Full" : "Low")+(phase<.4f ? "-On" : "-Off");
                    File.WriteAllBytes(Folder+"/"+name+".png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);
                }
                camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(target);
            }
            typeof(IndustrialHUD).GetMethod("UpdateMeterWarning",Hidden).Invoke(hud,new object[]{.5f,.1f});
            Check(Get<Image[]>(hud,"meterCasing").All(image=>image.color.g>.2f),"Warning clears after healing");
            Set(hud,"entranceAge",1f);typeof(IndustrialHUD).GetMethod("UpdateMeterWarning",Hidden).Invoke(hud,new object[]{.1f,.1f});
            Check(Get<Image[]>(hud,"meterCasing").All(image=>image.color.g>.2f),"Startup flicker remains separate from the low-health warning");
            File.WriteAllText(Folder+"/Verification.txt","PASS\nEditor-only production HUD preview: no Play mode or gameplay run.\n"+string.Join("\n",checks.Distinct()));
            EditorApplication.Exit(0);
        }
        catch(Exception e){File.WriteAllText(Folder+"/Verification.txt","FAIL\n"+e);EditorApplication.Exit(1);}
    }
    static void CheckMeters(IndustrialHUD hud)
    {
        foreach(string name in new[]{"healthCells","manaCells","bossCells"})
        {
            var cells=Get<Image[]>(hud,name);var box=(RectTransform)cells[0].transform.parent;
            foreach(var cell in cells)
            {
                var bounds=RectTransformUtility.CalculateRelativeRectTransformBounds(box,cell.rectTransform);
                Check(bounds.min.x>=box.rect.xMin && bounds.max.x<=box.rect.xMax && bounds.min.y>=box.rect.yMin+11.9f && bounds.max.y<=box.rect.yMax-3,"All "+name+" rows stay inside their casing");
            }
        }
    }
    static void CheckProgress(IndustrialHUD hud)
    {
        var text=Get<Text>(hud,"chain");var fill=Get<RectTransform>(hud,"chainFill");var track=(RectTransform)fill.parent;
        Vector3 middle=track.TransformPoint(track.rect.center),label=text.rectTransform.TransformPoint(text.rectTransform.rect.center);
        Check(Mathf.Abs(middle.x-label.x)<.01f,"Continuous multiplier track is centered below the multiplier number");
        Check(Mathf.Abs(fill.rect.width-track.rect.width*Mathf.Clamp01(Score.GetPercentageNextMultiplier()))<.01f,"Multiplier line preserves exact fractional progress");
        Check(track.GetComponentsInChildren<Image>().Length==2,"Multiplier progress uses one continuous track and fill");
        var coins=Get<Text>(hud,"coins");var icon=(RectTransform)coins.transform.parent.Find("Coin shoulders");
        Check(Mathf.Abs(coins.rectTransform.TransformPoint(coins.rectTransform.rect.center).y-icon.TransformPoint(icon.rect.center).y)<.01f && coins.alignment==TextAnchor.MiddleLeft,"Wallet icon and number share a centered row");
    }
}
#endif
