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
    bool launched;
    float scatterTime;
    public bool IsScattering => scatterTime > 0;

    public void Launch(Vector2 velocity, float seconds = 1)
    {
        launched = true;
        scatterTime = Mathf.Max(.1f, seconds);
        rb = GetComponent<Rigidbody2D>();
        rb.linearVelocity = velocity;
    }

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        rb = GetComponent<Rigidbody2D>();
        if (!launched)
            rb.AddForce(Vector3.up * 100f);
    }

    private void FixedUpdate()
    {
        if (collected || player == null)
            return;
        if (launched)
        {
            // Rebound inside the arena so the explosion never throws its reward away.
            Vector2 p = rb.position, v = rb.linearVelocity;
            float left = Camera.LeftLimit + .25f, right = Camera.RightLimit - .25f;
            float bottom = Camera.DownLimit + .25f, top = Camera.UpLimit - .25f;
            if (p.x < left) { p.x = left; v.x = Mathf.Abs(v.x) * .65f; }
            if (p.x > right) { p.x = right; v.x = -Mathf.Abs(v.x) * .65f; }
            if (p.y < bottom) { p.y = bottom; v.y = Mathf.Abs(v.y) * .65f; }
            if (p.y > top) { p.y = top; v.y = -Mathf.Abs(v.y) * .65f; }
            rb.position = p;
            rb.linearVelocity = v;
            destroying = false;
            destroyingTime = 0;
            if (scatterTime > 0)
            {
                scatterTime = Mathf.Max(0, scatterTime - Time.fixedDeltaTime);
                return; // Let the burst open before pickup attraction gathers it.
            }
        }
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
