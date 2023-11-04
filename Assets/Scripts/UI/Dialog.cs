using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Dialog : MonoBehaviour
{
    private TextMeshProUGUI title;
    private TextMeshProUGUI flavour;
    private TextMeshProUGUI description;
    [SerializeField] private AnimationCurve appearCurve = new AnimationCurve(new Keyframe[] { new Keyframe(0, 0), new Keyframe(1, 1, 0, 1) });
    [SerializeField] private AnimationCurve disappearCurve = new AnimationCurve(new Keyframe[] { new Keyframe(0, 1, 0, 1), new Keyframe(1, 0) });
    bool isUp = false;
    bool isMoving = false;
    float movingTime = 0f;
    float totalMovingTime = 0.5f;
    List<string[]> actionQueue = new List<string[]>();
    private Vector3 up;
    private Vector3 down;

    public void Start()
    {
        up = transform.position;
        down = new Vector3(transform.position.x, -transform.position.y, transform.position.z);
        transform.position = down;
        TextMeshProUGUI[] texts = GetComponentsInChildren<TextMeshProUGUI>();
        title = texts[0];
        flavour = texts[1];
        description = texts[2];
    }

    public void Update()
    {
        if (isMoving)
        {
            if (movingTime < totalMovingTime)
            {
                movingTime += Time.deltaTime;
                if (isUp)
                {
                    transform.position = Vector3.Lerp(down, up, disappearCurve.Evaluate(movingTime / totalMovingTime));
                }
                else
                {
                    transform.position = Vector3.Lerp(down, up, appearCurve.Evaluate(movingTime / totalMovingTime));
                }
            }
            else
            {
                if (isUp)
                    transform.position = down;
                else
                    transform.position = up;
                isMoving = false;
                isUp = !isUp;
            }
        }
        if (!isMoving)
        {
            if (actionQueue.Count > 0)
            {
                string[] action = actionQueue[0];
                actionQueue.RemoveAt(0);
                if ((action[0] == "up" && !isUp) || (action[0] == "down" && isUp))
                {
                    isMoving = true;
                    movingTime = 0f;
                } else if (action[0] == "forceUp") {
                    isMoving = true;
                    movingTime = 0f;
                    isUp = false;
                }
                
                else if (action[0] == "setText")
                {
                    title.SetText(action[1]);
                    flavour.SetText(action[2]);
                    description.SetText(action[3]);
                    title.color = AItem.GetRarityColor(action[4]);                    
                }
            }
        }
    }

    /*public void ShowItemDesc(string _title, string _flavour, string _description)
    {
        bool hasDown = false;
        bool hasUp = false;
        bool subText = false;
        for (int i = 0; i < actionQueue.Count; i++ )
        {
            if (actionQueue[i][0] == "down")
                hasDown = true;
            if (actionQueue[i][0] == "up" && hasDown)
                hasUp = true;
            if (actionQueue[i][0] == "setText")
            {
                actionQueue[i] = new string[] { "setText", _title, _flavour, _description };
                subText = true;
            }
        }

        if (!hasDown)
            actionQueue.Add(new string[] { "down" });
        if (!subText)
            actionQueue.Add(new string[] { "setText", _title, _flavour, _description });
        if (!hasUp)
            actionQueue.Add(new string[] { "up" });
    }*/
    public void ShowItemDesc(string _title, string _flavour, string _description, string _rarity)
    {
        CancelAll();
        actionQueue.Add(new string[] { "setText", _title, _flavour, _description, _rarity });
        actionQueue.Add(new string[] { "forceUp" });
    }

    private void CancelAll()
    {
        isMoving = false;
        actionQueue.Clear();
    }

    public void Hide()
    {
        if (isUp || (!isUp && isMoving))
        {
            actionQueue.Add(new string[] { "down" });
        }
    }
}
