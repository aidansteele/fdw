using System;
using System.Collections.Generic;

namespace FdwSharp
{
    public class LambdaTable : ITable
    {
        private readonly Func<IReadOnlyList<Column>, IReadOnlyDictionary<string, string>, IEnumerable<IDictionary<string, object>>> _lambda;

        public LambdaTable(Func<IReadOnlyList<Column>, IReadOnlyDictionary<string, string>, IEnumerable<IDictionary<string, object>>> lambda)
        {
            _lambda = lambda;
        }

        public IEnumerable<IDictionary<string, object>> ScanTable(IReadOnlyList<Column> columns, IReadOnlyDictionary<string, string> options)
        {
            return _lambda(columns, options);
        }
    }
}