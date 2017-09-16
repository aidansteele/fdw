using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading;

namespace FdwSharp
{
    public class TestContext
    {
        private static readonly ConcurrentDictionary<string, ExecutionContext> ExecutionContextMap = new ConcurrentDictionary<string, ExecutionContext>();        
        private static DictionaryStack<string, ITable> Tables = new DictionaryStack<string, ITable>();

        public static IDisposable PushTables(IDictionary<string, ITable> tables)
        {
            return Tables.Push(tables);
        }
        
        public static IDisposable PushTable(string name, ITable table)
        {
            var dict = new Dictionary<string, ITable> {{name, table}};
            return Tables.Push(dict);
        }
        
        public static IDisposable WrapConnection(IDbConnection connection)
        {
            var rnd = new Random();
            var contextName = rnd.Next(1, int.MaxValue).ToString();
            ExecutionContextMap[contextName] = ExecutionContext.Capture();

            using (var cmd = connection.CreateCommand())
            {
                // appears SET isn't parameterisable
                cmd.CommandText = $"SET application_name TO {contextName}";
                cmd.ExecuteNonQuery();
            }

            return Disposable.Create(() =>
            {
                ExecutionContext unused;
                ExecutionContextMap.TryRemove(contextName, out unused);
            });
        }

        public static ITable GetTable()
        {
            return new ContextualTable();
        }

        private class ContextualTable : ITable
        {
            public IEnumerable<IDictionary<string, object>> ScanTable(IReadOnlyList<Column> columns, IReadOnlyDictionary<string, string> options)
            {
                ITable table = null;

                var appName = options["grpc_fdw.application_name"];
                var tableName = options["fdwsharp.table"];
                var context = ExecutionContextMap[appName];
                ExecutionContext.Run(context, state => table = Tables.Get(tableName), null);
                return table.ScanTable(columns, options);
            }   
        }
    }
}