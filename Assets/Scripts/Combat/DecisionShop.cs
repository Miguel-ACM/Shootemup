using UnityEngine;
using TMPro;
using System;
using System.Collections.Generic;

public sealed class DecisionShop : MonoBehaviour
{
    public bool paid;
    public bool scoreShop;
    Shipcontroller player;
    WaveOrchestrer owner;
    Action done;
    readonly List<AItem> items=new List<AItem>();
    readonly Dictionary<AItem,int> prices=new Dictionary<AItem,int>();
    AItem claimed;
    DecisionOverlay overlay;
    public float SkipProgress=>skipCharge/1.5f;
    Dialog dialog;
    int preview=-1;
    float age,skipCharge;
    bool closed;
    public static int RewardCount(float luck,float roll)
    {
        float chance=Mathf.Clamp(.08f+Mathf.Max(0,luck)*.35f,.08f,.4f);
        return roll<chance*.15f ? 4 : roll<chance ? 3 : 2;
    }
    public void Open(WaveOrchestrer orchestrer,Shipcontroller ship,string kind,Action completed)
    {
        owner=orchestrer;player=ship;paid=kind!="reward";scoreShop=kind=="scoreShop";done=completed;
        CombatDecision.Begin();player.GetComponent<ShipLoadout>().ResetSelection();
        foreach(var weapon in player.GetComponentsInChildren<AWeapon>())weapon.StopFiring();
        dialog=GameObject.FindGameObjectWithTag("Dialog").GetComponent<Dialog>();
        overlay=new GameObject("Shop decision overlay",typeof(RectTransform)).AddComponent<DecisionOverlay>();overlay.Build(dialog.font);
        int count=paid ? (UnityEngine.Random.value<.35f ? 4 : 3) : RewardCount(GameRules.luck,UnityEngine.Random.value);
        bool healthKit=paid && player.health<player.maxHealth*.45f;
        for(int i=0;i<count;i++)
        {
            string rarity="common";
            string itemKind=healthKit && i==count-1 ? "LargeHealthKit" : ItemPool.GetItem(out rarity);
            var go=Instantiate(Resources.Load<GameObject>("Items/Item"),new Vector3(Mathf.Lerp(Camera.LeftLimit+1,Camera.RightLimit-1,count==1 ? .5f : i/(float)(count-1)),Camera.UpLimit+3,1),Quaternion.identity);
            var item=(AItem)go.AddComponent(Type.GetType(itemKind));item.SetRarity(rarity);item.Register(owner);
            item.GoToPosition(new Vector3(go.transform.position.x,Camera.UpLimit*.5f,1));items.Add(item);
            int tier=rarity=="legendary" ? 3 : rarity=="epic" ? 2 : rarity=="rare" ? 1 : 0;
            int price=!paid ? 0 : scoreShop ? new[]{1200,2400,4200,6400}[tier] : tier==3 ? 3 : tier==2 ? 2 : 1;
            if(healthKit && i==count-1)price=scoreShop ? 900 : 1;
            prices[item]=price;
            overlay.Add(item);
        }
    }
    void Update()
    {
        if(closed || player==null)return;age+=Time.deltaTime;
        if(player.health<=0){Abort();return;}
        int nearest=-1;float d=99;
        for(int i=0;i<items.Count;i++)
        {
            if(items[i]==null)continue;
            float x=Vector2.Distance(player.transform.position,items[i].transform.position);
            if(x<d){d=x;nearest=i;}
            overlay.SetOffer(i,paid ? prices[items[i]]+(scoreShop ? " pts" : "x") : "",!paid || Affordable(prices[items[i]]));
        }
        if(d<1.8f && nearest>=0)
        {
            if(preview!=nearest){preview=nearest;var data=items[nearest].GetData();dialog.ShowItemDesc(data[0],data[1],data[2],items[nearest].rarity);}
        }
        else {preview=-1;dialog.Hide();}
        var camera=UnityEngine.Camera.main;
        var hitbox=player.GetComponent<Collider2D>();
        float bottom=hitbox!=null ? hitbox.bounds.min.y : player.transform.position.y;
        bool atBottom=bottom<=camera.transform.position.y-camera.orthographicSize+.45f;
        skipCharge=atBottom && age>2 && claimed==null ? skipCharge+Time.deltaTime : 0;
        overlay.SetSkip(SkipProgress);
        if(skipCharge>=1.5f)Close();
    }
    bool Affordable(int price)=>scoreShop ? Score.CurrentScore>=price : Score.CurrentMultiplier>price;
    bool Pay(int price)=>scoreShop ? Score.TrySpendScore(price) : Score.TrySpendMultiplier(price);
    public bool TryClaim(AItem item)
    {
        if(closed || !items.Contains(item))return false;
        if(claimed!=null)return claimed==item;
        AItem nearest=null;float d=float.MaxValue;
        foreach(var option in items)
        {
            if(option==null)continue;float distance=Vector2.SqrMagnitude(option.transform.position-player.transform.position);
            if(distance<d){d=distance;nearest=option;}
        }
        if(nearest!=item || !Pay(prices[item]))return false;
        claimed=item;return true;
    }
    public void Picked(GameObject selected)
    {
        if(claimed==null || claimed.gameObject!=selected)return;
        Close(selected);
    }
    public void Abort(){done=null;Close();}
    void Close(GameObject selected=null)
    {
        if(closed)return;closed=true;
        foreach(var item in items)if(item!=null && item.gameObject!=selected)item.Dismiss();
        if(overlay!=null)Destroy(overlay.gameObject);if(dialog!=null)dialog.Hide();
        CombatDecision.End();done?.Invoke();Destroy(this);
    }
    void OnDestroy(){if(!closed){CombatDecision.End();foreach(var item in items)if(item!=null)Destroy(item.gameObject);if(overlay!=null)Destroy(overlay.gameObject);}}
}
