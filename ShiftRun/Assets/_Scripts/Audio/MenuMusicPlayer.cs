using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MenuMusicPlayer : MonoBehaviour
{
    [Header("Menu tracks")]
    public List<AudioClip> menuTracks = new List<AudioClip>();

    [Header("Playback")]
    public bool playOnStart = true;
    public bool shuffle;
    public bool loopPlaylist = true;

    [Range(0f, 1f)]
    public float volume = 0.6f;

    AudioSource _source;
    int _currentIndex = -1;

    void Awake()
    {
        _source = GetComponent<AudioSource>();
        _source.playOnAwake = false;
        _source.loop = false;

        float savedVolume = GameSettings.Instance != null
            ? GameSettings.Instance.musicVolume
            : PlayerPrefs.GetFloat("ShiftRun_MusicVolume", volume);
        SetVolume(savedVolume);
    }

    void Start()
    {
        if (playOnStart)
            Play();
    }

    void Update()
    {
        if (_source.isPlaying || menuTracks.Count == 0)
            return;

        if (!loopPlaylist && !shuffle && _currentIndex >= menuTracks.Count - 1)
            return;

        PlayNext();
    }

    public void SetVolume(float value)
    {
        volume = Mathf.Clamp01(value);
        if (_source != null)
            _source.volume = volume;
    }

    public void Play()
    {
        if (menuTracks.Count == 0)
            return;

        _currentIndex = shuffle ? Random.Range(0, menuTracks.Count) : 0;
        PlayAt(_currentIndex);
    }

    public void PlayNext()
    {
        if (menuTracks.Count == 0)
            return;

        if (shuffle)
        {
            _currentIndex = Random.Range(0, menuTracks.Count);
        }
        else
        {
            _currentIndex++;
            if (_currentIndex >= menuTracks.Count)
                _currentIndex = loopPlaylist ? 0 : menuTracks.Count - 1;
        }

        PlayAt(_currentIndex);
    }

    void PlayAt(int index)
    {
        if (index < 0 || index >= menuTracks.Count)
            return;
        AudioClip clip = menuTracks[index];
        if (clip == null)
            return;

        _source.clip = clip;
        _source.volume = volume;
        _source.Play();
    }
}
