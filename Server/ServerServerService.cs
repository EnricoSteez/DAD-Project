using System;
using System.Threading.Tasks;
using Grpc.Core;
using Server.protos;

namespace Server
{
    /*************************************** SERVICES AMONG SERVERS ***************************************/
    /***************************************    (SERVER SIDE)    ***************************************/

    public class ServerServerService : ServerCoordinationServices.ServerCoordinationServicesBase
    {
        public Server Local { get; }

        public ServerServerService(Server local)
        {
            Local = local;
        }


        //service for the Partition Master. Gets executed by all non-master replicas
        //-------------------- SERVER SIDE UPDATE OWN VALUE AND CONFIRM TO THE MASTER --------------------


        public override Task<UnlockConfirmation> UpdateValue(UpdateValueRequest tuple, ServerCallContext context)
        {
            Console.WriteLine("Update Value Request Received");
            return Task.FromResult(UV(tuple));
        }

        private UnlockConfirmation UV(UpdateValueRequest tuple)
        {
            //here I'm sure I'm in the correct partition because this service is just for the master server
            //The master server of this partition is the only one that will ask for this
            //So I can just write in the dictionary without bothering
            //*****  when a new value is updated through this function, it is automatically set to unlocked  *****

            UnlockConfirmation result = new UnlockConfirmation
            {
                Ok = Local.UpdateSpecialPermission(new Resource(tuple.Id, tuple.Value, tuple.Version), tuple.PartitionId)
                //always true
            };

            return result;
        }

    }
}



//ROUTINE TO UPDATE SYNCHRONOUSLY ALL REPLICAS

/*
 * List<Task> tasks = new System.Collections.Generic.List<Task>();
 * Task requests = null;
   int failed = 0;

 * foreach (string id in Local.SystemNodes.Keys)
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

        //SEND UPDATED VALUE TO (AGAIN) OTHER REPLICAS OF THIS PARTITION
            failed = 0;
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
 */