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
    public TimelineAsset fallIn;
    public TimelineAsset jumpIn;

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
            CutsceneType.FallIn => fallIn,
            CutsceneType.JumpIn => jumpIn,
            _ => null,
        };
    }
}
