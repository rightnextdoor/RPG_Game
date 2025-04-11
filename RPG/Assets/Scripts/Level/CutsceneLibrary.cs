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
    public TimelineAsset fallIn;

    public TimelineAsset GetCutscene(CutsceneType type)
    {
        return type switch
        {
            CutsceneType.RunInFromLeft => runInFromLeft,
            CutsceneType.RunInFromRight => runInFromRight,
            CutsceneType.JumpInFromLeft => jumpInFromLeft,
            CutsceneType.JumpInFromRight => jumpInFromRight,
            CutsceneType.FallIn => fallIn,
            _ => null,
        };
    }
}
