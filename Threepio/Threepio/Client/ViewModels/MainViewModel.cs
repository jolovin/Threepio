﻿using System.Collections.Generic;
using Caliburn.Micro;
using GameServerInfo;
using Threepio.Server;
using Threepio.GameInterface;

namespace Threepio.Client.ViewModels
{
    public class MainViewModel : PropertyChangedBase
    {
        private GameServer gameServer;
        private ServerManagement serverManager;
        private AcademyGameConsole gameConsole;

        private const string WindowTitleDefault = "Threepio v1.0.0.0"; 
        private string _windowTitle = WindowTitleDefault;

        public List<string> AvailablePlayers { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public MainViewModel()
        {
            gameConsole = new AcademyGameConsole();
            GetAvailablePlayers();
        }

        /// <summary>
        /// Gets an updated list of players
        /// </summary>
        public void GetAvailablePlayers()
        {
            serverManager = new ServerManagement(gameServer);
            AvailablePlayers = serverManager.GetPlayers();
            NotifyOfPropertyChange(() => AvailablePlayers);
        }       

        public string WindowTitle
        {
            get { return _windowTitle; }
            set
            {
                _windowTitle = value;
                NotifyOfPropertyChange(() => WindowTitle);
            }
        }
    }
}
