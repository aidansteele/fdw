using System;

namespace FdwSharp
{
    public class FdwTestFixture : IDisposable
    {
        private FdwSharpServer _server;
        public FdwTestContext Context { get; }

        public FdwTestFixture()
        {
            Context = new FdwTestContext();
            _server = new FdwSharpServer(new FdwSharpServer.Options
            {
                Table = Context.GetTable(),
            });
            _server.Start();
        }

        public void Dispose()
        {
            _server.Shutdown().Wait();
        }
    }
}