using System;
using System.IO;
using System.Threading.Tasks;

namespace CharacterSheetImporter;

public abstract class StateMachineTextImporter<TBuilder> : ITextSheetImporter
{
    protected record struct StateResult(StateFunction Next, string Remainder)
    {
        public static implicit operator StateResult(StateFunction next) => new(next, null);
    }

    protected delegate StateResult StateFunction(string line, TBuilder state);
    
    public async Task<SheetImportResult> ImportAsync(string value)
    {
        StringReader reader = new StringReader(value);
        var builder = InitializeBuilder();
        StateResult currentState = new StateResult(InitialState, null);
        while (currentState.Next != _success && currentState.Next != _failure)
        {
            string line = await reader.ReadLineAsync();
            if (currentState.Remainder != null)
                line = currentState.Remainder + line;
            var nextState = currentState.Next(line, builder);
            currentState = nextState with { Next = nextState.Next ?? currentState.Next };
        }

        return currentState.Next == _success ? Finalize(builder) : new SheetImportResult(0, null);
    }

    protected abstract TBuilder InitializeBuilder();
    protected abstract SheetImportResult Finalize(TBuilder builder);
    
    protected abstract StateResult InitialState(string line, TBuilder state);

    private readonly StateFunction _success = (_,_) => throw new InvalidOperationException();
    private readonly StateFunction _failure = (_,_) => throw new InvalidOperationException();
    protected StateResult Success => new(_success, null);
    protected  StateResult Failure => new(_failure, null);
}