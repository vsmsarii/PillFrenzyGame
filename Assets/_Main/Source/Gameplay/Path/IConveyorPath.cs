using UnityEngine;

namespace PillFrenzy.Gameplay
{
    public interface IConveyorPath
    {
        float Length { get; }
        Pose GetPose(float distance);
    }
}
