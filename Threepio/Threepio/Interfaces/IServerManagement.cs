using System.Collections.Generic;

namespace Threepio.Interfaces
{
    public interface IServerManagement
    {
        List<string> GetPlayers();
        bool IsServerOnline();
    }
}
