using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class Warning : MonoBehaviour
{
    private TextMeshProUGUI warning;
    private TextMeshProUGUI textbottom;
    private TextMeshProUGUI texttop;
    private Image linetop;
    private Image linebottom;
    [SerializeField] AnimationCurve curve;
    float t = 0;
    private bool blinking = false;

    public void Start()
    {
        TextMeshProUGUI[] texts = GetComponentsInChildren<TextMeshProUGUI>();
        Image[] images = GetComponentsInChildren<Image>();
        warning = texts[0];
        textbottom = texts[1];
        texttop = texts[2];
        linetop = images[0];
        linebottom = images[1];
        curve.preWrapMode = WrapMode.Loop;
        curve.postWrapMode = WrapMode.Loop;

        warning.color = new Color(1, 1, 1, 0);
        textbottom.color = new Color(1, 1, 1, 0);
        texttop.color = new Color(1, 1, 1, 0);
        linetop.color = new Color(1, 1, 1, 0);
        linebottom.color = new Color(1, 1, 1, 0);
    }

    public void Update()
    {
        if (blinking)
        {
            t += Time.deltaTime;
            float move_x = 450 * Time.deltaTime;
            float a = curve.Evaluate(t);
            if (t > 3)
            {
                blinking = false;
                a = 0;
            }
            warning.color = new Color(1, 1, 1, a);
            textbottom.color = new Color(1, 1, 1, a);
            textbottom.transform.position -= new Vector3(move_x, 0);
            texttop.transform.position -= new Vector3(move_x, 0);
            texttop.color = new Color(1, 1, 1, a);
            linetop.color = new Color(1, 1, 1, a);
            linebottom.color = new Color(1, 1, 1, a);
            
        }

    }

    public void blink()
    {
        blinking = true;
    }
}
