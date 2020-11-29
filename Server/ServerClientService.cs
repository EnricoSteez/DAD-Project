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

        //----------------------------------

        //--------------------------------------------- READ OBJECT ---------------------------------------------

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


        //TODO check if Monitor.Enter works, i.e. if Local can get the lock to its own attributes (dictionary)
        // even if the service has the lock on the whole Local object

        //--------------------------------------------- WRITE OBJECT ---------------------------------------------

        public override Task<WriteObjectResponse> WriteObject(WriteObjectRequest request,
            ServerCallContext context)
        {
            Console.WriteLine("Client " + context.Host + " wants to write {0}", request.ObjectId);
            int waitTime = new Random().Next(Local.MinDelay, Local.MaxDelay);
            Console.WriteLine("Client served in {0} seconds", waitTime);
            Thread.Sleep(waitTime * 1000);
            return Task.FromResult(WO(request));

        }

        //TODO: choose whether to update here, just after the client's request or in a separate function every tot min

        private WriteObjectResponse WO(WriteObjectRequest request)
        {
            Monitor.Enter(Local);
            if (Local.Storage.ContainsKey(request.PartitionId))
            {
                WriteObjectResponse response = new WriteObjectResponse
                {
                    WriteResult = Local.AddObject(new Resource(request.ObjectId, request.Value), request.PartitionId)
                };

                //AddObject updates the value and does version++ (if the object is already stored)
                // or it adds the value to the storage with version = 1 (if the id is not present yet)

                Monitor.Exit(Local);

                return response;
            }
            else
            {
                Monitor.Exit(Local);
                return new WriteObjectResponse { WriteResult = -1 };
            }
        }


        //--------------------------------------------- LIST SERVER ---------------------------------------------


        public override Task<ListServerResponse> ListServer(ListServerRequest request, ServerCallContext context)
        {
            int waitTime = new Random().Next(Local.MinDelay, Local.MaxDelay);
            Thread.Sleep(waitTime * 1000);
            Console.WriteLine("ListGlobal request:\n" + Local.Storage.Values);
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
                    response.Versions.Add(resource.Version);
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
