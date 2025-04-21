using System.Collections.Generic;
using UnityEngine.Timeline;
using UnityEngine;

[System.Serializable]
public class SceneEntryData
{
    public SceneField sceneName;
    public CutsceneType entryType;
    public CutsceneType exitType;
}

public enum CutsceneType
{
    RunInFromLeft,
    RunInFromRight,
    JumpInFromLeft,
    JumpInFromRight,
    FallInFromLeft,
    FallInFromRight,
    FallIn,
    JumpIn
}


[CreateAssetMenu(fileName = "New Level Connection", menuName = "Levels/Connection")]
public class LevelConnection : ScriptableObject
{
    public static LevelConnection ActiveConnection { get; set; }

    public List<SceneEntryData> sceneEntries;

    public CutsceneType GetEntryCutsceneType(string sceneName)
    {
        foreach (var entry in sceneEntries)
        {
            if (entry.sceneName == sceneName)
                return entry.entryType;
        }
        return CutsceneType.RunInFromLeft; // default fallback
    }

    public CutsceneType GetExitCutsceneType(string sceneName)
    {
        foreach (var entry in sceneEntries)
        {
            if (entry.sceneName == sceneName)
                return entry.exitType;
        }
        return CutsceneType.RunInFromLeft; // default fallback
    }
}