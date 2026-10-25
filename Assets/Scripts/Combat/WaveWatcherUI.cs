using System;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed partial class WaveWatcher
{
    const int PageSize=10;
    RectTransform leftPanel,rightPanel,listPanel,variantPanel;
    TextMeshProUGUI pageLabel,variantLabel,mirrorLabel,tourLabel,loopNumber,copyLabel;
    TMP_InputField seedInput;
    readonly System.Collections.Generic.Dictionary<WatcherView,Image> tabs=new System.Collections.Generic.Dictionary<WatcherView,Image>();
    int page;
    float copiedUntil;
    float nextReference;
    bool referenceUnavailable;
    string feedbackFolder;
    bool capturing;
    string FeedbackFolder=>feedbackFolder??(feedbackFolder=Path.Combine(Application.isEditor ? Path.GetFullPath(Path.Combine(Application.dataPath,"..")) : Application.persistentDataPath,"WaveWatcherFeedback"));
    public string ReviewReference=>$"WATCHER | {LevelRun.CurrentName} | {SelectedEntry.Id} | seed={LevelRun.Seed} | loop={ReviewLoop}"+
        (View==WatcherView.Formations ? $" | variant={Variant+1}/13 (layout {ResolvedVariant}) | mirror={Mirror}" : "")+
        $" | aim={aim} | t={Elapsed:0.00}s | speed={PlaybackSpeed:0.#}x\n{recipe}";

    public void CopyForChat()
    {
        if(!Ready || capturing)return;Paused=true;Refresh();StartCoroutine(CaptureForChat());
    }
    System.Collections.IEnumerator CaptureForChat()
    {
        capturing=true;yield return new WaitForEndOfFrame();
        string folder=FeedbackFolder;
        try
        {
            Directory.CreateDirectory(folder);
            string reference=ReviewReference;
            File.WriteAllText(Path.Combine(folder,"Latest.txt"),reference);
            var screenshot=ScreenCapture.CaptureScreenshotAsTexture();
            try{File.WriteAllBytes(Path.Combine(folder,"Latest.png"),screenshot.EncodeToPNG());}finally{Destroy(screenshot);}
            GUIUtility.systemCopyBuffer=reference+"\nCapture: "+Path.Combine(folder,"Latest.png");
            copiedUntil=Time.unscaledTime+2;copyLabel.text="COPIED + SAVED";
        }
        catch(Exception error){Debug.LogWarning("Watcher capture: "+error.Message);GUIUtility.systemCopyBuffer=ReviewReference;copyLabel.text="REFERENCE COPIED";}
        capturing=false;
    }
    void SaveCurrentReference(bool active=true)
    {
        if(referenceUnavailable || (active && Time.unscaledTime<nextReference))return;
        nextReference=Time.unscaledTime+.5f;
        try{Directory.CreateDirectory(FeedbackFolder);File.WriteAllText(Path.Combine(FeedbackFolder,"Current.txt"),(active ? "ACTIVE" : "CLOSED")+"\n"+ReviewReference);}
        catch(Exception){referenceUnavailable=true;}
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
        leftPanel=Box(transform,"Watcher levels",new Vector2(-365,0),new Vector2(270,674),Panel);
        rightPanel=Box(transform,"Watcher catalog",new Vector2(365,0),new Vector2(270,674),Panel);
        Label(leftPanel,"WAVE WATCHER",22,new Vector2(0,307),new Vector2(250,36));
        foreach(var level in new[]{LevelId.Fields,LevelId.Snow,LevelId.City,LevelId.Thunder,LevelId.Tunnel,LevelId.Desert})
        {
            int row=levels.Count;var chosen=level;
            levels[level]=Button(leftPanel,level==LevelId.Fields ? "PLAINS" : level.ToString().ToUpperInvariant(),new Vector2(0,270-row*29),new Vector2(230,25),()=>SelectLevel(chosen)).GetComponent<Image>();
        }
        var seedBox=Box(leftPanel,"Seed",new Vector2(-26,83),new Vector2(178,32),ButtonColor);
        Label(seedBox,"SEED",12,new Vector2(-64,0),new Vector2(42,32));
        var inputBox=Box(seedBox,"Seed input",new Vector2(23,0),new Vector2(126,32),Color.clear);
        var inputText=Label(inputBox,"",16,Vector2.zero,new Vector2(120,32));inputText.raycastTarget=true;
        seedInput=inputBox.gameObject.AddComponent<TMP_InputField>();seedInput.textViewport=inputBox;seedInput.textComponent=inputText;
        seedInput.contentType=TMP_InputField.ContentType.IntegerNumber;seedInput.characterLimit=10;
        seedInput.onEndEdit.AddListener(value=>{if(int.TryParse(value,out int seed) && seed>0)SetSeed(seed);else seedInput.SetTextWithoutNotify(LevelRun.Seed.ToString());});
        Button(leftPanel,"NEW",new Vector2(92,83),new Vector2(46,32),NewSet);
        Button(leftPanel,"LOOP -",new Vector2(-79,43),new Vector2(73,30),()=>SelectLoop(ReviewLoop-1));
        loopNumber=Label(leftPanel,"",16,new Vector2(0,43),new Vector2(72,30));
        Button(leftPanel,"LOOP +",new Vector2(79,43),new Vector2(73,30),()=>SelectLoop(ReviewLoop+1));
        for(int i=-1;i<=1;i++)
        {
            int side=i;aims.Add(Button(leftPanel,i<0 ? "AIM L" : i>0 ? "AIM R" : "AIM C",new Vector2(i*79,2),new Vector2(73,30),()=>SetAim(side)).GetComponent<Image>());
        }
        Button(leftPanel,"REPLAY",new Vector2(0,-43),new Vector2(230,33),Replay);
        pauseLabel=Button(leftPanel,"PAUSE",new Vector2(-60,-84),new Vector2(110,33),TogglePause).GetComponentInChildren<TextMeshProUGUI>();
        Button(leftPanel,"STEP 1/60",new Vector2(60,-84),new Vector2(110,33),StepOnce);
        speedLabel=Button(leftPanel,"SPEED",new Vector2(-60,-125),new Vector2(110,33),CycleSpeed).GetComponentInChildren<TextMeshProUGUI>();
        loopLabel=Button(leftPanel,"LOOP",new Vector2(60,-125),new Vector2(110,33),ToggleLoop).GetComponentInChildren<TextMeshProUGUI>();
        Button(leftPanel,"< PREV",new Vector2(-60,-166),new Vector2(110,33),()=>NextEntry(-1));
        Button(leftPanel,"NEXT >",new Vector2(60,-166),new Vector2(110,33),()=>NextEntry(1));
        tourLabel=Button(leftPanel,"AUTO TOUR",new Vector2(0,-207),new Vector2(230,33),ToggleTour).GetComponentInChildren<TextMeshProUGUI>();
        copyLabel=Button(leftPanel,"COPY FOR CHAT",new Vector2(0,-252),new Vector2(230,36),CopyForChat).GetComponentInChildren<TextMeshProUGUI>();
        Button(leftPanel,"BACK",new Vector2(0,-307),new Vector2(230,30),Exit);
        heading=Label(rightPanel,"",22,new Vector2(0,307),new Vector2(250,36));
        string[] titles={"WAVES","PARTS","ENEMIES","BOSSES","SHOTS"};
        for(int i=0;i<titles.Length;i++)
        {
            var view=(WatcherView)i;
            tabs[view]=Button(rightPanel,titles[i],new Vector2(-79+i%3*79,265-i/3*34),new Vector2(73,28),()=>SelectView(view)).GetComponent<Image>();
        }
        listPanel=Box(rightPanel,"Entries",new Vector2(0,25),new Vector2(234,366),Color.clear);
        Button(rightPanel,"< PAGE",new Vector2(-79,-170),new Vector2(73,29),()=>ChangePage(-1));
        pageLabel=Label(rightPanel,"",14,new Vector2(0,-170),new Vector2(73,29));
        Button(rightPanel,"PAGE >",new Vector2(79,-170),new Vector2(73,29),()=>ChangePage(1));
        variantPanel=Box(rightPanel,"Formation variants",new Vector2(0,-209),new Vector2(234,30),Color.clear);
        Button(variantPanel,"-",new Vector2(-100,0),new Vector2(28,28),()=>SelectVariant(Variant-1));
        variantLabel=Label(variantPanel,"",14,new Vector2(-40,0),new Vector2(86,28));
        Button(variantPanel,"+",new Vector2(20,0),new Vector2(28,28),()=>SelectVariant(Variant+1));
        mirrorLabel=Button(variantPanel,"MIRROR",new Vector2(82,0),new Vector2(70,28),ToggleMirror).GetComponentInChildren<TextMeshProUGUI>();
        partLabel=Label(rightPanel,"",13,new Vector2(0,-261),new Vector2(234,65));
        status=Label(rightPanel,"",14,new Vector2(0,-315),new Vector2(250,28));
        RebuildCatalog();LayoutPanels();Refresh();
    }
    void LayoutPanels()
    {
        if(leftPanel==null)return;
        float margin=(GetComponent<RectTransform>().rect.width-405)*.5f;
        float scale=Mathf.Min(1,(margin-12)/270);
        leftPanel.anchoredPosition=new Vector2(-202.5f-margin*.5f,0);rightPanel.anchoredPosition=-leftPanel.anchoredPosition;
        leftPanel.localScale=rightPanel.localScale=new Vector3(scale,scale,1);
        if(copyLabel!=null && copiedUntil>0 && Time.unscaledTime>copiedUntil){copiedUntil=0;copyLabel.text="COPY FOR CHAT";}
    }
    void ChangePage(int direction)
    {
        int count=(catalog.Count+PageSize-1)/PageSize;page=(page+direction+count)%count;RebuildCatalog(false);
    }
    void RebuildCatalog(bool followSelection=true)
    {
        if(listPanel==null)return;
        if(followSelection)page=Selection/PageSize;
        bool refocus=EventSystem.current!=null && EventSystem.current.currentSelectedGameObject!=null && EventSystem.current.currentSelectedGameObject.transform.IsChildOf(listPanel);
        foreach(Transform child in listPanel){child.gameObject.SetActive(false);Destroy(child.gameObject);}
        encounters.Clear();
        for(int index=page*PageSize;index<Mathf.Min(catalog.Count,(page+1)*PageSize);index++)
        {
            int chosen=index;var entry=catalog[index];
            var button=Button(listPanel,entry.Name,new Vector2(0,162-(index%PageSize)*36),new Vector2(230,31),()=>SelectEntry(chosen));
            button.name=View==WatcherView.Waves ? entry.Encounter.ToString() : entry.Id;
            button.GetComponent<Image>().color=index==Selection ? Selected : ButtonColor;
            encounters[index]=button.GetComponent<Image>();
        }
        pageLabel.text=$"{page+1} / {(catalog.Count+PageSize-1)/PageSize}";Refresh();
        if(refocus && encounters.TryGetValue(Selection,out var selected))EventSystem.current.SetSelectedGameObject(selected.gameObject);
    }
    void Refresh()
    {
        if(heading==null)return;
        heading.text=$"{View.ToString().ToUpperInvariant()}  {Selection+1}/{catalog.Count}";
        pauseLabel.text=Paused ? "RESUME" : "PAUSE";loopLabel.text=Looping ? "REPEAT ON" : "REPEAT OFF";speedLabel.text=PlaybackSpeed.ToString("0.#")+"x SPEED";
        loopNumber.text="LOOP "+ReviewLoop;seedInput.SetTextWithoutNotify(LevelRun.Seed.ToString());tourLabel.text=Touring ? "STOP TOUR" : "AUTO TOUR";
        variantPanel.gameObject.SetActive(View==WatcherView.Formations);variantLabel.text=$"VAR {Variant+1}/13";mirrorLabel.text=Mirror<0 ? "LEFT" : "RIGHT";
        partLabel.text=SelectedEntry.Name+"\n"+recipe;
        foreach(var level in levels)level.Value.color=level.Key==Level ? Selected : ButtonColor;
        foreach(var tab in tabs)tab.Value.color=tab.Key==View ? Selected : ButtonColor;
        foreach(var row in encounters)row.Value.color=row.Key==Selection ? Selected : ButtonColor;
        SetAim(aim);
    }
}
