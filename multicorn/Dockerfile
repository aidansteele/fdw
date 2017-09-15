FROM postgres:9.6
RUN apt-get update
RUN apt-get install -y \
    build-essential \
    postgresql-server-dev-all \
    git \
    curl \
    python-dev \
    python-pip
RUN pip install pgxnclient
RUN pgxn install multicorn

RUN pip install grpcio
RUN pip install cffi
COPY setup.sh /docker-entrypoint-initdb.d/init-setup.sh

WORKDIR /app
COPY setup.py .
COPY grpc_fdw grpc_fdw
RUN python setup.py install

# pip install grpcio-tools
# python2 -m grpc_tools.protoc -I. --python_out=. --grpc_python_out=. postgres_fdw.proto 
