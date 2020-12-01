using Grpc.Core;
using Server.protos;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Client
{
    class ElectionServicesClass : ElectionServices.ElectionServicesBase
    {


        public override Task<AnnounceMasterResponse> AnnounceMaster(AnnounceMasterRequest request,
            ServerCallContext context)
        {

            string newMasterId = request.ServerId;
            string partitionId = request.PartitionId;


            Monitor.Enter(Program.partitions[partitionId]);
            string oldMaster = Program.partitions[partitionId][0];
            Program.partitions[partitionId].RemoveAt(0);
            Program.partitions[partitionId].Insert(0, newMasterId);
            Program.partitions[partitionId].Add(oldMaster);
            Monitor.Exit(Program.partitions[partitionId]);


            return Task.FromResult(new AnnounceMasterResponse { Success = true });

        }



    }
}
