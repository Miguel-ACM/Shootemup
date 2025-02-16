using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boss : AEnemy
{
    [SerializeField] private ResourceBar healthBar;
    private GameObject healthBarGO;

    new public void Start()
    {
        base.Start();
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