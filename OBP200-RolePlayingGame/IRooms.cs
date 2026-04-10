namespace OBP200_RolePlayingGame
{
    public interface IRoom
    {
        string Type { get; }
        string Name { get; }
        bool Enter();
    }
}