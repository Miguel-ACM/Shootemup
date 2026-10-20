using System.Collections;
using UnityEngine;

public class NewGame : AMenuElement {
    Fade fade;
    CloudSpawner cloudSpawner;

    protected override void Start() {
        base.Start();
        //fade = GameObject.Find("UI").GetComponent<Fade>();
        cloudSpawner = GameObject.Find("spawnClouds").GetComponent<CloudSpawner>();
    }
    
    public override void OnAccept() {
        //fade.FadeOut();
        cloudSpawner.SpawnClouds(false);
        // wait for second before loading new scene
        StartCoroutine(WaitForSecond());
    }

    private IEnumerator WaitForSecond() {
        yield return new WaitForSeconds(3f);
        GameRules.StartNewRun();
        UnityEngine.SceneManagement.SceneManager.LoadScene("L1");
    }
}
