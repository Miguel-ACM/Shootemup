using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boss : AEnemy
{
    float initialHealth;
    public float HealthFraction => health / Mathf.Max(1, initialHealth);
    [SerializeField] private ResourceBar healthBar;
    private GameObject healthBarGO;

    protected override void Start()
    {
        base.Start();
        initialHealth = health;
        isBoss = true;
        size = 3.5f;
        healthBarGO = GameObject.Find("UI/ItemsRight/BossHealth");
        healthBar = healthBarGO.GetComponent<ResourceBar>();
        healthBar.SetMax(health);
        healthBarGO.SetActive(true);
        healthBar.SetVisible(true);
    }

    public override void damage(float damage)
    {
        base.damage(damage);
        healthBar.Set(health);
    }

    protected override void Die()
    {
        healthBar.SetVisible(false);
        base.Die();
    }
}
