using UnityEngine;
using System.Collections;
public class BlankBullet : AActiveItem
{

    Vector3 pos;
    GameObject ps;

    new void Start()
    {
        base.Start();
        SetCost(20f);
        SetCD(2f);
    }

    protected new void Update()
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
        

        GameObject ps = (GameObject) Instantiate(Resources.Load("Particles/Ripple"), transform.position, Quaternion.identity);
        ps.transform.localScale = new Vector3(1, 1, 1);
        SoundManager.PlaySound("blankbullet", 0.15f);
        // var main = ps.GetComponent<ParticleSystem>().main;
        //main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        ps.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
        StartCoroutine("DeleteParticle");
        lastCD = 0;
        return mana - cost;
    }

    IEnumerator DeleteParticle()
    {
        yield return new WaitForSeconds(2);

        Destroy(ps);
    }

}
