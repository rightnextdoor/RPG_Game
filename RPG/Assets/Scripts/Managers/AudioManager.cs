using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    [Header("Mixer")]
    public AudioMixer masterMixer;

    [Header("Fade Durations")]
    [SerializeField] private float fadeOutDuration = 1f;
    [SerializeField] private float fadeInDuration = 1f;

    [Header("SFX Groups")]
    public List<SoundLibrary> soundGroups;

    [Header("Music")]
    public List<ZoneMusicLibrary> zoneMusicDefinitions;
    public List<BackgroundMusicLibrary> backgroundMusicGroups;

    private List<Sound> currentZoneMusicList = new List<Sound>();
    private List<Sound> shuffledMusic = new List<Sound>();
    private int currentMusicIndex = 0;

    [SerializeField] private List<ZoneSceneMapping> zoneSceneMapping = new List<ZoneSceneMapping>();

    private Sound currentBackgroundMusic;
    private Coroutine crossfadeCoroutine;

    private string currentZone = "";
    private bool isRandomMusicEnabled = true;

    void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
        InitializeSounds();
    }

    private void InitializeSounds()
    {
        foreach (var group in soundGroups)
        {
            foreach (var sound in group.sounds)
            {
                GameObject obj = new GameObject("Sound_" + sound.name);
                obj.transform.SetParent(transform);
                sound.source = obj.AddComponent<AudioSource>();
                sound.source.clip = sound.clip;
                sound.source.volume = sound.volume;
                sound.source.pitch = sound.pitch;
                sound.source.loop = sound.loop;
                sound.source.outputAudioMixerGroup = sound.mixerGroup;
            }
        }

        foreach (var group in zoneMusicDefinitions)
        {
            foreach (var sound in group.musicTracks)
            {
                GameObject obj = new GameObject("Sound_" + sound.name);
                obj.transform.SetParent(transform);
                sound.source = obj.AddComponent<AudioSource>();
                sound.source.clip = sound.clip;
                sound.source.volume = sound.volume;
                sound.source.pitch = sound.pitch;
                sound.source.loop = sound.loop;
                sound.source.outputAudioMixerGroup = sound.mixerGroup;
            }
        }

        foreach (var group in backgroundMusicGroups)
        {
            foreach (var sound in group.musicTracks)
            {
                GameObject obj = new GameObject("Sound_" + sound.name);
                obj.transform.SetParent(transform);
                sound.source = obj.AddComponent<AudioSource>();
                sound.source.clip = sound.clip;
                sound.source.volume = sound.volume;
                sound.source.pitch = sound.pitch;
                sound.source.loop = sound.loop;
                sound.source.outputAudioMixerGroup = sound.mixerGroup;
            }
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        string newZone = GetZoneFromScene(scene.name);
        if (newZone != currentZone)
        {
            currentZone = newZone;
            PlayZoneMusic(currentZone);
        }
    }

    private string GetZoneFromScene(string sceneName)
    {
        if (zoneSceneMapping == null)
        {
            Debug.LogWarning("ZoneSceneMapping is not assigned in AudioManager.");
            return null;
        }
        foreach (var map in zoneSceneMapping)
        {
            foreach (var zone in map.zones)
            {
                if (zone.sceneNames.Contains(sceneName))
                    return zone.zoneName;
            }
        }
        

        Debug.LogWarning($"Scene '{sceneName}' not mapped to any zone.");
        return null;
    }

    public void PlayZoneMusic(string zoneName)
    {
        ZoneMusicLibrary def = zoneMusicDefinitions.Find(z => z.zoneName == zoneName);
        if (def == null)
        {
            Debug.LogWarning("Zone music not found for: " + zoneName);
            return;
        }

        currentZoneMusicList = new List<Sound>(def.musicTracks);
        ShuffleMusic();
        PlayNextRandomTrack();
    }

    private void ShuffleMusic()
    {
        shuffledMusic = currentZoneMusicList.OrderBy(x => Random.value).ToList();
        currentMusicIndex = 0;
    }

    private void PlayNextRandomTrack()
    {
        if (!isRandomMusicEnabled || shuffledMusic.Count == 0) return;

        if (currentMusicIndex >= shuffledMusic.Count)
        {
            ShuffleMusic();
        }

        PlayMusic(shuffledMusic[currentMusicIndex], () => {
            currentMusicIndex++;
            PlayNextRandomTrack();
        });
    }

    private void PlayMusic(Sound sound, System.Action onComplete = null)
    {
        if (crossfadeCoroutine != null) StopCoroutine(crossfadeCoroutine);
        crossfadeCoroutine = StartCoroutine(CrossfadeMusic(sound, onComplete));
    }

    private IEnumerator CrossfadeMusic(Sound newSound, System.Action onComplete = null)
    {
        if (currentBackgroundMusic != null && currentBackgroundMusic.source != null)
        {
            while (currentBackgroundMusic.source.volume > 0.01f)
            {
                currentBackgroundMusic.source.volume -= Time.deltaTime;
                yield return null;
            }
            currentBackgroundMusic.source.Stop();
        }

        newSound.source.volume = 0f;
        newSound.source.Play();
        currentBackgroundMusic = newSound;

        while (newSound.source.volume < newSound.volume)
        {
            newSound.source.volume += Time.deltaTime;
            yield return null;
        }

        if (!newSound.loop && onComplete != null)
        {
            yield return new WaitForSeconds(newSound.clip.length);
            onComplete.Invoke();
        }
    }

    public void PlayEventMusic(string groupName)
    {
        var group = backgroundMusicGroups.Find(g => g.backgroundName == groupName);
        if (group == null)
        {
            Debug.LogWarning("Background music group not found: " + groupName);
            return;
        }

        if (group.musicTracks.Count == 0) return;

        isRandomMusicEnabled = false;
        PlayMusic(group.musicTracks[0]);
    }

    public void StopEventMusicAndResumeRandom()
    {
        if (currentBackgroundMusic != null && currentBackgroundMusic.source != null)
        {
            StartCoroutine(CrossfadeMusicToRandom());
        }
        else
        {
            isRandomMusicEnabled = true;
            PlayNextRandomTrack();
        }
    }

    private IEnumerator CrossfadeMusicToRandom()
    {
        // Check if zone music is available and ready
        if (currentBackgroundMusic == null || currentBackgroundMusic.source == null)
        {
            Debug.LogWarning("Zone music not available. Skipping music transition.");
            yield break;  // Exit early if no music is available.
        }

        // Fade out current event music
        float startVolume = currentBackgroundMusic.source.volume;
        float t = 0f;

        while (t < fadeOutDuration)
        {
            currentBackgroundMusic.source.volume = Mathf.Lerp(startVolume, 0f, t / fadeOutDuration);
            t += Time.deltaTime;
            yield return null;
        }

        currentBackgroundMusic.source.Stop();

        isRandomMusicEnabled = true;

        if (shuffledMusic.Count == 0 || !IsZoneMusicLoaded())
        {
            yield return new WaitUntil(() => IsZoneMusicLoaded());  // Wait until zone music is ready if necessary
        }

        if (shuffledMusic.Count == 0)
        {
            ShuffleMusic();
        }

        if (currentMusicIndex >= shuffledMusic.Count)
        {
            ShuffleMusic();
        }

        Sound nextMusic = shuffledMusic[currentMusicIndex];
        currentMusicIndex++;

        nextMusic.source.volume = 0f;
        nextMusic.source.Play();
        currentBackgroundMusic = nextMusic;

        float fadeInTime = 0f;
        while (fadeInTime < fadeInDuration)
        {
            nextMusic.source.volume = Mathf.Lerp(0f, nextMusic.volume, fadeInTime / fadeInDuration);
            fadeInTime += Time.deltaTime;
            yield return null;
        }

        nextMusic.source.volume = nextMusic.volume;

        if (!nextMusic.loop)
        {
            yield return new WaitForSeconds(nextMusic.clip.length);
            PlayNextRandomTrack();
        }
    }

    // Helper function to check if the zone music is loaded
    private bool IsZoneMusicLoaded()
    {
        // Check if zone music has been loaded (this depends on your specific loading mechanism)
        return currentBackgroundMusic != null && currentBackgroundMusic.source != null && currentBackgroundMusic.source.isPlaying;
    }

    public void PlaySFX(string name, Transform sourceTransform = null, float maxDistance = 15f)
    {
        foreach (var group in soundGroups)
        {
            var sound = group.sounds.FirstOrDefault(s => s.name == name);
            if (sound != null)
            {
                if (sourceTransform != null &&
                    Vector2.Distance(PlayerManager.instance.player.transform.position, sourceTransform.position) > maxDistance)
                    return;

                if (sound.loop)
                {
                    if (!sound.source.isPlaying)
                        sound.source.Play();
                }
                else
                {
                    sound.source.PlayOneShot(sound.clip);
                }

                return;
            }
        }

        Debug.LogWarning("SFX not found: " + name);
    }


    public void PlaySFXWithDelay(string name, float delay, Transform sourceTransform = null, float maxDistance = 15f)
    {
        StartCoroutine(PlaySFXDelayedCoroutine(name, delay, sourceTransform, maxDistance));
    }

    private IEnumerator PlaySFXDelayedCoroutine(string name, float delay, Transform sourceTransform, float maxDistance)
    {
        yield return new WaitForSeconds(delay);
        PlaySFX(name, sourceTransform, maxDistance);
    }

    public void StopSFX(string name)
    {
        foreach (var group in soundGroups)
        {
            var sound = group.sounds.FirstOrDefault(s => s.name == name);
            if (sound != null && sound.source.isPlaying)
            {
                sound.source.Stop();
                return;
            }
        }

        Debug.LogWarning("SFX not found to stop: " + name);
    }

    public void StopSFXWithFade(string name, float fadeDuration)
    {
        StartCoroutine(FadeOutSFX(name, fadeDuration));
    }

    private IEnumerator FadeOutSFX(string name, float duration)
    {
        foreach (var group in soundGroups)
        {
            var sound = group.sounds.FirstOrDefault(s => s.name == name);
            if (sound != null)
            {
                float startVolume = sound.source.volume;
                float t = 0f;

                while (t < duration)
                {
                    sound.source.volume = Mathf.Lerp(startVolume, 0f, t / duration);
                    t += Time.deltaTime;
                    yield return null;
                }

                sound.source.Stop();
                sound.source.volume = sound.volume;
                yield break;
            }
        }

        Debug.LogWarning("SFX not found for fade out: " + name);
    }
}



