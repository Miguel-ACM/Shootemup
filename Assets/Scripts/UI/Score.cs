using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class Score : MonoBehaviour
{
    static string fmt = "000000000.##";
    static int score = 0;
    static int multiplier = 1;
    static float scoreThisMult = 0;
    static float timeSinceNoScoreUp = 0f;
    static int[] multUpThresholds = { 200, 400, 800, 1600, 3200, 6400, 12800, 24800 };
    static ResourceBar scoreMultBar;
    static TextMeshProUGUI tm;
    static TextMeshProUGUI multiplierText;
    static AnimationCurve increaseSizeCurve;
    static float timeScoreDown = 2f;


    public static void IncreaseScore(int quantity)
    {
        timeSinceNoScoreUp = 0f;
        score += Mathf.RoundToInt(quantity * multiplier * GameRules.scoreMultiplier);
        if (multiplier < 8 && scoreThisMult < multUpThresholds[multiplier - 1])
        {
            scoreThisMult += quantity;
        }
        tm.SetText(score.ToString(fmt));
    }

    public static float GetPercentageNextMultiplier()
    {
        return scoreThisMult / multUpThresholds[multiplier - 1];
    }

    void Update()
    {
        if (scoreThisMult >= multUpThresholds[multiplier - 1] && multiplier < 8)
        {
            SetMultiplier(multiplier + 1);
            scoreThisMult = 100;
        }
        timeSinceNoScoreUp += Time.deltaTime;
        if (timeSinceNoScoreUp > timeScoreDown && scoreThisMult > 0)
        {
            scoreThisMult -= Time.deltaTime * 50 * multiplier;
            if (scoreThisMult < 0)
            {
                if (multiplier > 1)
                {
                    SetMultiplier(multiplier - 1);
                    timeSinceNoScoreUp = 0f;
                    scoreThisMult = multUpThresholds[multiplier - 1] / 2;
                }
                else 
                {
                    scoreThisMult = 0;
                }
                
            }
        }
        scoreMultBar.Set(GetPercentageNextMultiplier());
    }

    public static void MultiplierDown()
    {
        if (multiplier > 1)
        {
            SetMultiplier(multiplier - 1);
            scoreThisMult = multUpThresholds[multiplier - 1] / 2;
        }
        else
        {
            scoreThisMult = 0;
        }
    }

    private static void SetMultiplier(int new_mult)
    {
        bool disp_animation = false;
        if (multiplier < new_mult)
        {
            disp_animation = true;
        }
        multiplier = new_mult;
        if (multiplier > 1)
        {
            multiplierText.SetText("x" + multiplier.ToString());
        } else
        {
            multiplierText.SetText("");
        }
        

    }

    private void Start()
    {
        tm = GetComponent<TextMeshProUGUI>();
        multiplierText = GetComponentsInChildren<TextMeshProUGUI>()[1];
        scoreMultBar = GetComponentInChildren<ResourceBar>();
        increaseSizeCurve = new AnimationCurve(new Keyframe[] { new Keyframe(0,0), new Keyframe(0.5f, 1f), new Keyframe(1, 0) });
        increaseSizeCurve.preWrapMode = WrapMode.Clamp;
        increaseSizeCurve.postWrapMode = WrapMode.Clamp;
    }
}
