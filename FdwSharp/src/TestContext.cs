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
        private static readonly ConcurrentDictionary<string, ExecutionContext> ExecutionContextMap = new ConcurrentDictionary<string, ExecutionContext>();        
        private static DictionaryStack<string, ITable> Tables = new DictionaryStack<string, ITable>();

        public static IDisposable PushTables(IDictionary<string, ITable> tables)
        {
            return Tables.Push(tables);
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
            return new Table();
        }

        private class Table : ITable
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