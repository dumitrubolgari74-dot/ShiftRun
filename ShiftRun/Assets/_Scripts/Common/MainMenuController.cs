using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class MainMenuController : MonoBehaviour
{
    [Header("Сцена после «Играть»")]
    [Tooltip("Индекс сцены из Build Settings (обычно 1 = первая игровая сцена).")]
    public int firstLevelBuildIndex = 1;

    [Header("Ссылки")]
    public SettingsController settings;
    [Tooltip("Опционально: панель настроек как GameObject.")]
    public GameObject settingsPanelObject;
    [Tooltip("Опционально: панель главного меню (кнопки) как GameObject.")]
    public GameObject mainMenuPanelObject;

    public void StartGame()
    {
        LoadSceneByIndex(firstLevelBuildIndex);
    }

    public void OpenSettings()
    {
        ResolveMenuPanels();

        if (settings != null)
            settings.OpenSettings();

        if (mainMenuPanelObject != null)
            mainMenuPanelObject.SetActive(false);
        if (settingsPanelObject != null)
            settingsPanelObject.SetActive(true);

        StartCoroutine(ForceOpenSettingsEndOfFrame());
    }

    public void LoadSceneByIndex(int buildIndex)
    {
        if (buildIndex < 0 || buildIndex >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogWarning($"[MainMenu] Некорректный build index: {buildIndex}.");
            return;
        }

        SceneManager.LoadScene(buildIndex);
    }

    public void LoadSceneByNumber(string number)
    {
        if (!int.TryParse(number, out int buildIndex))
        {
            Debug.LogWarning($"[MainMenu] Невозможно разобрать номер сцены: {number}");
            return;
        }

        LoadSceneByIndex(buildIndex);
    }

    public void LoadScene(string sceneNameOrIndex)
    {
        if (int.TryParse(sceneNameOrIndex, out int buildIndex))
        {
            LoadSceneByIndex(buildIndex);
            return;
        }

        Debug.LogWarning("[MainMenu] Используйте build index (номер сцены), а не имя.");
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void Awake()
    {
        GameSettings.EnsureExists();

        if (settings == null)
            settings = GetComponentInChildren<SettingsController>(true);

        ResolveMenuPanels();
        AutoWireMainMenuButtons();

        if (GameSettings.Instance != null)
            GameSettings.Instance.ApplyMusicVolume();
    }

    void AutoWireMainMenuButtons()
    {
        TryBindButton("Start", StartGame);
        TryBindButton("Settings", OpenSettings);
        TryBindButton("Quit", QuitGame);
    }

    void ResolveMenuPanels()
    {
        if (settings != null)
        {
            if (settingsPanelObject == null)
                settingsPanelObject = settings.settingsPanel != null ? settings.settingsPanel : settings.gameObject;
            if (mainMenuPanelObject == null)
                mainMenuPanelObject = settings.mainMenuPanel;
        }

        if (mainMenuPanelObject == null)
        {
            Transform buttons = FindDeepChild(transform, "Buttons")
                             ?? FindDeepChild(transform, "MainMenu")
                             ?? FindDeepChild(transform, "MainMenuPanel");
            if (buttons != null)
                mainMenuPanelObject = buttons.gameObject;
        }

        if (settingsPanelObject == null)
        {
            Transform panel = FindDeepChild(transform, "SettingsPanel")
                            ?? FindDeepChild(transform, "Settings");
            if (panel != null)
                settingsPanelObject = panel.gameObject;
        }
    }

    void TryBindButton(string buttonName, UnityEngine.Events.UnityAction action)
    {
        Transform t = FindDeepChild(transform, buttonName);
        if (t == null)
            return;

        Button btn = t.GetComponent<Button>();
        if (btn == null)
            return;

        if (btn.onClick.GetPersistentEventCount() > 0)
            return;

        btn.onClick.AddListener(action);
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

    IEnumerator ForceOpenSettingsEndOfFrame()
    {
        yield return null;

        if (mainMenuPanelObject != null)
            mainMenuPanelObject.SetActive(false);
        if (settingsPanelObject != null)
            settingsPanelObject.SetActive(true);

        if (settings != null && settings.settingsPanel != null)
            settings.settingsPanel.SetActive(true);
    }
}
