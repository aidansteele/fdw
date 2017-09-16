using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading;

namespace FdwSharp
{
    class DictionaryStack<TKey, TValue>
    {
        private readonly AsyncLocal<ImmutableStack<IDictionary<TKey, TValue>>> _stackStorage = new AsyncLocal<ImmutableStack<IDictionary<TKey, TValue>>>();
        private ImmutableStack<IDictionary<TKey, TValue>> Stack
        {
            get => _stackStorage.Value;
            set => _stackStorage.Value = value;
        }
        
        internal ImmutableStack<IDictionary<TKey, TValue>> GetOrCreateStack()
        {
            var stack = Stack;
            if (stack != null) return stack;
            
            stack = ImmutableStack<IDictionary<TKey, TValue>>.Empty;
            Stack = stack;
            return stack;
        }

        public TValue Get(TKey key)
        {
            foreach (var dict in GetOrCreateStack())
            {
                if (dict.ContainsKey(key)) return dict[key];
            }

            return default(TValue);
        }

        public IDisposable Push(IDictionary<TKey, TValue> dict)
        {
            var oldStack = GetOrCreateStack();
            Stack = Stack.Push(dict);
            
            return Disposable.Create(() =>
            {
                Stack = oldStack;
            });
        }
    }
    
    public class TestContext
    {
        private static ConcurrentDictionary<string, ExecutionContext> TableMap = new ConcurrentDictionary<string, ExecutionContext>();
        private static readonly AsyncLocal<string> LocalNameStorage = new AsyncLocal<string>();

        private static string LocalName
        {
            get => LocalNameStorage.Value;
            set => LocalNameStorage.Value = value;
        }
        
        private static DictionaryStack<string, ITable> Tables = new DictionaryStack<string, ITable>();

        public static IDisposable PushTables(IDbConnection connection, IDictionary<string, ITable> tables)
        {
            var rnd = new Random();
            LocalName = rnd.Next(1, int.MaxValue).ToString();
            var disposable = Tables.Push(tables);
            TableMap[LocalName] = ExecutionContext.Capture();

            using (var cmd = connection.CreateCommand())
            {
                // appears SET isn't parameterisable
                cmd.CommandText = $"SET application_name TO {LocalName}";
                cmd.ExecuteNonQuery();
            }

            return new CompositeDisposable(disposable, Disposable.Create(() =>
            {
                ExecutionContext unused;
                TableMap.TryRemove(LocalName, out unused);
                LocalName = null;
            }));
        }

        public static ITable GetTable()
        {
            return new Table();
        }

        private class Table : ITable
        {
            public IEnumerable<IDictionary<string, object>> ScanTable(IReadOnlyList<Column> columns, IReadOnlyDictionary<string, string> options)
            {
                var appName = options["grpc_fdw.application_name"];
                var tableName = options["fdwsharp.table"];
                ITable table = null;
                ExecutionContext.Run(TableMap[appName], state => table = Tables.Get(tableName), null);
                return table.ScanTable(columns, options);
            }   
        }
    }
}