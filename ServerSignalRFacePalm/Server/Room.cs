namespace ServerSignalRFacePalm.Server
{
    public class Room
    {
        public string Number { get; set; } = string.Empty;
        public Dictionary<string, Player> PlayerState { get; set; } = new Dictionary<string, Player>();
        public string CurrentPlayer { get; set; } = string.Empty;
    }
}