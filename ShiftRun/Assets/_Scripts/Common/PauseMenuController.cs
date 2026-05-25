using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenuController : MonoBehaviour
{
    public static PauseMenuController Instance { get; private set; }

    [Header("UI")]
    public GameObject pausePanel;
    public SettingsController settingsController;

    [Header("Controls")]
    public KeyCode toggleKey = KeyCode.Escape;
    public bool pauseOnStart;

    [Header("Navigation")]
    [Tooltip("Индекс сцены главного меню в Build Settings.")]
    public int mainMenuBuildIndex = 0;

    bool _isPaused;

    static bool _sceneHookInstalled;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void InstallSceneHook()
    {
        if (_sceneHookInstalled)
            return;

        _sceneHookInstalled = true;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.buildIndex == 0)
            return;

        if (FindObjectOfType<PauseMenuController>() != null)
            return;

        var go = new GameObject("PauseMenuController");
        go.AddComponent<PauseMenuController>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        GameSettings.EnsureExists();
        AutoResolveReferences();
    }

    void Start()
    {
        SetPaused(pauseOnStart);
        AutoWirePauseButtons();
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            if (_isPaused)
                ResumeGame();
            else
                PauseGame();
        }
    }

    void OnDisable()
    {
        if (Instance == this)
            Instance = null;

        Time.timeScale = 1f;
        SetGameMusicPaused(false);
    }

    public void PauseGame() => SetPaused(true);
    public void ResumeGame() => SetPaused(false);

    public void TogglePause() => SetPaused(!_isPaused);

    public void OpenSettings()
    {
        if (settingsController != null)
            settingsController.OpenSettings();
    }

    public void BackToMainMenu()
    {
        Time.timeScale = 1f;
        SetGameMusicPaused(false);
        SceneManager.LoadScene(mainMenuBuildIndex);
    }

    void SetPaused(bool paused)
    {
        _isPaused = paused;
        Time.timeScale = paused ? 0f : 1f;
        SetGameMusicPaused(paused);

        if (pausePanel != null)
            pausePanel.SetActive(paused);

        if (!paused && settingsController != null)
            settingsController.CloseSettings();
    }

    static void SetGameMusicPaused(bool paused)
    {
        var players = FindObjectsOfType<GameMusicPlayer>(true);
        for (int i = 0; i < players.Length; i++)
        {
            if (paused)
                players[i].PausePlayback();
            else
                players[i].ResumePlayback();
        }
    }

    void AutoResolveReferences()
    {
        if (pausePanel == null)
        {
            Transform t = FindDeepChildByName("PauseMenu");
            if (t != null)
                pausePanel = t.gameObject;
        }

        if (settingsController == null && pausePanel != null)
            settingsController = pausePanel.GetComponentInChildren<SettingsController>(true);
    }

    void AutoWirePauseButtons()
    {
        if (pausePanel == null)
            return;

        TryBindButton("Continue", ResumeGame);
        TryBindButton("Resume", ResumeGame);
        TryBindButton("Main Menu", BackToMainMenu);
        TryBindButton("Settings", OpenSettings);
    }

    void TryBindButton(string buttonName, UnityEngine.Events.UnityAction action)
    {
        Transform t = FindDeepChildByName(buttonName, pausePanel.transform);
        if (t == null)
            return;

        Button btn = t.GetComponent<Button>();
        if (btn == null)
            return;

        if (btn.onClick.GetPersistentEventCount() > 0)
            return;

        btn.onClick.AddListener(action);
    }

    Transform FindDeepChildByName(string name, Transform root = null)
    {
        if (root != null)
            return FindDeepChild(root, name);

        Scene scene = SceneManager.GetActiveScene();
        var roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Transform found = FindDeepChild(roots[i].transform, name);
            if (found != null)
                return found;
        }

        return null;
    }

    static Transform FindDeepChild(Transform root, string name)
    {
        if (root == null)
            return null;
        if (root.name == name)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDeepChild(root.GetChild(i), name);
            if (found != null)
                return found;
        }

        return null;
    }
}
