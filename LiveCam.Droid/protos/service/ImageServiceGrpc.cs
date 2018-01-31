// Generated by the protocol buffer compiler.  DO NOT EDIT!
// source: protos/service/image_service.proto
#pragma warning disable 1591
#region Designer generated code

using System;
using System.Threading;
using System.Threading.Tasks;
using grpc = global::Grpc.Core;

namespace ImageService {
  public static partial class ImageService
  {
    static readonly string __ServiceName = "image_service.ImageService";

    static readonly grpc::Marshaller<global::Query> __Marshaller_Query = grpc::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Query.Parser.ParseFrom);
    static readonly grpc::Marshaller<global::QueryResponse> __Marshaller_QueryResponse = grpc::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::QueryResponse.Parser.ParseFrom);

    static readonly grpc::Method<global::Query, global::QueryResponse> __Method_make_request = new grpc::Method<global::Query, global::QueryResponse>(
        grpc::MethodType.Unary,
        __ServiceName,
        "make_request",
        __Marshaller_Query,
        __Marshaller_QueryResponse);

    /// <summary>Service descriptor</summary>
    public static global::Google.Protobuf.Reflection.ServiceDescriptor Descriptor
    {
      get { return global::ImageService.ImageServiceReflection.Descriptor.Services[0]; }
    }

    /// <summary>Base class for server-side implementations of ImageService</summary>
    public abstract partial class ImageServiceBase
    {
      public virtual global::System.Threading.Tasks.Task<global::QueryResponse> make_request(global::Query request, grpc::ServerCallContext context)
      {
        throw new grpc::RpcException(new grpc::Status(grpc::StatusCode.Unimplemented, ""));
      }

    }

    /// <summary>Client for ImageService</summary>
    public partial class ImageServiceClient : grpc::ClientBase<ImageServiceClient>
    {
      /// <summary>Creates a new client for ImageService</summary>
      /// <param name="channel">The channel to use to make remote calls.</param>
      public ImageServiceClient(grpc::Channel channel) : base(channel)
      {
      }
      /// <summary>Creates a new client for ImageService that uses a custom <c>CallInvoker</c>.</summary>
      /// <param name="callInvoker">The callInvoker to use to make remote calls.</param>
      public ImageServiceClient(grpc::CallInvoker callInvoker) : base(callInvoker)
      {
      }
      /// <summary>Protected parameterless constructor to allow creation of test doubles.</summary>
      protected ImageServiceClient() : base()
      {
      }
      /// <summary>Protected constructor to allow creation of configured clients.</summary>
      /// <param name="configuration">The client configuration.</param>
      protected ImageServiceClient(ClientBaseConfiguration configuration) : base(configuration)
      {
      }

      public virtual global::QueryResponse make_request(global::Query request, grpc::Metadata headers = null, DateTime? deadline = null, CancellationToken cancellationToken = default(CancellationToken))
      {
        return make_request(request, new grpc::CallOptions(headers, deadline, cancellationToken));
      }
      public virtual global::QueryResponse make_request(global::Query request, grpc::CallOptions options)
      {
        return CallInvoker.BlockingUnaryCall(__Method_make_request, null, options, request);
      }
      public virtual grpc::AsyncUnaryCall<global::QueryResponse> make_requestAsync(global::Query request, grpc::Metadata headers = null, DateTime? deadline = null, CancellationToken cancellationToken = default(CancellationToken))
      {
        return make_requestAsync(request, new grpc::CallOptions(headers, deadline, cancellationToken));
      }
      public virtual grpc::AsyncUnaryCall<global::QueryResponse> make_requestAsync(global::Query request, grpc::CallOptions options)
      {
        return CallInvoker.AsyncUnaryCall(__Method_make_request, null, options, request);
      }
      /// <summary>Creates a new instance of client from given <c>ClientBaseConfiguration</c>.</summary>
      protected override ImageServiceClient NewInstance(ClientBaseConfiguration configuration)
      {
        return new ImageServiceClient(configuration);
      }
    }

    /// <summary>Creates service definition that can be registered with a server</summary>
    /// <param name="serviceImpl">An object implementing the server-side handling logic.</param>
    public static grpc::ServerServiceDefinition BindService(ImageServiceBase serviceImpl)
    {
      return grpc::ServerServiceDefinition.CreateBuilder()
          .AddMethod(__Method_make_request, serviceImpl.make_request).Build();
    }

  }
}
#endregion
