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
        // The title shares the gameplay scenes' PS1 dithering and colour depth.
        var camera = UnityEngine.Camera.main;
        if (camera != null)
        {
            var presentation = camera.GetComponent<PSXPresentation>();
            if (presentation == null)
                presentation = camera.gameObject.AddComponent<PSXPresentation>();
            presentation.Configure(Resources.Load<Material>("PSXScreenComposite"));
        }
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
