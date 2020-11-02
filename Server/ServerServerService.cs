using System;
using System.Threading.Tasks;
using Grpc.Core;
using Server.protos;

namespace Server
{
    /*************************************** SERVICES AMONG SERVERS ***************************************/
    /***************************************    SERVER SIDE    ***************************************/

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
            return Task.FromResult(LRS(request));
        }

        private LockResponse LRS(LockRequest request)
        {

            LockResponse response = new LockResponse
            {
                Ok = Local.LockObject(request.ObjectId)
            };

            return response;
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
            //*****  when a new value is updated, it is automatically set to unlocked  *****

            

            UnlockConfirmation result = new UnlockConfirmation
            {
                Ok = Local.UpdateSpecialPermission(new Resource(tuple.Id, tuple.Value))
                //always true
            };

            return result;
        }

    }
}
