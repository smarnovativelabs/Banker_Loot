using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using UnityStandardAssets.Characters.FirstPerson;

public class GamePlaySound : MonoBehaviour
{
    public static GamePlaySound instance;

    [Header("Audio Source")]
    public AudioSource gameSound;

    private void Awake()
    {
        instance = this;
    }

    public void GameSound(AudioClip _clip)
    {
        gameSound.clip = _clip;
        if(SoundController.instance.isSoundOn)
        {
            gameSound.clip = _clip;
            gameSound.Play();
        }
    }

    public void OnChangeMusicGamePlay()
    {
        if(SoundController.instance.isSoundOn)
        {
            //gameSound.Play();
          //  FirstPersonController.instance.isMusicOnFootStep = true;
        }
        else
        {
            //gameSound.Stop();
          //  FirstPersonController.instance.isMusicOnFootStep = false;
        }
    }
}