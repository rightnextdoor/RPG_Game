using System.Collections;
using System;
using UnityEngine;
using Random = UnityEngine.Random;
using Unity.VisualScripting;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    [SerializeField] private float sfxMinimumDistance;

    [SerializeField] private SoundFX[] soundFX;
    [SerializeField] private BGM[] bgm;
    [SerializeField] private BGM_Random[] bgmRandom;
    public bool playRandomBgm;

    private BGM_Random[] bgmChange;

    private BGM playBGM;
    private bool canPlaySFX;
    private bool isPlaying;

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
        {

            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);

        Invoke("AllowSFX", .1f);
        SetupAudio();
    }

    private void SetupAudio()
    {
        foreach (SoundFX s in soundFX)
        {
            s.source = gameObject.AddComponent<AudioSource>();
            s.source.clip = s.clip;
            s.source.volume = s.volume;
            s.source.pitch = s.pitch;
            s.source.loop = s.loop;
            s.source.playOnAwake = s.playOnAwake;
            s.source.outputAudioMixerGroup = s.output;
        }

        foreach (BGM_Random s in bgmRandom)
        {
            s.source = gameObject.AddComponent<AudioSource>();
            s.source.clip = s.clip;
            s.source.volume = s.volume;
            s.source.pitch = s.pitch;
            s.source.loop = s.loop;
            s.source.outputAudioMixerGroup = s.output;
        }

        foreach (BGM s in bgm)
        {
            s.source = gameObject.AddComponent<AudioSource>();
            s.source.clip = s.clip;
            s.source.volume = s.volume;
            s.source.pitch = s.pitch;
            s.source.loop = s.loop;
            s.source.outputAudioMixerGroup = s.output;
        }
    }

    private void Start()
    {
        bgmChange = bgmRandom;
    }

    private void Update()
    {
        if (playBGM != null)
        {
            if (!playBGM.source.isPlaying)
                playRandomBgm = true;
        }

        if (playRandomBgm)
        {
            if(!isPlaying)
                PlayRandomBGM();
        }
    }

    #region SoundFX
    public void PlaySFX(string _sfxName, Transform _source)
    {
        SoundFX s = Array.Find(soundFX, sound => sound.name == _sfxName);
        if (s == null)
        {
            Debug.LogWarning("Sounds: " + _sfxName + " not found");
            return;
        }

        if (canPlaySFX == false)
            return;

        if (_source != null && Vector2.Distance(PlayerManager.instance.player.transform.position, _source.position) > sfxMinimumDistance)
            return;

        s.source.volume = s.volume;
        s.source.pitch = Random.Range(.85f, 1.1f);
        s.source.playOnAwake = s.playOnAwake;
        s.source.loop = s.loop;
        s.source.Play();
    }

    public void StopSFX(string _sfxName)
    {
        foreach (SoundFX s in soundFX)
        {
            if (s.name == _sfxName)
                s.source.Stop();
        }
    }

    public void StopSFXWithTime(string _sfxName)
    {
        SoundFX s = Array.Find(soundFX, sound => sound.name == _sfxName);
        if (s == null)
        {
            Debug.LogWarning("Sounds: " + _sfxName + " not found");
            return;
        }

        StartCoroutine(DecreaseVolume(s.source));
    }

    private void AllowSFX() => canPlaySFX = true;

    #endregion


    #region BGM

    public void PlayBGM(string _name)
    {
        playRandomBgm = false;
        StopAllBGM();

        BGM s = Array.Find(bgm, random => random.name == _name);
        if (s == null)
        {
            Debug.LogWarning("Sounds: " + _name + " not found");
            return;
        }

        s.source.volume = s.volume;
        s.source.pitch = s.pitch;
        s.source.loop = s.loop;

        s.source.Play();
        playBGM = s;
    }

    public void PlayRandomBGM()
    {
        playRandomBgm = true;
        StopAllBGM();
        
        int index = Random.Range(0, bgmChange.Length); // added -1 for index out of range.
        string name = bgmChange[index].name;

        BGM_Random s = Array.Find(bgmChange, random => random.name == name);
        if (s == null)
        {
            Debug.LogWarning("Sounds: " + name + " not found");
            return;
        }

        s.source.volume = s.volume;
        s.source.pitch = s.pitch;
        s.source.loop = s.loop;

        s.source.Play();
        isPlaying = true;
    }


    private void StopAllBGM()
    {
        string name = null;
        foreach (BGM_Random s in bgmChange)
        {
            if (s.source.isPlaying)
            {
                name = s.name;
            }
            s.source.Stop();
        }
        foreach (BGM s in bgm)
        {
            s.source.Stop();
        }
        isPlaying = false;

        RemoveBGMFromList(name);
    }

    private void RemoveBGMFromList(string name)
    {
        if (name != null)
        {
            if (bgmChange.Length == 1)
            {
                bgmChange = new BGM_Random[bgmRandom.Length];
                bgmChange = bgmRandom;
                return;
            }
            
            BGM_Random[] _bgmList = new BGM_Random[bgmChange.Length - 1];
            int count = 0;
            for (int i = 0; i < bgmChange.Length; i++)
            {
                if (bgmChange[i].name != name)
                {
                    _bgmList[count] = bgmChange[i];
                    count++;
                }
            }

            bgmChange = new BGM_Random[_bgmList.Length];
            bgmChange = _bgmList;
        }
    }

    #endregion

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

}
