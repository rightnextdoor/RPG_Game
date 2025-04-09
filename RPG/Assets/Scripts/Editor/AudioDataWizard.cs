using UnityEngine;
using UnityEditor;
using UnityEngine.Audio;
using System.Collections.Generic;
using System.IO;

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

    private const string AUDIO_MANAGER_PATH = "Assets/Prefabs/Managers/AudioManager.prefab";

    private Vector2 soundFXScrollPos;
    private Vector2 backgroundMusicScrollPos;
    private Vector2 zoneMusicScrollPos;

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
        // Set a minimum space at the top for the tabs and some initial padding
        GUILayout.Space(10);

        // Display tabs at the top
        tabIndex = GUILayout.Toolbar(tabIndex, tabs);
        GUILayout.Space(15);

        // Start a vertical layout to contain both the content and the buttons at the bottom
        GUILayout.BeginVertical();

        // Scrollable area for the content (Sound FX, Background Music, Zone Music)
        GUILayout.BeginScrollView(Vector2.zero, GUILayout.ExpandHeight(true)); // Scrollable area will take up the available space
        switch (tabIndex)
        {
            case 0: DrawSoundFXTab(); break;
            case 1: DrawBackgroundMusicTab(); break;
            case 2: DrawZoneMusicTab(); break;
        }
        GUILayout.EndScrollView(); // End scroll view

        // Space to separate content and buttons
        GUILayout.Space(10);

        // Add buttons at the bottom
        if (tabIndex == 0)
        {
            // Add new Sound FX and create Sound FX
            if (GUILayout.Button("+ Add Sound FX"))
                soundFXList.Add(CreateDefaultSound(defaultSFXMixer));

            if (GUILayout.Button("Create Sound FX"))
            {
                // Create the asset and save it using the CreateAsset method
                var asset = CreateAsset<SoundLibrary>(soundName, "SoundFX", soundFXList);
                asset.soundName = soundName;
                AddToAudioManagerList(asset, typeof(SoundLibrary));
                ClearAllFields();
            }
        }
        else if (tabIndex == 1)
        {
            // Add new Background Track and create Background Music
            if (GUILayout.Button("+ Add Background Track"))
                backgroundMusicList.Add(CreateDefaultSound(defaultBGMMixer));

            if (GUILayout.Button("Create Background Music"))
            {
                // Create the asset and save it using the CreateAsset method
                var asset = CreateAsset<BackgroundMusicLibrary>(backgroundName, "BackgroundMusic", backgroundMusicList);
                asset.backgroundName = backgroundName;
                AddToAudioManagerList(asset, typeof(BackgroundMusicLibrary));
                ClearAllFields();
            }
        }
        else if (tabIndex == 2)
        {
            // Add new Zone Track and create Zone Music
            if (GUILayout.Button("+ Add Zone Track"))
                zoneMusicList.Add(CreateDefaultSound(defaultBGMMixer));

            if (GUILayout.Button("Create Zone Music"))
            {
                // Create the asset and save it using the CreateAsset method
                var asset = CreateAsset<ZoneMusicLibrary>(zoneName, "ZoneMusic", zoneMusicList);
                asset.zoneName = zoneName;
                AddToAudioManagerList(asset, typeof(ZoneMusicLibrary));
                ClearAllFields();
            }
        }

        GUILayout.EndVertical(); // End vertical layout
    }

    private void DrawSoundFXTab()
    {
        soundName = EditorGUILayout.TextField("Sound FX Group Name", soundName);
        GUILayout.Space(10);

        // Scrollable area for Sound FX list
        soundFXScrollPos = GUILayout.BeginScrollView(soundFXScrollPos, GUILayout.Height(500));  // Adjust height as needed
        DrawSoundList(soundFXList, defaultSFXMixer);
        GUILayout.EndScrollView();

        GUILayout.Space(10);
        //if (GUILayout.Button("+ Add Sound FX")) soundFXList.Add(CreateDefaultSound(defaultSFXMixer));
        //if (GUILayout.Button("Create Sound FX"))
        //{
        //    var asset = CreateAsset<SoundLibrary>(soundName, "SoundFX", soundFXList);
        //    asset.soundName = soundName;
        //    AddToAudioManagerList(asset, typeof(SoundLibrary));
        //    ClearAllFields();
        //}
    }

    private void DrawBackgroundMusicTab()
    {
        backgroundName = EditorGUILayout.TextField("Background Music Name", backgroundName);
        GUILayout.Space(10);

        // Scrollable area for Background Music list
        backgroundMusicScrollPos = GUILayout.BeginScrollView(backgroundMusicScrollPos, GUILayout.Height(500));  // Adjust height as needed
        DrawSoundList(backgroundMusicList, defaultBGMMixer);
        GUILayout.EndScrollView();

        GUILayout.Space(10);
        //if (GUILayout.Button("+ Add Background Track")) backgroundMusicList.Add(CreateDefaultSound(defaultBGMMixer));
        //if (GUILayout.Button("Create Background Music"))
        //{
        //    var asset = CreateAsset<BackgroundMusicLibrary>(backgroundName, "BackgroundMusic", backgroundMusicList);
        //    asset.backgroundName = backgroundName;
        //    AddToAudioManagerList(asset, typeof(BackgroundMusicLibrary));
        //    ClearAllFields();
        //}
    }

    private void DrawZoneMusicTab()
    {
        zoneName = EditorGUILayout.TextField("Zone Name", zoneName);
        GUILayout.Space(10);

        // Scrollable area for Zone Music list
        zoneMusicScrollPos = GUILayout.BeginScrollView(zoneMusicScrollPos, GUILayout.Height(500));  // Adjust height as needed
        DrawSoundList(zoneMusicList, defaultBGMMixer);
        GUILayout.EndScrollView();

        GUILayout.Space(10);
        //if (GUILayout.Button("+ Add Zone Track")) zoneMusicList.Add(CreateDefaultSound(defaultBGMMixer));
        //if (GUILayout.Button("Create Zone Music"))
        //{
        //    var asset = CreateAsset<ZoneMusicLibrary>(zoneName, "ZoneMusic", zoneMusicList);
        //    asset.zoneName = zoneName;
        //    AddToAudioManagerList(asset, typeof(ZoneMusicLibrary));
        //    ClearAllFields();
        //}
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

    private T CreateAsset<T>(string name, string subfolder, List<Sound> dataList) where T : ScriptableObject
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

        AssetDatabase.CreateAsset(asset, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = asset;
        return asset;
    }

    private void AddToAudioManagerList(ScriptableObject newAsset, System.Type type)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AUDIO_MANAGER_PATH);
        if (prefab == null)
        {
            Debug.LogWarning("AudioManager prefab not found.");
            return;
        }

        AudioManager manager = prefab.GetComponent<AudioManager>();
        if (manager == null)
        {
            Debug.LogWarning("AudioManager script not found on prefab.");
            return;
        }

        Undo.RecordObject(manager, "Add Audio Data");

        if (type == typeof(SoundLibrary))
        {
            if (!manager.soundGroups.Contains((SoundLibrary)newAsset))
                manager.soundGroups.Add((SoundLibrary)newAsset);
        }
        else if (type == typeof(BackgroundMusicLibrary))
        {
            if (!manager.backgroundMusicGroups.Contains((BackgroundMusicLibrary)newAsset))
                manager.backgroundMusicGroups.Add((BackgroundMusicLibrary)newAsset);
        }
        else if (type == typeof(ZoneMusicLibrary))
        {
            if (!manager.zoneMusicDefinitions.Contains((ZoneMusicLibrary)newAsset))
                manager.zoneMusicDefinitions.Add((ZoneMusicLibrary)newAsset);
        }

        EditorUtility.SetDirty(manager);
        PrefabUtility.SavePrefabAsset(prefab);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
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

    private void ClearAllFields()
    {
        soundName = "NewSoundFX";
        backgroundName = "NewBackgroundMusic";
        zoneName = "NewZoneMusic";
        soundFXList.Clear();
        backgroundMusicList.Clear();
        zoneMusicList.Clear();
    }
}
