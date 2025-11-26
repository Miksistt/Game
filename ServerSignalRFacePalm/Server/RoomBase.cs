using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Concurrent;

namespace ServerSignalRFacePalm.Server
{
    public class RoomBase
    {
        ConcurrentDictionary<string, Room> rooms = new();
        ConcurrentDictionary<string, Queue<RoomAction>> defenceActions = new();
        ConcurrentDictionary<string, Queue<RoomAction>> attackActions = new();

        public string CreateRoom(string roomId, Player player)
        {
            if (rooms.ContainsKey(roomId))
            {
                throw new Exception("Комната с таким ID уже существует");
            }

            var newRoom = new Room { Number = roomId };
            newRoom.PlayerState.Add(player.Name, player);

            rooms.TryAdd(roomId, newRoom);
            defenceActions.TryAdd(roomId, new Queue<RoomAction>());
            attackActions.TryAdd(roomId, new Queue<RoomAction>());

            return roomId;
        }

        public bool AddPlayerToRoom(string roomId, Player player)
        {
            if (rooms.TryGetValue(roomId, out Room room))
            {
                if (room.PlayerState.Count >= 6)
                {
                    return false;
                }

                if (!room.PlayerState.ContainsKey(player.Name))
                {
                    room.PlayerState.Add(player.Name, player);
                    return true;
                }
            }
            return false;
        }

        public bool Ready(string roomId)
        {
            if (rooms.TryGetValue(roomId, out Room room))
            {
                return room.PlayerState.Count >= 2;
            }
            return false;
        }

        public Room? GetRoom(string roomId)
        {
            return rooms.TryGetValue(roomId, out var room) ? room : null;
        }

        internal async Task AddRoundActionAsync(RoomAction action, IHubCallerClients clients)
        {
            if (!rooms.TryGetValue(action.GroupId, out Room room))
                return;
            if (!room.PlayerState.ContainsKey(action.Actor))
                return;
            if (room.PlayerState[action.Actor].HP <= 0)
                return;

            var alreadyDefence = defenceActions[action.GroupId].Any(s => s.Actor == action.Actor);
            var alreadyAttack = attackActions[action.GroupId].Any(s => s.Actor == action.Actor);

            if (alreadyAttack || alreadyDefence)
                return;

            if (action.ActionType == 1)
                attackActions[action.GroupId].Enqueue(action);
            else
                defenceActions[action.GroupId].Enqueue(action);

            int alivePlayers = room.PlayerState.Count(s => s.Value.HP > 0);
            int totalActions = attackActions[action.GroupId].Count + defenceActions[action.GroupId].Count;

            if (totalActions >= alivePlayers)
            {
                await PlayRoundAsync(action.GroupId, clients);
            }
        }

        private async Task PlayRoundAsync(string groupId, IHubCallerClients clients)
        {
            List<string> actions = new List<string>();
            Random random = new();
            Dictionary<string, int> defence = new();

            var defenceList = defenceActions[groupId].ToList();
            foreach (var action in defenceList)
            {
                defence[action.Actor] = random.Next(2, 6);
                actions.Add($"Игрок {action.Actor} защищается. Очков защиты: {defence[action.Actor]}");
            }

            var attackList = attackActions[groupId].ToList();
            foreach (var action in attackList)
            {
                if (!rooms[groupId].PlayerState.ContainsKey(action.Actor) ||
                    rooms[groupId].PlayerState[action.Actor].HP <= 0 ||
                    !rooms[groupId].PlayerState.ContainsKey(action.Target) ||
                    rooms[groupId].PlayerState[action.Target].HP <= 0)
                    continue;

                var damage = random.Next(1, 4);
                actions.Add($"Игрок {action.Actor} атакует игрока {action.Target}. Начальная атака: {damage}");

                if (defence.ContainsKey(action.Target))
                {
                    var defenceValue = defence[action.Target];
                    if (defenceValue >= damage)
                    {
                        defence[action.Target] -= damage;
                        damage = 0;
                        actions.Add($"Игрок {action.Target} полностью блокирует атаку своей защитой");
                    }
                    else
                    {
                        damage -= defenceValue;
                        defence[action.Target] = 0;
                        actions.Add($"Игрок {action.Target} частично блокирует атаку. Пробито урона: {damage}");
                    }
                }

                if (damage > 0)
                {
                    rooms[groupId].PlayerState[action.Target].HP -= damage;
                    actions.Add($"Игрок {action.Target} получает {damage} урона. Осталось HP: {rooms[groupId].PlayerState[action.Target].HP}");

                    if (rooms[groupId].PlayerState[action.Target].HP <= 0)
                    {
                        actions.Add($"Игрок {action.Target} умирает!");
                        rooms[groupId].PlayerState[action.Target].HP = 0;
                    }
                }
            }

            defenceActions[groupId].Clear();
            attackActions[groupId].Clear();

            await clients.Group(groupId).SendAsync("PastRoundInfo", actions);

            var alivePlayers = rooms[groupId].PlayerState.Where(s => s.Value.HP > 0).ToList();

            if (alivePlayers.Count > 1)
            {
                rooms[groupId].CurrentPlayer = "ALL";
                await clients.Group(groupId).SendAsync("StartRound", rooms[groupId]);
            }
            else if (alivePlayers.Count == 1)
            {
                var winner = alivePlayers.First().Value;
                await clients.Group(groupId).SendAsync("Winner", winner);

                rooms.TryRemove(groupId, out _);
                defenceActions.TryRemove(groupId, out _);
                attackActions.TryRemove(groupId, out _);
            }
        }

        internal async Task StartAsync(string groupId, IHubCallerClients clients)
        {
            if (!rooms.TryGetValue(groupId, out Room room))
                return;

            if (room.PlayerState.Count >= 2)
            {
                room.CurrentPlayer = "ALL";
                await clients.Group(groupId).SendAsync("StartRound", room);
            }
        }

        public List<string> GetRoomList()
        {
            return rooms.Keys.ToList();
        }
    }
}