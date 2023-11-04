using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PathCreation;

public class FollowPath : AMover
{
    [SerializeField] private string pathName;
    private PathCreator pathCreator;
    private EndOfPathInstruction endOfPathInstruction;
    public float speed = 5f;
    float distanceTravelled;
    private float origZ;
    Transform moveTarget;


    new void Start()
    {
        base.Start();
        GameObject path = Instantiate(Resources.Load<GameObject>("Paths/" + pathName));
        pathCreator = path.GetComponent<PathCreator>();
        moveTarget = this.transform;
        if (transform.parent != null)
            moveTarget = transform.parent;
        origZ = transform.position.z;
        //pathCreator = this.GetComponent<PathCreator>();
        distanceTravelled = -1;
        endOfPathInstruction = EndOfPathInstruction.Loop;
    }

    // If the path changes during the game, update the distance travelled so that the follower's position on the new path
    // is as close as possible to its position on the old path

    public override void Move()
    {
        if (distanceTravelled < 0)
        {
            distanceTravelled = pathCreator.path.GetClosestDistanceAlongPath(transform.position);
        }
        distanceTravelled += speed * Time.deltaTime * GameRules.enemySpeed;
        Vector2 newPos = pathCreator.path.GetPointAtDistance(distanceTravelled, endOfPathInstruction);
        moveTarget.position = new Vector3(newPos.x, newPos.y, origZ);
    }


}
