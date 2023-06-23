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
    private bool _isDefault = false;
    private bool _showDetails = false;
    private Timer _rollTimer;

    private string CurrentSheetName
    {
        get => _currentSheetName;
        set
        {
            if (_currentSheetName == value)
                return;
            int i = _availableSheets.IndexOf(_currentSheetName);
            _availableSheets[i] = value;
            _currentSheetName = value;
            StateHasChanged();
        }
    }

    private List<string> _availableSheets = SampleSheet.Available.Keys.ToList();
    private string _currentSheetName;

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
                await SaveState();
            },
            errorMessage =>
            {
                _errorMessage = errorMessage;
                return Task.CompletedTask;
            }
        );
    }

    private async Task CurrentSheetRenamed()
    {
        await SaveState();
    }

    private async Task CurrentSheetChanged()
    {
        await LoadSheet(_currentSheetName);
        StateHasChanged();
    }

    private Task RollChanged()
    {
        return ParseAndSaveRolls(_rollText);
    }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        await LoadSavedSheets();
    }

    private async Task LoadSavedSheets()
    {
        string name = await LocalStorage.GetItemAsync<string>("current-sheet-name");
        var allNames = (await LocalStorage.KeysAsync()).Where(k => k.StartsWith("sheet:")).Select(k => k[6..]).ToList();
        if (allNames.Count == 0)
        {
            PrepareFirstTimeVisit();
            return;
        }

        _currentSheetName = name;
        _availableSheets = allNames.ToList();
        _availableSheets.AddRange(SampleSheet.Available.Keys);
        await LoadSheet(name);
    }

    private void PrepareFirstTimeVisit()
    {
        _isDefault = true;
        _availableSheets = SampleSheet.Available.Keys.ToList();
        _currentSheetName = _availableSheets[0];
        
        SampleSheet sampleSheet = SampleSheet.Available[_currentSheetName];
        
        _rollText = sampleSheet.Text;
        _sheetDefinition = sampleSheet.Sheet;
        _sheet = sampleSheet.Sheet.Empty();
        StateHasChanged();
    }

    private async Task LoadSheet(string name)
    {
        _currentSheetName = name;
        _rollText = await LocalStorage.GetItemAsync<string>("sheet:" + name);
        if (_rollText == null)
        {
            if (SampleSheet.Available.TryGetValue(name, out var sample))
            {
                _rollText = sample.Text;
                _sheetDefinition = sample.Sheet;
                _sheet = sample.Sheet.Empty();
            }
        }
        else
        {
            await ParseAndSaveRolls(_rollText);
        }

        StateHasChanged();
    }

    private async Task DeleteCurrentSheet()
    {
        _availableSheets.Remove(_currentSheetName);
        if (_availableSheets.Count == 0)
        {
            _availableSheets.Add(_currentSheetName = "New Sheet 1");
            _rollText = null;
            _sheet = Maybe<EvaluatedSheet<Maybe<RollExpressionResult>>>.None;
        }
        else
        {
            await LoadSheet(_availableSheets[0]);
        }

        await SaveState();
    }

    private async Task SaveState()
    {
        if (SampleSheet.Available.ContainsKey(_currentSheetName))
            return;
        
        await LocalStorage.SetItemAsync("current-sheet-name", _currentSheetName);
        foreach (var item in await LocalStorage.KeysAsync())
        {
            if (item == "current-sheet-name")
                continue;
            if (item.StartsWith("sheet:"))
            {
                var name = item[6..];
                if (!_availableSheets.Contains(name))
                {
                    Console.WriteLine($"Removing deleted sheet: {name}");
                    await LocalStorage.RemoveItemAsync(item);
                }
                
                continue;
            }
            Console.WriteLine($"Removing unexpected local storage key: {item}");
            await LocalStorage.RemoveItemAsync(item);
        }

        Console.WriteLine($"Saving current sheet: {_currentSheetName}");
        await LocalStorage.SetItemAsync("sheet:" + _currentSheetName, _rollText);
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

    private void NewSheet()
    {
        for(int i=1;;i++)
        {
            string candidateName = $"New Sheet {i}";
            if (!_availableSheets.Contains(candidateName))
            {
                _currentSheetName = candidateName;
                break;
            }
        }

        _rollText = "";
        _sheetDefinition = Maybe<SheetDefinition>.None;
        _sheet = Maybe<EvaluatedSheet<Maybe<RollExpressionResult>>>.None;
        _availableSheets.Add(_currentSheetName);
    }
}