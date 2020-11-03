using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Http.Features;
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
            Console.WriteLine("Client " + context.Host + " wants to READ {0} from Partition {1}",
                request.ObjectId, request.PartitionId);
            int waitTime = new Random().Next(Local.MinDelay, Local.MaxDelay);
            Console.WriteLine("Client served in {0} seconds", waitTime);
            Thread.Sleep(waitTime * 1000);
            return Task.FromResult(RO(request));

        }

        private ReadObjectResponse RO(ReadObjectRequest request)
        {
            ReadObjectResponse response = new ReadObjectResponse
            {
                Value = Local.RetrieveObject(request.ObjectId, request.PartitionId)
            };

            return response;
        }

        //--------------------WRITE OBJECT--------------------


        public override Task<WriteObjectResponse> WriteObject(WriteObjectRequest request,
            ServerCallContext context)
        {
            Console.WriteLine("Client " + context.Host + " wants to write {0}", request.ObjectId);
            int waitTime = new Random().Next(Local.MinDelay, Local.MaxDelay);
            Console.WriteLine("Client served in {0} seconds", waitTime);
            Thread.Sleep(waitTime * 1000);
            return Task.FromResult(WO(request));

        }

        private WriteObjectResponse WO(WriteObjectRequest request)
        {
            List<Task> tasks = new System.Collections.Generic.List<Task>();
            Task requests = null;
            if (Local.Storage.ContainsKey(request.PartitionId))
            {
                //here I check if the object the clients wants to write is already stored in this partition or not
                //and I do the locking request to all other servers replicating this partition
                //only if necessary
                if (Local.Storage[request.PartitionId].Elements.ContainsKey(request.ObjectId))
                {
                    foreach (string id in Local.SystemNodes.Keys)
                    {
                        ServerIdentification sampleServer = Local.SystemNodes[id];

                        foreach (string part in sampleServer.Partitions)
                        {
                            //look for other replicas (excluding myself)
                            //by scrolling all partitions of every server and finding match with the requested one
                            if (request.PartitionId == part && Local.Server_id != sampleServer.Id)
                            {
                                GrpcChannel channel = GrpcChannel.ForAddress(
                                    sampleServer.Ip.ToString() + ":" + (1000 + int.Parse(id)).ToString());


                                ServerCoordinationServices.ServerCoordinationServicesClient client =
                                    new ServerCoordinationServices.ServerCoordinationServicesClient(channel);

                                LockRequest req = new LockRequest
                                {
                                    ObjectId = request.ObjectId,
                                    PartitionId = request.PartitionId
                                };

                                tasks.Add(Task.Run(() => {
                                    try
                                    {
                                        LockResponse l = client.LockResourceService(req);
                                    }
                                    catch { 
                                    }
                                })) ;
                                    
                            }
                        }
                    }
                    requests = Task.WhenAll(tasks);
                }

                requests.Wait();
                WriteObjectResponse response = new WriteObjectResponse
                {
                    /*
                     * I'm sure I'm the master replica because only the master replica for any resource
                     * is  to receive the write request and serve it
                     * Hence why Local.Server_id in the whoIsMaster field of the new Resource
                    */

                    WriteResult = Local.AddObject(new Resource(request.ObjectId, request.Value), request.PartitionId)
                };


                //SEND UPDATED VALUE TO (AGAIN) OTHER REPLICAS OF THIS PARTITION
                //There's no way to avoid doing the same loop twice because of the confirmations gathering "stall"
                foreach (string id in Local.SystemNodes.Keys)
                {
                    ServerIdentification sampleServer = Local.SystemNodes[id];

                    UpdateValueRequest valueToUpdate = new UpdateValueRequest
                    {
                        Id = request.ObjectId,
                        Value = request.Value,
                        PartitionId = request.PartitionId
                    };

                    foreach (string part in sampleServer.Partitions)
                    {
                        if (request.PartitionId == part && Local.Server_id != sampleServer.Id)
                        {
                            GrpcChannel channel = GrpcChannel.ForAddress(
                                sampleServer.Ip + ":" + (1000 + int.Parse(sampleServer.Id)).ToString());


                            ServerCoordinationServices.ServerCoordinationServicesClient client =
                                new ServerCoordinationServices.ServerCoordinationServicesClient(channel);

                            client.UpdateValue(valueToUpdate);

                            channel.ShutdownAsync();
                        }

                        //TODO WAIT FOR ALL CONFIRMATIONS?  
                    }
                }

                return response;
            }

            return new WriteObjectResponse { WriteResult = -1 };
        }


        public override Task<ListServerResponse> ListServer(ListServerRequest request, ServerCallContext context)
        {
            int waitTime = new Random().Next(Local.MinDelay, Local.MaxDelay);
            Thread.Sleep(waitTime * 1000);
            return Task.FromResult(LS(request));
        }

        private ListServerResponse LS(ListServerRequest request)
        {
            ListServerResponse response = new ListServerResponse();
            foreach(Partition p in Local.Storage.Values)
            {
                //if this server is master of a partition, then it's also master of all the resources of the partition
                bool isMaster = p.MasterId.Equals(Local.Server_id);
                
                foreach (Resource resource in p.Elements.Values)
                {
                    response.StoredObjects.Add(resource.ObjectId);
                    response.IsMasterReplica.Add(isMaster);
                }
            }

            return response;
        }

        public override Task<ListGlobalResponse> ListGlobal(ListGlobalRequest request, ServerCallContext context)
        {
            int waitTime = new Random().Next(Local.MinDelay, Local.MaxDelay);
            Thread.Sleep(waitTime * 1000);
            return Task.FromResult(LG(request));
        }

        private ListGlobalResponse LG(ListGlobalRequest request)
        {
            ListGlobalResponse response = new ListGlobalResponse();
            
            foreach(ServerIdentification s in Local.SystemNodes.Values)
            {
                //TODO make a service among servers to retrieve informations on demand only if it's necessary
                //in our structure, no server has knowledge of the objects stored in other servers
                //all a server knows about other nodes is the ServerIdentification mask
                //which contains Server ID, stored PartitionIDs and IP Address.


                foreach(ServerIdentification server in Local.SystemNodes.Values)
                {
                    GrpcChannel channel = GrpcChannel.ForAddress(
                                server.Ip + ":" + (1000 + int.Parse(server.Id)).ToString());


                    ServerCoordinationServices.ServerCoordinationServicesClient client =
                        new ServerCoordinationServices.ServerCoordinationServicesClient(channel);

                    SendInfoResponse res = client.SendInfo(new SendInfoRequest());

                    response.Partitions.Add(res.Partitions);
                    response.Objects.Add(res.Objects);

                    channel.ShutdownAsync();
                }
            }

            return response;
        }
    }
}
