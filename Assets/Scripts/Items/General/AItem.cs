using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class AItem : MonoBehaviour
{
    public static readonly HashSet<AItem> Available = new HashSet<AItem>();
    void OnEnable()
    {
        Available.Add(this);
    }

    void OnDisable()
    {
        Available.Remove(this);
    }

    public bool CanPick(Shipcontroller ship) => ship != null && ship.health > 0 && !GameOverState.Active && !moving && !collected && !attracting &&
        Vector2.Distance(transform.position, ship.transform.position) <= ItemAttractRange;
    // Start is called before the first frame update
    protected string itemName;
    protected string flavour;
    protected string description;
    protected float lerpTime = 1.5f;
    float elapsedTime = 0;
    bool moving = false;
    bool attracting, collected;
    float attractionSpeed;
    public bool IsAttracting => attracting;
    public bool Settled => !moving && sgo != null;
    public bool Collected => collected;
    public int ReplacementSlot
    {
        get; private set;
    }

    public ItemType Kind => type;
    public bool IsDismissing
    {
        get; private set;
    }

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
        weapon,
        passiveWeapon,
    }

    protected virtual void Start()
    {
        ps = GetComponent<ParticleSystem>();
        curve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));
        curve.preWrapMode = WrapMode.ClampForever;
        curve.postWrapMode = WrapMode.ClampForever;
        curve.SmoothTangents(0, 3);
        sgo = GameObject.FindGameObjectWithTag("Player");
        s = sgo.GetComponent<Shipcontroller>();
        UpdateColor();
        if (GetComponent<ItemRarityHalo>() == null)
            gameObject.AddComponent<ItemRarityHalo>();
        if (ps != null)
            ps.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
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
    protected virtual void Update()
    {
        if (collected || sgo == null)
            return;
        if (attracting)
        {
            attractionSpeed = Mathf.MoveTowards(attractionSpeed, 25, 40 * Time.deltaTime);
            var target = sgo.transform.position;
            target.z = transform.position.z;
            transform.position = Vector3.MoveTowards(transform.position, target, attractionSpeed * Time.deltaTime);
            if (Vector2.Distance(transform.position, target) < .5f)
                Collect();
            return;
        }
        // Items use a fixed, short attraction range: the Magnet (GameRules.pickDistance) only affects pickups.
        if (!moving && Vector2.Distance(transform.position, sgo.transform.position) <= ItemAttractRange)
            TryAttract();
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
        if (!moving && !collected && collision.CompareTag("Player"))
            TryAttract();
    }


    public const float ItemAttractRange = .55f;

    void TryAttract()
    {
        if (!CanPick(s))
            return;
        var loadout = sgo.GetComponent<ShipLoadout>();
        int slot = loadout != null ? loadout.SlotFor(this) : 1;
        if (slot < 0)
            return;
        if (waveOrchestrer != null && !waveOrchestrer.TryClaimItem(this))
            return;
        ReplacementSlot = slot;
        attracting = true;
        attractionSpeed = 5;
    }

    public bool TryPick(int slot)
    {
        if (!CanPick(s) || slot < 0 || slot > 1)
            return false;
        var loadout = s.GetComponent<ShipLoadout>();
        if (loadout == null || loadout.SlotFor(this) >= 0 || (!(this is AWeaponItem) && !(this is AActiveItem)))
            return false;
        if (waveOrchestrer != null && !waveOrchestrer.TryClaimItem(this))
            return false;
        ReplacementSlot = slot;
        Collect();
        return true;
    }

    void Collect()
    {
        if (collected)
            return;
        collected = true;
        PSXImpactEffects.ItemGet(transform.position, GetRarityColor(rarity));
        // Show the collected item in the side panel next to the playfield.
        var visual = GetComponent<PSXObjectVisual>();
        if (visual != null && visual.model != null && visual.modelRenderer != null)
            PSXInventoryDisplay.Add(visual.model.GetComponent<MeshFilter>()?.sharedMesh, visual.modelRenderer.sharedMaterial);
        OnPickup();
        s.PickUpItem(this);
    }

    // Debug grants use the same effects and inventory feedback without a world pickup.
    public void GrantTo(Shipcontroller ship, int slot = 0)
    {
        s = ship;
        sgo = ship.gameObject;
        ReplacementSlot = Mathf.Clamp(slot, 0, 1);
        Collect();
    }

    private void setRarityColor(Color c)
    {
        //ParticleSystem.MainModule settings = ps.main;
        if (ps == null)
            return;
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
                return new Color(1f, .74f, .12f);
            case "epic":
                return new Color(.85f, .36f, 1f);
            case "rare":
                return new Color(.25f, .75f, 1f);
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

    public void Dismiss()
    {
        if (collected)
            return;
        collected = true;
        IsDismissing = true;
        foreach (var collider in GetComponents<Collider2D>())
            collider.enabled = false;
        var body = GetComponent<Rigidbody2D>();
        if (body != null)
            body.simulated = false;
        var visual = GetComponent<PSXObjectVisual>();
        if (visual != null)
            visual.Dismiss();
        else
            Destroy(gameObject, .48f);
    }

    public abstract void OnBegin();

    public virtual void OnPickup()
    {
        collected = true;
        if (waveOrchestrer != null)
        {
            waveOrchestrer.NotifyItemPicked(gameObject);
        }
        if (type == ItemType.passive || type == ItemType.partner || type == ItemType.weapon || type == ItemType.passiveWeapon)
            Destroy(gameObject);
        else if (type == ItemType.active)
            Invisibilize();
    }

    private void Invisibilize()
    {
        var visual = GetComponent<PSXObjectVisual>();
        if (visual != null)
            visual.Hide();
        Destroy(ps);
        Destroy(GetComponent<SpriteRenderer>());
        transform.parent = sgo.transform;
        transform.localPosition = new Vector3(0, 0, 0);
    }
}
