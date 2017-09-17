using System.Collections.Generic;
using System.Threading.Tasks;

namespace FdwSharp
{
    public enum TableImportRestriction
    {
        None = 0,
        Limit = 1,
        Except = 2,
    }
    
    public interface ITableImporter
    {
        Task<IEnumerable<TableDefinition>> ImportTables(string schema, IDictionary<string, string> serverOptions, IDictionary<string, string> importOptions, TableImportRestriction importRestriction, ICollection<string> restrictedTables);
    }
}