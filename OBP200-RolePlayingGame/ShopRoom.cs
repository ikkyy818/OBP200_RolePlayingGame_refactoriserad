namespace OBP200_RolePlayingGame
{
    public class ShopRoom : IRoom
    {
        public string Type => "shop";
        public string Name { get; }

        private readonly Func<bool> _onEnter;

        public ShopRoom(string name, Func<bool> onEnter)
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