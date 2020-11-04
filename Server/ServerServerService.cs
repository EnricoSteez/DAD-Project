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

        //-------------------- SERVER SIDE LOCK OWN RESOURCE AND SEND CONFIRMATION-------------------- >> ??? ASK


        public override Task<LockResponse> LockResourceService(LockRequest request, ServerCallContext context)
        {
            Console.WriteLine("LockResourceService Received");
            return Task.FromResult(LRS(request));
        }

        private LockResponse LRS(LockRequest request)
        {

            LockResponse response = new LockResponse
            {
                Ok = Local.LockObject(request.ObjectId, request.PartitionId)
            };

            return response;
        }



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
                Ok = Local.UpdateSpecialPermission(new Resource(tuple.Id, tuple.Value), tuple.PartitionId)
                //always true
            };

            return result;
        }





        //-------------------- SERVER SIDE SEND INFO ABOUT PARTITION AND RESOURCES --------------------
        public override Task<SendInfoResponse> SendInfo(SendInfoRequest request, ServerCallContext context)
        {
            return Task.FromResult(SI(request));
        }

        private SendInfoResponse SI(SendInfoRequest request)
        {
            SendInfoResponse response = new SendInfoResponse();

            foreach(Partition p in Local.Storage.Values)
            {
                response.Partitions.Add(p.Id);
                foreach(Resource r in p.Elements.Values)
                {
                    response.Objects.Add(r.ObjectId);
                }
            }

            return response;
        }
    }
}
