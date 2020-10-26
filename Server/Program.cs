using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Grpc.Core;
using Server.protos;

namespace Server
{
    public class Server
    {
        private Dictionary<int, string> Storage { get; }
        public int Partition_id { get; }
        public int Server_id { get; }
        public bool IsMasterReplica { get; }



        public Server(int partition_id, int server_id, bool isMasterReplica)
        {
            Partition_id = partition_id;
            Server_id = server_id;
            IsMasterReplica = isMasterReplica;
            //empty storage at startup
            Storage = new Dictionary<int, string>();
        }


        public int addObject(int id, string value)
        {
            int result = 0;
            if (Storage.ContainsKey(id))
                return 1;
            else
            {
                Storage.Add(id, value);
            }

            return result;
        }

        public string retrieveObject(int id)
        {
            return Storage.GetValueOrDefault(id);
        }
    }

    public class ServerService : ServerStorageServices.ServerStorageServicesBase
    {
        public Server Server { get; }

        public ServerService(Server server)
        {
            Server = server;
        }

        //------------------------------------READ OBJECT-------------------------------------------

        public override Task<ReadObjectResponse> ReadObject(ReadObjectRequest request,
            ServerCallContext context)
        {
            Console.WriteLine("Client " + context.Host + "Asked for something");
            return Task.FromResult(RO(request));

        }

        private ReadObjectResponse RO(ReadObjectRequest request)
        {
            Console.WriteLine("He wants object with ID: " + request.ObjectId +
                " from partition " + request.PartitionId);

            ReadObjectResponse response = new ReadObjectResponse
            {
                Value = Server.retrieveObject(request.ObjectId)
            };

            return response;
        }

        //------------------------------------WRITE OBJECT------------------------------------------


        public override Task<WriteObjectResponse> WriteObject(WriteObjectRequest request,
            ServerCallContext context)
        {
            return Task.FromResult(WO(request));
        }

        private WriteObjectResponse WO(WriteObjectRequest request)
        {
            WriteObjectResponse response = new WriteObjectResponse
            {
                WriteResult = Server.addObject(request.ObjectId, request.Value)
            };

            return response;
        }
    }

    class Program
    { 

        static void Main(string[] args)
        {
            //decide parameters HERE IN THE MAIN BEFORE CREATING
            Server local = new Server(1,1,true);
            

            Grpc.Core.Server server = new Grpc.Core.Server {
                Services = { ServerStorageServices.BindService(new ServerService(local)) },
                Ports = {new ServerPort("localhost", 1000+local.Partition_id, ServerCredentials.Insecure)}
            };
            server.Start();
            

            Console.WriteLine("Hello World!");
        }


    }
}
