using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(AudioSource))]
public class FoneMusic : MonoBehaviour
{
    public static FoneMusic Instance { get; private set; }

    [Header("Треки")]
    public List<AudioClip> tracks = new List<AudioClip>();
    [Tooltip("Треки для меню (обычно сцена 0).")]
    public List<AudioClip> menuTracks = new List<AudioClip>();
    [Tooltip("Треки для игровых сцен.")]
    public List<AudioClip> gameTracks = new List<AudioClip>();

    [Header("Воспроизведение")]
    public bool playOnStart = true;
    public bool shuffle;
    public bool loopPlaylist = true;
    [Tooltip("Использовать отдельные списки menu/game по сценам.")]
    public bool useMenuGameTrackLists = true;
    [Tooltip("Build index сцены меню.")]
    public int menuSceneBuildIndex = 0;
    [Tooltip("Автовыбор трека по индексу сцены из Build Settings.")]
    public bool autoSelectTrackBySceneBuildIndex = true;
    [Tooltip("Смещение индекса сцены для выбора трека. 0 = scene 0 -> track 0.")]
    public int sceneTrackOffset;
    [Header("Scene music map (JSON)")]
    [Tooltip("Использовать JSON-карту для выбора трека по сцене.")]
    public bool useSceneMusicMapJson = true;
    [Tooltip("Путь к JSON в Resources (без расширения).")]
    public string sceneMusicMapResourcePath = "Audio/scene_music_map";

    [Range(0f, 1f)]
    public float volume = 0.6f;

    [Tooltip("Не уничтожать при смене сцены.")]
    public bool persistBetweenScenes = true;

    AudioSource _source;
    int _currentIndex = -1;
    readonly List<int> _shuffleOrder = new List<int>();
    SceneMusicMapData _sceneMusicMap;

    [System.Serializable]
    class SceneMusicEntry
    {
        public int buildIndex;
        public int trackIndex;
    }

    [System.Serializable]
    class SceneMusicMapData
    {
        public bool stopIfNotMapped = true;
        public int fallbackTrackIndex = -1;
        public SceneMusicEntry[] scenes;
    }

    void Awake()
    {
        if (GameSettings.Instance != null)
            volume = GameSettings.Instance.musicVolume;

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

        _source = GetComponent<AudioSource>();
        _source.playOnAwake = false;
        _source.loop = false;
        _source.volume = volume;

        LoadSceneMusicMap();
    }

    void Start()
    {
        if (playOnStart)
            Play();
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Update()
    {
        if (_source.isPlaying || tracks.Count == 0)
            return;

        if (!loopPlaylist && !shuffle && _currentIndex >= tracks.Count - 1)
            return;

        PlayNext();
    }

    public void Play()
    {
        if (tracks.Count == 0)
            return;

        if (shuffle)
            PlayRandom();
        else
        {
            _currentIndex = 0;
            PlayClipAt(_currentIndex);
        }
    }

    public void PlayNext()
    {
        if (tracks.Count == 0)
            return;

        if (shuffle)
        {
            PlayRandom();
            return;
        }

        _currentIndex++;
        if (_currentIndex >= tracks.Count)
            _currentIndex = loopPlaylist ? 0 : tracks.Count - 1;

        PlayClipAt(_currentIndex);
    }

    public void Stop()
    {
        _source.Stop();
    }

    public void SetVolume(float value)
    {
        volume = Mathf.Clamp01(value);
        _source.volume = volume;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (TryApplyMenuGameTrackLists(scene.buildIndex))
            return;

        if (TryApplySceneMusicFromJson(scene.buildIndex))
            return;

        if (!autoSelectTrackBySceneBuildIndex || tracks.Count == 0)
            return;

        int index = scene.buildIndex - sceneTrackOffset;
        if (index < 0 || index >= tracks.Count)
            return;

        if (index == _currentIndex && _source.isPlaying)
            return;

        _currentIndex = index;
        PlayClipAt(_currentIndex);
    }

    bool TryApplyMenuGameTrackLists(int buildIndex)
    {
        if (!useMenuGameTrackLists)
            return false;

        if (buildIndex == menuSceneBuildIndex)
        {
            int menuIndex = ResolveTrackIndexFromPool(menuTracks, 0);
            if (menuIndex >= 0)
            {
                PlayMappedTrack(menuIndex);
                return true;
            }

            return false;
        }

        int gameIndex = ResolveTrackIndexFromPool(gameTracks, Mathf.Max(0, buildIndex - 1));
        if (gameIndex >= 0)
        {
            PlayMappedTrack(gameIndex);
            return true;
        }

        return false;
    }

    int ResolveTrackIndexFromPool(List<AudioClip> pool, int sceneRelativeIndex)
    {
        if (pool == null || pool.Count == 0 || tracks == null || tracks.Count == 0)
            return -1;

        AudioClip pick = pool[Mathf.Abs(sceneRelativeIndex) % pool.Count];
        if (pick == null)
            return -1;

        for (int i = 0; i < tracks.Count; i++)
        {
            if (tracks[i] == pick)
                return i;
        }

        tracks.Add(pick);
        return tracks.Count - 1;
    }

    bool TryApplySceneMusicFromJson(int buildIndex)
    {
        if (!useSceneMusicMapJson || tracks.Count == 0 || _sceneMusicMap == null)
            return false;

        if (_sceneMusicMap.scenes != null)
        {
            for (int i = 0; i < _sceneMusicMap.scenes.Length; i++)
            {
                SceneMusicEntry entry = _sceneMusicMap.scenes[i];
                if (entry == null || entry.buildIndex != buildIndex)
                    continue;

                PlayMappedTrack(entry.trackIndex);
                return true;
            }
        }

        if (_sceneMusicMap.fallbackTrackIndex >= 0)
        {
            PlayMappedTrack(_sceneMusicMap.fallbackTrackIndex);
            return true;
        }

        if (_sceneMusicMap.stopIfNotMapped)
        {
            Stop();
            return true;
        }

        return false;
    }

    void PlayMappedTrack(int trackIndex)
    {
        if (trackIndex < 0 || trackIndex >= tracks.Count)
            return;
        if (_currentIndex == trackIndex && _source.isPlaying)
            return;

        _currentIndex = trackIndex;
        PlayClipAt(_currentIndex);
    }

    void LoadSceneMusicMap()
    {
        _sceneMusicMap = null;
        if (!useSceneMusicMapJson || string.IsNullOrWhiteSpace(sceneMusicMapResourcePath))
            return;

        TextAsset mapAsset = Resources.Load<TextAsset>(sceneMusicMapResourcePath);
        if (mapAsset == null || string.IsNullOrWhiteSpace(mapAsset.text))
            return;

        try
        {
            _sceneMusicMap = JsonUtility.FromJson<SceneMusicMapData>(mapAsset.text);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[FoneMusic] Ошибка JSON scene map: {ex.Message}");
        }
    }

    void PlayRandom()
    {
        if (_shuffleOrder.Count == 0 || _shuffleOrder.Count != tracks.Count)
            RebuildShuffleOrder();

        _currentIndex = _shuffleOrder[Random.Range(0, _shuffleOrder.Count)];
        PlayClipAt(_currentIndex);
    }

    void RebuildShuffleOrder()
    {
        _shuffleOrder.Clear();
        for (int i = 0; i < tracks.Count; i++)
            _shuffleOrder.Add(i);
    }

    void PlayClipAt(int index)
    {
        if (index < 0 || index >= tracks.Count || tracks[index] == null)
            return;

        _source.clip = tracks[index];
        _source.volume = volume;
        _source.Play();
    }
}
