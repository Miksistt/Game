using Microsoft.AspNetCore.SignalR;
using ServerSignalRFacePalm.Server;

namespace ServerSignalRFacePalm.Server
{
    public class MainHub : Hub
    {
        private readonly RoomBase _roomBase;
        private readonly PlayerBase _playerBase;

        public MainHub(RoomBase roomBase, PlayerBase playerBase)
        {
            _roomBase = roomBase;
            _playerBase = playerBase;
        }

        public async Task<string> CreateRoom(string roomId, string playerName)
        {
            try
            {
                var player = _playerBase.GetOrAdd(playerName, Context.ConnectionId);
                var createdRoomId = _roomBase.CreateRoom(roomId, player);
                await Groups.AddToGroupAsync(Context.ConnectionId, createdRoomId);
                await Clients.Caller.SendAsync("RoomCreated", createdRoomId);
                return createdRoomId;
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("Error", ex.Message);
                throw;
            }
        }

        public async Task<List<string>> GetRoomList()
        {
            return _roomBase.GetRoomList();
        }

        public async Task JoinRoom(string roomId, string playerName)
        {
            var player = _playerBase.GetOrAdd(playerName, Context.ConnectionId);
            var success = _roomBase.AddPlayerToRoom(roomId, player);

            if (success)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
                await Clients.Group(roomId).SendAsync("PlayerJoinedRoom", roomId);

                var room = _roomBase.GetRoom(roomId);
                if (room != null)
                {
                    await Clients.Group(roomId).SendAsync("RoomInfoUpdated", room);
                }
            }
            else
            {
                await Clients.Caller.SendAsync("Error", "Не удалось присоединиться к комнате");
            }
        }

        public async Task MakeAction(RoomAction action)
        {
            await _roomBase.AddRoundActionAsync(action, Clients);
        }

        public async Task StartGame(string roomId)
        {
            if (_roomBase.Ready(roomId))
            {
                await _roomBase.StartAsync(roomId, Clients);
            }
            else
            {
                await Clients.Caller.SendAsync("Error", "Для начала игры нужно минимум 2 игрока");
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var player = _playerBase.Get(Context.ConnectionId);
            if (player != null)
            {
                player.IsConnected = false;
                _playerBase.Remove(Context.ConnectionId);
            }
            await base.OnDisconnectedAsync(exception);
        }
    }
}