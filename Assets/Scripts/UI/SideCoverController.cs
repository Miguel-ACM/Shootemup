using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SideCoverController : MonoBehaviour
{
    [SerializeField] private GameObject blackRectLeft;
    [SerializeField] private GameObject blackRectRight;
    private float latestWidth = -1f;
    private float latestHeight = -1f;
    //[SerializeField] private GameObject blackRectRight;

    private void Start()
    {
        UpdateAspectRatio();
        latestWidth = Screen.width;
        latestHeight = Screen.height;
    }

    private void Update()
    {
        UpdateAspectRatio();
    }

    private void UpdateAspectRatio()
    {
        if (Screen.height != latestHeight || Screen.width != latestWidth)
        {
            float currentAspect = (float)Screen.width / Screen.height;
            Debug.Log("w " + Screen.width + " " + Screen.height);
            Debug.Log(currentAspect);

            if (currentAspect > 0.5625)  // 16:9 aspect ratio
            {
                RectTransform blackRectLeftTransform = blackRectLeft.transform as RectTransform;
                RectTransform blackRectRightTransform = blackRectRight.transform as RectTransform;
           
                Vector2 parentSize = ((transform.GetComponentInParent<Canvas>()).transform as RectTransform).sizeDelta;
                Vector2 newSize = new Vector2((parentSize[0] - parentSize[1] * 9 / 16) / 2, parentSize[1]);

                blackRectLeftTransform.sizeDelta = newSize;
                blackRectRightTransform.sizeDelta = newSize;
                blackRectLeft.SetActive(true);
                blackRectRight.SetActive(true);
            }
            else
            {
                blackRectLeft.SetActive(false);
                blackRectRight.SetActive(false);
            }
            latestWidth = Screen.width;
            latestHeight = Screen.height;
        }
    }
}
