using Grpc.Core;
using Server.protos;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    class PuppetMasterServerServices : PuppetMasterServices.PuppetMasterServicesBase
    {
        public override Task<CrashResponseObject> CrashRequest(CrashRequestObject request, ServerCallContext context)
        {
            
            return base.CrashRequest(request, context);
        }
    }
}
