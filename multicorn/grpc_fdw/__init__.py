import multicorn
from multicorn.utils import log_to_postgres
import grpc
import fdw_pb2
import fdw_pb2_grpc
from cffi import FFI

class MyForeignDataWrapper(multicorn.ForeignDataWrapper):

    def __init__(self, options, columns):
        super(MyForeignDataWrapper, self).__init__(options, columns)
        log_to_postgres("creating")        
        
        addr = options["grpc_fdw.address"]
        channel = grpc.insecure_channel(addr)
        self.stub = fdw_pb2_grpc.PostgresFdwStub(channel)
        self.options = options
        self.columns = columns

        ffi = FFI()
        ffi.cdef("extern const char *GetConfigOption(const char *name, bool missing_ok, bool restrict_superuser);")
        self.C = ffi.dlopen(None)

    @classmethod
    def import_schema(self, schema, srv_options, options, restriction_type, restricts):
        addr = srv_options["grpc_fdw.address"]
        channel = grpc.insecure_channel(addr)
        stub = fdw_pb2_grpc.PostgresFdwStub(channel)
        
        inp = fdw_pb2.ImportForeignSchemaInput(
            schema=schema, 
            serverOptions=srv_options,
            importOptions=options,
            restrictionType=fdw_pb2.ImportForeignSchemaInput.NONE, # FIXME
            restricted=restricts
        )

        outp = stub.ImportForeignSchema(inp)
        return [multicorn.TableDefinition(
            table_name=t.name,
            columns=[multicorn.ColumnDefinition(
                column_name=c.name,
                type_oid=c.oid,
                typmod=c.mod,
                type_name=c.typeName,
                base_type_name=c.baseTypeName,
                options=c.options
            ) for c in t.columns],
            options=t.options,
        ) for t in outp.tables]

    def appName(self):
        ret = self.C.GetConfigOption("application_name", True, True)
        ffi = FFI()        
        return ffi.string(ret)

    def execute(self, quals, columns):
        req_columns = [fdw_pb2.ColumnDefinition(
            name=c.column_name, 
            oid=c.type_oid, 
            mod=c.typmod, 
            typeName=c.type_name, 
            baseTypeName=c.base_type_name, 
            options=c.options
        ) for (n, c) in self.columns.items()]

        self.options["grpc_fdw.application_name"] = self.appName()

        inp = fdw_pb2.PerformForeignScanInput(columns=req_columns, options=self.options)        
        for res in self.stub.PerformForeignScan(inp):
            for row in res.rows:
                line = {}
                for column_name in columns:
                    val = row.fields[column_name]
                    valtype = val.WhichOneof("test_oneof")
                    if valtype == None:
                        continue
                    fn = getattr(val, valtype)
                    line[column_name] = fn
                yield line
        # df = pd.read_csv(self.filename, engine='python')

        # for qual in quals:
        #     if qual.operator == '<':
        #         df = df[df[qual.field_name] < qual.value]

        # df = df[list(columns)]
        # for i, row in df.iterrows():
        #     yield row.to_dict()

