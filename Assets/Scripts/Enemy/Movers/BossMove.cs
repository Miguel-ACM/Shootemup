using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossMove : AMover
{
    public float[] limits;
    public Vector3 direction = new Vector3(0f, -1f, 0f);
    public float speed = -1f;
    bool movingIn = true;
    private float elapsedTime = 0f;
    [SerializeField] private float lerpTime = 3f;
    private Vector3 startPosition;
    [SerializeField] private Vector3 lerpPosition;
    private AnimationCurve curve;
    [SerializeField] private float timeUntilOut = -1f;
    [SerializeField] private AMover nextMover;


    new void Start()
    {
        base.Start();
        nextMover.SetMove(false);
        limits = Camera.getLimits();
        curve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));
        curve.preWrapMode = WrapMode.ClampForever;
        curve.postWrapMode = WrapMode.ClampForever;
        curve.SmoothTangents(0, 3);
        startPosition = transform.position;
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
                nextMover.SetMove(true);
                shooter.OnArrive();
                movingIn = false;
            }
        }
    }


}
