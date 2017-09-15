using System;
using System.Threading;
using Grpc.Core;
using PostgresFdw;
using Xunit;

namespace FdwSharp.Tests
{
    public class UnitTest1
    {
        [Fact]
        public async void Test1()
        {
            var channel = new Channel("127.0.0.1:50051", ChannelCredentials.Insecure);
            var client = new PostgresFdw.PostgresFdw.PostgresFdwClient(channel);
            
            using (var call = client.PerformForeignScan(new PerformForeignScanInput()))
            {
                while (await call.ResponseStream.MoveNext(new CancellationToken()))
                {
                    var row = call.ResponseStream.Current;
                    Console.WriteLine($"Received {row}");
                }
            }

            channel.ShutdownAsync().Wait();
        }
    }
}
