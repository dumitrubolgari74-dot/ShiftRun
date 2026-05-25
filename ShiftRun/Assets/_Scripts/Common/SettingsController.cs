using UnityEngine;
using UnityEngine.UI;

public class SettingsController : MonoBehaviour
{
    [Header("Панели")]
    public GameObject settingsPanel;
    public GameObject mainMenuPanel;

    [Header("UI")]
    public Slider musicSlider;

    void Awake()
    {
        GameSettings.EnsureExists();
        AutoResolveReferences();
    }

    void Start()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        AutoWireSettingsButtons();

        if (musicSlider != null)
        {
            float volume = GameSettings.Instance != null
                ? GameSettings.Instance.musicVolume
                : PlayerPrefs.GetFloat("ShiftRun_MusicVolume", 0.6f);

            musicSlider.SetValueWithoutNotify(volume);
            musicSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        }
    }

    void OnEnable()
    {
        RefreshMusicSliderFromSettings();
    }

    public void OpenSettings()
    {
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);

        if (settingsPanel != null)
            settingsPanel.SetActive(true);

        RefreshMusicSliderFromSettings();
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

    void RefreshMusicSliderFromSettings()
    {
        if (musicSlider == null)
            return;

        float volume = GameSettings.Instance != null
            ? GameSettings.Instance.musicVolume
            : PlayerPrefs.GetFloat("ShiftRun_MusicVolume", 0.6f);
        musicSlider.SetValueWithoutNotify(volume);
    }

    void AutoResolveReferences()
    {
        if (settingsPanel == null)
            settingsPanel = gameObject;

        if (mainMenuPanel == null && transform.parent != null)
        {
            Transform sibling = transform.parent.Find("Buttons");
            if (sibling == null)
                sibling = transform.parent.Find("MainMenu");
            if (sibling == null)
                sibling = transform.parent.Find("MainMenuPanel");

            if (sibling != null)
                mainMenuPanel = sibling.gameObject;
        }

        if (musicSlider == null)
            musicSlider = GetComponentInChildren<Slider>(true);
    }

    void AutoWireSettingsButtons()
    {
        TryBindButton("Back", CloseSettings);
        TryBindButton("Close", CloseSettings);
    }

    void TryBindButton(string buttonName, UnityEngine.Events.UnityAction action)
    {
        if (settingsPanel == null)
            return;

        Transform t = FindDeepChild(settingsPanel.transform, buttonName);
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
}
