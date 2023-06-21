using System;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace PixelsBlazorInterop;

[SupportedOSPlatform("browser")]
public class PixelsManager : IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private IJSObjectReference _module; 

    public PixelsManager(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task<PixelsDie> RequestPixel()
    {
        Console.WriteLine(await _jsRuntime.InvokeAsync<string>("pixelWebModule.chadTestFunc"));
        var jsRef = await _jsRuntime.InvokeAsync<IJSObjectReference>("pixelWebModule.requestPixel");
        return new PixelsDie(jsRef, this);
    }

    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        if (_module != null) await _module.DisposeAsync();
    }
}