using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[CreateAssetMenu(menuName = "Cutscenes/Cutscene Library")]
public class CutsceneLibrary : ScriptableObject
{
    public TimelineAsset runInFromLeft;
    public TimelineAsset runInFromRight;
    public TimelineAsset jumpInFromLeft;
    public TimelineAsset jumpInFromRight;
    public TimelineAsset fallInFromLeft;
    public TimelineAsset fallInFromRight;

    public TimelineAsset GetCutscene(CutsceneType type)
    {
        return type switch
        {
            CutsceneType.RunInFromLeft => runInFromLeft,
            CutsceneType.RunInFromRight => runInFromRight,
            CutsceneType.JumpInFromLeft => jumpInFromLeft,
            CutsceneType.JumpInFromRight => jumpInFromRight,
            CutsceneType.FallInFromLeft => fallInFromLeft,
            CutsceneType.FallInFromRight => fallInFromRight,
            _ => null,
        };
    }
}
