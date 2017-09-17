using System.Collections.Generic;
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
}