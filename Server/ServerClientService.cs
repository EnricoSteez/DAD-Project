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


        private static void OnTimedEvent(Object source, System.Timers.ElapsedEventArgs e)
        {
            Console.WriteLine("The Elapsed event was raised at {0}", e.SignalTime);
        }
        public static void delay()
        {
            // Create a timer and set a two second interval.
            System.Timers.Timer aTimer = new System.Timers.Timer(2000);

            // Hook up the Elapsed event for the timer. 
            aTimer.Elapsed += OnTimedEvent;

            aTimer.AutoReset = false;

            aTimer.Start();

        }

        //--------------------READ OBJECT--------------------

        public override Task<ReadObjectResponse> ReadObject(ReadObjectRequest request,
            ServerCallContext context)
        {
            Console.WriteLine("Client " + context.Host + " wants to READ {0} from Partition {1}",
                request.ObjectId, request.PartitionId);
            int waitTime = new Random().Next(Local.MinDelay, Local.MaxDelay);
            Console.WriteLine("Client served in {0} seconds", waitTime);
            //Thread.Sleep(waitTime * 1000); //Timer event
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
            //Thread.Sleep(waitTime * 1000);
            return Task.FromResult(WO(request));

        }

        private WriteObjectResponse WO(WriteObjectRequest request)
        {
            List<Task> tasks = new System.Collections.Generic.List<Task>();
            Task requests = null;
            int failed = 0;
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
                                    "http://" + sampleServer.Ip.ToString() + ":" + (1000 + int.Parse(id)).ToString());


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
                                        LockResponse l = client.LockResourceService(req, deadline: DateTime.UtcNow.AddSeconds(5));
                                    }
                                    catch (RpcException ex) when (ex.StatusCode == StatusCode.DeadlineExceeded)
                                    {
                                        Interlocked.Increment(ref failed);
                                        Console.WriteLine("Timeout.");
                                    }
                                })) ;
                                    
                            }
                        }
                    }
                    requests = Task.WhenAll(tasks);
                    requests.Wait();
                    if (requests.Status == TaskStatus.RanToCompletion)
                        Console.WriteLine("All lock requests succeeded.");
                    else if (requests.Status == TaskStatus.Faulted)
                        Console.WriteLine("{0} lock requests timed out", failed);
                }




                tasks.Clear();
                Console.WriteLine("Going to Write");
                requests = null;
                failed = 0;

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
                            Console.WriteLine("found a server with this partition");
                            GrpcChannel channel = null;
                            try
                            {
                                channel = GrpcChannel.ForAddress(
                                "http://" + sampleServer.Ip + ":" + (1000 + int.Parse(sampleServer.Id)).ToString());
                                Console.WriteLine("created the channel");
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine("failed to create channel: " + e.Message);
                            }
                            

                            ServerCoordinationServices.ServerCoordinationServicesClient client =
                                new ServerCoordinationServices.ServerCoordinationServicesClient(channel);
                            tasks.Add(Task.Run(() => {
                                try
                                {
                                    UnlockConfirmation l = client.UpdateValue(valueToUpdate, deadline: DateTime.UtcNow.AddSeconds(5));
                                    channel.ShutdownAsync();

                                }
                                catch (RpcException ex) when (ex.StatusCode == StatusCode.DeadlineExceeded)
                                {
                                    Interlocked.Increment(ref failed);
                                    Console.WriteLine("Timeout.");
                                }
                            }));

                        }   
                    }

                }
                Console.WriteLine("waiting for update confirmations");
                requests = Task.WhenAll(tasks);

                requests.Wait();

                if (requests.Status == TaskStatus.RanToCompletion)
                    Console.WriteLine("All unlock requests succeeded.");
                else if (requests.Status == TaskStatus.Faulted)
                    Console.WriteLine("{0} unlock requests timed out", failed);

                return response;
            }

            return new WriteObjectResponse { WriteResult = -1 };
        }


        public override Task<ListServerResponse> ListServer(ListServerRequest request, ServerCallContext context)
        {
            int waitTime = new Random().Next(Local.MinDelay, Local.MaxDelay);
            Thread.Sleep(waitTime * 1000);
            Console.WriteLine(Local.Storage.Values);
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
            List<Task> tasks = new System.Collections.Generic.List<Task>();
            int failed = 0;

            Console.WriteLine("listGlobal");
            ListGlobalResponse response = new ListGlobalResponse();

            foreach(ServerIdentification server in Local.SystemNodes.Values)
            {
                GrpcChannel channel = GrpcChannel.ForAddress(
                            "http://" + server.Ip + ":" + (1000 + int.Parse(server.Id)).ToString());


                ServerCoordinationServices.ServerCoordinationServicesClient client =
                    new ServerCoordinationServices.ServerCoordinationServicesClient(channel);

                SendInfoResponse res = null;


                tasks.Add(Task.Run(() => {
                    try
                    {
                        SendInfoResponse res = client.SendInfo(new SendInfoRequest(), deadline: DateTime.UtcNow.AddSeconds(5));
                        Console.WriteLine("Conected to server {0}", server.Id);
                    }
                    catch (RpcException ex) when (ex.StatusCode == StatusCode.DeadlineExceeded)
                    {
                        Interlocked.Increment(ref failed);
                        Console.WriteLine("Timeout.");
                    }
                    catch(Exception e)
                    {
                        Interlocked.Increment(ref failed);
                        Console.WriteLine("Error: " + e.StackTrace);
                    }
                }));

                Task union = Task.WhenAll(tasks);

                union.Wait();

                if (failed > 0)
                {
                    Console.WriteLine("Failed: " + failed);
                }

                /* this is super dumb but using the same message (PartitionIdentification)
                    * in two different methods that reside in different .proto files
                    * creates a lot of conflicts
                    * initializing the same message in both files create redeclaration
                    * while initializing it only in 1 file leads to "undefined message"
                    * we'll fix this after...
                    */

                foreach (PartitionID pid in res.Partitions)
                {
                    bool toInsert = true;
                    foreach (PartitionIdentification partitionIdentification in response.Partitions)
                    {
                        if (pid.PartitionId.Equals(partitionIdentification.PartitionId))
                        {
                            toInsert = false;
                        }
                    }
                    if (toInsert)
                    {
                        PartitionIdentification p = new PartitionIdentification
                        {
                            PartitionId = pid.PartitionId
                        };


                        p.ObjectIds.Add(pid.ObjectIds);

                        response.Partitions.Add(p);
                        
                    }
                }

                channel.ShutdownAsync();
                    
            }

            return response;
        }
    }
}
