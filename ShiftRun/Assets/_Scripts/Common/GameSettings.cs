using UnityEngine;

/// <summary>
/// Сохранённые настройки (PlayerPrefs). Живёт между сценами.
/// </summary>
public class GameSettings : MonoBehaviour
{
    public static GameSettings Instance { get; private set; }

    const string MusicVolumeKey = "ShiftRun_MusicVolume";

    [Range(0f, 1f)]
    public float musicVolume = 0.6f;

    public bool persistBetweenScenes = true;

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

        musicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, musicVolume);
        ApplyMusicVolume();
    }

    public void SetMusicVolume(float value)
    {
        musicVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(MusicVolumeKey, musicVolume);
        PlayerPrefs.Save();
        ApplyMusicVolume();
    }

    public void ApplyMusicVolume()
    {
        if (FoneMusic.Instance != null)
            FoneMusic.Instance.SetVolume(musicVolume);
    }
}
