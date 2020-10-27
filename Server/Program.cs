using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Grpc.Core;
using Server.protos;

namespace Server
{
    public class Server
    {
        private Dictionary<string, string> Storage { get; }
        public string Partition_id { get; }
        public string Server_id { get; }
        public bool IsMasterReplica { get; }
        public  IPAddress Ip { get; }

        public Server() //dummy implementation for debugging with just 1 server at localhost
        {
            Partition_id = "1";
            Server_id = "1";
            IsMasterReplica = true;
            Ip = IPAddress.Parse("127.0.0.1");
            //empty storage at startup
            Storage = new Dictionary<string, string>();
        }

        public Server(string partition_id, string server_id, string ip, bool isMasterReplica)
        {
            Partition_id = partition_id;
            Server_id = server_id;
            IsMasterReplica = isMasterReplica;
            Ip = IPAddress.Parse(ip);
            //empty storage at startup
            Storage = new Dictionary<string, string>();
        }


        public int AddObject(string id, string value)
        {
            int result = 0;
            if (Storage.ContainsKey(id))
            {
                Storage[id] = value;
            }
            else
            {
                Storage.Add(id, value);
            }

            return result;
        }

        public string RetrieveObject(string id)
        {
            return Storage.GetValueOrDefault(id);
        }

    }

    /*************************************** SERVICES FOR CLIENTS ***************************************/

    public class ServerClientService : ServerStorageServices.ServerStorageServicesBase
    {
        public Server Local { get; }

        public ServerClientService(Server server)
        {
            Local = server;
        }

        //--------------------READ OBJECT--------------------

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
                Value = Local.RetrieveObject(request.ObjectId)
            };

            return response;
        }

        //--------------------WRITE OBJECT--------------------


        public override Task<WriteObjectResponse> WriteObject(WriteObjectRequest request,
            ServerCallContext context)
        {
            //the client takes care of connecting to the master server of the correct partition before writing
            //No reason to check here
            return Task.FromResult(WO(request));

        }

        private WriteObjectResponse WO(WriteObjectRequest request)
        {
            //TODO: SEND LOCK REQUEST TO OTHER REPLICAS OF THIS PARTITION AND WAIT FOR RESPONSE


            WriteObjectResponse response = new WriteObjectResponse
            {
                WriteResult = Local.AddObject(request.ObjectId, request.Value)
            };


            //TODO: SEND UPDATED VALUE TO OTHER REPLICAS OF THIS PARTITION AND WAIT FOR RESPONSE

            return response;
        }
    }

    /*************************************** SERVICES AMONG SERVERS ***************************************/

    /***************************************    SERVER SIDE    ***************************************/

    public class ServerServerService : ServerCoordinationServices.ServerCoordinationServicesBase
    {
        public Server Local { get; }

        public ServerServerService(Server local)
        {
            Local = local;
        }

        //TODO
        //-------------------- SERVER SIDE LOCK OWN RESOURCE AND SEND CONFIRMATION-------------------- >> ??? ASK

        public override Task<LockResponse> LockResourceService(LockRequest request, ServerCallContext context)
        {
            return Task.FromResult(LRS(request));
        }

        private LockResponse LRS(LockRequest request)
        {
            throw new NotImplementedException();
            
            /*
             * lock local resource in the dictionary
             * send lock confirmation
             */
        }


        //-------------------- SERVER SIDE UPDATE OWN VALUE AND CONFIRM TO THE MASTER -------------------- >> ??? ASK

        public override Task<UnlockConfirmation> UpdateValue(NewValue tuple, ServerCallContext context)
        {
            return Task.FromResult(UV(tuple));
        }

        private UnlockConfirmation UV(NewValue tuple)
        {
            //here I'm sure I'm in the correct partition because this service is just for the master server
            //The master server of this partition is the only one that will ask for this
            //So I can just write in the dictionary without bothering 
            Local.AddObject(tuple.Id, tuple.Value);

            UnlockConfirmation result = new UnlockConfirmation
            {
                Ok = 1
            };

            return result;
        }
    }




    class Program
    { 

        static void Main(string[] args)
        {
            //decide parameters HERE IN THE MAIN BEFORE CREATING
            Server local = new Server();
            

            Grpc.Core.Server server = new Grpc.Core.Server {
                Services =
                {
                    ServerStorageServices.BindService(new ServerClientService(local)),
                    ServerCoordinationServices.BindService(new ServerServerService(local))
                },
                
                Ports = {new ServerPort("localhost", 1000+int.Parse(local.Partition_id), ServerCredentials.Insecure)}
            };
            server.Start();

            /*and then in the writeObject implementation 
             * if I'm master replica
             * I should perform the lock requests to all other servers of my partition
             * before writing locally 
             * and then send them the updated value
             */

            Console.WriteLine("Hello World!");
        }


    }
}
