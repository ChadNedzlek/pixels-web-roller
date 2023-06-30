using System.IO;
using System.Threading.Tasks;

namespace CharacterSheetImporter;

public interface IStreamSheetImporter
{
    public Task<SheetImportResult> ImportAsync(Stream stream);
}