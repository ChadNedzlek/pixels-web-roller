﻿using System;
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
        await _jsRuntime.InvokeVoidAsync("pixelWebModule.repeatConnect", jsRef);
        var requestPixel = new PixelDevice(jsRef, this);
        await requestPixel.InitializeAsync();
        return requestPixel;
    }

    private class PixelDevice : IPixelDevice, IAsyncDisposable
    {
        private readonly IJSObjectReference _jsRef;
        private readonly PixelsManager _pixelsManager;
        
        public event Action<IPixelDevice, string> RollingStateChanged;
        public string RollState { get; private set; }
        public int Face { get; private set; }
        public long PixelId { get; private set; }
        public string Name { get; private set; }

        private DotNetObjectReference<PixelDevice> _thisRef;
        private int _callbackId;

        public PixelDevice(IJSObjectReference jsRef, PixelsManager pixelsManager)
        {
            _jsRef = jsRef;
            _pixelsManager = pixelsManager;
        }

        public async ValueTask DisposeAsync()
        {
            await _pixelsManager._jsRuntime.InvokeVoidAsync(
                "pixelWebModule.removePropertyListener",
                _jsRef,
                "rollState",
                _callbackId
            );
            await _jsRef.InvokeVoidAsync("disconnect");
            await _jsRef.DisposeAsync();
            _thisRef.Dispose();
        }

        public async Task InitializeAsync()
        {
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
            _thisRef = DotNetObjectReference.Create(this);
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