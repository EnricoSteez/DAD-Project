// <auto-generated>
//     Generated by the protocol buffer compiler.  DO NOT EDIT!
//     source: protos/PuppetMasterService.proto
// </auto-generated>
#pragma warning disable 0414, 1591
#region Designer generated code

using grpc = global::Grpc.Core;

namespace Server.protos {
  public static partial class PuppetMasterServices
  {
    static readonly string __ServiceName = "PuppetMasterServices";

    static void __Helper_SerializeMessage(global::Google.Protobuf.IMessage message, grpc::SerializationContext context)
    {
      #if !GRPC_DISABLE_PROTOBUF_BUFFER_SERIALIZATION
      if (message is global::Google.Protobuf.IBufferMessage)
      {
        context.SetPayloadLength(message.CalculateSize());
        global::Google.Protobuf.MessageExtensions.WriteTo(message, context.GetBufferWriter());
        context.Complete();
        return;
      }
      #endif
      context.Complete(global::Google.Protobuf.MessageExtensions.ToByteArray(message));
    }

    static class __Helper_MessageCache<T>
    {
      public static readonly bool IsBufferMessage = global::System.Reflection.IntrospectionExtensions.GetTypeInfo(typeof(global::Google.Protobuf.IBufferMessage)).IsAssignableFrom(typeof(T));
    }

    static T __Helper_DeserializeMessage<T>(grpc::DeserializationContext context, global::Google.Protobuf.MessageParser<T> parser) where T : global::Google.Protobuf.IMessage<T>
    {
      #if !GRPC_DISABLE_PROTOBUF_BUFFER_SERIALIZATION
      if (__Helper_MessageCache<T>.IsBufferMessage)
      {
        return parser.ParseFrom(context.PayloadAsReadOnlySequence());
      }
      #endif
      return parser.ParseFrom(context.PayloadAsNewBuffer());
    }

    static readonly grpc::Marshaller<global::Server.protos.ServerRequestObject> __Marshaller_ServerRequestObject = grpc::Marshallers.Create(__Helper_SerializeMessage, context => __Helper_DeserializeMessage(context, global::Server.protos.ServerRequestObject.Parser));
    static readonly grpc::Marshaller<global::Server.protos.ServerResponseObject> __Marshaller_ServerResponseObject = grpc::Marshallers.Create(__Helper_SerializeMessage, context => __Helper_DeserializeMessage(context, global::Server.protos.ServerResponseObject.Parser));
    static readonly grpc::Marshaller<global::Server.protos.PartitionRequestObject> __Marshaller_PartitionRequestObject = grpc::Marshallers.Create(__Helper_SerializeMessage, context => __Helper_DeserializeMessage(context, global::Server.protos.PartitionRequestObject.Parser));
    static readonly grpc::Marshaller<global::Server.protos.PartitionResponseObject> __Marshaller_PartitionResponseObject = grpc::Marshallers.Create(__Helper_SerializeMessage, context => __Helper_DeserializeMessage(context, global::Server.protos.PartitionResponseObject.Parser));
    static readonly grpc::Marshaller<global::Server.protos.StatusRequestObject> __Marshaller_StatusRequestObject = grpc::Marshallers.Create(__Helper_SerializeMessage, context => __Helper_DeserializeMessage(context, global::Server.protos.StatusRequestObject.Parser));
    static readonly grpc::Marshaller<global::Server.protos.StatusResponseObject> __Marshaller_StatusResponseObject = grpc::Marshallers.Create(__Helper_SerializeMessage, context => __Helper_DeserializeMessage(context, global::Server.protos.StatusResponseObject.Parser));
    static readonly grpc::Marshaller<global::Server.protos.ClientRequestObject> __Marshaller_ClientRequestObject = grpc::Marshallers.Create(__Helper_SerializeMessage, context => __Helper_DeserializeMessage(context, global::Server.protos.ClientRequestObject.Parser));
    static readonly grpc::Marshaller<global::Server.protos.ClientResponseObject> __Marshaller_ClientResponseObject = grpc::Marshallers.Create(__Helper_SerializeMessage, context => __Helper_DeserializeMessage(context, global::Server.protos.ClientResponseObject.Parser));
    static readonly grpc::Marshaller<global::Server.protos.CrashRequestObject> __Marshaller_CrashRequestObject = grpc::Marshallers.Create(__Helper_SerializeMessage, context => __Helper_DeserializeMessage(context, global::Server.protos.CrashRequestObject.Parser));
    static readonly grpc::Marshaller<global::Server.protos.CrashResponseObject> __Marshaller_CrashResponseObject = grpc::Marshallers.Create(__Helper_SerializeMessage, context => __Helper_DeserializeMessage(context, global::Server.protos.CrashResponseObject.Parser));
    static readonly grpc::Marshaller<global::Server.protos.FreezeRequestObject> __Marshaller_FreezeRequestObject = grpc::Marshallers.Create(__Helper_SerializeMessage, context => __Helper_DeserializeMessage(context, global::Server.protos.FreezeRequestObject.Parser));
    static readonly grpc::Marshaller<global::Server.protos.FreezeResponseObject> __Marshaller_FreezeResponseObject = grpc::Marshallers.Create(__Helper_SerializeMessage, context => __Helper_DeserializeMessage(context, global::Server.protos.FreezeResponseObject.Parser));
    static readonly grpc::Marshaller<global::Server.protos.UnfreezeRequestObject> __Marshaller_UnfreezeRequestObject = grpc::Marshallers.Create(__Helper_SerializeMessage, context => __Helper_DeserializeMessage(context, global::Server.protos.UnfreezeRequestObject.Parser));
    static readonly grpc::Marshaller<global::Server.protos.UnfreezeResponseObject> __Marshaller_UnfreezeResponseObject = grpc::Marshallers.Create(__Helper_SerializeMessage, context => __Helper_DeserializeMessage(context, global::Server.protos.UnfreezeResponseObject.Parser));

    static readonly grpc::Method<global::Server.protos.ServerRequestObject, global::Server.protos.ServerResponseObject> __Method_ServerRequest = new grpc::Method<global::Server.protos.ServerRequestObject, global::Server.protos.ServerResponseObject>(
        grpc::MethodType.Unary,
        __ServiceName,
        "ServerRequest",
        __Marshaller_ServerRequestObject,
        __Marshaller_ServerResponseObject);

    static readonly grpc::Method<global::Server.protos.PartitionRequestObject, global::Server.protos.PartitionResponseObject> __Method_PartitionRequest = new grpc::Method<global::Server.protos.PartitionRequestObject, global::Server.protos.PartitionResponseObject>(
        grpc::MethodType.Unary,
        __ServiceName,
        "PartitionRequest",
        __Marshaller_PartitionRequestObject,
        __Marshaller_PartitionResponseObject);

    static readonly grpc::Method<global::Server.protos.StatusRequestObject, global::Server.protos.StatusResponseObject> __Method_StatusRequest = new grpc::Method<global::Server.protos.StatusRequestObject, global::Server.protos.StatusResponseObject>(
        grpc::MethodType.Unary,
        __ServiceName,
        "StatusRequest",
        __Marshaller_StatusRequestObject,
        __Marshaller_StatusResponseObject);

    static readonly grpc::Method<global::Server.protos.ClientRequestObject, global::Server.protos.ClientResponseObject> __Method_ClientRequest = new grpc::Method<global::Server.protos.ClientRequestObject, global::Server.protos.ClientResponseObject>(
        grpc::MethodType.Unary,
        __ServiceName,
        "ClientRequest",
        __Marshaller_ClientRequestObject,
        __Marshaller_ClientResponseObject);

    static readonly grpc::Method<global::Server.protos.CrashRequestObject, global::Server.protos.CrashResponseObject> __Method_CrashRequest = new grpc::Method<global::Server.protos.CrashRequestObject, global::Server.protos.CrashResponseObject>(
        grpc::MethodType.Unary,
        __ServiceName,
        "CrashRequest",
        __Marshaller_CrashRequestObject,
        __Marshaller_CrashResponseObject);

    static readonly grpc::Method<global::Server.protos.FreezeRequestObject, global::Server.protos.FreezeResponseObject> __Method_FreezeRequest = new grpc::Method<global::Server.protos.FreezeRequestObject, global::Server.protos.FreezeResponseObject>(
        grpc::MethodType.Unary,
        __ServiceName,
        "FreezeRequest",
        __Marshaller_FreezeRequestObject,
        __Marshaller_FreezeResponseObject);

    static readonly grpc::Method<global::Server.protos.UnfreezeRequestObject, global::Server.protos.UnfreezeResponseObject> __Method_UnfreezeRequest = new grpc::Method<global::Server.protos.UnfreezeRequestObject, global::Server.protos.UnfreezeResponseObject>(
        grpc::MethodType.Unary,
        __ServiceName,
        "UnfreezeRequest",
        __Marshaller_UnfreezeRequestObject,
        __Marshaller_UnfreezeResponseObject);

    /// <summary>Service descriptor</summary>
    public static global::Google.Protobuf.Reflection.ServiceDescriptor Descriptor
    {
      get { return global::Server.protos.PuppetMasterServiceReflection.Descriptor.Services[0]; }
    }

    /// <summary>Client for PuppetMasterServices</summary>
    public partial class PuppetMasterServicesClient : grpc::ClientBase<PuppetMasterServicesClient>
    {
      /// <summary>Creates a new client for PuppetMasterServices</summary>
      /// <param name="channel">The channel to use to make remote calls.</param>
      public PuppetMasterServicesClient(grpc::ChannelBase channel) : base(channel)
      {
      }
      /// <summary>Creates a new client for PuppetMasterServices that uses a custom <c>CallInvoker</c>.</summary>
      /// <param name="callInvoker">The callInvoker to use to make remote calls.</param>
      public PuppetMasterServicesClient(grpc::CallInvoker callInvoker) : base(callInvoker)
      {
      }
      /// <summary>Protected parameterless constructor to allow creation of test doubles.</summary>
      protected PuppetMasterServicesClient() : base()
      {
      }
      /// <summary>Protected constructor to allow creation of configured clients.</summary>
      /// <param name="configuration">The client configuration.</param>
      protected PuppetMasterServicesClient(ClientBaseConfiguration configuration) : base(configuration)
      {
      }

      public virtual global::Server.protos.ServerResponseObject ServerRequest(global::Server.protos.ServerRequestObject request, grpc::Metadata headers = null, global::System.DateTime? deadline = null, global::System.Threading.CancellationToken cancellationToken = default(global::System.Threading.CancellationToken))
      {
        return ServerRequest(request, new grpc::CallOptions(headers, deadline, cancellationToken));
      }
      public virtual global::Server.protos.ServerResponseObject ServerRequest(global::Server.protos.ServerRequestObject request, grpc::CallOptions options)
      {
        return CallInvoker.BlockingUnaryCall(__Method_ServerRequest, null, options, request);
      }
      public virtual grpc::AsyncUnaryCall<global::Server.protos.ServerResponseObject> ServerRequestAsync(global::Server.protos.ServerRequestObject request, grpc::Metadata headers = null, global::System.DateTime? deadline = null, global::System.Threading.CancellationToken cancellationToken = default(global::System.Threading.CancellationToken))
      {
        return ServerRequestAsync(request, new grpc::CallOptions(headers, deadline, cancellationToken));
      }
      public virtual grpc::AsyncUnaryCall<global::Server.protos.ServerResponseObject> ServerRequestAsync(global::Server.protos.ServerRequestObject request, grpc::CallOptions options)
      {
        return CallInvoker.AsyncUnaryCall(__Method_ServerRequest, null, options, request);
      }
      public virtual global::Server.protos.PartitionResponseObject PartitionRequest(global::Server.protos.PartitionRequestObject request, grpc::Metadata headers = null, global::System.DateTime? deadline = null, global::System.Threading.CancellationToken cancellationToken = default(global::System.Threading.CancellationToken))
      {
        return PartitionRequest(request, new grpc::CallOptions(headers, deadline, cancellationToken));
      }
      public virtual global::Server.protos.PartitionResponseObject PartitionRequest(global::Server.protos.PartitionRequestObject request, grpc::CallOptions options)
      {
        return CallInvoker.BlockingUnaryCall(__Method_PartitionRequest, null, options, request);
      }
      public virtual grpc::AsyncUnaryCall<global::Server.protos.PartitionResponseObject> PartitionRequestAsync(global::Server.protos.PartitionRequestObject request, grpc::Metadata headers = null, global::System.DateTime? deadline = null, global::System.Threading.CancellationToken cancellationToken = default(global::System.Threading.CancellationToken))
      {
        return PartitionRequestAsync(request, new grpc::CallOptions(headers, deadline, cancellationToken));
      }
      public virtual grpc::AsyncUnaryCall<global::Server.protos.PartitionResponseObject> PartitionRequestAsync(global::Server.protos.PartitionRequestObject request, grpc::CallOptions options)
      {
        return CallInvoker.AsyncUnaryCall(__Method_PartitionRequest, null, options, request);
      }
      public virtual global::Server.protos.StatusResponseObject StatusRequest(global::Server.protos.StatusRequestObject request, grpc::Metadata headers = null, global::System.DateTime? deadline = null, global::System.Threading.CancellationToken cancellationToken = default(global::System.Threading.CancellationToken))
      {
        return StatusRequest(request, new grpc::CallOptions(headers, deadline, cancellationToken));
      }
      public virtual global::Server.protos.StatusResponseObject StatusRequest(global::Server.protos.StatusRequestObject request, grpc::CallOptions options)
      {
        return CallInvoker.BlockingUnaryCall(__Method_StatusRequest, null, options, request);
      }
      public virtual grpc::AsyncUnaryCall<global::Server.protos.StatusResponseObject> StatusRequestAsync(global::Server.protos.StatusRequestObject request, grpc::Metadata headers = null, global::System.DateTime? deadline = null, global::System.Threading.CancellationToken cancellationToken = default(global::System.Threading.CancellationToken))
      {
        return StatusRequestAsync(request, new grpc::CallOptions(headers, deadline, cancellationToken));
      }
      public virtual grpc::AsyncUnaryCall<global::Server.protos.StatusResponseObject> StatusRequestAsync(global::Server.protos.StatusRequestObject request, grpc::CallOptions options)
      {
        return CallInvoker.AsyncUnaryCall(__Method_StatusRequest, null, options, request);
      }
      public virtual global::Server.protos.ClientResponseObject ClientRequest(global::Server.protos.ClientRequestObject request, grpc::Metadata headers = null, global::System.DateTime? deadline = null, global::System.Threading.CancellationToken cancellationToken = default(global::System.Threading.CancellationToken))
      {
        return ClientRequest(request, new grpc::CallOptions(headers, deadline, cancellationToken));
      }
      public virtual global::Server.protos.ClientResponseObject ClientRequest(global::Server.protos.ClientRequestObject request, grpc::CallOptions options)
      {
        return CallInvoker.BlockingUnaryCall(__Method_ClientRequest, null, options, request);
      }
      public virtual grpc::AsyncUnaryCall<global::Server.protos.ClientResponseObject> ClientRequestAsync(global::Server.protos.ClientRequestObject request, grpc::Metadata headers = null, global::System.DateTime? deadline = null, global::System.Threading.CancellationToken cancellationToken = default(global::System.Threading.CancellationToken))
      {
        return ClientRequestAsync(request, new grpc::CallOptions(headers, deadline, cancellationToken));
      }
      public virtual grpc::AsyncUnaryCall<global::Server.protos.ClientResponseObject> ClientRequestAsync(global::Server.protos.ClientRequestObject request, grpc::CallOptions options)
      {
        return CallInvoker.AsyncUnaryCall(__Method_ClientRequest, null, options, request);
      }
      public virtual global::Server.protos.CrashResponseObject CrashRequest(global::Server.protos.CrashRequestObject request, grpc::Metadata headers = null, global::System.DateTime? deadline = null, global::System.Threading.CancellationToken cancellationToken = default(global::System.Threading.CancellationToken))
      {
        return CrashRequest(request, new grpc::CallOptions(headers, deadline, cancellationToken));
      }
      public virtual global::Server.protos.CrashResponseObject CrashRequest(global::Server.protos.CrashRequestObject request, grpc::CallOptions options)
      {
        return CallInvoker.BlockingUnaryCall(__Method_CrashRequest, null, options, request);
      }
      public virtual grpc::AsyncUnaryCall<global::Server.protos.CrashResponseObject> CrashRequestAsync(global::Server.protos.CrashRequestObject request, grpc::Metadata headers = null, global::System.DateTime? deadline = null, global::System.Threading.CancellationToken cancellationToken = default(global::System.Threading.CancellationToken))
      {
        return CrashRequestAsync(request, new grpc::CallOptions(headers, deadline, cancellationToken));
      }
      public virtual grpc::AsyncUnaryCall<global::Server.protos.CrashResponseObject> CrashRequestAsync(global::Server.protos.CrashRequestObject request, grpc::CallOptions options)
      {
        return CallInvoker.AsyncUnaryCall(__Method_CrashRequest, null, options, request);
      }
      public virtual global::Server.protos.FreezeResponseObject FreezeRequest(global::Server.protos.FreezeRequestObject request, grpc::Metadata headers = null, global::System.DateTime? deadline = null, global::System.Threading.CancellationToken cancellationToken = default(global::System.Threading.CancellationToken))
      {
        return FreezeRequest(request, new grpc::CallOptions(headers, deadline, cancellationToken));
      }
      public virtual global::Server.protos.FreezeResponseObject FreezeRequest(global::Server.protos.FreezeRequestObject request, grpc::CallOptions options)
      {
        return CallInvoker.BlockingUnaryCall(__Method_FreezeRequest, null, options, request);
      }
      public virtual grpc::AsyncUnaryCall<global::Server.protos.FreezeResponseObject> FreezeRequestAsync(global::Server.protos.FreezeRequestObject request, grpc::Metadata headers = null, global::System.DateTime? deadline = null, global::System.Threading.CancellationToken cancellationToken = default(global::System.Threading.CancellationToken))
      {
        return FreezeRequestAsync(request, new grpc::CallOptions(headers, deadline, cancellationToken));
      }
      public virtual grpc::AsyncUnaryCall<global::Server.protos.FreezeResponseObject> FreezeRequestAsync(global::Server.protos.FreezeRequestObject request, grpc::CallOptions options)
      {
        return CallInvoker.AsyncUnaryCall(__Method_FreezeRequest, null, options, request);
      }
      public virtual global::Server.protos.UnfreezeResponseObject UnfreezeRequest(global::Server.protos.UnfreezeRequestObject request, grpc::Metadata headers = null, global::System.DateTime? deadline = null, global::System.Threading.CancellationToken cancellationToken = default(global::System.Threading.CancellationToken))
      {
        return UnfreezeRequest(request, new grpc::CallOptions(headers, deadline, cancellationToken));
      }
      public virtual global::Server.protos.UnfreezeResponseObject UnfreezeRequest(global::Server.protos.UnfreezeRequestObject request, grpc::CallOptions options)
      {
        return CallInvoker.BlockingUnaryCall(__Method_UnfreezeRequest, null, options, request);
      }
      public virtual grpc::AsyncUnaryCall<global::Server.protos.UnfreezeResponseObject> UnfreezeRequestAsync(global::Server.protos.UnfreezeRequestObject request, grpc::Metadata headers = null, global::System.DateTime? deadline = null, global::System.Threading.CancellationToken cancellationToken = default(global::System.Threading.CancellationToken))
      {
        return UnfreezeRequestAsync(request, new grpc::CallOptions(headers, deadline, cancellationToken));
      }
      public virtual grpc::AsyncUnaryCall<global::Server.protos.UnfreezeResponseObject> UnfreezeRequestAsync(global::Server.protos.UnfreezeRequestObject request, grpc::CallOptions options)
      {
        return CallInvoker.AsyncUnaryCall(__Method_UnfreezeRequest, null, options, request);
      }
      /// <summary>Creates a new instance of client from given <c>ClientBaseConfiguration</c>.</summary>
      protected override PuppetMasterServicesClient NewInstance(ClientBaseConfiguration configuration)
      {
        return new PuppetMasterServicesClient(configuration);
      }
    }

  }
}
#endregion