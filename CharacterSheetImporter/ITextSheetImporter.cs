using System.Threading.Tasks;

namespace CharacterSheetImporter;

public interface ITextSheetImporter
{
    public Task<SheetImportResult> ImportAsync(string value);
}