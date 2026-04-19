using System.Resources;
using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class SCManager : MonoBehaviour
{

    public static SCManager instance;
    string lastScene;

    private void Awake()
    {

        if (instance == null) instance = this;
        else { Destroy(gameObject); return; }
        DontDestroyOnLoad(gameObject);

    }
    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
        lastScene = sceneName;
    }

    public void LoadSceneAdditive(string sceneName, Transform referenceTransform)
    {
        //SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
        StartCoroutine(LoadAndCenter(sceneName, referenceTransform));
    }
    private IEnumerator LoadAndCenter(string sceneName, Transform referenceTransform)
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        while (!asyncLoad.isDone)
            yield return null;

        Scene mapScene = SceneManager.GetSceneByName(sceneName);

        foreach (GameObject obj in mapScene.GetRootGameObjects())
        {
            obj.transform.position = referenceTransform.position + referenceTransform.forward * 5f;

            obj.transform.rotation = referenceTransform.rotation;
        }
    }
    public void UnloadScene(string sceneName)
    {
        SceneManager.UnloadSceneAsync(sceneName);
    }
    public void ReloadScene()
    {
        if (lastScene != null)
        {
            SceneManager.LoadScene(lastScene);
        }
        else
        {
            Scene currentScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(currentScene.name);
        }
    }
    public void CloseGame()
    {
    #if UNITY_STANDALONE
        Application.Quit();
    #endif
    #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
    #endif
    }
}