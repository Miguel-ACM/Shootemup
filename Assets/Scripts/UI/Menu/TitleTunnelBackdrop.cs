using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class TitleTunnelBackdrop : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset()
    {
        SceneManager.sceneLoaded -= SceneReady;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Install()
    {
        SceneManager.sceneLoaded += SceneReady;
    }

    static void SceneReady(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Title")
            new GameObject("Title scenery").AddComponent<TitleTunnelBackdrop>();
    }

    IEnumerator Start()
    {
        var menu = FindAnyObjectByType<NewGame>()?.GetComponentInParent<Menu>();
        if (menu != null)
        {
            var panel = menu.transform.parent.GetComponent<Image>();
            if (panel != null)
                panel.color = new Color(.025f, .04f, .045f, .38f);
        }
        yield return LevelEnvironment.CreateTitleBackdrop();
    }
}
