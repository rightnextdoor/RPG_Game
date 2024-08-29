using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    [SerializeField] private float sfxMinimumDistance;
    [SerializeField] private AudioSource[] sfx;
    [SerializeField] private AudioSource[] bgm;

    public bool playBgm;
    private int bgmIndex;
    private int sfxIndex;

    private bool canPlaySFX;

    private void Awake()
    {
        if (instance != null)
            Destroy(instance.gameObject);
        else
            instance = this;

        Invoke("AllowSFX", 1f);
    }

    private void Update()
    {
        if (!playBgm)
            StopAllBGM();
        else
        {
            if (!bgm[bgmIndex].isPlaying)
                PlayRandomBGM();
        }
    }

    public void PlaySFX(string _sfxName, Transform _source)
    {
        FindSFXIndex(_sfxName);

        if (sfxIndex == -1)
        {
            Debug.LogWarning("No play with sfx name: " + _sfxName);
            return;
        }

        //if (sfx[sfxIndex].isPlaying)
        //    return;

        if (canPlaySFX == false)
            return;

        if (_source != null && Vector2.Distance(PlayerManager.instance.player.transform.position, _source.position) > sfxMinimumDistance)
            return;

        if (sfxIndex < sfx.Length)
        {
            sfx[sfxIndex].pitch = Random.Range(.85f, 1.1f);
            sfx[sfxIndex].Play();
        }
    }

    public void StopSFX(string _sfxName)
    {
        FindSFXIndex(_sfxName);

        if (sfxIndex == -1)
        {
            Debug.LogWarning("No stop with sfx name: " + _sfxName);
            return;
        }
        sfx[sfxIndex].Stop();
    }

    public void StopSFXWithTime(string _sfxName)
    {
        FindSFXIndex(_sfxName);

        if (sfxIndex == -1)
        {
            Debug.LogWarning("No play with sfx with time name: " + _sfxName);
            return;
        }

        StartCoroutine(DecreaseVolume(sfx[sfxIndex]));
    }

    private IEnumerator DecreaseVolume(AudioSource _audio)
    {
        float defaultVolume = _audio.volume;

        while (_audio.volume > .1f)
        {
            _audio.volume -= _audio.volume * .2f;
            yield return new WaitForSeconds(.25f);

            if (_audio.volume <= .1f)
            {
                _audio.Stop();
                _audio.volume = defaultVolume;
                break;
            }
        }
    }

    private void FindSFXIndex(string _sfxName)
    {
        for (int i = 0; i < sfx.Length; i++)
        {
            if (sfx[i].name == _sfxName)
            {
                sfxIndex = i;
                return;
            }
            else
                sfxIndex = -1;
        }
    }

    public void PlayRandomBGM()
    {
        bgmIndex = Random.Range(0, bgm.Length);
        PlayBGMWithIndex(bgmIndex);
    }

    public void PlayBGMWithIndex(int _bgmIndex)
    {
        bgmIndex = _bgmIndex;

        StopAllBGM();
        bgm[bgmIndex].Play();
    }

    public void PlayBGM(string _bgmName)
    {
        FindBGMIndex(_bgmName);

        StopAllBGM();
        bgm[bgmIndex].Play();
    }

    private void StopAllBGM()
    {
        for (int i = 0; i < bgm.Length; i++)
        {
            bgm[i].Stop();
        }
    }

    public void StopBGMWithTime(string _bgmName)
    {
        FindBGMIndex(_bgmName);

        if (bgmIndex == -1)
        {
            Debug.LogWarning("No play with bgm with time name: " + _bgmName);
            return;
        }

        StartCoroutine(DecreaseVolume(bgm[bgmIndex]));
    }

    private void FindBGMIndex(string _bgmName)
    {
        for (int i = 0; i < bgm.Length; i++)
        {
            if (bgm[i].name == _bgmName)
            {
                bgmIndex = i;
                return;
            }
            else
                sfxIndex = -1;
        }
    }

    private void AllowSFX() => canPlaySFX = true;
}
