#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

/// <summary>Temporary play-mode visual review and recycling check, added by ReviewPSXValley.</summary>
public sealed class ValleyReviewProbe : MonoBehaviour
{
    IEnumerator Start()
    {
        yield return new WaitForSecondsRealtime(.6f);
        Time.timeScale=1;
        var field=FindAnyObjectByType<PSXGrassField>();
        var sections=field.GetComponentsInChildren<MeshFilter>().Where(m=>m.name.StartsWith("Valley section")).ToArray();
        bool meshes=sections.Length==4 && sections.All(m=>m.sharedMesh!=null && m.sharedMesh.vertexCount>0);
        bool finite=sections.All(m=>m.sharedMesh.vertices.All(v=>!float.IsNaN(v.x+v.y+v.z)&&!float.IsInfinity(v.x+v.y+v.z)));
        bool noCollisions=field.GetComponentsInChildren<Collider2D>().Length==0 && field.GetComponentsInChildren<Collider>().Length==0;
        var landscape=Resources.Load<Material>("PSXValley");
        bool shader=landscape!=null && landscape.shader!=null && landscape.shader.isSupported && !UnityEditor.ShaderUtil.ShaderHasError(landscape.shader);
        var layout=new PSXValleyLayout(LevelRun.Seed);
        bool seams=Mathf.Abs(layout.Centre(0)-layout.Centre(96))<.0001f &&
            Mathf.Abs(layout.HalfWidth(0)-layout.HalfWidth(96))<.0001f;
        yield return new WaitForEndOfFrame();
        Capture("PSXValley_Bridge.png");
        yield return new WaitForSecondsRealtime(2.5f);
        yield return new WaitForEndOfFrame();Capture("PSXValley_River.png");
        // Cross the section and full-loop boundaries without a long unattended play session.
        var travel=typeof(PSXGrassField).GetField("travel",BindingFlags.Instance|BindingFlags.NonPublic);
        travel.SetValue(field,95.9f);
        yield return new WaitForSecondsRealtime(.25f);
        var ys=sections.Select(m=>m.transform.localPosition.y).OrderBy(y=>y).ToArray();
        bool spacing=ys.Length==4;
        for(int i=1;i<ys.Length;i++)spacing&=Mathf.Abs(ys[i]-ys[i-1]-24)<.001f;
        bool wrapped=(float)travel.GetValue(field)<4;
        yield return new WaitForEndOfFrame();Capture("PSXValley_Wrap.png");
        string result="meshes="+meshes+", finite="+finite+", decorativeOnly="+noCollisions+", shader="+shader+", riverSeam="+seams+", sectionSpacing="+spacing+", fullLoopWrap="+wrapped;
        File.WriteAllText("Temp/ValleyReviewResult.txt",(meshes&&finite&&noCollisions&&shader&&seams&&spacing&&wrapped ? "PASS " : "FAIL ")+result);
        Debug.Log("VALLEY REVIEW "+result);
    }
    static void Capture(string name)
    {
        var image=ScreenCapture.CaptureScreenshotAsTexture();
        Directory.CreateDirectory("ArtSource/PSXv2/ValleyReview");
        File.WriteAllBytes("ArtSource/PSXv2/ValleyReview/"+name,image.EncodeToPNG());Destroy(image);
    }
}
#endif
