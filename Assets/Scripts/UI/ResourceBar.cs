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
        Initialize();
        foreach (Image image in images)
        {
            image.enabled = visible;
        }

    }

    void Initialize()
    {
        if (slider == null) slider = GetComponent<Slider>();
        if (images == null) images = GetComponentsInChildren<Image>(true);
    }

    public void SetMax(float res)
    {
        maxResource = res;
    }

    public void Set(float res)
    {
        Initialize();
        slider.value = Mathf.Clamp01(res / maxResource);
    }

    public void SetVisible(bool visible)
    {
        Initialize();
        this.visible = visible;
        foreach (Image image in images)
        {
            image.enabled = visible;
        }
    }

    // Start is called before the first frame update

}
