using System.Collections.Generic;

namespace FdwSharp
{
    public class SingleRowTable : ITable
    {
        private readonly object[] _vals;

        public SingleRowTable(params object[] vals)
        {
            _vals = vals;
        }

        public IEnumerable<IDictionary<string, object>> ScanTable(IReadOnlyList<Column> columns, IReadOnlyDictionary<string, string> options)
        {
            var enumerator = _vals.GetEnumerator();
            var dict = new Dictionary<string, object>();
            
            while (enumerator.MoveNext())
            {
                var key = enumerator.Current as string;
                enumerator.MoveNext();
                var val = enumerator.Current;
                dict.Add(key, val);

            }
            
            return new List<IDictionary<string, object>>{ dict };
        }
    }
}