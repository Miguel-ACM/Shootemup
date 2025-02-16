using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ABullet : MonoBehaviour
{
    public float speed = 0.00002f;
    public Vector3 direction = new Vector3(0f,1f,-10f);
    public float damage = 1f;
    public float size = 1f;
    public float followness = 0.01f;

    public void setProperties(Dictionary<string, float> properties){
        speed = Utils.GetValueOrDefault<string, float>(properties, "speed", 0.00002f);
        damage = Utils.GetValueOrDefault<string, float>(properties, "damage", 1f);
        size = Utils.GetValueOrDefault<string, float>(properties, "size", 1f);
        if (size != 1f)
        {
            transform.localScale = new Vector3(size, size, size);
        }
        followness = Utils.GetValueOrDefault<string, float>(properties, "followness", 0f);
        float directionX = Utils.GetValueOrDefault<string, float>(properties, "directionX", 0f);
        float directionY = Utils.GetValueOrDefault<string, float>(properties, "directionY", 1f);
        direction = new Vector3(directionX, directionY, -10f);
    }
}
