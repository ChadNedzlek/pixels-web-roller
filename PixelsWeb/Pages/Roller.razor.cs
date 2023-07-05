using System.Collections.Immutable;
using CharacterSheetImporter;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using PixelsBlazorInterop;
using Rolling;
using Rolling.Models.Definitions;
using Rolling.Models.Rolls;
using Rolling.Parsing;
using Rolling.Visitors;
using Utilities;

namespace PixelsWeb.Pages;

public partial class Roller : IAsyncDisposable
{
    private class AnimationState
    {
        public bool IsRolling;
    }

    private class SectionRenderOptions
    {
        public SectionRenderOptions(string name)
        {
            Name = name;
        }

        public string Name { get; }
        public bool Compact { get; set; }
    }

    private readonly RollDescriptionEvaluator _describer = new();
    private readonly List<IPixelDevice> _connectedDice = new();
    private readonly HashSet<IPixelDevice> _rollingDice = new();
    private readonly List<IPixelDevice> _completedDice = new();
    private readonly Dictionary<long, AnimationState> _animations = new();
    private List<SectionRenderOptions> _sectionRenderOptions = new();
    private readonly RollParser _parser = new();
    private string _rollText;
    private string _errorMessage;
    private string _rollErrorText;
    private Maybe<SheetDefinition> _sheetDefinition;
    private Maybe<EvaluatedSheet<Maybe<RollExpressionResult>>> _sheet;
    private bool _isBluetoothSupported = true;
    private bool _showDetails = false;
    private Timer _rollTimer;

    private List<string> _availableSheets = SampleSheet.Available.Keys.ToList();
    private string _currentSheetName;
    private double _rotAnimation;
    private string _importText;
    private bool _isLoading;
    private int? _reconnectCount;

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

    private async Task ConnectPixels()
    {
        try
        {
            IPixelDevice die = await PixelsManager.RequestPixel();
            if (_connectedDice.Any(c => c.PixelId == die.PixelId))
            {
                Console.WriteLine($"Detected reconnect of pixel {die.Name}, discarding");
                return;
            }

            await NewPixelsDieAdded(die);
            await LocalStorage.SetItemAsync("savedDice", _connectedDice.Select(c => c.SystemId).ToList());
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            await JsRuntime.InvokeVoidAsync("showFailedConnectToast", "failed-to-connect-toast");
        }
    }

    private async Task NewPixelsDieAdded(IPixelDevice die)
    {
        await die.ConnectAsync();
        _connectedDice.Add(die);
        _animations.Add(die.PixelId, new AnimationState());
        die.RollingStateChanged += DieRolled;
    }

    private void DieRolled(IPixelDevice pixelDevice, string state)
    {
        switch (state)
        {
            case "onFace":
            {
                var anim = _animations.GetOrAdd(pixelDevice.PixelId);
                if (anim.IsRolling)
                {
                    anim.IsRolling = false;
                    ((IJSInProcessRuntime)JsRuntime).InvokeVoid(
                        "pixelWebModule.stopAnimation",
                        "animRoll_" + pixelDevice.PixelId
                    );
                }
                ((IJSInProcessRuntime)JsRuntime).InvokeVoid(
                    "pixelWebModule.startAnimation",
                    "animFlash_" + pixelDevice.PixelId
                );

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
            }
            case "crooked":
                break;
            case "handling":
            case "rolling":
            {
                if (_rollingDice.Count == 0)
                {
                    // We grabbed the first die, a new roll!
                    _completedDice.Clear();
                }

                var anim = _animations.GetOrAdd(pixelDevice.PixelId);
                if (!anim.IsRolling)
                {
                    anim.IsRolling = true;
                    ((IJSInProcessRuntime)JsRuntime).InvokeVoid(
                        "pixelWebModule.startAnimation",
                        "animRoll_" + pixelDevice.PixelId
                    );
                }

                _rollingDice.Add(pixelDevice);
                break;
            }
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

        DoRolls(sheet, rolls);
        StateHasChanged();
    }

    private void DoRolls(SheetDefinition sheet, ImmutableList<DieRoll> rolls)
    {
        try
        {
            _sheet = sheet.Roll(rolls);
        }
        catch (Exception e)
        {
            _rollErrorText = e.ToString();
        }
    }

    private async Task ParseRolls(string value, bool resetOptions, bool saveSheet)
    {
        if (value == null) return;
        Either<SheetDefinition, string> res = _parser.TryParse(value);
        await res.Match(
            async result =>
            {
                _errorMessage = null;
                SetSheet(_rollText, result, resetOptions);
                if (saveSheet)
                {
                    await SaveState(resetOptions);
                }
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
        await SaveState(false);
    }

    private async Task CurrentSheetChanged()
    {
        await LoadSheet(_currentSheetName);
        StateHasChanged();
    }

    private Task RollChanged()
    {
        return ParseRolls(_rollText, false, true);
    }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        await Task.WhenAll(
            LoadSavedSheets(),
            CheckBluetoothSupport(),
            ReconnectDice()
        );
    }

    private async Task ReconnectDice()
    {
        var dice = await LocalStorage.GetItemAsync<List<string>>("savedDice");
        if (dice == null || dice.Count == 0)
            return;
        _reconnectCount = dice.Count;
        StateHasChanged();
        var reconnected = await PixelsManager.ReconnectAll(dice);
        await Task.WhenAll(reconnected.Select(NewPixelsDieAdded));
        _reconnectCount = default;
        StateHasChanged();
    }

    private async Task CheckBluetoothSupport()
    {
        try
        {
            _isBluetoothSupported = await JsRuntime.InvokeAsync<bool>("navigator.bluetooth.getAvailability");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            _isBluetoothSupported = false;
        }
    }

    private void AnimateFrame(object state)
    {
        _rotAnimation = (_rotAnimation + 0.1) % 1;
        StateHasChanged();
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
        _availableSheets = SampleSheet.Available.Keys.ToList();
        _currentSheetName = _availableSheets[0];
        
        SampleSheet sampleSheet = SampleSheet.Available[_currentSheetName];
        
        SetSheet(sampleSheet.Text, sampleSheet.Sheet, true);
        StateHasChanged();
    }

    private async Task LoadSheet(string name)
    {
        _currentSheetName = name;
        _rollText = await LocalStorage.GetItemAsync<string>("sheet:" + name);
        Console.WriteLine("Loaded options");
        var savedOptions = await LocalStorage.GetItemAsync<List<SectionRenderOptions>>("opt:" + name);
        if (savedOptions != null)
        {
            Console.WriteLine("Applying saved options");
            _sectionRenderOptions = savedOptions;
        }
        
        if (_rollText == null)
        {
            if (SampleSheet.Available.TryGetValue(name, out var sample))
            {
                SetSheet(sample.Text, sample.Sheet, savedOptions == null);
            }
        }
        else
        {
            await ParseRolls(_rollText, savedOptions == null, false);
        }


        StateHasChanged();
    }

    private void SetSheet(string sheetText, SheetDefinition sheetDefinition, bool resetOptions)
    {
        _rollText = sheetText;
        _sheetDefinition = sheetDefinition;
        if (resetOptions || sheetDefinition.Sections.Count != _sectionRenderOptions.Count)
        {
            Console.WriteLine("Resetting options");
            _sectionRenderOptions = sheetDefinition.Sections
                .Select((s, i) => new SectionRenderOptions(s.Name.Or($"Section {i + 1}")))
                .ToList();
        }

        _sheet = sheetDefinition.Empty();
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

        await SaveState(false);
    }

    private async Task SaveState(bool saveOptions)
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
            
            if (item.StartsWith("opt:"))
            {
                var name = item[4..];
                if (!_availableSheets.Contains(name))
                {
                    Console.WriteLine($"Removing deleted options: {name}");
                    await LocalStorage.RemoveItemAsync(item);
                }
                
                continue;
            }
            Console.WriteLine($"Removing unexpected local storage key: {item}");
            await LocalStorage.RemoveItemAsync(item);
        }

        Console.WriteLine($"Saving current sheet: {_currentSheetName}");
        await LocalStorage.SetItemAsync("sheet:" + _currentSheetName, _rollText);
        if (saveOptions)
        {
            await SaveRenderOptions();
        }
    }

    private async Task SaveRenderOptions()
    {
        Console.WriteLine("Saving options!");
        await LocalStorage.SetItemAsync("opt:" + _currentSheetName, _sectionRenderOptions);
    }

    private void RollAll()
    {
        if (!_sheetDefinition.TryValue(out SheetDefinition sheet))
        {
            return;
        }
        
        DoRolls(sheet, ImmutableList<DieRoll>.Empty);
    }
    
    public async ValueTask DisposeAsync()
    {
        await SaveRenderOptions();
        foreach (var die in _connectedDice)
        {
            await die.DisposeAsync();
        }
        
        _connectedDice.Clear();
        //await _rotationAnimation.DisposeAsync();
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
        _sectionRenderOptions = new List<SectionRenderOptions>();
        _availableSheets.Add(_currentSheetName);
    }

    private async Task ImportFile(InputFileChangeEventArgs args)
    {
        _isLoading = true;
        StateHasChanged();
        await JsRuntime.InvokeVoidAsync("hideAndClearModal", "import-modal");
        try
        {
            await using Stream stream = args.File.OpenReadStream(maxAllowedSize: 10_000_000);
            Maybe<ImportedSheet> parsed = await SheetImport.ImportSheet(stream);
            if (parsed.TryValue(out ImportedSheet sheet))
            {
                if (!_availableSheets.Contains(sheet.Name))
                {
                    _availableSheets.Add(sheet.Name);
                    _currentSheetName = sheet.Name;
                    _rollText = sheet.SheetText;
                    await ParseRolls(sheet.SheetText, true, true);
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed to import sheet: {e}");
        }
        _isLoading = false;
        StateHasChanged();
    }

    private async Task ImportTextSubmitted()
    {
        _isLoading = true;
        StateHasChanged();
        await JsRuntime.InvokeVoidAsync("hideAndClearModal", "import-modal");
        try
        {
            Maybe<ImportedSheet> parsed = await SheetImport.ImportSheet(_importText);
            if (parsed.TryValue(out ImportedSheet sheet))
            {
                if (!_availableSheets.Contains(sheet.Name))
                {
                    _availableSheets.Add(sheet.Name);
                    _currentSheetName = sheet.Name;
                    _rollText = sheet.SheetText;
                    await ParseRolls(sheet.SheetText, true, true);
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed to import sheet: {e}");
        }
        _isLoading = false;
        StateHasChanged();
    }
}