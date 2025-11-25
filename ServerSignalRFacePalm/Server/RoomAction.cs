namespace ServerSignalRFacePalm.Server
{
    public class RoomAction
    {
        public string GroupId { get; set; } = string.Empty;
        public string Actor { get; set; } = string.Empty;
        public string Target { get; set; } = string.Empty;
        public int ActionType { get; set; }
    }
}