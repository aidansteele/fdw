using System.Collections.Generic;

namespace FdwSharp
{
    public interface ITable
    {
        IEnumerable<IDictionary<string, object>> ScanTable(IReadOnlyList<Column> columns, IReadOnlyDictionary<string, string> options);
    }
}