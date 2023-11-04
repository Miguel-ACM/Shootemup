using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DamagePopup : MonoBehaviour
{
    private TextMeshPro text;

    public static DamagePopup Create(Vector3 position, float damage)
    {
        Debug.Log(GameAssets.i.DamageText);
        Transform damagePopupTransform = Instantiate(GameAssets.i.DamageText, position, Quaternion.identity);
        DamagePopup damagePopup = damagePopupTransform.GetComponent<DamagePopup>();
        damagePopup.Setup(damage);

        return damagePopup;
    }
   
    private void Awake()
    {
        text = GetComponent<TextMeshPro>();
    }

    public void Setup(float damageAmount)
    {
        text.SetText(((int)damageAmount).ToString());
    }

}
