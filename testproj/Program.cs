using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using FdwSharp;
using Newtonsoft.Json;
using Npgsql;

namespace testproj
{
    class Program
    {
        class ProductsTable : ITable
        {
            public IEnumerable<IDictionary<string, object>> ScanTable(IReadOnlyList<Column> columns, IReadOnlyDictionary<string, string> options)
            {
                for (var i = 0; i < 3; i++)
                {
                    dynamic row = new ExpandoObject();
                    row.productid = i;
                    row.productname = $"Product {i}";
                    yield return row;
                }
            }
        }
        
        class PurchasesTable : ITable
        {
            public IEnumerable<IDictionary<string, object>> ScanTable(IReadOnlyList<Column> columns, IReadOnlyDictionary<string, string> options)
            {
                for (var i = 0; i < 10; i++)
                {
                    dynamic row = new ExpandoObject();
                    row.purchaseid = i;
                    row.customerid = 100 - i;
                    row.productid = i % 3;
                    yield return row;
                }
            }
        }

        static async Task<IReadOnlyList<IDictionary<string, object>>> ReaderToDictionaries(DbDataReader reader)
        {
            var dicts = new List<IDictionary<string, object>>();
            
            while (await reader.ReadAsync())
            {
                var dict = Enumerable.Range(0, reader.FieldCount).ToDictionary(reader.GetName, reader.GetValue);
                dicts.Add(dict);
            }

            return dicts;
        }
        
        static void Main(string[] args)
        {
            var context = new ContextualTable();
            var server = new FdwSharpServer(context, new FdwSharpServer.Options());
            server.Start();
            
            var connString = "Host=localhost;Username=postgres;Password=postgres;Database=postgres;Port=5433;Maximum Pool Size=90;Application Name=moo";
            
            var t1 = Task.Run(async () =>
            {
                var conn = new NpgsqlConnection(connString);
                conn.Open();
                
                var table = new SingleRowTable(
                    "productid", 1, 
                    "productname", "product A", 
                    "purchaseid", 2, 
                    "customerid", 99
                );

                using (context.WithTable(conn, table))
                {
                    var cmd = new NpgsqlCommand("SELECT purchases.*, productname FROM purchases JOIN products ON purchases.productid = products.productid WHERE purchases.customerid = @custid;", conn);
                    cmd.Parameters.AddWithValue("custid", 99);
                    
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        var rows = await ReaderToDictionaries(reader);
                        Console.WriteLine(JsonConvert.SerializeObject(rows));
                    }
                }
            });

            var t2 = Task.Run(async () =>
            {
                var conn = new NpgsqlConnection(connString);
                conn.Open();
                
                var table = new LambdaTable((columns, options) =>
                {
                    return new List<IDictionary<string, object>>
                    {
                        new Dictionary<string, object>
                        {
                            {"productid", 1},
                            {"productname", "some other product"},
                            {"purchaseid", 2},
                            {"customerid", 99}
                        },
                    };
                });
//                var table = new TableSelector(new Dictionary<string, ITable>()
//                {
//                    { "Products", new ProductsTable() },
//                    { "Purchases", new PurchasesTable() },
//                });
            
                using (context.WithTable(conn, table))
                {
                    var cmd = new NpgsqlCommand("SELECT purchases.*, productname FROM purchases JOIN products ON purchases.productid = products.productid WHERE purchases.customerid = @custid;", conn);
                    cmd.Parameters.AddWithValue("custid", 99);
                    
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        var rows = await ReaderToDictionaries(reader);
                        Console.WriteLine(JsonConvert.SerializeObject(rows));
                    }
                }
            });

            Task.WaitAll(t1, t2);
            server.Shutdown().Wait();
        }
    }
}
