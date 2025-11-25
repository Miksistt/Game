using System.Collections.Concurrent;

namespace ServerSignalRFacePalm.Server
{
    public class PlayerBase
    {
        private readonly ConcurrentDictionary<string, Player> _players = new();
        private readonly ConcurrentDictionary<string, string> _connectionToPlayer = new();

        public Player GetOrAdd(string playerName, string connectionId)
        {
            CleanupPlayer(connectionId);

            string uniqueName = GetUniqueName(playerName);
            var player = new Player { Name = uniqueName, ConnectionId = connectionId };

            _players[uniqueName] = player;
            _connectionToPlayer[connectionId] = uniqueName;

            return player;
        }

        private string GetUniqueName(string baseName)
        {
            if (!_players.ContainsKey(baseName))
                return baseName;

            int counter = 1;
            string newName;
            do
            {
                newName = $"{baseName}{counter}";
                counter++;
            } while (_players.ContainsKey(newName));

            return newName;
        }

        private void CleanupPlayer(string connectionId)
        {
            
            var playersToRemove = _players.Where(p => p.Value.ConnectionId == connectionId).ToList();
            foreach (var player in playersToRemove)
            {
                _players.TryRemove(player.Key, out _);
            }
            _connectionToPlayer.TryRemove(connectionId, out _);
        }

        public Player? Get(string connectionId)
        {
            return _connectionToPlayer.TryGetValue(connectionId, out var playerName)
                ? _players.TryGetValue(playerName, out var player) ? player : null
                : null;
        }

        public void Remove(string connectionId)
        {
            CleanupPlayer(connectionId);
        }
    }
}