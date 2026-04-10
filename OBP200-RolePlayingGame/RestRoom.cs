namespace OBP200_RolePlayingGame
{
    public class RestRoom : IRoom
    {
        public string Type => "rest";
        public string Name { get; }

        private readonly Func<bool> _onEnter;

        public RestRoom(string name, Func<bool> onEnter)
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