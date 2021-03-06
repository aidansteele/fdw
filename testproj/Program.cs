﻿using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using FdwSharp;
using Newtonsoft.Json;
using Npgsql;
using PostgresFdw;
using TableDefinition = FdwSharp.TableDefinition;

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
            var ctx = new FdwTestContext();
            var _server = new FdwSharpServer(new FdwSharpServer.Options
            {
                TableImporter = new Importer(),
                Table = ctx.GetTable()
            });
            _server.Start();
            Console.ReadKey();
            
//            var fix = new FdwTestFixture();
//            
            var connString = "Host=localhost;Username=postgres;Password=postgres;Database=postgres;Port=5433;Maximum Pool Size=90;Application Name=moo";
            
            var productsTable1 = new SingleRowTable(
                "productid", 1, 
                "productname", "product A"
            );
            
            var productsTable2 = new SingleRowTable(
                "productid", 1, 
                "productname", "product B"
            );

            var dothings = new Func<Task>(async () =>
            {
                var conn = new NpgsqlConnection(connString);
                conn.Open();

                using (ctx.WrapConnection(conn))
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
            
            using (ctx.PushTables(new Dictionary<string, ITable>{{"purchases", new PurchasesTable()}}))
            {
                Task t1, t2;
                
                using (ctx.PushTable("products", productsTable1))
                {
                    t1 = Task.Run(dothings);                    
                }

                using (ctx.PushTable("products", productsTable2))
                {
                    t2 = Task.Run(dothings);    
                }
                
                Task.WaitAll(t1, t2);
            }
        }
    }

    internal class Importer : ITableImporter
    {
        public Task<IEnumerable<TableDefinition>> ImportTables(string schema, IDictionary<string, string> serverOptions, IDictionary<string, string> importOptions,
            TableImportRestriction importRestriction, ICollection<string> restrictedTables)
        {
            var products = new TableDefinition(
                "products", 
                new List<ColumnDefinition>
                {
                    new ColumnDefinition { Name = "productid", TypeName = "int" },
                    new ColumnDefinition { Name = "productname", TypeName = "text" },
                }, 
                new Dictionary<string, string>()
            );

            var purchases = new TableDefinition(
                "purchases",
                new List<ColumnDefinition>
                {
                    new ColumnDefinition {Name = "productid", TypeName = "int"},
                    new ColumnDefinition {Name = "purchaseid", TypeName = "int"},
                    new ColumnDefinition {Name = "customerid", TypeName = "int"},
                },
                new Dictionary<string, string>()
            );

            IEnumerable<TableDefinition> tables = new List<TableDefinition> {products, purchases};
            return Task.FromResult(tables);
        }
    }
}
