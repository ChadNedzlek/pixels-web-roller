using System.Collections.Immutable;
using Microsoft.JSInterop;
using PixelsBlazorInterop;
using Rolling;
using Rolling.Models.Definitions;
using Rolling.Models.Rolls;
using Rolling.Parsing;
using Rolling.Utilities;
using Rolling.Visitors;

namespace PixelsWeb.Pages;

public partial class Roller : IAsyncDisposable
{
    private readonly RollDescriptionEvaluator _describer = new();
    private readonly List<IPixelDevice> _connectedDice = new();
    private readonly HashSet<IPixelDevice> _rollingDice = new();
    private readonly List<IPixelDevice> _completedDice = new();
    private readonly RollParser _parser = new();
    
    private string _rollText;
    private string _errorMessage;
    private Maybe<SheetDefinition> _sheetDefinition;
    private Maybe<EvaluatedSheet<Maybe<RollExpressionResult>>> _sheet;
    private bool _showDetails = false;
    private Timer _rollTimer;

    private async Task ConnectPixels()
    {
        try
        {
            IPixelDevice die = await PixelsManager.RequestPixel();
            _connectedDice.Add(die);
            die.RollingStateChanged += DieRolled;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            await JsRuntime.InvokeVoidAsync("showFailedConnectToast", "failed-to-connect-toast");
        }
    }

    private void DieRolled(IPixelDevice pixelDevice, string state)
    {
        switch (state)
        {
            case "onFace":
                if (!_rollingDice.Contains(pixelDevice))
                    return;
                _rollTimer?.Dispose();
                _rollingDice.Remove(pixelDevice);
                _completedDice.Add(pixelDevice);
                if (_rollingDice.Count > 0)
                    _rollTimer = new Timer(FinishRoll, null, TimeSpan.FromSeconds(5), Timeout.InfiniteTimeSpan);
                else
                    FinishRoll(null);
                break;
            case "crooked":
                break;
            case "handling":
            case "rolling":
                if (_rollingDice.Count == 0)
                {
                    // We grabbed the first die, a new roll!
                    _completedDice.Clear();
                }
                _rollingDice.Add(pixelDevice);
                break;
        }
        StateHasChanged();
    }

    private void FinishRoll(object state)
    {
        // We are done, one way or the other
        _rollingDice.Clear();
        var rolls = _completedDice.Select(c => new DieRoll(c.Face, 20, c.PixelId)).ToImmutableList();
        
        if (!_sheetDefinition.TryValue(out SheetDefinition sheet))
        {
            return;
        }

        _sheet = sheet.Roll(rolls);
        StateHasChanged();
    }

    private async Task ParseAndSaveRolls(string value)
    {
        if (value == null) return;
        Either<SheetDefinition, string> res = _parser.TryParse(value);
        await res.Match(
            async result =>
            {
                _errorMessage = null;
                _sheetDefinition = result;
                _sheet = result.Empty();
                await LocalStorage.SetItemAsync("roll-text", value);
            },
            errorMessage =>
            {
                _errorMessage = errorMessage;
                return Task.CompletedTask;
            }
        );
    }

    private Task RollChanged()
    {
        return ParseAndSaveRolls(_rollText);
    }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        _rollText = await LocalStorage.GetItemAsync<string>("roll-text");
        if (_rollText != null) await ParseAndSaveRolls(_rollText);
    }

    private void LoadSheet()
    {
    }

    private void DeleteCurrentSheet()
    {
    }

    private void RollAll()
    {
        if (!_sheetDefinition.TryValue(out SheetDefinition sheet))
        {
            return;
        }

        _sheet = sheet.Roll(ImmutableList<DieRoll>.Empty);
    }
    
    public async ValueTask DisposeAsync()
    {
        foreach (var die in _connectedDice)
        {
            await die.DisposeAsync();
        }
        
        _connectedDice.Clear();
    }
}