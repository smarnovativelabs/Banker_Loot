using UnityEngine;

/// <summary>
/// Looping background music for one level. Follows the Music on/off setting
/// (SoundController.isMusicOn / the settings-panel Music button) and silences the
/// menu music while this level is loaded.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class LevelMusic : MonoBehaviour
{
    public AudioSource source;
    [Range(0f, 1f)] public float volume = 0.35f;
    [Tooltip("Silence the menu background music while this level music is active.")]
    public bool replaceMenuMusic = true;

    bool subscribed;

    void Awake()
    {
        if (source == null) source = GetComponent<AudioSource>();
        source.loop = true;
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.volume = volume;
    }

    void OnEnable()
    {
        var sc = SoundController.instance;
        if (sc != null)
        {
            sc.levelMusicActive = replaceMenuMusic;
            if (replaceMenuMusic && sc.bgMusicPlayer != null) sc.bgMusicPlayer.Stop();
            SoundController.MusicToggled += Apply;
            subscribed = true;
            Apply(sc.isMusicOn);
        }
        else
        {
            // Level opened directly (no SoundController yet): use the saved setting.
            Apply(PlayerPrefs.GetInt("Music", 1) == 1);
        }
    }

    void OnDisable()
    {
        if (subscribed)
        {
            SoundController.MusicToggled -= Apply;
            subscribed = false;
        }
        if (source != null) source.Stop();

        var sc = SoundController.instance;
        if (sc != null && replaceMenuMusic)
        {
            sc.levelMusicActive = false;
            if (sc.isMusicOn && sc.bgMusicPlayer != null) sc.bgMusicPlayer.Play();
        }
    }

    void Apply(bool musicOn)
    {
        if (source == null) return;
        if (musicOn) { if (!source.isPlaying) source.Play(); }
        else source.Stop();
    }
}
