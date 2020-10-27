using Grpc.Net.Client;
using Server.protos;

namespace Client
{
    class GrpcServer
    {

        public string Partition_id { get; set; }
        //private string Server_id { get; }
        //private bool IsMasterReplica { get; }
        public string Url { get; }

        public ServerStorageServices.ServerStorageServicesClient Service { get; }


        public GrpcServer( string url)
        {

            // Server_id = server_id;
            Url = url;
            GrpcChannel channel = GrpcChannel.ForAddress(Url);
            Service = new ServerStorageServices.ServerStorageServicesClient(channel);
        }

        public GrpcServer(string partition_id, /*string server_id,*/ string url)
        {

            Partition_id = partition_id;
            // Server_id = server_id;
            Url = url;
            GrpcChannel channel = GrpcChannel.ForAddress(Url);
            Service = new ServerStorageServices.ServerStorageServicesClient(channel);
        }
    }
}
