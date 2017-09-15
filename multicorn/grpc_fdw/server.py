
import grpc
from concurrent import futures
import time
import fdw_pb2
import fdw_pb2_grpc

class PostgresFdwServicer(fdw_pb2_grpc.PostgresFdwServicer):
    def __init__(self):
        pass

    def PerformForeignScan(self, request, context):
        for index in range(20):
            row = {}
            for column in request.columns:
                row[column] = '%s %s' % (column, index)
            yield fdw_pb2.PerformForeignScanOutput(row=row)

def serve():
  server = grpc.server(futures.ThreadPoolExecutor(max_workers=10))
  fdw_pb2_grpc.add_PostgresFdwServicer_to_server(PostgresFdwServicer(), server)
  server.add_insecure_port('[::]:50051')
  server.start()
  
  try:
    while True:
      time.sleep(86400)
  except KeyboardInterrupt:
    server.stop(0)

if __name__ == '__main__':
  serve()
