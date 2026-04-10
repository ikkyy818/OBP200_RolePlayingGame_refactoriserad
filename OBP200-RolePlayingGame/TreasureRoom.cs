namespace OBP200_RolePlayingGame
{ 
    public class TreasureRoom : IRoom
    {
        public string Type => "treasure";
        public string Name { get; }

        private readonly Func<bool> _onEnter;

        public TreasureRoom(string name, Func<bool> onEnter)
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