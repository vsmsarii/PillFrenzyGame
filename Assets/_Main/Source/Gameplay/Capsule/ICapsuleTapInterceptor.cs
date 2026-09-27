namespace PillFrenzy.Gameplay
{
    public interface ICapsuleTapInterceptor
    {
        bool TryIntercept(CapsuleController tapped);
    }
}
