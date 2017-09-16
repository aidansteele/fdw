using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reactive.Disposables;
using System.Threading;

namespace FdwSharp
{
    internal class DictionaryStack<TKey, TValue>
    {
        private readonly AsyncLocal<ImmutableStack<IDictionary<TKey, TValue>>> _stackStorage = new AsyncLocal<ImmutableStack<IDictionary<TKey, TValue>>>();
        private ImmutableStack<IDictionary<TKey, TValue>> Stack
        {
            get
            {
                var stack = _stackStorage.Value;
                if (stack != null) return stack;
            
                stack = ImmutableStack<IDictionary<TKey, TValue>>.Empty;
                _stackStorage.Value = stack;
                return stack;
            }
            set => _stackStorage.Value = value;
        }

        public TValue Get(TKey key)
        {
            foreach (var dict in Stack)
            {
                if (dict.ContainsKey(key)) return dict[key];
            }

            return default(TValue);
        }

        public IDisposable Push(IDictionary<TKey, TValue> dict)
        {
            var oldStack = Stack;
            Stack = Stack.Push(dict);
            
            return Disposable.Create(() =>
            {
                Stack = oldStack;
            });
        }
    }
}