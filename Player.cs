namespace SocialDeductionGame
{
    public class Player
    {
        public string playerName;
        public RoleType role;
        public TeamType team;
        public bool isAlive = true;

        public bool hasSaved = false;
        public bool hasKilled = false;

        public Player(string name)
        {
            playerName = name;
            role = RoleType.Townfolk;
            team = TeamType.Town;
        }
    }
}