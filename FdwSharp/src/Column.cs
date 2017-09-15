using System.Collections.Generic;

namespace FdwSharp
{
    public struct Column
    {
        public string Name;
        public int Oid;
        public int Mod;
        public string TypeName;
        public string BaseTypeName;
        public IReadOnlyDictionary<string, string> Options;
    }
}