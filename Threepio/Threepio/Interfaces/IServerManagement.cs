using System.Collections.Generic;

namespace Threepio.Interfaces
{
    public interface IServerManagement
    {
        List<string> GetPlayers();
        string GetPlayer(string partialPlayerName);
        bool IsServerOnline();
    }
}
