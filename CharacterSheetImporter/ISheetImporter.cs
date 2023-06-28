using System.IO;
using System.Threading.Tasks;

namespace CharacterSheetImporter;

public interface ISheetImporter
{
    public Task<SheetImportResult> ImportAsync(Stream stream);
}