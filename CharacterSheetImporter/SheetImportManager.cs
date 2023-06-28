using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CharacterSheetImporter.Importers;
using Utilities;

namespace CharacterSheetImporter;

public class SheetImportManager
{
    private readonly List<ISheetImporter> _importers = new() { new DndBeyondImporter() };
    
    public async Task<Maybe<ImportedSheet>> ImportSheet(Stream stream)
    {
        MemoryStream mem = new ();
        await stream.CopyToAsync(mem);
        SheetImportResult best = new (0, null);
        foreach (var importer in _importers)
        {
            mem.Seek(0, SeekOrigin.Begin);
            try
            {
                var res = await importer.ImportAsync(mem);
                if (res.Confidence == 1)
                    return res.Sheet;
                if (res.Confidence > best.Confidence)
                    best = res;
            }
            catch (Exception)
            {
                // Failed to parse with that parser, so it's not the right one
            }
        }

        return best.Sheet;
    }
}