using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoadingManager : MonoBehaviour
{
    public static SceneLoadingManager Instance { get; private set; }

    bool isPaused;
    GameObject panel;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                if (panel != null)
                {
                    panel.SetActive(false);
                }
                Time.timeScale = 1f;
                isPaused = false;
            }
            else
            {
                if (panel != null)
                {
                    panel.SetActive(true);
                }
                Time.timeScale = 0f;
                isPaused = true;
            }
        }
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "MainScene" || scene.name == "Boss Scene")
        {
            panel = GameObject.FindWithTag("PauseMenu");
        }
    }

    public void GoToNextScene()
    {
        StartCoroutine(waitAndLoad(SceneManager.GetActiveScene().buildIndex + 1));
    }

    // IEnumerator waitAndLoad(int scenenum)
    // {
    //     yield return new WaitForSeconds(3);
    //     SceneManager.LoadScene(scenenum);

    // }

    IEnumerator waitAndLoad(int sceneIndex)
    {
        yield return new WaitForSeconds(1);
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneIndex);
        op.allowSceneActivation = false;   // load now, but don't switch yet

        float timer = 0f;

        // Wait until at least 3 seconds have passed AND loading is done
        while (timer < 2f || op.progress < 0.9f)
        {
            timer += Time.unscaledDeltaTime;
            yield return null;
        }

        op.allowSceneActivation = true;    // switch to the new scene
    }

    public void ReloadScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void LoadSceneByName(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
