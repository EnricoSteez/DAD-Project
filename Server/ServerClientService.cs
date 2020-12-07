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
    /******************************************** SERVICES FOR CLIENTS ********************************************/

    public class ServerClientService : ServerStorageServices.ServerStorageServicesBase
    {


        public Server Local { get; }



        public ServerClientService(Server server)
        {
            Local = server;
        }


        //----------TESTINGS------------------------

        /*private static void OnTimedEvent(Object source, System.Timers.ElapsedEventArgs e)
        {
            Server.Print(Local.Server_id, String.Format("The Elapsed event was raised at {0}", e.SignalTime));
        }*/
        /*public static void delay()
        {
            // Create a timer and set a two second interval.
            System.Timers.Timer aTimer = new System.Timers.Timer(2000);

            // Hook up the Elapsed event for the timer. 
            aTimer.Elapsed += OnTimedEvent;

            aTimer.AutoReset = false;

            aTimer.Start();

        }*/

        //----------------------------------

        //---------------------------REGISTER CLIENT---------------

        public override Task<RegisterResponse> Register(RegisterRequest request, ServerCallContext context)
        {
            Local.AddClient(request.Url);
            RegisterResponse res = new RegisterResponse();
            string newmasters_print = "";
            if (Local.new_masters != null)
            {
                foreach (string partitionId in Local.new_masters.Keys)
                {
                    newmasters_print += partitionId + " ";
                    res.NewMasters.Add(new NewMastersStructure { PartitionId = partitionId, ServerId = Local.new_masters[partitionId] });
                }
            }
            
            Server.Print(Local.Server_id, "Server " + Local.Server_id + " registered client " + request.Url + "and sent partitions " + newmasters_print);
            return Task.FromResult(res);
        }

        //---------------

        //--------------------------------------------- READ OBJECT ---------------------------------------------

        public override Task<ReadObjectResponse> ReadObject(ReadObjectRequest request,
            ServerCallContext context)
        {
            Server.Print(Local.Server_id, String.Format("Client " + context.Host + " wants to READ {0} from Partition {1}",
                request.ObjectId, request.PartitionId));
            int waitTime = new Random().Next(Local.MinDelay, Local.MaxDelay);
            ReadObjectResponse res = RO(request);
            Server.Print(Local.Server_id, String.Format("Client served in {0} seconds", waitTime));
            //Thread.Sleep(waitTime * 1000); //Timer event
            return Task.FromResult(res);

        }

        private ReadObjectResponse RO(ReadObjectRequest request)
        {
            ReadObjectResponse response = new ReadObjectResponse();

            Resource r = Local.RetrieveObject(request.ObjectId, request.PartitionId, request.LastVersion);

            response.Id = r.ObjectId;
            response.Value = r.Value;
            response.Version = r.Version;

            /* RetrieveObjects returns:
             * 
             * (OLDER VERSION, OLDER VERSION, 0) if the version that the replica has stored is older than the client's last known version
             * (N/A, N/A, 0) if the resource is not on that replica
             * (Id, Value, Version) if everything is ok
             */

            return response;
        }




        //--------------------------------------------- WRITE OBJECT ---------------------------------------------

        public override Task<WriteObjectResponse> WriteObject(WriteObjectRequest request,
            ServerCallContext context)
        {
            Server.Print(Local.Server_id, String.Format("Client " + context.Peer + " wants to write {0}", request.ObjectId));
            int waitTime = new Random().Next(Local.MinDelay, Local.MaxDelay);
            Thread.Sleep(waitTime);
            WriteObjectResponse res = WO(request);
            Server.Print(Local.Server_id, String.Format("Client served in {0} seconds", waitTime));
            return Task.FromResult(res);

        }
        //TODO implement open connections and RetrieveServer like in the client Program


        private WriteObjectResponse WO(WriteObjectRequest request)
        {
            WriteObjectResponse response = new WriteObjectResponse();

            Monitor.Enter(Local);
            if (Local.Storage.ContainsKey(request.PartitionId))
            {
                //the response is the version of the added object (calculated locally)
                response.WriteResult = Local.AddObject(new Resource(request.ObjectId, request.Value, -1), request.PartitionId);
                
                //if I put version = -1 it means I'm writing locally, then
                    //AddObject updates the value and does version++ (if the object is already stored)
                    // or it adds the value to the storage with version = 1 (if the id is not present yet)
                //if I put the version it means the update is coming from the master (see ServerServerService.cs)

                Monitor.Exit(Local);
                UpdateResponse l = new UpdateResponse { Ok = -1 };
                int failed = 0;
                int countUpdates = 0;
                List<Task> tasks = new List<Task>();

                foreach (ServerIdentification sampleServer in Local.SystemNodes.Values)
                {
                    if (Local.Server_id != sampleServer.Id && sampleServer.Partitions.Contains(request.PartitionId))
                    {
                        ServerCoordinationServices.ServerCoordinationServicesClient replica =
                            Local.RetrieveServer(sampleServer.Id);

                        countUpdates++;

                        //here the other node will do AddObject but with a version != -1
                        //so before adding it will check whether he has to update or he's already up to date with the master
                        //it should update because this follows a write
                        UpdateValueRequest req = new UpdateValueRequest
                        {
                            PartitionId = request.PartitionId,
                            Id = request.ObjectId,
                            Value = request.Value,
                            Version = response.WriteResult
                        };

                        tasks.Add(Task.Run(() =>
                        {
                            try
                            {   
                                Server.Print(Local.Server_id, "Update request to " + sampleServer.Id + ", waiting for confirmation");
                                l = replica.UpdateValue(req, deadline: DateTime.UtcNow.AddSeconds(8));
                                Server.Print(Local.Server_id, sampleServer.Id + " updated");
                            } //TODO check error model
                            catch (RpcException ex) when
                                (ex.StatusCode == StatusCode.DeadlineExceeded || ex.StatusCode== StatusCode.Unavailable)
                            {
                                Server.Print(Local.Server_id, "Server "+ sampleServer.Id + " unavailable");
                                failed++;
                            }
                        }));
                    }
                }
                if(tasks.Count > 0)
                {
                    Task firstThatUpdates = Task.WhenAny(tasks);

                    //here I wait for the first non failed task that returned true
                    //I stop if all of them fail
                    while (l.Ok == -1 && failed < countUpdates)
                    {
                        firstThatUpdates.Wait();
                    }

                    if (firstThatUpdates.Status == TaskStatus.RanToCompletion && l.Ok > 0)
                        Server.Print(Local.Server_id, "At least one replica updated the value. Returning back to the client");
                    else
                    {
                        Server.Print(Local.Server_id, "All update requests timed out, sending -1 to the client");
                        response.WriteResult = -1;
                    }
                }

                else
                {
                    Server.Print(Local.Server_id, "no replica to update. returning.");
                }
                
            }
            else
            {
                Monitor.Exit(Local);
                response.WriteResult = -1;
            }

            return response;
            
        }


        //--------------------------------------------- LIST SERVER ---------------------------------------------


        public override Task<ListServerResponse> ListServer(ListServerRequest request, ServerCallContext context)
        {
            int waitTime = new Random().Next(Local.MinDelay, Local.MaxDelay);
            Thread.Sleep(waitTime * 1000);
            Server.Print(Local.Server_id, "ListServer request:\n" + Local.Storage.ToString());
            return Task.FromResult(LS(request));
        }

        private ListServerResponse LS(ListServerRequest request)
        {
            ListServerResponse response = new ListServerResponse();
            foreach (Partition p in Local.Storage.Values)
            {
                //if this server is master of a partition, then it's also master of all the resources of the partition
                bool isMaster = p.MasterId.Equals(Local.Server_id);

                foreach (Resource resource in p.Elements.Values)
                {
                    response.Objects.Add(new ListServerResource
                    {
                        Id = resource.ObjectId,
                        Version = resource.Version,
                        IsMasterReplica = isMaster
                    });
                }
            }

            return response;
        }


        //--------------------------------------------- LIST GLOBAL ---------------------------------------------

        public override Task<ListGlobalResponse> ListGlobal(ListGlobalRequest request, ServerCallContext context)
        {
            int waitTime = new Random().Next(Local.MinDelay, Local.MaxDelay);
            Thread.Sleep(waitTime * 1000);
            return Task.FromResult(LG(request));
        }

        private ListGlobalResponse LG(ListGlobalRequest request)
        {
            ListGlobalResponse response = new ListGlobalResponse();
            foreach (Partition p in Local.Storage.Values)
            {
                PartitionIdentification pid = new PartitionIdentification()
                {
                    PartitionId = p.Id
                };

                foreach (Resource r in p.Elements.Values)
                {
                    pid.ObjectIds.Add(r.ObjectId);
                    pid.Versions.Add(r.Version);
                }

                response.Partitions.Add(pid);
            }

            return response;
        }
    }
}
