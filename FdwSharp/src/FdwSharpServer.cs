using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using PostgresFdw;

namespace FdwSharp
{
    public class FdwSharpServer
    {
        public class Options
        {
            public string Address;
            public int Port;
            public ITable Table;
            public ITableImporter TableImporter;

            public Options()
            {
                Address = "localhost";
                Port = 50051;
            }
        }
        
        private readonly ServerImpl _serverImpl;
        
        public FdwSharpServer(Options options)
        {
            _serverImpl = new ServerImpl(options);
        }

        public void Start()
        {
            _serverImpl.Server.Start();
        }

        public Task Shutdown()
        {
            return _serverImpl.Server.ShutdownAsync();
        }
        
        private class ServerImpl: PostgresFdw.PostgresFdw.PostgresFdwBase
        {
            private readonly ITable _table;
            private readonly ITableImporter _importer;
            internal readonly Server Server;

            internal ServerImpl(Options options)
            {
                _table = options.Table;
                _importer = options.TableImporter;

                Server = new Server
                {
                    Services = { PostgresFdw.PostgresFdw.BindService(this) },
                    Ports = { new ServerPort(options.Address, options.Port, ServerCredentials.Insecure) }
                };
            }

            private Row RowToRow(IDictionary<string, object> input)
            {
                var output = new Row();

                foreach (var kvp in input)
                {
                    var pv = new Row.Types.RowValue();
                    // http://www.npgsql.org/doc/types/basic.html
                    if (kvp.Value is string str) { pv.StringValue = str; }
                    else if (kvp.Value is int inum) { pv.IntValue = inum; }
                    else if (kvp.Value is long lnum) { pv.LongValue = lnum; }
                    else if (kvp.Value is float fnum) { pv.FloatValue = fnum; }
                    else if (kvp.Value is double dnum ) { pv.DoubleValue = dnum; }
                    else if (kvp.Value is bool boolv ) { pv.BoolValue = boolv; }
                
                    output.Fields.Add(kvp.Key, pv);
                }

                return output;
            }

            private Column ColToCol(ColumnDefinition col)
            {
                return new Column
                {
                    Name = col.Name,
                    Oid = col.Oid,
                    Mod = col.Mod,
                    TypeName = col.TypeName,
                    BaseTypeName = col.BaseTypeName,
                    Options = col.Options
                };
            }
            
            public override async Task PerformForeignScan(PerformForeignScanInput request, IServerStreamWriter<PerformForeignScanOutput> responseStream, ServerCallContext context)
            {
                var columns = request.Columns.Select(ColToCol).ToList();
                
                var rows = _table.ScanTable(columns, request.Options);
                var protoRows = rows.Select(RowToRow);
            
                var output = new PerformForeignScanOutput { Rows = { protoRows }  };
                await responseStream.WriteAsync(output);
            }

            public override async Task<ImportForeignSchemaOutput> ImportForeignSchema(ImportForeignSchemaInput request, ServerCallContext context)
            {
                var restriction = RestrictionToRestriction(request);

                var tables = await _importer.ImportTables(request.Schema, request.ServerOptions, request.ImportOptions, restriction, request.Restricted);
                                
                var protoTables = tables.Select(table =>
                {
                    var options = table.Options.ToDictionary(
                        kvp => $"\"{kvp.Key}\"", 
                        kvp => kvp.Value
                    );
                    options.Add("\"fdwsharp.table\"", table.Name);

                    return new PostgresFdw.TableDefinition
                    {
                        Name = table.Name,
                        Columns = {table.Columns},
                        Options = {options},
                    };
                });

                return new ImportForeignSchemaOutput { Tables = { protoTables } };
            }

            private TableImportRestriction RestrictionToRestriction(ImportForeignSchemaInput request)
            {
                switch (request.RestrictionType)
                {
                    case ImportForeignSchemaInput.Types.RestrictionType.None: return TableImportRestriction.None;
                    case ImportForeignSchemaInput.Types.RestrictionType.Limit: return TableImportRestriction.Limit;
                    case ImportForeignSchemaInput.Types.RestrictionType.Except: return TableImportRestriction.Except;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
}
