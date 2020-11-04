using Grpc.Net.Client;
using Server.protos;
using System;
using System.Net.Http;

namespace Client
{
    class GrpcServer
    {

        // public string Partition_id { get; set; }
        //private string Server_id { get; }
        //private bool IsMasterReplica { get; }
        public string Url { get; }

        public ServerStorageServices.ServerStorageServicesClient Service { get; }


        public GrpcServer( string url)
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            Url = url;
            GrpcChannel channel = GrpcChannel.ForAddress(Url);
            Service = new ServerStorageServices.ServerStorageServicesClient(channel);
        }

        /*public GrpcServer(string partition_id, string url)
        {

            Partition_id = partition_id;
            // Server_id = server_id;
            Url = url;
            GrpcChannel channel = GrpcChannel.ForAddress(Url);
            Service = new ServerStorageServices.ServerStorageServicesClient(channel);
        }*/
    }
}
