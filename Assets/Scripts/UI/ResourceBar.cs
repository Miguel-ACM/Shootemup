using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ResourceBar : MonoBehaviour
{
    [SerializeField] Slider slider;
    [SerializeField] private bool visible = true;
    private Image[] images;
    float maxResource = 1;
    

    private void Awake()
    {
        images = gameObject.GetComponentsInChildren<Image>();
        foreach (Image image in images)
        {
            image.enabled = visible;
        }

        slider = GetComponent<Slider>();
    }

    public void SetMax(float res)
    {
        maxResource = res;
    }

    public void Set(float res)
    {
        slider.value = Mathf.Clamp(res / maxResource, 0, maxResource);
    }

    public void SetVisible(bool visible)
    {
        this.visible = visible;
        foreach (Image image in images)
        {
            image.enabled = visible;
        }
    }

    // Start is called before the first frame update

}
