using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundController : MonoBehaviour
{
    public static SoundController instance;

    public bool isSoundOn;

    public bool isMusicOn;
    public AudioSource inGameSoundSource;
    public AudioSource bgMusicPlayer;
    public AudioSource interactionSoundPlayer;
    public AudioClip itemPlaceSound;
    public AudioClip farmDoorOpen;
    public AudioClip farmDoorClose;

    private void Awake()
    {
        InstanceCheck();
    }
    void InstanceCheck()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void OnSoundChanged(int _val)
    {
        PlayerPrefs.SetInt("Sound", _val);
        isSoundOn = _val == 1;
    }

    public void OnMusicChanged(int _val)
    {
        PlayerPrefs.SetInt("Music", _val);
        isMusicOn = _val == 1;
        if (isMusicOn)
        {
            bgMusicPlayer.Play();
        }
        else
        {
            bgMusicPlayer.Stop();
        }
    }
    public void OnPlayInteractionSound(AudioClip _clip)
    {
        if (isSoundOn)
            interactionSoundPlayer.PlayOneShot(_clip);
    }
    public void PlayInGameSound(AudioClip _clip)
    {
        if (_clip == null)
        {
            return;
        }
        if (isSoundOn)
            inGameSoundSource.PlayOneShot(_clip);
    }
}