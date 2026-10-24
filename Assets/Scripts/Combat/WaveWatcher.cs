using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// A build-accessible pattern viewer. Enemies, movement, aim and projectiles are the real combat code.
public sealed class WaveWatcher : MonoBehaviour
{
    public const int FirstWave=2,LastWave=20,Miniboss=21,BossOne=22,BossTwo=23;
    static bool requested;
    public static WaveWatcher Current {get;private set;}
    public LevelId Level=>LevelRun.Current;
    public int Encounter {get;private set;}=FirstWave;
    public bool Paused {get;private set;}
    public bool Looping {get;private set;}=true;
    public float PlaybackSpeed {get;private set;}=1;
    public float Elapsed {get;private set;}
    public bool Ready {get;private set;}
    public int Spawned {get;private set;}
    Shipcontroller ship;
    Coroutine playback;
    Canvas[] hiddenCanvases;
    TMP_FontAsset font;
    TextMeshProUGUI heading,status,pauseLabel,loopLabel,speedLabel;
    readonly Dictionary<LevelId,Image> levels=new Dictionary<LevelId,Image>();
    readonly Dictionary<int,Image> encounters=new Dictionary<int,Image>();
    readonly List<Image> aims=new List<Image>();
    int aim;
    float nextStatus;
    static readonly Color Panel=new Color(.035f,.055f,.06f,1),ButtonColor=new Color(.10f,.15f,.16f,1),Selected=new Color(.52f,.39f,.18f,1);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset(){requested=false;Current=null;SceneManager.sceneLoaded-=SceneReady;}
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Install(){SceneManager.sceneLoaded+=SceneReady;}
    static void SceneReady(Scene scene,LoadSceneMode mode)
    {
        if(scene.name=="Title")
        {
            requested=false;Current=null;
            var start=FindAnyObjectByType<NewGame>();if(start==null)return;
            var menu=start.GetComponentInParent<Menu>();
            var row=new GameObject("Wave Watcher",typeof(RectTransform),typeof(Image));row.transform.SetParent(menu.transform,false);
            var rect=row.GetComponent<RectTransform>();rect.sizeDelta=new Vector2(726,52);rect.anchoredPosition=new Vector2(0,-190);
            row.AddComponent<WaveWatcherMenu>();
            var label=new GameObject("Wave Watcher label",typeof(RectTransform),typeof(TextMeshProUGUI));label.transform.SetParent(row.transform,false);
            label.GetComponent<RectTransform>().sizeDelta=rect.sizeDelta;
            var text=label.GetComponent<TextMeshProUGUI>();text.font=menu.GetComponentInChildren<TextMeshProUGUI>().font;
            text.text="WAVE WATCHER";text.fontSize=24;text.alignment=TextAlignmentOptions.Center;text.raycastTarget=false;
            menu.RefreshElements();
        }
        else if(scene.name=="L1" && requested)
        {
            var flow=FindAnyObjectByType<WaveOrchestrer>();if(flow!=null){flow.enabled=false;flow.CancelInvoke();flow.StopAllCoroutines();}
            Current=new GameObject("Wave watcher controls",typeof(RectTransform)).AddComponent<WaveWatcher>();
        }
    }
    public static void Launch()
    {
        if(CloudTransition.Busy || requested)return;
        requested=true;GameRules.StartNewRun();LevelRun.Reset();BGMManager.PlayLevelMusic(LevelRun.Current);CloudTransition.Load("L1");
    }
    IEnumerator Start()
    {
        yield return null; // Let ship art, instruments, weapons and input finish their normal startup.
        ship=FindAnyObjectByType<Shipcontroller>();
        if(ship==null){Debug.LogError("Wave Watcher requires the gameplay ship");yield break;}
        ship.ObserverMode=true;ship.enabled=false;
        var body=ship.GetComponent<Rigidbody2D>();if(body!=null){body.linearVelocity=Vector2.zero;body.bodyType=RigidbodyType2D.Kinematic;}
        foreach(var weapon in ship.GetComponentsInChildren<AWeapon>()){weapon.StopFiring();weapon.enabled=false;}
        foreach(var menu in FindObjectsByType<Menu>()){menu.Suspended=true;menu.gameObject.SetActive(false);}
        var dialog=FindAnyObjectByType<Dialog>();if(dialog!=null){font=dialog.font;dialog.Hide();}
        var hidden=new List<Canvas>();
        foreach(var canvas in FindObjectsByType<Canvas>())
            if(canvas.name!="PSX pixel display" && canvas.name!="Cloud scene transitions")hidden.Add(canvas);
        hiddenCanvases=hidden.ToArray();
        if(FindAnyObjectByType<TunnelReflectors>()==null)new GameObject("Watcher tunnel rails").AddComponent<TunnelReflectors>();
        Build();SetAim(0);HidePilot();
        while(CloudTransition.Busy)yield return null;
        yield return new WaitForSeconds(WaveOrchestrer.OpeningBreather);
        Ready=true;Replay();
        if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(levels[Level].gameObject);
    }
    void Update()
    {
        if(Ready && ((Keyboard.current?.escapeKey.wasPressedThisFrame ?? false) || (Gamepad.current?.buttonEast.wasPressedThisFrame ?? false)))Exit();
    }
    void LateUpdate()
    {
        if(ship==null)return;
        HidePilot();
        if(hiddenCanvases!=null)foreach(var canvas in hiddenCanvases)if(canvas!=null)canvas.enabled=false;
        Time.timeScale=CloudTransition.Busy ? 1 : Paused ? 0 : PlaybackSpeed;
        if(Time.unscaledTime<nextStatus)return;nextStatus=Time.unscaledTime+.15f;
        status.text=$"{Elapsed:0.0}s\n{AEnemy.Live.Count} ships\n{CombatRound.Live.Count} bullets";
    }
    void HidePilot()
    {
        foreach(var renderer in ship.GetComponentsInChildren<Renderer>())renderer.forceRenderingOff=true;
        foreach(var collider in ship.GetComponentsInChildren<Collider2D>())collider.enabled=false;
    }
    public void SelectLevel(LevelId level)
    {
        if(!Ready || CloudTransition.Busy)return;
        LevelRun.Reset(level);LevelEnvironment.Apply();BGMManager.PlayLevelMusic(level);SetAim(aim);Replay();
    }
    public void SelectEncounter(int encounter)
    {
        if(!Ready || encounter<FirstWave || encounter>BossTwo)return;
        Encounter=encounter;Replay();
    }
    public void Replay()
    {
        if(!Ready)return;
        if(playback!=null)StopCoroutine(playback);
        playback=StartCoroutine(Play());Refresh();
    }
    public void TogglePause(){Paused=!Paused;Refresh();}
    public void ToggleLoop(){Looping=!Looping;Refresh();}
    public void CycleSpeed(){PlaybackSpeed=PlaybackSpeed==1 ? .5f : PlaybackSpeed==.5f ? 2 : 1;Refresh();}
    public void SetAim(int side)
    {
        aim=Mathf.Clamp(side,-1,1);
        if(ship!=null)ship.transform.position=new Vector3(aim*Camera.RightLimit*.58f,Camera.DownLimit+1.8f,-1);
        for(int i=0;i<aims.Count;i++)aims[i].color=i-1==aim ? Selected : ButtonColor;
    }
    public void Exit()
    {
        if(CloudTransition.Busy)return;
        requested=false;Time.timeScale=1;Paused=false;PlaybackSpeed=1;CloudTransition.Load("Title");
    }
    void OnDestroy(){if(Current==this)Current=null;Time.timeScale=1;}
    static void ClearArena()
    {
        foreach(var enemy in FindObjectsByType<AEnemy>()){enemy.enabled=false;Destroy(enemy.gameObject);}
        foreach(var bullet in FindObjectsByType<AEnemyBullet>())bullet.Vanish();
        foreach(var bullet in FindObjectsByType<ABullet>())Destroy(bullet.gameObject);
        foreach(var pickup in FindObjectsByType<APickable>())Destroy(pickup.gameObject);
    }
    IEnumerator Play()
    {
        ClearArena();Elapsed=0;Spawned=0;yield return null;
        while(CloudTransition.Busy)yield return null;
        if(Encounter>=BossOne)
        {
            var boss=(DistrictBoss)DistrictEnemy.Spawn(DistrictWaves.BossType,new Vector3(0,Camera.UpLimit+3,1));Spawned=1;
            yield return null;yield return null; // Both City hulls finish Start before selecting the survivor phase.
            if(Encounter==BossTwo)
            {
                if(boss is CityDuoBoss duo && duo.partner!=null)
                {
                    duo.partner.health*=.55f;duo.lifeDropChance=duo.manaDropChance=duo.moneyDropChance=0;duo.damage(1000000);
                }
                else boss.health*=.49f;
            }
            while(true){Elapsed+=Time.deltaTime;yield return null;}
        }
        var wave=Encounter==Miniboss ? DistrictWaves.Miniboss() : DistrictWaves.Create(Encounter);
        int next=0;float quiet=0;
        while(true)
        {
            Elapsed+=Time.deltaTime;
            while(next<wave.units.Count && wave.units[next].time<=Elapsed)
            {
                var unit=wave.units[next++];
                var enemy=DistrictEnemy.Spawn(DistrictWaves.EnemyType(unit.enemy),new Vector3(unit.position.x*Camera.RightLimit,unit.position.y*Camera.UpLimit,1));
                enemy.FlyPhrase(unit);Spawned++;
            }
            bool clear=next==wave.units.Count && AEnemy.Live.Count==0 && CombatRound.Live.Count==0;
            quiet=clear ? quiet+Time.deltaTime : 0;
            if(Looping && quiet>.8f){playback=StartCoroutine(Play());yield break;}
            yield return null;
        }
    }
    void Build()
    {
        var canvas=gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=80;
        var scaler=gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=1;
        gameObject.AddComponent<GraphicRaycaster>();
        var leftMask=Box(transform,"Left margin",Vector2.zero,Vector2.zero,Color.black);
        leftMask.anchorMin=Vector2.zero;leftMask.anchorMax=new Vector2(.5f,1);leftMask.offsetMin=Vector2.zero;leftMask.offsetMax=new Vector2(-202.5f,0);
        var rightMask=Box(transform,"Right margin",Vector2.zero,Vector2.zero,Color.black);
        rightMask.anchorMin=new Vector2(.5f,0);rightMask.anchorMax=Vector2.one;rightMask.offsetMin=new Vector2(202.5f,0);rightMask.offsetMax=Vector2.zero;
        var left=Box(transform,"Watcher levels",new Vector2(-365,0),new Vector2(270,650),Panel);
        var right=Box(transform,"Watcher waves",new Vector2(365,0),new Vector2(270,650),Panel);
        Label(left,"WAVE WATCHER",22,new Vector2(0,298),new Vector2(250,38));
        foreach(var level in new[]{LevelId.Fields,LevelId.Snow,LevelId.City,LevelId.Thunder,LevelId.Tunnel})
        {
            int row=levels.Count;var chosen=level;
            levels[level]=Button(left,level==LevelId.Fields ? "PLAINS" : level.ToString().ToUpperInvariant(),new Vector2(0,225-row*42),new Vector2(230,36),()=>SelectLevel(chosen)).GetComponent<Image>();
        }
        Button(left,"REPLAY",new Vector2(0,-10),new Vector2(230,36),Replay);
        Label(left,"AIM",14,new Vector2(0,-63),new Vector2(230,25));
        for(int i=-1;i<=1;i++)
        {
            int side=i;aims.Add(Button(left,i<0 ? "LEFT" : i>0 ? "RIGHT" : "CENTRE",new Vector2(i*79,-100),new Vector2(73,34),()=>SetAim(side)).GetComponent<Image>());
        }
        speedLabel=Button(left,"SPEED",new Vector2(0,-156),new Vector2(230,36),CycleSpeed).GetComponentInChildren<TextMeshProUGUI>();
        pauseLabel=Button(left,"PAUSE",new Vector2(0,-204),new Vector2(230,36),TogglePause).GetComponentInChildren<TextMeshProUGUI>();
        loopLabel=Button(left,"LOOP",new Vector2(0,-252),new Vector2(230,36),ToggleLoop).GetComponentInChildren<TextMeshProUGUI>();
        Button(left,"BACK",new Vector2(0,-300),new Vector2(230,36),Exit);
        heading=Label(right,"",24,new Vector2(0,298),new Vector2(250,38));
        for(int wave=FirstWave;wave<=LastWave;wave++)
        {
            int chosen=wave,index=wave-FirstWave;
            encounters[wave]=Button(right,wave.ToString(),new Vector2(-87+(index%4)*58,225-(index/4)*44),new Vector2(50,36),()=>SelectEncounter(chosen)).GetComponent<Image>();
        }
        encounters[Miniboss]=Button(right,"MINIBOSS",new Vector2(0,-10),new Vector2(230,36),()=>SelectEncounter(Miniboss)).GetComponent<Image>();
        encounters[BossOne]=Button(right,"BOSS  /  PHASE 1",new Vector2(0,-56),new Vector2(230,36),()=>SelectEncounter(BossOne)).GetComponent<Image>();
        encounters[BossTwo]=Button(right,"BOSS  /  PHASE 2",new Vector2(0,-102),new Vector2(230,36),()=>SelectEncounter(BossTwo)).GetComponent<Image>();
        status=Label(right,"",16,new Vector2(0,-186),new Vector2(250,70));Refresh();
    }
    void Refresh()
    {
        if(heading==null)return;
        heading.text=Encounter==Miniboss ? "MINIBOSS" : Encounter>=BossOne ? "BOSS" : "WAVE "+Encounter;
        pauseLabel.text=Paused ? "RESUME" : "PAUSE";loopLabel.text=Looping ? "LOOP  ON" : "LOOP  OFF";speedLabel.text="SPEED  "+PlaybackSpeed.ToString("0.#")+"x";
        foreach(var level in levels)level.Value.color=level.Key==Level ? Selected : ButtonColor;
        foreach(var wave in encounters)wave.Value.color=wave.Key==Encounter ? Selected : ButtonColor;
        SetAim(aim);
    }
    RectTransform Box(Transform parent,string name,Vector2 position,Vector2 size,Color color)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);
        var rect=go.GetComponent<RectTransform>();rect.anchoredPosition=position;rect.sizeDelta=size;go.GetComponent<Image>().color=color;return rect;
    }
    TextMeshProUGUI Label(Transform parent,string value,float size,Vector2 position,Vector2 dimensions)
    {
        var go=new GameObject("Watcher label",typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(parent,false);
        var rect=go.GetComponent<RectTransform>();rect.anchoredPosition=position;rect.sizeDelta=dimensions;
        var text=go.GetComponent<TextMeshProUGUI>();if(font!=null)text.font=font;text.text=value;text.fontSize=size;text.alignment=TextAlignmentOptions.Center;
        text.textWrappingMode=TextWrappingModes.NoWrap;text.enableAutoSizing=true;text.fontSizeMin=10;text.fontSizeMax=size;
        text.color=new Color(.83f,.87f,.81f);text.raycastTarget=false;return text;
    }
    Button Button(Transform parent,string label,Vector2 position,Vector2 size,UnityEngine.Events.UnityAction clicked)
    {
        var rect=Box(parent,label,position,size,ButtonColor);var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=rect.GetComponent<Image>();
        var colors=button.colors;colors.highlightedColor=new Color(1.25f,1.25f,1.1f);colors.selectedColor=colors.highlightedColor;button.colors=colors;
        Label(rect,label,16,Vector2.zero,size);button.onClick.AddListener(clicked);return button;
    }
}

public sealed class WaveWatcherMenu : AMenuElement
{
    protected override void Start()
    {
        base.Start();
        // Fit the title frame around all four entries, including the added tools.
        var border=menu.GetComponent<Image>();if(border==null)return;
        Vector2 minimum=Vector2.one*float.MaxValue,maximum=Vector2.one*float.MinValue;
        var corners=new Vector3[4];
        foreach(var entry in menu.GetComponentsInChildren<AMenuElement>())
        {
            entry.GetComponent<RectTransform>().GetWorldCorners(corners);
            foreach(var corner in corners)
            {
                Vector2 point=menu.transform.InverseTransformPoint(corner);
                minimum=Vector2.Min(minimum,point);maximum=Vector2.Max(maximum,point);
            }
        }
        var frame=new GameObject("Title entries frame",typeof(RectTransform),typeof(Image));frame.transform.SetParent(menu.transform,false);frame.transform.SetAsFirstSibling();
        var rect=frame.GetComponent<RectTransform>();rect.anchoredPosition=(minimum+maximum)*.5f;rect.sizeDelta=maximum-minimum+Vector2.one*24;
        var image=frame.GetComponent<Image>();image.sprite=border.sprite;image.type=border.type;image.color=border.color;image.raycastTarget=false;border.enabled=false;
        var menuRect=menu.GetComponent<RectTransform>();menuRect.anchoredPosition-=new Vector2(0,(minimum.y+maximum.y)*.5f);
    }
    public override void OnAccept()=>WaveWatcher.Launch();
}
