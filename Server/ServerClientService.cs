using System;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;
using Server.protos;

namespace Server
{
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

        //TODO implement waiting mechanism until resource "Lock" flag is false
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
            //Send lock request for the target object to all other nodes in the pool part of this own server's partition

            foreach (string id in Local.SystemNodes.Keys)
            {
                Local.SystemNodes.TryGetValue(id, out ServerIdentification sampleServer);

                if (Local.Partition_id == sampleServer.Partition)
                {
                    GrpcChannel channel = GrpcChannel.ForAddress(
                        sampleServer.Ip.ToString() + ":" + (1000 + int.Parse(id)).ToString());


                    ServerCoordinationServices.ServerCoordinationServicesClient client =
                        new ServerCoordinationServices.ServerCoordinationServicesClient(channel);

                    LockRequest req = new LockRequest
                    {
                        ObjectId = request.ObjectId
                    };


                    client.LockResourceService(req);
                }

                //TODO WAIT FOR ALL RESPONSES?

            }

            WriteObjectResponse response = new WriteObjectResponse
            {
                WriteResult = Local.AddObject(request.ObjectId, new Resource(request.ObjectId, request.Value))
            };


            //SEND UPDATED VALUE TO (AGAIN) OTHER REPLICAS OF THIS PARTITION
            //There's no way to avoid doing the same loop twice because of the confirmations gathering "stall"
            foreach (string id in Local.SystemNodes.Keys)
            {
                Local.SystemNodes.TryGetValue(id, out ServerIdentification sampleServer);

                if (Local.Partition_id == sampleServer.Partition)
                {
                    GrpcChannel channel = GrpcChannel.ForAddress(
                        sampleServer.Ip.ToString() + ":" + (1000 + int.Parse(id)).ToString());


                    ServerCoordinationServices.ServerCoordinationServicesClient client =
                        new ServerCoordinationServices.ServerCoordinationServicesClient(channel);

                    NewValue newValue = new NewValue
                    {
                        Id = request.ObjectId,
                        Value = request.Value
                    };


                    client.UpdateValue(newValue);

                    
                }

                //TODO WAIT FOR ALL CONFIRMATIONS?
            }

            return response;
        }
    }
}
