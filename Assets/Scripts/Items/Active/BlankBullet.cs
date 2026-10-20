using UnityEngine;
using System.Collections;
public class BlankBullet : AActiveItem
{

    Vector3 pos;

    protected override void Start()
    {
        base.Start();
        SetCost(20f);
        SetCD(2f);
    }

    protected override void Update()
    {
        base.Update();
        if (lastCD < cd)
        {
            GameObject[] enemyBullets = GameObject.FindGameObjectsWithTag("EnemyBullet");
            for (int i = 0; i < enemyBullets.Length; i++)
            {
                if (Vector2.Distance(enemyBullets[i].transform.position, pos) < 5f)
                {
                    Destroy(enemyBullets[i]);
                }
            }
        }

    }

    private void Awake()
    {
        SetData("Blank bullet", "Puf!", "Erase all the bullets nearby.", AItem.ItemType.active);
    }

    public override float OnPress(float mana)
    {
        float v = CheckValidPress(mana);
        if (v < 0)
            return v;

        pos = transform.position;
        

        // The old lit-sprite ripple rendered nothing under the 2D renderer; show the 5-unit
        // clearing radius as a PSX shockwave ring instead.
        PSXFireballs.Shockwave(transform.position, 5f, .45f, new Color(.55f, .8f, 1f));
        SoundManager.PlaySound("blankbullet", 0.15f);
        lastCD = 0;
        return mana - cost;
    }

}
