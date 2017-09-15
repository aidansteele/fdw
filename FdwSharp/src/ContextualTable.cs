using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Threading;

namespace FdwSharp
{
    public class ContextualTable: ITable
    {
        private static ConcurrentDictionary<string, ITable> TableMap = new ConcurrentDictionary<string, ITable>();
        private static AsyncLocal<string> _localNameStorage = new AsyncLocal<string>();

        private static string LocalName
        {
            get => _localNameStorage.Value;
            set => _localNameStorage.Value = value;
        }

        public IDisposable WithTable(IDbConnection connection, ITable table)
        {
            var rnd = new Random();
            LocalName = rnd.Next(1, int.MaxValue).ToString();
            TableMap[LocalName] = table;

            using (var cmd = connection.CreateCommand())
            {
                // appears SET isn't parameterisable
                cmd.CommandText = $"SET application_name TO {LocalName}";
                cmd.ExecuteNonQuery();
            }
            
            return System.Reactive.Disposables.Disposable.Create(() =>
            {
                ITable unused;
                TableMap.TryRemove(LocalName, out unused);
                LocalName = null;
            });
        }

        public IEnumerable<IDictionary<string, object>> ScanTable(IReadOnlyList<Column> columns, IReadOnlyDictionary<string, string> options)
        {
            var appName = options["grpc_fdw.application_name"];
            var table = TableMap[appName];
            return table.ScanTable(columns, options);
        }
    }
}