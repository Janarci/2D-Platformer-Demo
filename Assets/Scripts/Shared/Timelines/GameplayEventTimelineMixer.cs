using UnityEngine;
using UnityEngine.Playables;

public sealed class GameplayEventTimelineMixer : PlayableBehaviour
{
    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        var actorRoot = playerData as GameObject;
        var inputCount = playable.GetInputCount();

        for (var i = 0; i < inputCount; i++)
        {
            var input = playable.GetInput(i);
            var inputWeight = playable.GetInputWeight(i);

            if (!input.IsValid())
            {
                continue;
            }

            var clipPlayable = (ScriptPlayable<GameplayEventTimelineBehaviour>)input;
            var behaviour = clipPlayable.GetBehaviour();
            behaviour?.ProcessClip(clipPlayable, info, actorRoot, inputWeight);
        }
    }
}
