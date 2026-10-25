using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using EZCameraShake;

public class Shipcontroller : MonoBehaviour
{
    public event System.Action HitTaken;
    public bool ObserverMode {get;set;}
    public GameObject bulletPrimary;

    [SerializeField] private ResourceBar healthBar;
    [SerializeField] private ResourceBar manaBar;
    public float maxHealth = 100f;
    public float health = 100f;
    public float maxMana = 100f;
    public float mana = 100f;
    public float invulnerabilitySeconds = 1.5f;
    float invulnerabilityDeltaTime = 0.01f;
    bool invulnerable = false;

    float horizontalMove = 0f;
    float verticalMove = 0f;
    float verticalMoveMultiplier = 1.2f;
    float[] limits;
  

    
    public bool isShootingPrimary = false;
    public bool isStopingPrimary = true;

    //private int currBulletOutIdx = 0;

    public float slowMultiplier = 0.5f;
    private float speedMultiplier = 1f;

    Collider2D circleCollider;   // hexagonal PolygonCollider2D since the PSXv2 hitbox
    SpriteRenderer renderer;

    float hitSoundTimeInterval = 0.001f;
    float nextHitTime = 0f;

    AActiveItem activeRight = null;
    bool isActiveRightPressed;

    AActiveItem activeLeft = null;
    bool isActiveLeftPressed;

    Vector3 lastPosition = Vector3.zero;

    List<ACompanion> companions = new List<ACompanion>();
    Dictionary<string, int> numCompanionsPerType = new Dictionary<string, int>(){
            {"sides", 0},
            {"orbital", 0},
            {"follower", 0}
        };
    CircularArray<Vector2> previousPositions = null;

    Inputs input;
    Vector2 move;
    public Vector2 MoveInput=>move;
    public bool IsInvulnerable=>invulnerable || (GetComponent<ShipBarrelRoll>()?.Active ?? false);

    void Awake(){
        input = new Inputs();
        gameObject.AddComponent<ShipLoadout>();
        gameObject.AddComponent<CombatUpgrades>();
        healthBar.SetMax(maxHealth);
        healthBar.Set(health);

        manaBar.SetMax(maxMana);
        manaBar.Set(mana);

        input.PlayerControls.Focus.started += FocusPressed;// = ctx.ReadValue<float>() * slowMultiplier;
        input.PlayerControls.Focus.canceled += FocusReleased; // ctx => speedMultiplier = 1f;

        input.PlayerControls.ActiveRight.started += ActiveRightPressed;
        input.PlayerControls.ActiveRight.canceled += ActiveRightReleased;

        input.PlayerControls.ActiveLeft.started += ActiveLeftPressed;
        input.PlayerControls.ActiveLeft.canceled += ActiveLeftReleased;

        input.PlayerControls.Focus.started += FocusPressed;// = ctx.ReadValue<float>() * slowMultiplier;
        input.PlayerControls.Focus.canceled += FocusReleased;

        input.PlayerControls.Move.performed += ctx => move = ctx.ReadValue<Vector2>(); //PrimaryPressed();
        input.PlayerControls.Move.canceled += ctx => move = ctx.ReadValue<Vector2>();
    }

    void OnEnable() {
        input.PlayerControls.Enable();
    }

    public void AddActiveRight(AActiveItem a)
    {
        activeRight = a;
    }

    public void AddActiveLeft(AActiveItem a)
    {
        activeLeft = a;
    }

    public void AddCompanion(string companionName, int numItems=1)
    {
        for (int k = 0; k < numItems; k++)
        {
            GameObject c = Instantiate(Resources.Load<GameObject>("Companions/" + companionName),
                new Vector3(this.transform.position.x, this.transform.position.y, this.transform.position.z - 1), Quaternion.identity);
            ACompanion comp = c.GetComponent<ACompanion>();
            ACompanion.CompanionType ctype = comp.GetCompanionType();
            string ctypestr = ctype.ToString();
            companions.Add(comp);
            numCompanionsPerType[ctypestr] += 1;
            if (ctype == ACompanion.CompanionType.follower)
            {
                c.GetComponent<FollowPlayer>().SetIndex(numCompanionsPerType[ctypestr]);
                if (numCompanionsPerType[ctype.ToString()] == 1)
                {
                    Vector2 pos2 = this.transform.position;
                    previousPositions = new CircularArray<Vector2>(GameRules.followCompanionDelay, pos2);
                }
                else
                {
                    previousPositions.ChangeSize(GameRules.followCompanionDelay * numCompanionsPerType[ctypestr], this.transform.position);
                }
            }
            else if (ctype == ACompanion.CompanionType.orbital)
            {
                c.GetComponent<OrbitPlayer>().SetIndex(numCompanionsPerType[ctypestr], numCompanionsPerType[ctypestr]);
                int i = 0;
                while (i < numCompanionsPerType[ctypestr])
                {
                    if (i >= companions.Count)
                        break;
                    if (companions[i].GetCompanionType() == ctype)
                        companions[i].GetComponent<OrbitPlayer>().SetMaxIndex(numCompanionsPerType[ctypestr]);
                    i++;
                }
            }
        }
    }

    public Vector2 GetPreviousPosition(int index)
    {
        return previousPositions.Get(index);
    }

    // Start is called before the first frame update
    void Start()
    {
        limits = Camera.getLimits();
        circleCollider = GetComponent<Collider2D>();
        renderer = GetComponent<SpriteRenderer>();
        //bigCollision = GameObject.GetComponent<PollygonCollider2D();

    }

    // CONTROLS -----------------------------------------------------------

    void FocusPressed(InputAction.CallbackContext ctx)
    {
        speedMultiplier = ctx.ReadValue<float>() * slowMultiplier;
    }

    void FocusReleased(InputAction.CallbackContext ctx)
    {
        speedMultiplier = 1f;
    }

    void ActiveRightPressed(InputAction.CallbackContext ctx)
    {
        if(Time.timeScale<=0 && !WorldTimeStop.PlayerCanAct)return;
        if(GameRules.playerDisabled || GetComponent<ShipLoadout>().ConsumedInputThisFrame)return;
        isActiveRightPressed = true;
        

        if (activeRight != null)
        {
            float m = 0;
            m = UseActive(activeRight);
            if (m > -1)
            {
                mana = m;
                manaBar.Set(mana);
            }
        }
    }


    float UseActive(AActiveItem active)
    {
        var upgrades=GetComponent<CombatUpgrades>();float stored=upgrades!=null ? upgrades.Reserve : 0;
        float result=active.OnPress(mana+stored);
        if(result<0)return result;
        float spent=Mathf.Max(0,mana+stored-result);
        float fromReserve=upgrades!=null ? upgrades.SpendReserve(spent) : 0;
        return Mathf.Clamp(mana-(spent-fromReserve),0,maxMana);
    }
    void ActiveLeftPressed(InputAction.CallbackContext ctx)
    {
        if(Time.timeScale<=0 && !WorldTimeStop.PlayerCanAct)return;
        if(GameRules.playerDisabled || GetComponent<ShipLoadout>().ConsumedInputThisFrame)return;
        isActiveLeftPressed=true;
        if(activeLeft!=null){float result=UseActive(activeLeft);if(result>=0){mana=result;manaBar.Set(mana);}}
    }
    void ActiveLeftReleased(InputAction.CallbackContext ctx){isActiveLeftPressed=false;if(activeLeft!=null)activeLeft.OnRelease(mana);}
    public void ReceiveHazardDamage(float amount){if(!IsInvulnerable && health>0 && !GameRules.playerDisabled)GetDamaged(amount);}

    void ActiveRightReleased(InputAction.CallbackContext ctx)
    {
        isActiveRightPressed = false;
        if (activeRight != null)
            activeRight.OnRelease(mana);
    }

    /*void PrimaryPressed(InputAction.CallbackContext ctx)
    {
        isStopingPrimary = false;
        if (!isShootingPrimary && !isShootingDisabled)
        {
            isShootingPrimary = true;
            InvokeRepeating("shootPrimary", 0f, 1 / GameRules.playerShootSpeed);
        }
    }

    void PrimaryReleased(InputAction.CallbackContext ctx)
    {
        isStopingPrimary = true;
    }*/

    public void AddPassiveWeapon(string weaponName)
    {
        GameObject c = Instantiate(Resources.Load<GameObject>("Weapons/" + weaponName),
                new Vector3(this.transform.position.x, this.transform.position.y, this.transform.position.z - 1), Quaternion.identity);
        c.transform.SetParent(transform);
        c.GetComponent<AWeapon>().SetWeaponInput(AWeapon.WeaponInput.both);
    }

    public void AddWeaponSecondary(string weaponName)
    {
        GameObject c = Instantiate(Resources.Load<GameObject>("Weapons/" + weaponName),
                new Vector3(this.transform.position.x, this.transform.position.y, this.transform.position.z - 1), Quaternion.identity);
        c.transform.SetParent(transform);
        c.GetComponent<AWeapon>().SetWeaponInput(AWeapon.WeaponInput.secondary);
    }

    // Update is called once per frame
    void Update()
    {
        speedMultiplier=input.PlayerControls.Focus.IsPressed() ? slowMultiplier : 1;
        horizontalMove = move.x * GameRules.playerMoveSpeed * WorldTimeStop.PlayerDelta * speedMultiplier;
        verticalMove = move.y * GameRules.playerMoveSpeed * verticalMoveMultiplier * WorldTimeStop.PlayerDelta * speedMultiplier;

        transform.position = new Vector3(Mathf.Clamp(transform.position.x + horizontalMove, limits[2], limits[3]), Mathf.Clamp(transform.position.y + verticalMove, limits[1], limits[0]), transform.position.z);
        
    }

    private void FixedUpdate()
    {
        if (!GameRules.playerDisabled && activeRight != null && isActiveRightPressed)
            activeRight.OnHold(mana);
        if (!GameRules.playerDisabled && activeLeft != null && isActiveLeftPressed)
            activeLeft.OnHold(mana);

        
        if (previousPositions != null)
        {
            
            
            if (lastPosition != transform.position)
            {
                Vector2 pos2 = transform.position;
                previousPositions.Push(pos2);
            }
            lastPosition = transform.position;

        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.tag == "EnemyBullet")
        {
            var round=other.GetComponent<CombatRound>();
            if(round!=null && !round.PixelOverlaps(circleCollider))return;
            
            if (!IsInvulnerable && !GameRules.playerDisabled)
            {
                AEnemyBullet bullet = other.GetComponent<AEnemyBullet>();
                GetDamaged(bullet.getDamage());
                bullet.OnTrigger();
            }

        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        // A broad contact may begin in transparent ink; check the exact pixels
        // as the round moves through that contact instead of missing a later hit.
        var round=other.GetComponent<CombatRound>();
        if(round!=null && round.UsesPixelContact)OnTriggerEnter2D(other);
    }

    public float GetManaChanceMultiplier()
    {
        float manaPer = mana / maxMana;
        if (manaPer == 1f)
            return 0;
        return 100 * Mathf.Pow(0.01f, manaPer);
    }

    public float GetHealthChanceMultiplier()
    {
        float healthPer = health / maxHealth;
        if (healthPer == 1f)
            return 0;
        return 4 * Mathf.Pow(0.25f, healthPer);
    }

    IEnumerator Invulnerability(float invulnerabilitySeconds)
    {
        invulnerable = true;
        Color tmp = renderer.color;
        tmp.a = 0.3f;
        renderer.color = tmp;
        bool inv = false;
        // Count real time: WaitForSeconds(0.01f) waits a whole frame, so adding 0.01 per loop
        // made invulnerability last longer the higher the frame time.
        float invulnerableUntil = Time.time + invulnerabilitySeconds;
        while (Time.time < invulnerableUntil)
        {
            // Alternate between 0 and 1 scale to simulate flashing
            if (inv)
            {
                inv = false;
                tmp.a = 1f;
                renderer.color = tmp;
            }
            else
            {
                inv = true;
                tmp.a = 0.15f;
                renderer.color = tmp;
            }
            yield return new WaitForSeconds(invulnerabilityDeltaTime);
        }
        tmp.a = 1f;
        renderer.color = tmp;
        invulnerable = false;
    }

    void Die()
    {
        StopAllCoroutines();
        SoundManager.PlaySound("playerDead", 0.5f);
        PSXPolygonDebris.Spawn(gameObject);
        PSXImpactEffects.Explosion(transform.position, 1.6f);
        BGMManager.StopBGM();
        circleCollider.enabled = false;
        Color tmp = renderer.color;
        tmp.a = 0.0f;
        renderer.color = tmp;
        GameRules.playerMoveSpeed = 0;
        GameRules.playerDisabled = true;
        var visual=GetComponent<PSXObjectVisual>();if(visual!=null)visual.Hide();
        GameOverState.Show();

    }

    void GetDamaged(float damage)
    {
        if(ObserverMode || health<=0 || GameOverState.Active)return;
        if(damage>0)HitTaken?.Invoke();
        PSXImpactEffects.Burst(transform.position,new Color(.15f,.65f,1));
        SoundManager.PlaySound("hurt1", 0.2f);
        health -= damage;
        // Kept small: a short nudge that reads as an impact without throwing the playfield around.
        float moveQuantity = Mathf.Clamp(damage * 0.3f, 0, 4);
        float roughness = Mathf.Clamp(damage * 0.25f, 0, 3);
        CameraShaker.Instance.ShakeOnce(moveQuantity, roughness, 0.1f, 0.5f);
        healthBar.Set(health);
        invulnerable = true;
        Score.MultiplierDown();
        if (health <= 0)
        {
            Die();
        }
        else
        {
            StartCoroutine("Invulnerability", invulnerabilitySeconds);
        }
    }

    /*private void NextShootIdx()
    {
        currBulletOutIdx = (currBulletOutIdx + 1) % shootNumber;
    }*/

    /*
    void shootPrimary()
    {
        if (isStopingPrimary || isShootingDisabled) {
            isShootingPrimary = false;
            CancelInvoke("shootPrimary");
            return;
        }

        SoundManager.PlaySound("shot1", 0.1f);
        for (int i = 0; i < GameRules.playerShootNumber; i++){
            Bullet b = Instantiate(bulletPrimary, spawnPoints[i] + transform.position, Quaternion.identity).GetComponent<Bullet>();
            b.setProperties(new Dictionary<string, float> { { "speed", GameRules.playerShootTravelSpeed }, { "damage", GameRules.playerShootDamage }, { "size", GameRules.playerShootSize }, {"followness", GameRules.playerShootFollowness } });
        }
        //Bullet b = Instantiate(bulletPrimary, spawnPoints[currBulletOutIdx] + transform.position, Quaternion.identity).GetComponent<Bullet>();
        //b.setProperties(new Dictionary<string, float> { { "speed", shootTravelSpeed }, { "damage", shootDamage }, { "size", shootSize }, { "followness", shootFollowness } });
        //NextShootIdx();
    }*/

    public void PlayHitSound(string sound)
    {
        if (Time.time >= nextHitTime)
        {
            SoundManager.PlaySoundRandomPitch(sound, 0.05f, new float[] { 0.8f, 1.2f });
            // write down when we will be finished:

            nextHitTime = Time.time + hitSoundTimeInterval;
        }
    }

    public void PickUpItem(AItem item)
    {
        if (isShootingPrimary)
        {
            //CancelInvoke("shootPrimary");
            //InvokeRepeating("shootPrimary", 0f, 1 / GameRules.playerShootSpeed);
            //InvokeRepeating("shootPrimary", 0f, 1 / shootSpeed / shootNumber);
        }

    }

    public void HealFlat(float heal)
    {
        health = Mathf.Clamp(health + heal * GameRules.healMultiplier, 0, maxHealth);
        healthBar.Set(health);
    }

    public void HealFraction(float fraction)
    {
        health = Mathf.Clamp(health + maxHealth * Mathf.Clamp01(fraction), 0, maxHealth);
        healthBar.Set(health);
    }

    public void HealManaFlat(float heal)
    {
        var upgrades=GetComponent<CombatUpgrades>();
        if(upgrades!=null)upgrades.StoreOverflow(Mathf.Max(0,mana+heal*GameRules.manaHealMultiplier-maxMana));
        mana = Mathf.Clamp(mana + heal * GameRules.manaHealMultiplier, 0, maxMana);
        manaBar.Set(mana);
    }

    // SETTER ------------------------------------------------

    public void updateBulletSpawnPoints()
    {
        // Picked-up weapons are spawned as children after Awake, so look them up every time.
        foreach (AWeapon weapon in GetComponentsInChildren<AWeapon>())
        {
            weapon.updateSpawnPoints();
        }
    }

    void OnDisable() {
        input?.PlayerControls.Disable();
    }

    // Inputs instances stay enabled after their scene unloads, and their callbacks then run
    // against this destroyed object, so release it with the object.
    void OnDestroy() {
        input?.Dispose();
    }
}
