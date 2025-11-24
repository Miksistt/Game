using Game.Models;
using Game.Services;
using Game.Utilities;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Game.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly MainHubService _hubService;
        private string _playerName = "Игрок" + new Random().Next(1000, 9999);
        private string _roomId = string.Empty;
        private string _status = "Не подключено";
        private Room? _currentRoom;
        private bool _isMyTurn = false;
        private bool _isRoomCreator = false;
        private bool _canMakeMove = false;

        public MainViewModel()
        {
            _hubService = new MainHubService();
            SetupEvents();

            ConnectCommand = new RelayCommand(async () => await Connect());
            CreateRoomCommand = new RelayCommand(async () => await CreateRoom());
            JoinRoomCommand = new RelayCommand(async () => await JoinRoom());
            AttackCommand = new RelayCommand<string>(async (target) => await MakeAction(1, target),
                (target) => _canMakeMove && _isMyTurn && !string.IsNullOrEmpty(target));
            DefendCommand = new RelayCommand<string>(async (target) => await MakeAction(2, target),
                (target) => _canMakeMove && _isMyTurn && !string.IsNullOrEmpty(target));
            StartGameCommand = new RelayCommand(async () => await StartGame());

            _ = Connect();
        }

        public string PlayerName
        {
            get => _playerName;
            set { _playerName = value; OnPropertyChanged(); }
        }

        public string RoomId
        {
            get => _roomId;
            set { _roomId = value; OnPropertyChanged(); }
        }

        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); }
        }

        public Room? CurrentRoom
        {
            get => _currentRoom;
            set { _currentRoom = value; OnPropertyChanged(); OnPropertyChanged(nameof(OtherPlayers)); }
        }

        public bool IsMyTurn
        {
            get => _isMyTurn;
            set { _isMyTurn = value; OnPropertyChanged(); UpdateCommands(); }
        }

        public bool IsRoomCreator
        {
            get => _isRoomCreator;
            set { _isRoomCreator = value; OnPropertyChanged(); }
        }

        public ObservableCollection<string> Logs { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> RoundActions { get; } = new ObservableCollection<string>();

        public List<string> OtherPlayers
        {
            get
            {
                if (CurrentRoom == null || CurrentRoom.PlayerState == null)
                    return new List<string>();
                return CurrentRoom.PlayerState.Keys.Where(name => name != PlayerName).ToList();
            }
        }

        public ICommand ConnectCommand { get; }
        public ICommand CreateRoomCommand { get; }
        public ICommand JoinRoomCommand { get; }
        public ICommand AttackCommand { get; }
        public ICommand DefendCommand { get; }
        public ICommand StartGameCommand { get; }

        private void SetupEvents()
        {
            _hubService.OnRoomCreated += (roomId) =>
            {
                RoomId = roomId;
                IsRoomCreator = true;
                AddLog($"Комната создана: {roomId}");
                Status = $"Комната создана - Ожидаем игроков";
            };

            _hubService.OnPlayerJoinedRoom += (roomId) =>
            {
                AddLog($"Новый игрок присоединился к комнате");
                Status = $"Игрок присоединился - Ожидаем начала игры";
            };

            _hubService.OnStartRound += (room) =>
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    AddLog($"=== НАЧАЛСЯ НОВЫЙ РАУНД ===");

                    CurrentRoom = room;
                    _canMakeMove = true; // Разрешаем делать ходы
                    IsMyTurn = room.CurrentPlayer == "ALL";

                    if (IsMyTurn)
                    {
                        AddLog($"Ваш ход! Выберите действие");
                        Status = "Ваш ход! Выберите действие";
                    }
                    else
                    {
                        AddLog($"Ожидаем ходов других игроков");
                        Status = "Ожидаем ходов других игроков";
                    }

                    RoundActions.Clear();
                    UpdateCommands();
                });
            };

            _hubService.OnPastRoundInfo += (actions) =>
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    AddLog($"=== РАУНД ЗАВЕРШЕН ===");

                    RoundActions.Clear();
                    foreach (var action in actions)
                    {
                        RoundActions.Add(action);
                        AddLog(action);
                    }

                    
                    _canMakeMove = false;
                    IsMyTurn = false;
                    Status = "Раунд завершен";
                    UpdateCommands();
                });
            };

            _hubService.OnWinner += (winner) =>
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    Status = $"Победитель: {winner.Name}!";
                    AddLog($"🎉 Победитель: {winner.Name} с {winner.HP} HP!");
                    _canMakeMove = false;
                    IsMyTurn = false;
                    CurrentRoom = null;
                    IsRoomCreator = false;
                    UpdateCommands();
                });
            };

            _hubService.OnError += (error) =>
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    AddLog($"Ошибка: {error}");
                    Status = $"Ошибка: {error}";
                });
            };
        }

        private async Task MakeAction(int actionType, string target)
        {
            if (CurrentRoom == null || !_canMakeMove || !_isMyTurn || string.IsNullOrEmpty(target))
                return;

            try
            {
                // Сразу блокируем дальнейшие ходы
                _canMakeMove = false;
                UpdateCommands();

                var action = new RoomAction
                {
                    GroupId = CurrentRoom.Number,
                    Actor = PlayerName,
                    Target = target,
                    ActionType = actionType
                };

                await _hubService.MakeAction(action);
                AddLog($"Вы выбрали: {(actionType == 1 ? "Атаковать" : "Защитить")} {target}");

                Status = "Ход отправлен. Ожидаем других игроков...";
            }
            catch (Exception ex)
            {
                AddLog($"Ошибка действия: {ex.Message}");
                Status = $"Ошибка: {ex.Message}";
                
                _canMakeMove = true;
                UpdateCommands();
            }
        }

        private void UpdateCommands()
        {
            
            if (AttackCommand is RelayCommand<string> attackCmd)
                attackCmd.RaiseCanExecuteChanged();
            if (DefendCommand is RelayCommand<string> defendCmd)
                defendCmd.RaiseCanExecuteChanged();
        }

        private async Task Connect()
        {
            try
            {
                await _hubService.ConnectAsync();
                Status = "Подключено к серверу";
                AddLog("Подключено к серверу");
            }
            catch (Exception ex)
            {
                Status = $"Ошибка подключения: {ex.Message}";
                AddLog($"Ошибка подключения: {ex.Message}");
            }
        }

        private async Task CreateRoom()
        {
            try
            {
                var roomId = await _hubService.CreateRoom(RoomId, PlayerName);
                AddLog($"Создаем комнату как {PlayerName}");
            }
            catch (Exception ex)
            {
                AddLog($"Ошибка создания комнаты: {ex.Message}");
                Status = $"Ошибка: {ex.Message}";
            }
        }

        private async Task JoinRoom()
        {
            if (string.IsNullOrEmpty(RoomId))
            {
                AddLog("Введите ID комнаты");
                Status = "Введите ID комнаты";
                return;
            }

            try
            {
                await _hubService.JoinRoom(RoomId, PlayerName);
                AddLog($"Присоединяемся к комнате {RoomId} как {PlayerName}");
                IsRoomCreator = false;
            }
            catch (Exception ex)
            {
                AddLog($"Ошибка присоединения: {ex.Message}");
                Status = $"Ошибка: {ex.Message}";
            }
        }

        private async Task StartGame()
        {
            if (string.IsNullOrEmpty(RoomId))
            {
                AddLog("Нет активной комнаты");
                return;
            }

            try
            {
                await _hubService.StartGame(RoomId, PlayerName);
                AddLog("Запускаем игру...");
            }
            catch (Exception ex)
            {
                AddLog($"Не удалось запустить игру: {ex.Message}");
            }
        }

        private void AddLog(string message)
        {
            Logs.Add($"{DateTime.Now:HH:mm:ss} - {message}");
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}