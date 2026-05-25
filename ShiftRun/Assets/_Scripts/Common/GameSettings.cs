using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

public class GameSettings : MonoBehaviour
{
    public static GameSettings Instance { get; private set; }

    const string MusicVolumeKey = "ShiftRun_MusicVolume";

    [Range(0f, 1f)]
    public float musicVolume = 0.6f;

    public bool persistBetweenScenes = true;
    [Tooltip("Использовать JSON-файл для сохранения/загрузки аудио-настроек.")]
    public bool useJsonPersistence = true;

    [System.Serializable]
    class AudioSettingsSaveData
    {
        public float musicVolume = 0.6f;
    }

    static string SettingsFilePath =>
        Path.Combine(Application.persistentDataPath, "audio-settings.json");

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        EnsureExists();
    }

    public static GameSettings EnsureExists()
    {
        if (Instance != null)
            return Instance;

        var existing = FindObjectOfType<GameSettings>();
        if (existing != null)
            return existing;

        var go = new GameObject("GameSettings");
        return go.AddComponent<GameSettings>();
    }

    void Awake()
    {
        if (persistBetweenScenes)
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Instance = this;
        }

        LoadSettings();
        ApplyMusicVolume();
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void SetMusicVolume(float value)
    {
        musicVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(MusicVolumeKey, musicVolume);
        PlayerPrefs.Save();
        SaveSettings();
        ApplyMusicVolume();
    }

    public void ApplyMusicVolume()
    {
        if (FoneMusic.Instance != null)
            FoneMusic.Instance.SetVolume(musicVolume);

        var menuPlayers = FindObjectsOfType<MenuMusicPlayer>(true);
        for (int i = 0; i < menuPlayers.Length; i++)
            menuPlayers[i].SetVolume(musicVolume);

        var gamePlayers = FindObjectsOfType<GameMusicPlayer>(true);
        for (int i = 0; i < gamePlayers.Length; i++)
            gamePlayers[i].SetVolume(musicVolume);
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyMusicVolume();
    }

    void LoadSettings()
    {
        if (!useJsonPersistence)
        {
            musicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, musicVolume);
            return;
        }

        try
        {
            if (File.Exists(SettingsFilePath))
            {
                string json = File.ReadAllText(SettingsFilePath);
                var data = JsonUtility.FromJson<AudioSettingsSaveData>(json);
                if (data != null)
                {
                    musicVolume = Mathf.Clamp01(data.musicVolume);
                    return;
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[GameSettings] Не удалось загрузить JSON: {ex.Message}");
        }

        musicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, musicVolume);
    }

    void SaveSettings()
    {
        if (!useJsonPersistence)
            return;

        try
        {
            var data = new AudioSettingsSaveData { musicVolume = musicVolume };
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(SettingsFilePath, json);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[GameSettings] Не удалось сохранить JSON: {ex.Message}");
        }
    }
}
