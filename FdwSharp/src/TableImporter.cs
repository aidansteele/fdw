using System.Collections.Generic;
using System.Threading.Tasks;
using PostgresFdw;

namespace FdwSharp
{
    public class TableDefinition
    {
        internal string Name;
        internal IList<ColumnDefinition> Columns;
        internal IDictionary<string, string> Options;

        public TableDefinition(string name, IList<ColumnDefinition> columns, IDictionary<string, string> options)
        {
            Name = name;
            Columns = columns;
            Options = options;
        }
    }

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