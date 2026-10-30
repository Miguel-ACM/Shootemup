using UnityEngine;
using System.Collections;
public class BlankBullet : AActiveItem
{
    // Bullets keep being erased for this long, independently of the shorter cooldown.
    const float ClearTime = 2f;
    Vector3 pos;

    protected override void Start()
    {
        base.Start();
        SetCost(20f);
        SetCD(1f);
    }

    protected override void Update()
    {
        base.Update();
        if (lastCD < ClearTime)
        {
            GameObject[] enemyBullets = GameObject.FindGameObjectsWithTag("EnemyBullet");
            for (int i = 0; i < enemyBullets.Length; i++)
            {
                if (Vector2.Distance(enemyBullets[i].transform.position, pos) < 5f)
                {
                    var round = enemyBullets[i].GetComponent<AEnemyBullet>();
                    if (round is PulseLaser)
                        continue;
                    if (CombatUpgrades.Current != null)
                        CombatUpgrades.Current.Block();
                    if (round != null)
                        round.Vanish();
                    else
                        Destroy(enemyBullets[i]);
                }
            }
        }

    }

    private void Awake()
    {
        SetData("Blank bullet", "Puf!", "Erase nearby bullets.", AItem.ItemType.active);
    }

    public override float OnPress(float mana)
    {
        float v = CheckValidPress(mana);
        if (v < 0)
            return v;

        pos = transform.position;


        // Its own cartridge burst: spokes and a turning rim hold the 5-unit area for the clearing window.
        ClearEffects.Blank(transform.position, 5f, ClearTime);
        SoundManager.PlaySound("blankbullet", 0.15f);
        lastCD = 0;
        return mana - cost;
    }
}
