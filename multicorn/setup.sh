#!/bin/bash
set -euxo pipefail

psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" <<-EOSQL
    CREATE EXTENSION multicorn;

    CREATE SERVER example foreign data wrapper multicorn options (
        "wrapper" 'grpc_fdw.GrpcForeignDataWrapper',
        "grpc_fdw.address" 'docker.for.mac.localhost:50051'
    );

    IMPORT FOREIGN SCHEMA moo FROM SERVER example INTO public OPTIONS (hey 'there');

    -- CREATE FOREIGN TABLE products (
    --     productid int,
    --     productname text
    -- ) server example options(
    --     "fdwsharp.table" 'Products'
    -- );

    -- CREATE FOREIGN TABLE purchases (
    --     purchaseid int,
    --     customerid int,
    --     productid int
    -- ) server example options(
    --     "fdwsharp.table" 'Purchases'
    -- );

    --SELECT purchases.*, productname FROM purchases JOIN products ON purchases.productid = products.productid WHERE purchases.customerid = 99;
    --SELECT * FROM moo; -- should fail
EOSQL
