using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading;

namespace FdwSharp
{
    public class FdwTestContext
    {
        private readonly ConcurrentDictionary<string, ExecutionContext> _executionContextMap;        
        private readonly DictionaryStack<string, ITable> _tables;

        public FdwTestContext()
        {
            _executionContextMap = new ConcurrentDictionary<string, ExecutionContext>();
            _tables = new DictionaryStack<string, ITable>();
        }

        public IDisposable PushTables(IDictionary<string, ITable> tables)
        {
            return _tables.Push(tables);
        }
        
        public IDisposable PushTable(string name, ITable table)
        {
            var dict = new Dictionary<string, ITable> {{name, table}};
            return _tables.Push(dict);
        }
        
        public IDisposable WrapConnection(IDbConnection connection)
        {
            var rnd = new Random();
            var contextName = rnd.Next(1, int.MaxValue).ToString();
            _executionContextMap[contextName] = ExecutionContext.Capture();

            using (var cmd = connection.CreateCommand())
            {
                // appears SET isn't parameterisable
                cmd.CommandText = $"SET application_name TO {contextName}";
                cmd.ExecuteNonQuery();
            }

            return Disposable.Create(() =>
            {
                ExecutionContext unused;
                _executionContextMap.TryRemove(contextName, out unused);
            });
        }

        public ITable GetTable()
        {
            return new ContextualTable(this);
        }

        private class ContextualTable : ITable
        {
            private readonly FdwTestContext _context;
            
            public ContextualTable(FdwTestContext context)
            {
                _context = context;
            }

            public IEnumerable<IDictionary<string, object>> ScanTable(IReadOnlyList<Column> columns, IReadOnlyDictionary<string, string> options)
            {
                ITable table = null;

                var appName = options["grpc_fdw.application_name"];
                var tableName = options["fdwsharp.table"];
                var context = _context._executionContextMap[appName];
                ExecutionContext.Run(context, state => table = _context._tables.Get(tableName), null);
                return table.ScanTable(columns, options);
            }   
        }
    }
}