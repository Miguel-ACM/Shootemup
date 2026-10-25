#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

/// <summary>Unsaved L1 render regression: real assets, damage paths and shop completion.</summary>
public sealed class RenderingRegressionProbe : MonoBehaviour
{
    readonly Dictionary<string,bool> checks=new Dictionary<string,bool>();
    IEnumerator Start()
    {
        var history=new CircularArray<int>(2,7);history.ChangeSize(4,9);
        bool initialHistory=history.Get(0)==7 && history.Get(3)==9;
        history.Push(11);history.Push(12);history.ChangeSize(2,0);
        checks["followerHistoryResizesBeforeAndAfterMovement"]=initialHistory && history.Get(0)==12 && history.Get(1)==11;
        yield return new WaitForSecondsRealtime(.6f);Time.timeScale=1;
        var waves=FindAnyObjectByType<WaveOrchestrer>();waves.enabled=false;waves.CancelInvoke();waves.StopAllCoroutines();
        foreach(var enemy in FindObjectsByType<AEnemy>())Destroy(enemy.gameObject);
        foreach(var item in FindObjectsByType<AItem>())Destroy(item.gameObject);
        var player=GameObject.FindGameObjectWithTag("Player").GetComponent<Shipcontroller>();player.enabled=false;player.transform.position=new Vector3(0,-5,20);
        var boss=Instantiate(Resources.Load<GameObject>("Enemy/Bosses/stage1boss"),new Vector3(0,4.5f,1),Quaternion.identity);
        foreach(var script in boss.GetComponentsInChildren<MonoBehaviour>())if(!(script is PSXObjectVisual) && !(script is PSXVisibilityRelay))script.enabled=false;
        foreach(var body in boss.GetComponentsInChildren<Rigidbody2D>())body.simulated=false;
        foreach(var collider in boss.GetComponentsInChildren<Collider2D>())collider.enabled=false;
        player.AddCompanion("BabyYou");player.AddCompanion("Twerp");
        yield return null;
        foreach(var companion in FindObjectsByType<ACompanion>())
        {
            foreach(var mover in companion.GetComponents<AMover>())mover.enabled=false;
            bool baby=companion.name.StartsWith("Baby");companion.transform.position=new Vector3(baby ? -1.3f : 1.3f,-3.3f,19);
        }
        var moon=MakeItem<AMoonItem>(new Vector3(-2,1,1),"epic");
        var ship=MakeItem<BabyYouItem>(new Vector3(0,1,1),"rare");
        var twerp=MakeItem<TwerpItem>(new Vector3(2,1,1),"legendary");
        // Prevent decorative choices from collecting while photographing their aura.
        moon.enabled=ship.enabled=twerp.enabled=false;
        yield return new WaitForSecondsRealtime(.5f);yield return new WaitForEndOfFrame();
        var camera=UnityEngine.Camera.main;
        checks["cameraRetreatSurvivesShaker"]=camera.transform.position.z< -30;
        checks["bossFullyInsideDepthRange"]=VisibleBounds(camera,boss.GetComponent<PSXObjectVisual>().modelRenderer);
        checks["playerRendered"]=VisibleBounds(camera,player.GetComponentInChildren<MeshRenderer>());
        foreach(var companion in FindObjectsByType<ACompanion>())
        {
            var renderer=companion.GetComponent<PSXObjectVisual>().modelRenderer;
            checks[companion.name.StartsWith("Baby") ? "babyYouReadable" : "twerpReadable"]=VisibleBounds(camera,renderer) && renderer.bounds.size.x>.35f;
        }
        checks["itemModelsVisible"]=VisibleBounds(camera,ship.GetComponent<PSXObjectVisual>().modelRenderer) && VisibleBounds(camera,twerp.GetComponent<PSXObjectVisual>().modelRenderer);
        var aura=moon.transform.Find("Rarity aura").GetComponent<ParticleSystem>();
        checks["softAuraEmitting"]=aura!=null && aura.isPlaying && aura.particleCount>10 && aura.GetComponent<ParticleSystemRenderer>().sharedMaterial.shader.name=="Shootemup/PSX Aura";
        Capture("RenderFix_ModelsAura.png");
        var beam=Instantiate(Resources.Load<GameObject>("Bullets/LineDrawer"));
        beam.GetComponent<LineDrawer>().DrawLine(new[]{new Vector3(-2,-1,19),new Vector3(2,-1,19)},.8f);
        yield return new WaitForEndOfFrame();
        var line=beam.GetComponent<LineRenderer>();checks["laserVisible"]=line.positionCount==2 && line.sharedMaterial.shader.name=="Shootemup/PSX Exhaust" && line.isVisible;
        Capture("RenderFix_Laser.png");
        Destroy(beam);
        var target=Instantiate(Resources.Load<GameObject>("Enemy/enemy1"),new Vector3(0,-.1f,1),Quaternion.identity);
        yield return null;target.GetComponent<AEnemy>().health=1000;
        foreach(var shooter in target.GetComponents<AShooter>()){shooter.CancelInvoke();shooter.enabled=false;}
        foreach(var mover in target.GetComponents<AMover>())mover.enabled=false;
        var electric=Instantiate(Resources.Load<GameObject>("Bullets/PlayerElectricityBullet"),new Vector3(0,-2.5f,19),Quaternion.identity);
        float health=target.GetComponent<AEnemy>().health;
        yield return null;yield return new WaitForEndOfFrame();
        bool electricity=false;
        foreach(var drawer in FindObjectsByType<LineDrawer>())if(drawer.GetComponent<LineRenderer>().positionCount>2 && drawer.GetComponent<LineRenderer>().isVisible)electricity=true;
        checks["electricityVisibleAndDealsDamage"]=electricity && target.GetComponent<AEnemy>().health<health;
        Capture("RenderFix_Electricity.png");
        yield return new WaitForSecondsRealtime(.6f);
        target.GetComponent<AEnemy>().HitAt(target.transform.position,Vector2.right);target.GetComponent<AEnemy>().damage(.1f);
        var shared=GameObject.Find("PSX shared impacts").GetComponent<ParticleSystem>();
        var particles=new ParticleSystem.Particle[512];int count=shared.GetParticles(particles);bool forward=count>0;
        for(int i=0;i<count;i++)if(Vector2.Dot(particles[i].velocity.normalized,Vector2.right)<.75f)forward=false;
        checks["hitSparksFollowIncomingDirection"]=forward;
        yield return new WaitForEndOfFrame();Capture("RenderFix_DirectionalHit.png");
        float originalScale=moon.GetComponent<PSXObjectVisual>().model.localScale.x;
        moon.Dismiss();ship.Dismiss();twerp.Dismiss();
        yield return new WaitForSecondsRealtime(.18f);
        checks["unchosenItemsAnimateBeforeRemoval"]=moon!=null && moon.IsDismissing && moon.GetComponent<PSXObjectVisual>().model.localScale.x<originalScale*.9f;
        yield return new WaitForEndOfFrame();Capture("RenderFix_Dismiss.png");
        yield return new WaitForSecondsRealtime(.5f);checks["unchosenItemsFinishRemoval"]=moon==null && ship==null && twerp==null;
        // Check the actual shop completion path, including one reward and the other option's fade.
        var chosen=MakeItem<Caffeine>(player.transform.position+new Vector3(1.8f,1,0),"legendary");
        var other=MakeItem<AMoonItem>(player.transform.position+new Vector3(-2.8f,1,0),"common");
        var flags=BindingFlags.Instance|BindingFlags.NonPublic;
        typeof(WaveOrchestrer).GetField("items",flags).SetValue(waves,new List<GameObject>{chosen.gameObject,other.gameObject});
        chosen.Register(waves);other.Register(waves);float firing=GameRules.playerShootSpeed;
        float timeout=Time.realtimeSinceStartup+2;
        while(chosen!=null && Time.realtimeSinceStartup<timeout)yield return null;
        checks["shopRejectsOtherOptionsGracefully"]=chosen==null && other!=null && other.IsDismissing;
        yield return new WaitForSecondsRealtime(.65f);
        checks["shopAwardsExactlyOnce"]=other==null && Mathf.Approximately(GameRules.playerShootSpeed,firing+5);
        waves.CancelInvoke();waves.StopAllCoroutines();
        var entries=new List<string>();bool pass=true;
        foreach(var check in checks){entries.Add("\""+check.Key+"\":"+(check.Value ? "true" : "false"));pass&=check.Value;}
        string report="{\"passed\":"+(pass ? "true" : "false")+","+string.Join(",",entries)+"}";
        File.WriteAllText("ArtSource/PSXPreview/RenderingAudit.json",report);Debug.Log("Rendering audit: "+report);
        File.WriteAllText("ArtSource/PSXPreview/open-gameplay.request","Normal L1 after rendering regressions");UnityEditor.EditorApplication.isPlaying=false;
    }
    static T MakeItem<T>(Vector3 position,string rarity) where T:AItem
    {
        var root=Instantiate(Resources.Load<GameObject>("Items/Item"),position,Quaternion.identity);
        foreach(var collider in root.GetComponents<Collider2D>())collider.enabled=false;
        var item=root.AddComponent<T>();item.SetRarity(rarity);return item;
    }
    static bool VisibleBounds(UnityEngine.Camera camera,Renderer renderer)
    {
        if(renderer==null || !renderer.enabled || !renderer.isVisible)return false;
        var bounds=renderer.bounds;
        for(int i=0;i<8;i++)
        {
            var p=bounds.center+Vector3.Scale(bounds.extents,new Vector3((i&1)==0 ? -1 : 1,(i&2)==0 ? -1 : 1,(i&4)==0 ? -1 : 1));
            float z=camera.transform.InverseTransformPoint(p).z;if(z<camera.nearClipPlane || z>camera.farClipPlane)return false;
        }
        return true;
    }
    static void Capture(string name)
    {
        var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes("ArtSource/PSXPreview/"+name,image.EncodeToPNG());Destroy(image);
    }
}
#endif
