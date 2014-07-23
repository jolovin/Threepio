using System.Collections.Generic;
using System.Configuration;
using Threepio.Interfaces;
using GameServerInfo;
namespace Threepio.Server
{
    public class ServerManagement : IServerManagement
    {
        private GameServer _server;
        private List<string> _players;

        private const GameType gameType = GameType.JediKnightJediAcademy;

        /// <summary>
        /// Constructor. Initializes an instance of GameServer and connects
        /// to the specified server set in the app.config.
        /// </summary>
        /// <param name="server"></param>
        public ServerManagement(GameServer server)
        {
            _server = server;

            var serverIP = ConfigurationManager.AppSettings["ServerIP"];
            var serverPort = int.Parse(ConfigurationManager.AppSettings["ServerPort"]);
             
            _server = new GameServer(serverIP, serverPort, gameType);
            _server.DebugMode = true;

            _players = new List<string>();
        }

        /// <summary>
        /// Gets a list of players currently on the server.
        /// </summary>
        /// <returns>A list of player names</returns>
        public List<string> GetPlayers()
        {
            /* Clear the list, since we want a fresh list of players */
            _players.Clear();

            /* Get the latest info */
            _server.QueryServer();

            foreach(Player player in _server.Players)
            {
                _players.Add(GameServer.CleanName(player.Name));
            }

            return _players;
        }

        /// <summary>
        /// Indicates whether the server is online or not.
        /// </summary>
        /// <returns>True or false</returns>
        public bool IsServerOnline()
        {
            return _server.IsOnline;
        }
    }
}
