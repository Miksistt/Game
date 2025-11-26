namespace ServerSignalRFacePalm.Server
{
    public class Player
    {
        public string Name { get; set; } = string.Empty;
        public string ConnectionId { get; set; } = string.Empty;
        public bool IsConnected { get; set; } = true;
        public int HP { get; set; } = 10;
    }
}