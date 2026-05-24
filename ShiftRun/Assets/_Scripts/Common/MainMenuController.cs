using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Главное меню: старт, настройки, выход.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("Сцена после «Играть»")]
    public string firstLevelScene = "Test";

    [Header("Ссылки")]
    public SettingsController settings;
    public FoneMusic music;

    public void StartGame()
    {
        LoadScene(firstLevelScene);
    }

    public void OpenSettings()
    {
        if (settings != null)
            settings.OpenSettings();
    }

    public void LoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("[MainMenu] Имя сцены пустое.");
            return;
        }

        SceneManager.LoadScene(sceneName);
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
        if (music == null)
            music = FoneMusic.Instance;

        if (GameSettings.Instance != null)
            GameSettings.Instance.ApplyMusicVolume();
    }
}
