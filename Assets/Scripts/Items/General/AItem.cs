using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class AItem : MonoBehaviour
{
    // Start is called before the first frame update
    protected string itemName;
    protected string flavour;
    protected string description;
    protected float lerpTime = 1.5f;
    float elapsedTime = 0;
    bool moving = false;
    Vector3 lerpPosition;
    Vector3 startPosition;
    private AnimationCurve curve = new AnimationCurve();
    public Shipcontroller s;
    public GameObject sgo;
    ParticleSystem ps;
    public string rarity = "common";
    WaveOrchestrer waveOrchestrer;
    ItemType type;

    
    public enum ItemType
    {
        passive,
        active,
        partner,
        weapon
    }

    protected void Start()
    {
        ps = GetComponent<ParticleSystem>();
        curve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));
        curve.preWrapMode = WrapMode.ClampForever;
        curve.postWrapMode = WrapMode.ClampForever;
        curve.SmoothTangents(0, 3);
        sgo = GameObject.FindGameObjectWithTag("Player");
        s = sgo.GetComponent<Shipcontroller>();
        UpdateColor();
    }


    protected void SetData(string _itemName, string _flavour, string _description, ItemType _type)
    {
        itemName = _itemName;
        flavour = _flavour;
        description = _description;
        type = _type;
    }

    public string[] GetData()
    {
        return new string[] { itemName, flavour, description };
    }

    public void GoToPosition(Vector3 position)
    {
        moving = true;
        lerpPosition = position;
        startPosition = transform.position;
        elapsedTime = 0;
    }

    // Update is called once per frame
    protected void Update()
    {
        if (moving)
        {
            elapsedTime += Time.deltaTime;
            float percentageComplete = elapsedTime / lerpTime;
            transform.position = Vector3.Lerp(startPosition, lerpPosition, curve.Evaluate(percentageComplete));
            if (percentageComplete >= 1)
            {
                moving = false;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!moving)
        {
            OnPickup();
            s.PickUpItem(this);
        }
    }


    private void setRarityColor(Color c)
    {
        //ParticleSystem.MainModule settings = ps.main;
        var main = ps.main;
        main.startColor = c;
    }
    //ParticleSystem.MinMaxGradient mmg = new ParticleSystem.MinMaxGradient(c);
    //settings.startColor = mmg;


    public static Color GetRarityColor(string r)
    {
        switch (r)
        {
            case "legendary":
                return new Color(1, 0.902f, 0);
            case "epic":
                return new Color(0.651f, 0, 1);
            case "rare":
                return new Color(0.071f, 0.69f, 1);
            case "common":
                return new Color(0.857f, 0.857f, 0.857f);
            default:
                return new Color(0.361f, 0.361f, 0.361f);
        }
    }

    private void UpdateColor()
    {
        setRarityColor(GetRarityColor(rarity));
    }

    public void SetRarity(string _rarity)
    {
        rarity = _rarity;
    }

    public void Register(WaveOrchestrer w)
    {
        waveOrchestrer = w;
    }

    public abstract void OnBegin();

    public virtual void OnPickup()
    {
        if (waveOrchestrer != null)
        {
            waveOrchestrer.NotifyItemPicked(gameObject);
        }
        if (type == ItemType.passive || type == ItemType.partner || type == ItemType.weapon)
            Destroy(gameObject);
        else if (type == ItemType.active)
            Invisibilize();
    }

    private void Invisibilize()
    {
        Destroy(ps);
        Destroy(GetComponent<SpriteRenderer>());
        transform.parent = sgo.transform;
        transform.localPosition = new Vector3(0, 0, 0);
    }
}
