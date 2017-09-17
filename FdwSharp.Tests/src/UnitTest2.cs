using System.Collections.Generic;
using Npgsql;
using Xunit;

namespace FdwSharp.Tests
{
    [CollectionDefinition("DbCollection")]
    public class DatabaseCollection : ICollectionFixture<FdwTestFixture>{}
    
    [Collection("DbCollection")]
    public class UnitTest
    {
        private FdwTestFixture _fdwTestFixture;

        public UnitTest(FdwTestFixture fixture)
        {
            _fdwTestFixture = fixture;
        }
        
        [Fact]
        public void Test1()
        {
            var connString = "Host=localhost;Username=postgres;Password=postgres;Database=postgres;Port=5433;Maximum Pool Size=90;Application Name=moo";
            var conn = new NpgsqlConnection(connString);
            conn.Open();
            
            var productsTable = new SingleRowTable(
                "productid", 1, 
                "productname", "product A"
            );

            var tables = new Dictionary<string, ITable>
            {
                {"Purchases", new PurchasesTable()},
                {"Products", productsTable}
            };
            
            using (_fdwTestFixture.Context.PushTables(tables))
            {
                using (_fdwTestFixture.Context.WrapConnection(conn))
                {
                    // do things ...
                }                  
            }
        }
    }
}
