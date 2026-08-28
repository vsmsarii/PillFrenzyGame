namespace PillFrenzy.Core
{
    public interface ILateTickable 
    {
        void LateTick(float deltaTime);
    }
}