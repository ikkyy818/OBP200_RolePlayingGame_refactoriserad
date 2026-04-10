namespace OBP200_RolePlayingGame
{
    public class BossRoom : IRoom
    {
        public string Type => "boss";
        public string Name { get; }

        private readonly Func<bool> _onEnter;

        public BossRoom(string name, Func<bool> onEnter)
        {
            Name = name;
            _onEnter = onEnter;
        }

        public bool Enter()
        {
            return _onEnter();
        }
    }
}