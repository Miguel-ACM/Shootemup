using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed partial class FieldsBomberBoss : ArmoredHullBoss
{
    // Follow the actual wing cutouts, engine shoulders, barrels and long nose.
    static readonly Vector2[] outline ={new Vector2(-1.7f,.5f),new Vector2(-.58f,1.1f),new Vector2(0,.8f),new Vector2(.58f,1.1f),new Vector2(1.7f,.5f),
        new Vector2(1.48f,-.08f),new Vector2(1.52f,-.72f),new Vector2(1.28f,-.72f),new Vector2(1.28f,-.58f),new Vector2(.75f,-.9f),
        new Vector2(.45f,-.62f),new Vector2(.28f,-.95f),new Vector2(0,-1.13f),new Vector2(-.28f,-.95f),new Vector2(-.45f,-.62f),
        new Vector2(-.75f,-.9f),new Vector2(-1.28f,-.58f),new Vector2(-1.28f,-.72f),new Vector2(-1.52f,-.72f),new Vector2(-1.48f,-.08f)};
    protected override Vector2[] Outline => outline;

    static readonly Vector3[] bays ={new Vector3(-1.19f,-.24f,-.51f),new Vector3(-.97f,-.36f,-.51f),new Vector3(-.75f,-.48f,-.51f),
        new Vector3(.75f,-.48f,-.51f),new Vector3(.97f,-.36f,-.51f),new Vector3(1.19f,-.24f,-.51f)};
    Vector2 phraseAxis = Vector2.down;
    protected override void Configure()
    {
        hullScale = 1.4f;
        health = 1200;
        attackInterval = (.8f);
        shellTravelDistance = 4.6f;
    }

    protected override void MoveBoss(float dt)
    {
        station.x = Mathf.Sin(motionAge * .5f) * 2;
        base.MoveBoss(dt);
    }

    protected override void PressureAttack()
    {
    } // One score owns every battery and its rests.
}
