using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ItemPool
{
    private static List<string> common = new List<string> {"RabbitsFoot", "BlankBullet", "TwerpItem", "Magnet", "AK45Item"};
    private static List<string> rare = new List<string> { "Caffeine", "ProteinSupplement", "AMoonItem", "BabyYouItem", "ElectricityItem" };
    private static List<string> epic = new List<string> { "AnotherArm", "AngryMoonItem"};
    private static List<string> legendary = new List<string> { "Telekinesis", "ThreeMoonsItem" };
    private static List<string> defaultPool = new List<string> { "TwerpItem", "AMoonItem", "BabyYouItem" };

    private static string LowerPool(string pool)
    {
        if (pool == "rare")
            return "common";
        if (pool == "epic")
            return "rare";
        if (pool == "legendary")
            return "epic";
        return "default";
    }


    private static string GetNonEmptyPoolString(string pool){
        int i = 0;
        List<string> p;
        while (i < 3)
        {
            pool = LowerPool(pool);
            p = PoolStringToList(pool);
            if (p.Count > 0)
            {
                return pool;
            }
            i++;
        }
        return "default";
    }

    private static List<string> PoolStringToList(string pool)
    {
        List<string> p;
        switch (pool)
        {
            case "common":
                p = common;
                break;
            case "rare":
                p = rare;
                break;
            case "epic":
                p = epic;
                break;
            case "legendary":
                p = legendary;
                break;
            default:
                p = defaultPool;
                break;
        }
        return p;
    }

    private static string GetItemStringFromPool(string pool, out string actualPool)
    {
        List<string> p = PoolStringToList(pool);
        if (p.Count == 0)
        {
            pool = GetNonEmptyPoolString(pool);
            p = PoolStringToList(pool);
        }
        int itemI = Random.Range(0, p.Count);
        string itemString = p[itemI];
        if (pool != "default")
        {
            p.RemoveAt(itemI);
        }
        actualPool = pool;
        return itemString;



    }

    public static string GetItem(out string pool)
    {
        float randomNumber = Random.Range(0f, 1f) + GameRules.luck;
        pool = "legendary";
        if (randomNumber < 0.52f) {
            pool = "common";
        } else if (randomNumber < 0.82f) {
            pool = "rare";
        } else if (randomNumber < 0.96f) {
            pool = "epic";
        }
        pool = "legendary";

        if (randomNumber < 0.22f)
        {
            pool = "common";
        }
        else if (randomNumber < 0.52f)
        {
            pool = "rare";
        }
        else if (randomNumber < 0.76f)
        {
            pool = "epic";
        }
        string itemString = GetItemStringFromPool(pool, out string actualPool);
        pool = actualPool;
        return itemString;
        //GameObject item = Resources.Load<GameObject>("Items/" + itemString);
        //return item;

    }


}
