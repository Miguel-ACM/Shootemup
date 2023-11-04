using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DriftDown : AMover
{
    public float[] limits;
    public Vector3 direction = new Vector3(0f, -1f, 0f);
    public float speed = -1f;
    bool movingIn = true;
    bool movingOut = false;
    private float elapsedTime = 0f;
    private float lerpTime = 1.5f;
    private Vector3 startPosition;
    private Vector3 lerpPosition;
    private AnimationCurve curve;
    [SerializeField] private float timeUntilOut = 10f;


    new void Start()
    {
        base.Start();
        limits = Camera.getLimits();
        curve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));
        curve.preWrapMode = WrapMode.ClampForever;
        curve.postWrapMode = WrapMode.ClampForever;
        curve.SmoothTangents(0, 3);
        startPosition = transform.position;
        lerpPosition = new Vector3(transform.position.x, transform.position.y - limits[0] * 2, transform.position.z);
        if (timeUntilOut > 0f)
            StartCoroutine("MoveOut");
    }

    public override void Move()
    {
        if (movingIn)
        {
            elapsedTime += Time.deltaTime;
            float percentageComplete = elapsedTime / lerpTime;
            transform.position = Vector3.Lerp(startPosition, lerpPosition, curve.Evaluate(percentageComplete));
            if (percentageComplete >= 1)
            {
                movingIn = false;
                shooter.OnArrive();
            }
        }
        else if (movingOut)
        {
            elapsedTime += Time.deltaTime;
            float percentageComplete = elapsedTime / lerpTime;
            transform.position = Vector3.Lerp(lerpPosition, startPosition, curve.Evaluate(percentageComplete));
            if (percentageComplete >= 1)
            {
                movingOut = false;
                shooter.OnLeave();
            }
        }
    }

    IEnumerator MoveOut()
    {
        yield return new WaitForSeconds(timeUntilOut + lerpTime);
        elapsedTime = 0f;
        movingOut = true;
        // Code to execute after the delay
    }

    public override void OnShootEnd()
    {
        if (timeUntilOut < 0f)
            movingOut = true;
    }


}
