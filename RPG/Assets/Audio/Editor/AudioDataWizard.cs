using UnityEngine;
using UnityEditor;
using UnityEngine.Audio; 
using System.Collections.Generic;


public class AudioDataWizard : EditorWindow
{
    private int tabIndex = 0;
    private string[] tabs = { "Sound FX", "Background Music", "Zone Music" };

    private string soundName = "NewSoundFX";
    private string backgroundName = "NewBackgroundMusic";
    private string zoneName = "NewZoneMusic";

    private List<Sound> soundFXList = new List<Sound>();
    private List<Sound> backgroundMusicList = new List<Sound>();
    private List<Sound> zoneMusicList = new List<Sound>();

    private AudioMixerGroup defaultSFXMixer;
    private AudioMixerGroup defaultBGMMixer;

    [MenuItem("Tools/Audio/Audio Data Wizard")]
    public static void ShowWindow()
    {
        GetWindow<AudioDataWizard>("Audio Data Wizard");
    }

    private void OnEnable()
    {
        defaultSFXMixer = FindMixerGroup("Sound Effects");
        defaultBGMMixer = FindMixerGroup("Background Music");
    }

    private void OnGUI()
    {
        GUILayout.Space(10);
        tabIndex = GUILayout.Toolbar(tabIndex, tabs);
        GUILayout.Space(15);

        switch (tabIndex)
        {
            case 0: DrawSoundFXTab(); break;
            case 1: DrawBackgroundMusicTab(); break;
            case 2: DrawZoneMusicTab(); break;
        }
    }

    private void DrawSoundFXTab()
    {
        soundName = EditorGUILayout.TextField("Sound FX Group Name", soundName);
        GUILayout.Space(10);

        DrawSoundList(soundFXList, defaultSFXMixer);

        GUILayout.Space(10);
        if (GUILayout.Button("+ Add Sound FX"))
            soundFXList.Add(CreateDefaultSound(defaultSFXMixer));

        if (GUILayout.Button("Create Sound FX"))
        {
            CreateAsset<SoundLibrary>(soundName, "SoundFX", soundFXList);
            soundName = "NewSoundFX";
            soundFXList.Clear(); 
        }
    }

    private void DrawBackgroundMusicTab()
    {
        backgroundName = EditorGUILayout.TextField("Background Music Name", backgroundName);
        GUILayout.Space(10);

        DrawSoundList(backgroundMusicList, defaultBGMMixer);

        GUILayout.Space(10);
        if (GUILayout.Button("+ Add Background Track"))
            backgroundMusicList.Add(CreateDefaultSound(defaultBGMMixer));

        if (GUILayout.Button("Create Background Music"))
        {
            CreateAsset<BackgroundMusicLibrary>(backgroundName, "BackgroundMusic", backgroundMusicList);
            backgroundName = "NewBackgroundMusic";
            backgroundMusicList.Clear(); 
        }
    }

    private void DrawZoneMusicTab()
    {
        zoneName = EditorGUILayout.TextField("Zone Name", zoneName);
        GUILayout.Space(10);

        DrawSoundList(zoneMusicList, defaultBGMMixer);

        GUILayout.Space(10);
        if (GUILayout.Button("+ Add Zone Track"))
            zoneMusicList.Add(CreateDefaultSound(defaultBGMMixer));

        if (GUILayout.Button("Create Zone Music"))
        {
            CreateAsset<ZoneMusicLibrary>(zoneName, "ZoneMusic", zoneMusicList);
            zoneName = "NewZoneMusic";
            zoneMusicList.Clear(); 
        }
    }


    private void DrawSoundList(List<Sound> list, AudioMixerGroup defaultGroup)
    {
        if (list == null) return;

        for (int i = 0; i < list.Count; i++)
        {
            EditorGUILayout.BeginVertical("box");
            list[i].name = EditorGUILayout.TextField("Name", list[i].name);
            list[i].clip = (AudioClip)EditorGUILayout.ObjectField("Clip", list[i].clip, typeof(AudioClip), false);
            list[i].volume = EditorGUILayout.Slider("Volume", list[i].volume, 0f, 1f);
            list[i].pitch = EditorGUILayout.Slider("Pitch", list[i].pitch, 0.1f, 3f);
            list[i].loop = EditorGUILayout.Toggle("Loop", list[i].loop);
            list[i].mixerGroup = (AudioMixerGroup)EditorGUILayout.ObjectField("Mixer Group", list[i].mixerGroup, typeof(AudioMixerGroup), false);

            if (GUILayout.Button("Remove")) list.RemoveAt(i);
            EditorGUILayout.EndVertical();
            GUILayout.Space(5);
        }
    }

    private Sound CreateDefaultSound(AudioMixerGroup defaultGroup)
    {
        return new Sound
        {
            name = "NewSound",
            clip = null,
            volume = 0.5f,
            pitch = 1f,
            loop = false,
            mixerGroup = defaultGroup
        };
    }

    private void CreateAsset<T>(string name, string subfolder, List<Sound> dataList) where T : ScriptableObject
    {
        string basePath = "Assets/Audio/SoundData";
        string folderPath = $"{basePath}/{subfolder}";

        if (!AssetDatabase.IsValidFolder(basePath))
            AssetDatabase.CreateFolder("Assets/Audio", "SoundData");

        if (!AssetDatabase.IsValidFolder(folderPath))
            AssetDatabase.CreateFolder(basePath, subfolder);

        string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{folderPath}/{name}.asset");
        T asset = ScriptableObject.CreateInstance<T>();

        if (asset is SoundLibrary sfxLib) sfxLib.sounds = new List<Sound>(dataList);
        else if (asset is BackgroundMusicLibrary bgmLib) bgmLib.musicTracks = new List<Sound>(dataList);
        else if (asset is ZoneMusicLibrary zoneLib) zoneLib.musicTracks = new List<Sound>(dataList);

        if (asset is SoundLibrary) ((SoundLibrary)(object)asset).soundName = name;
        if (asset is BackgroundMusicLibrary) ((BackgroundMusicLibrary)(object)asset).backgroundName = name;
        if (asset is ZoneMusicLibrary) ((ZoneMusicLibrary)(object)asset).zoneName = name;

        AssetDatabase.CreateAsset(asset, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = asset;
    }

    private AudioMixerGroup FindMixerGroup(string groupName)
    {
        var mixers = Resources.FindObjectsOfTypeAll<AudioMixer>();
        foreach (var mixer in mixers)
        {
            var groups = mixer.FindMatchingGroups(groupName);
            if (groups.Length > 0)
                return groups[0];
        }
        return null;
    }

}
