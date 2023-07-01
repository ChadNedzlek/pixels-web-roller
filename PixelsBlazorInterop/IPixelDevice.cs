using System;
using System.Threading.Tasks;

namespace PixelsBlazorInterop;

public interface IPixelDevice : IAsyncDisposable
{
    event Action<IPixelDevice, string> RollingStateChanged;
    event Action<IPixelDevice> Disconnected;
    
    string RollState { get; }
    int Face { get; }
    long PixelId { get; }
    string Name { get; }
    string SystemId { get; }

    Task ConnectAsync();
}