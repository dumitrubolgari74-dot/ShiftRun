using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Панель настроек в главном меню: громкость музыки и кнопка «Назад».
/// </summary>
public class SettingsController : MonoBehaviour
{
    [Header("Панели")]
    public GameObject settingsPanel;
    public GameObject mainMenuPanel;

    [Header("UI")]
    public Slider musicSlider;

    void Start()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (musicSlider != null)
        {
            float volume = GameSettings.Instance != null
                ? GameSettings.Instance.musicVolume
                : PlayerPrefs.GetFloat("ShiftRun_MusicVolume", 0.6f);

            musicSlider.SetValueWithoutNotify(volume);
            musicSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        }
    }

    public void OpenSettings()
    {
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);

        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);
    }

    public void OnMusicVolumeChanged(float value)
    {
        if (GameSettings.Instance != null)
            GameSettings.Instance.SetMusicVolume(value);
        else if (FoneMusic.Instance != null)
            FoneMusic.Instance.SetVolume(value);
    }
}
