using System.Collections.Generic;

namespace FdwSharp
{
    public class TableSelector : ITable
    {
        private readonly IDictionary<string, ITable> _tableMap;

        public TableSelector(IDictionary<string, ITable> tableMap)
        {
            _tableMap = tableMap;
        }

        public IEnumerable<IDictionary<string, object>> ScanTable(IReadOnlyList<Column> columns, IReadOnlyDictionary<string, string> options)
        {
            var tableName = options["fdwsharp.table"];
            var table = _tableMap[tableName];
            return table.ScanTable(columns, options);
        }
    }
}