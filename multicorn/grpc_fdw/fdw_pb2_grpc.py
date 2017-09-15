# Generated by the gRPC Python protocol compiler plugin. DO NOT EDIT!
import grpc

import fdw_pb2 as fdw__pb2


class PostgresFdwStub(object):
  # missing associated documentation comment in .proto file
  pass

  def __init__(self, channel):
    """Constructor.

    Args:
      channel: A grpc.Channel.
    """
    self.PerformForeignScan = channel.unary_stream(
        '/PostgresFdw.PostgresFdw/PerformForeignScan',
        request_serializer=fdw__pb2.PerformForeignScanInput.SerializeToString,
        response_deserializer=fdw__pb2.PerformForeignScanOutput.FromString,
        )


class PostgresFdwServicer(object):
  # missing associated documentation comment in .proto file
  pass

  def PerformForeignScan(self, request, context):
    """rpc GetForeignRelSize(GetForeignRelSizeInput) returns (GetForeignRelSizeOutput) {}
    rpc GetForeignPaths(GetForeignPathsInput) returns (GetForeignPathsOutput) {}
    rpc GetForeignPlan(GetForeignPlanInput) returns (GetForeignPlanOutput) {}
    """
    context.set_code(grpc.StatusCode.UNIMPLEMENTED)
    context.set_details('Method not implemented!')
    raise NotImplementedError('Method not implemented!')


def add_PostgresFdwServicer_to_server(servicer, server):
  rpc_method_handlers = {
      'PerformForeignScan': grpc.unary_stream_rpc_method_handler(
          servicer.PerformForeignScan,
          request_deserializer=fdw__pb2.PerformForeignScanInput.FromString,
          response_serializer=fdw__pb2.PerformForeignScanOutput.SerializeToString,
      ),
  }
  generic_handler = grpc.method_handlers_generic_handler(
      'PostgresFdw.PostgresFdw', rpc_method_handlers)
  server.add_generic_rpc_handlers((generic_handler,))
