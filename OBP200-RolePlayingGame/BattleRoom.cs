namespace OBP200_RolePlayingGame
{ 
    public class BattleRoom : IRoom
    {
        public string Type => "battle";
        public string Name { get; }

        private readonly Func<bool> _onEnter;

        public BattleRoom(string name, Func<bool> onEnter)
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