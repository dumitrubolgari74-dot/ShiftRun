using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Фоновая музыка: список треков, проигрывание по очереди или случайно.
/// Повесь на объект с AudioSource (или создастся автоматически).
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class FoneMusic : MonoBehaviour
{
    public static FoneMusic Instance { get; private set; }

    [Header("Треки")]
    public List<AudioClip> tracks = new List<AudioClip>();

    [Header("Воспроизведение")]
    public bool playOnStart = true;
    public bool shuffle;
    public bool loopPlaylist = true;

    [Range(0f, 1f)]
    public float volume = 0.6f;

    [Tooltip("Не уничтожать при смене сцены.")]
    public bool persistBetweenScenes = true;

    AudioSource _source;
    int _currentIndex = -1;
    readonly List<int> _shuffleOrder = new List<int>();

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
    }

    void Start()
    {
        if (playOnStart)
            Play();
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
