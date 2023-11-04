using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public abstract class APickable : MonoBehaviour
{
    GameObject player;
    float minDist = 3f;
    float moveSpeed = 100f;
    float pickDist = 1f;
    Rigidbody2D rb;
    bool destroying;
    float destroyingTime = 0f;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        rb = GetComponent<Rigidbody2D>();
        rb.AddForce(Vector3.up * 100f);
    }

    private void FixedUpdate()
    {
        Vector2 thisPosition = transform.position;
        Vector2 playerPosition = player.transform.position;
        float distance = Vector3.Distance(thisPosition, playerPosition);
        if (distance <= minDist * GameRules.pickDistance)
        {
            Vector3 direction = Utils.GetVelocityBetweenVectors(transform.position, player.transform.position);

            rb.velocity = direction * moveSpeed / 4;
        }
        else if (player.transform.position.y > (Camera.UpLimit * 0.4))
        {
            Vector2 direction = Utils.GetVelocityBetweenVectors(transform.position, player.transform.position);
            destroying = false;
            destroyingTime = 0f;
            rb.velocity = direction * moveSpeed / 2;
        }
        if (distance <= pickDist)
        {
            PickUp();
        }
        if (destroying)
        {
            destroyingTime += Time.deltaTime;
            if (destroyingTime > 2)
                Destroy(gameObject);
        }
    }

    protected virtual void PickUp()
    {
        Destroy(gameObject);
    }

    private void OnBecameInvisible()
    {
        destroying = true;
        destroyingTime = 0f;
    }
}
