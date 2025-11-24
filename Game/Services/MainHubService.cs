using Microsoft.AspNetCore.SignalR.Client;
using Game.Models;
using System.Windows;

namespace Game.Services
{
    public class MainHubService
    {
        private HubConnection? _connection;

        public event Action<string>? OnRoomCreated;
        public event Action<string>? OnPlayerJoinedRoom;
        public event Action<Room>? OnStartRound;
        public event Action<List<string>>? OnPastRoundInfo;
        public event Action<Player>? OnWinner;
        public event Action<string>? OnError;

        public async Task ConnectAsync()
        {
            try
            {
                _connection = new HubConnectionBuilder()
                    .WithUrl("https://localhost:7205/game")
                    .WithAutomaticReconnect()
                    .Build();

                SetupHandlers();
                await _connection.StartAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка подключения к серверу: {ex.Message}");
            }
        }

        private void SetupHandlers()
        {
            _connection.On<string>("RoomCreated", (roomId) =>
                Application.Current.Dispatcher.Invoke(() => OnRoomCreated?.Invoke(roomId)));

            _connection.On<string>("PlayerJoinedRoom", (roomId) =>
                Application.Current.Dispatcher.Invoke(() => OnPlayerJoinedRoom?.Invoke(roomId)));

            _connection.On<Room>("StartRound", (room) =>
                Application.Current.Dispatcher.Invoke(() => OnStartRound?.Invoke(room)));

            _connection.On<List<string>>("PastRoundInfo", (actions) =>
                Application.Current.Dispatcher.Invoke(() => OnPastRoundInfo?.Invoke(actions)));

            _connection.On<Player>("Winner", (winner) =>
                Application.Current.Dispatcher.Invoke(() => OnWinner?.Invoke(winner)));

            _connection.On<string>("Error", (error) =>
                Application.Current.Dispatcher.Invoke(() => OnError?.Invoke(error)));
        }

        public async Task<string> CreateRoom(string roomId, string playerName)
        {
            try
            {
                if (_connection?.State == HubConnectionState.Connected)
                    return await _connection.InvokeAsync<string>("CreateRoom", roomId, playerName);

                throw new Exception("Нет подключения к серверу");
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка создания комнаты: {ex.Message}");
            }
        }

        public async Task JoinRoom(string roomId, string playerName)
        {
            try
            {
                if (_connection?.State == HubConnectionState.Connected)
                    await _connection.InvokeAsync("JoinRoom", roomId, playerName);
                else
                    throw new Exception("Нет подключения к серверу");
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка присоединения к комнате: {ex.Message}");
            }
        }

        public async Task MakeAction(RoomAction action)
        {
            try
            {
                if (_connection?.State == HubConnectionState.Connected)
                    await _connection.InvokeAsync("MakeAction", action);
                else
                    throw new Exception("Нет подключения к серверу");
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка выполнения действия: {ex.Message}");
            }
        }
        public async Task<List<string>> GetRoomList()
        {
            try
            {
                if (_connection?.State == HubConnectionState.Connected)
                    return await _connection.InvokeAsync<List<string>>("GetRoomList");
                else
                    throw new Exception("Нет подключения к серверу");
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка получения списка комнат: {ex.Message}");
            }
        }
        public async Task StartGame(string roomId, string playerName)
        {
            try
            {
                if (_connection?.State == HubConnectionState.Connected)
                    await _connection.InvokeAsync("StartGame", roomId);
                else
                    throw new Exception("Нет подключения к серверу");
            }
            catch (Exception ex)
            {
                throw new Exception($"Не удалось запустить игру: {ex.Message}");
            }
        }

        public async Task DisconnectAsync()
        {
            if (_connection != null)
                await _connection.DisposeAsync();
        }
    }
}