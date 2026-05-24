#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// GOI → Setup Meniu Scene — GameSystems, FoneMusic, настройки, кнопки, Build Settings.
/// </summary>
public static class MeniuSceneSetupEditor
{
    const string MeniuPath = "Assets/_Scenes/Meniu.unity";

    [MenuItem("GOI/Setup Meniu Scene")]
    public static void SetupMeniuScene()
    {
        if (!System.IO.File.Exists(MeniuPath))
        {
            Debug.LogError($"[Meniu Setup] Нет сцены: {MeniuPath}");
            return;
        }

        var scene = EditorSceneManager.OpenScene(MeniuPath, OpenSceneMode.Single);
        EnsureEventSystem();

        var canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[Meniu Setup] Canvas не найден.");
            return;
        }

        var systems = FindOrCreate("GameSystems");
        var gameSettings = GetOrAdd<GameSettings>(systems);
        gameSettings.persistBetweenScenes = true;

        var musicGo = FindOrCreate("FoneMusic", systems.transform);
        var music = GetOrAdd<FoneMusic>(musicGo);
        music.playOnStart = true;
        music.persistBetweenScenes = true;
        music.shuffle = false;
        music.loopPlaylist = true;
        music.volume = gameSettings.musicVolume;
        GetOrAdd<AudioSource>(musicGo);

        var menu = GetOrAdd<MainMenuController>(systems);
        menu.firstLevelScene = "Test";
        menu.music = music;

        var settingsCtrl = GetOrAdd<SettingsController>(systems);

        Transform menuPanel = FindChildByName(canvas.transform, "Start")?.parent;
        if (menuPanel == null)
            menuPanel = canvas.transform;

        var settingsPanel = FindOrCreateUiPanel(canvas.transform, "SettingsPanel");
        settingsPanel.SetActive(false);

        var slider = FindOrCreateMusicSlider(settingsPanel.transform);
        var backBtn = FindOrCreateButton(settingsPanel.transform, "BackButton", "Back", new Vector2(0, -80));

        settingsCtrl.settingsPanel = settingsPanel;
        settingsCtrl.mainMenuPanel = menuPanel.gameObject;
        settingsCtrl.musicSlider = slider;
        menu.settings = settingsCtrl;

        WireButton("Start", menu.StartGame);
        WireButton("Quit", menu.QuitGame);
        WireButton("Settings", menu.OpenSettings);
        WireButton("BackButton", settingsCtrl.CloseSettings);

        SetupBuildSettings();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[Meniu Setup] Готово: GameSystems, SettingsPanel, кнопки, Build Settings.");
    }

    static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null)
            return;

        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();
    }

    static GameObject FindOrCreate(string name, Transform parent = null)
    {
        var existing = GameObject.Find(name);
        if (existing != null)
            return existing;

        var go = new GameObject(name);
        if (parent != null)
            go.transform.SetParent(parent, false);

        return go;
    }

    static T GetOrAdd<T>(GameObject go) where T : Component
    {
        var c = go.GetComponent<T>();
        return c != null ? c : go.AddComponent<T>();
    }

    static Transform FindChildByName(Transform root, string name)
    {
        if (root.name == name)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            var found = FindChildByName(root.GetChild(i), name);
            if (found != null)
                return found;
        }

        return null;
    }

    static GameObject FindOrCreateUiPanel(Transform canvas, string name)
    {
        var existing = FindChildByName(canvas, name);
        if (existing != null)
            return existing.gameObject;

        var panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(canvas, false);
        var rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var img = panel.GetComponent<Image>();
        img.color = new Color(0.1f, 0.1f, 0.12f, 0.92f);

        CreateTmpLabel(panel.transform, "SettingsTitle", "Settings", new Vector2(0, 100), 28);
        return panel;
    }

    static Slider FindOrCreateMusicSlider(Transform parent)
    {
        var existing = FindChildByName(parent, "MusicSlider");
        if (existing != null)
            return existing.GetComponent<Slider>();

        CreateTmpLabel(parent, "MusicLabel", "Music volume", new Vector2(0, 40), 18);

        var sliderGo = new GameObject("MusicSlider", typeof(RectTransform), typeof(Slider));
        sliderGo.transform.SetParent(parent, false);
        var rt = sliderGo.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(260, 24);
        rt.anchoredPosition = new Vector2(0, 0);

        var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(sliderGo.transform, false);
        var bgRt = bg.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;
        bg.GetComponent<Image>().color = new Color(0.3f, 0.3f, 0.3f, 1f);

        var fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderGo.transform, false);
        var fillAreaRt = fillArea.GetComponent<RectTransform>();
        fillAreaRt.anchorMin = Vector2.zero;
        fillAreaRt.anchorMax = Vector2.one;
        fillAreaRt.offsetMin = new Vector2(8, 4);
        fillAreaRt.offsetMax = new Vector2(-8, -4);

        var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        var fillRt = fill.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;
        fill.GetComponent<Image>().color = new Color(0.85f, 0.7f, 0.35f, 1f);

        var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(sliderGo.transform, false);
        var handleAreaRt = handleArea.GetComponent<RectTransform>();
        handleAreaRt.anchorMin = Vector2.zero;
        handleAreaRt.anchorMax = Vector2.one;
        handleAreaRt.offsetMin = new Vector2(8, 0);
        handleAreaRt.offsetMax = new Vector2(-8, 0);

        var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handle.transform.SetParent(handleArea.transform, false);
        var handleRt = handle.GetComponent<RectTransform>();
        handleRt.sizeDelta = new Vector2(18, 18);
        handle.GetComponent<Image>().color = Color.white;

        var slider = sliderGo.GetComponent<Slider>();
        slider.fillRect = fillRt;
        slider.handleRect = handleRt;
        slider.targetGraphic = handle.GetComponent<Image>();
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0.6f;
        return slider;
    }

    static Button FindOrCreateButton(Transform parent, string name, string label, Vector2 pos)
    {
        var existing = FindChildByName(parent, name);
        if (existing != null)
            return existing.GetComponent<Button>();

        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(160, 32);
        rt.anchoredPosition = pos;
        go.GetComponent<Image>().color = new Color(0.82f, 0.77f, 0.6f, 1f);

        CreateTmpLabel(go.transform, "Text", label, Vector2.zero, 16);
        return go.GetComponent<Button>();
    }

    static void CreateTmpLabel(Transform parent, string name, string text, Vector2 pos, float fontSize)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(300, 40);
        rt.anchoredPosition = pos;

        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
    }

    static void WireButton(string objectName, UnityEngine.Events.UnityAction action)
    {
        var t = Object.FindObjectsByType<Button>(FindObjectsSortMode.None);
        foreach (var btn in t)
        {
            if (btn.gameObject.name != objectName)
                continue;

            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(action);
            EditorUtility.SetDirty(btn);
            return;
        }

        Debug.LogWarning($"[Meniu Setup] Кнопка не найдена: {objectName}");
    }

    static void SetupBuildSettings()
    {
        var scenes = new[]
        {
            "Assets/_Scenes/Meniu.unity",
            "Assets/_Scenes/Game/Test.unity",
            "Assets/_Scenes/Level_1.unity",
            "Assets/_Scenes/Level_2.unity",
            "Assets/_Scenes/Level_3.unity"
        };

        var list = new System.Collections.Generic.List<EditorBuildSettingsScene>();
        foreach (var path in scenes)
        {
            if (!System.IO.File.Exists(path))
                continue;

            list.Add(new EditorBuildSettingsScene(path, true));
        }

        EditorBuildSettings.scenes = list.ToArray();
        Debug.Log("[Meniu Setup] Build Settings обновлены.");
    }
}
#endif
