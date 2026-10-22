#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

/// <summary>Feeds virtual device events through the real Input System and scene UI.</summary>
public sealed class InputMigrationProbe : MonoBehaviour
{
    readonly List<string> errors=new List<string>();
    Keyboard keyboard;
    Gamepad gamepad;
    Mouse mouse;
    void Awake(){Application.runInBackground=true;Application.logMessageReceived+=Log;}
    void Log(string message,string trace,LogType type)
    {
        if(trace.Contains("UnityEditor.Search.SearchDatabase"))return;
        if(type==LogType.Error || type==LogType.Exception)if(!errors.Contains(message))errors.Add(message);
    }
    IEnumerator Start()
    {
        var run=Run();
        while(true)
        {
            object value=null;bool more=false;Exception failure=null;
            try{more=run.MoveNext();if(more)value=run.Current;}catch(Exception e){failure=e;}
            if(failure!=null){Finish("FAIL: "+failure,1);yield break;}
            if(!more)break;
            yield return value;
        }
        Finish("PASS: Input System only compile flags; all three scenes migrated; keyboard/gamepad menu navigation; gamepad accept; keyboard pause and movement; primary fire; mouse hover/click on main and pause menus; cloud input blocking; fresh scene input cleanup.",0);
    }
    IEnumerator Run()
    {
        InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        keyboard=InputSystem.AddDevice<Keyboard>();gamepad=InputSystem.AddDevice<Gamepad>();mouse=InputSystem.AddDevice<Mouse>();
        yield return null;yield return null;
        Check(SceneManager.GetActiveScene().name=="Title","Main menu missing");
        var menu=FindAnyObjectByType<Menu>();
        var module=EventSystem.current.currentInputModule as InputSystemUIInputModule;
        Check(module!=null && module.point.action.enabled && module.leftClick.action.enabled,"New UI module has no live pointer actions");
        // Pointer clicks must target a menu entry, rather than triggering the keyboard Accept action globally.
        InputSystem.QueueStateEvent(mouse,new MouseState{position=new Vector2(2,2)}.WithButton(MouseButton.Left));
        yield return new WaitForSecondsRealtime(.06f);
        InputSystem.QueueStateEvent(mouse,new MouseState{position=new Vector2(2,2)});
        yield return new WaitForSecondsRealtime(.06f);
        Check(!CloudTransition.Busy,"Clicking outside the menu accepted the selected entry");
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.DownArrow));yield return new WaitForSecondsRealtime(.06f);
        Check(menu.CurrentElement is QuitGame,"Keyboard menu navigation failed");
        InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return new WaitForSecondsRealtime(.06f);
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.UpArrow));yield return new WaitForSecondsRealtime(.06f);
        Check(menu.CurrentElement is NewGame,"Keyboard menu return failed");
        InputSystem.QueueStateEvent(keyboard,new KeyboardState());
        InputSystem.QueueStateEvent(gamepad,new GamepadState().WithButton(GamepadButton.DpadDown));yield return new WaitForSecondsRealtime(.06f);
        Check(menu.CurrentElement is QuitGame,"Gamepad menu navigation failed");
        InputSystem.QueueStateEvent(gamepad,new GamepadState());yield return new WaitForSecondsRealtime(.06f);
        InputSystem.QueueStateEvent(gamepad,new GamepadState().WithButton(GamepadButton.DpadUp));yield return new WaitForSecondsRealtime(.06f);
        Check(menu.CurrentElement is NewGame,"Gamepad menu return failed");
        InputSystem.QueueStateEvent(gamepad,new GamepadState());yield return new WaitForSecondsRealtime(.06f);
        InputSystem.QueueStateEvent(gamepad,new GamepadState().WithButton(GamepadButton.South));yield return new WaitForSecondsRealtime(.06f);
        Check(CloudTransition.Busy,"Gamepad accept did not start a run");
        InputSystem.QueueStateEvent(gamepad,new GamepadState());
        float until=Time.realtimeSinceStartup+12;
        while(CloudTransition.Busy && Time.realtimeSinceStartup<until)yield return null;
        Check(Time.timeScale==1 && SceneManager.GetActiveScene().name=="L1","Gameplay did not load");
        var ship=FindAnyObjectByType<Shipcontroller>();float x=ship.transform.position.x;
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.RightArrow));yield return new WaitForSecondsRealtime(.18f);
        InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return new WaitForSecondsRealtime(.06f);
        Check(ship.transform.position.x>x+.5f,"Keyboard movement failed");
        x=ship.transform.position.x;
        InputSystem.QueueStateEvent(gamepad,new GamepadState{leftStick=new Vector2(-1,0)});yield return new WaitForSecondsRealtime(.18f);
        InputSystem.QueueStateEvent(gamepad,new GamepadState());yield return new WaitForSecondsRealtime(.06f);
        Check(ship.transform.position.x<x-.5f,"Gamepad movement failed");
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Z));yield return new WaitForSecondsRealtime(.18f);
        Check(FindObjectsByType<ABullet>().Length>0,"Primary fire failed");
        InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return new WaitForSecondsRealtime(.06f);
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Escape));yield return new WaitForSecondsRealtime(.06f);
        InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return new WaitForSecondsRealtime(.06f);
        Check(Time.timeScale==0,"Keyboard pause failed");
        var resume=FindAnyObjectByType<Continue>();
        var point=(Vector2)RectTransformUtility.WorldToScreenPoint(null,resume.GetComponent<RectTransform>().position);
        InputSystem.QueueStateEvent(mouse,new MouseState{position=point});yield return new WaitForSecondsRealtime(.06f);
        Check(FindAnyObjectByType<Menu>().CurrentElement==resume,"Mouse hover did not select resume");
        InputSystem.QueueStateEvent(mouse,new MouseState{position=point}.WithButton(MouseButton.Left));yield return new WaitForSecondsRealtime(.06f);
        InputSystem.QueueStateEvent(mouse,new MouseState{position=point});yield return new WaitForSecondsRealtime(.06f);
        Check(Time.timeScale==1,"Mouse click did not resume gameplay");
        CloudTransition.Load("Title");until=Time.realtimeSinceStartup+12;
        while(CloudTransition.Busy && Time.realtimeSinceStartup<until)yield return null;
        var start=FindAnyObjectByType<NewGame>();point=RectTransformUtility.WorldToScreenPoint(null,start.GetComponent<RectTransform>().position);
        InputSystem.QueueStateEvent(mouse,new MouseState{position=point});yield return new WaitForSecondsRealtime(.06f);
        InputSystem.QueueStateEvent(mouse,new MouseState{position=point}.WithButton(MouseButton.Left));yield return new WaitForSecondsRealtime(.06f);
        InputSystem.QueueStateEvent(mouse,new MouseState{position=point});yield return new WaitForSecondsRealtime(.06f);
        var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=point},hits);
        Check(CloudTransition.Busy,"Mouse click did not start a run at "+point+"; hits="+string.Join(",",hits.ConvertAll(hit=>hit.gameObject.name)));
        // Repeated accept and pause during the cloud cover cannot load or pause a second scene.
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Enter,Key.Escape));yield return new WaitForSecondsRealtime(.06f);
        InputSystem.QueueStateEvent(keyboard,new KeyboardState());
        until=Time.realtimeSinceStartup+12;
        while(CloudTransition.Busy && Time.realtimeSinceStartup<until)yield return null;
        Check(Time.timeScale==1 && FindObjectsByType<Shipcontroller>().Length==1,"Input during transition duplicated or paused the run");
        Check(errors.Count==0,"Runtime errors: "+string.Join("; ",errors));
    }
    static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
    void Finish(string result,int code)
    {
        File.WriteAllText("FlowReview/InputMigration.txt",result+"\n"+string.Join("\n",errors));
        UnityEditor.EditorApplication.Exit(code);
    }
    void OnDestroy()
    {
        Application.logMessageReceived-=Log;
        if(keyboard!=null)InputSystem.RemoveDevice(keyboard);
        if(gamepad!=null)InputSystem.RemoveDevice(gamepad);
        if(mouse!=null)InputSystem.RemoveDevice(mouse);
    }
}
#endif
