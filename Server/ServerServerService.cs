using System;
using System.Threading;
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


        public override Task<UpdateResponse> UpdateValue(UpdateValueRequest tuple, ServerCallContext context)
        {
            Server.Print(Local.Server_id, "Update Value Request Received");
            return Task.FromResult(UV(tuple));
        }

        private UpdateResponse UV(UpdateValueRequest tuple)
        {
            //Monitor.Enter(Local);
            Resource newValue = new Resource(tuple.Id, tuple.Value, tuple.Version);

            //Here I do AddObject with Version != -1
            //Which means it's an Update from the master and the version must be copied
            Server.Print(Local.Server_id, "will update");
            UpdateResponse result = new UpdateResponse
            {
                Ok = Local.AddObject(newValue, tuple.PartitionId)
            };

            //Monitor.Exit(Local);
            Server.Print(Local.Server_id, "updated " + tuple.Id);
            return result;
        }
    }
}
