using UnityEngine;

namespace PillFrenzy.Gameplay
{
    public interface IConveyorPath
    {
        float Length { get; }
        Vector3 GetPoint(float distance);
    }
}
