using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace PixelsBlazorInterop;

public class PixelsManager
{
    private readonly IJSRuntime _jsRuntime; 

    public PixelsManager(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task<IPixelDevice> RequestPixel()
    {
        var jsRef = await _jsRuntime.InvokeAsync<IJSObjectReference>("pixelWebModule.requestPixel");
        var requestPixel = new PixelDevice(jsRef, this);
        await requestPixel.InitializeAsync();
        return requestPixel;
    }

    public async Task<IEnumerable<IPixelDevice>> ReconnectAll(IEnumerable<string> ids)
    {
        PixelDevice[] pixels = await Task.WhenAll(ids.Select(ReconnectDeviceOrNull));
        return pixels.Where(p => p != null);
    }

    private async Task<PixelDevice> ReconnectDeviceOrNull(string i)
    {
        IJSObjectReference jsRef;
        try
        {
            jsRef = await _jsRuntime.InvokeAsync<IJSObjectReference>("pixelWebModule.reconnectPixel", i);
        }
        catch (JSException e) when (e.Message.StartsWith("vaettir.net::NO_DIE_CONNECTED"))
        {
            // Whatever die we thought we were connected to isn't here anymore.
            return null;
        }
        
        var requestPixel = new PixelDevice(jsRef, this);
        await requestPixel.InitializeAsync();
        return requestPixel;
    }

    private class PixelDevice : IPixelDevice, IAsyncDisposable
    {
        private readonly IJSObjectReference _jsRef;
        private readonly PixelsManager _pixelsManager;
        
        public event Action<IPixelDevice, string> RollingStateChanged;
        public event Action<IPixelDevice> Disconnected;
        
        public string RollState { get; private set; }
        public int Face { get; private set; }
        public long PixelId { get; private set; }
        public string SystemId { get; private set; }
        public string Name { get; private set; }

        private DotNetObjectReference<PixelDevice> _thisRef;
        private int? _callbackId;

        public PixelDevice(IJSObjectReference jsRef, PixelsManager pixelsManager)
        {
            _jsRef = jsRef;
            _pixelsManager = pixelsManager;
        }

        public async ValueTask DisposeAsync()
        {
            if (_callbackId.HasValue)
            {
                await _pixelsManager._jsRuntime.InvokeVoidAsync(
                    "pixelWebModule.removePropertyListener",
                    _jsRef,
                    "rollState",
                    _callbackId
                );
            }

            await _jsRef.InvokeVoidAsync("disconnect");
            await _jsRef.DisposeAsync();
            _thisRef.Dispose();
        }

        public async Task InitializeAsync()
        {
            await _pixelsManager._jsRuntime.InvokeVoidAsync("pixelWebModule.repeatConnect", _jsRef);
            
            PixelId = await _pixelsManager._jsRuntime.InvokeAsync<long>(
                "pixelWebModule.getProperty",
                _jsRef,
                "pixelId"
            );
            Name = await _pixelsManager._jsRuntime.InvokeAsync<string>(
                "pixelWebModule.getProperty",
                _jsRef,
                "name"
            );
            SystemId = await _pixelsManager._jsRuntime.InvokeAsync<string>(
                "pixelWebModule.getProperty",
                _jsRef,
                "systemId"
            );
            _thisRef = DotNetObjectReference.Create(this);
        }
        
        public async Task ConnectAsync()
        {
            if (_callbackId.HasValue)
                throw new InvalidOperationException("Already connected");
            
            _callbackId = await _pixelsManager._jsRuntime.InvokeAsync<int>(
                "pixelWebModule.addPropertyListener",
                _jsRef,
                "rollState",
                _thisRef,
                nameof(HandlePropertyChangeEvent)
            );
        }

        [JSInvokable]
        public void HandlePropertyChangeEvent(PixelInfo info)
        {
            RollState = info.RollState;
            Face = info.CurrentFace;

            RollingStateChanged?.Invoke(this, RollState);
        }
    }
}

public class PixelInfo
{
    [JsonPropertyName("rollState")]
    public string RollState { get; init; }
    [JsonPropertyName("currentFace")]
    public int CurrentFace { get; init; }
    [JsonPropertyName("pixelId")]
    public long PixelId { get; init; }
    [JsonPropertyName("name")]
    public string Name { get; init; }
}