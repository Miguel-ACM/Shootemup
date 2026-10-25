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
    bool collected;
    float destroyingTime = 0f;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        rb = GetComponent<Rigidbody2D>();
        rb.AddForce(Vector3.up * 100f);
    }

    private void FixedUpdate()
    {
        if (collected || player == null)
            return;
        Vector2 thisPosition = transform.position;
        Vector2 playerPosition = player.transform.position;
        float distance = Vector3.Distance(thisPosition, playerPosition);
        if (distance <= minDist * GameRules.pickDistance)
        {
            Vector3 direction = Utils.GetVelocityBetweenVectors(transform.position, player.transform.position);

            rb.linearVelocity = direction * moveSpeed / 4;
        }
        else if (player.transform.position.y > (Camera.UpLimit * 0.4))
        {
            Vector2 direction = Utils.GetVelocityBetweenVectors(transform.position, player.transform.position);
            destroying = false;
            destroyingTime = 0f;
            rb.linearVelocity = direction * moveSpeed / 2;
        }
        if (distance <= pickDist)
        {
            collected = true;
            PSXImpactEffects.Burst(transform.position, PSXImpactEffects.PickupColor(this), true);
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
