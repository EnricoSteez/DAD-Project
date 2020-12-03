using Grpc.Core;
using Grpc.Net.Client;
using Server.protos;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    class ElectionServicesClass : ElectionServices.ElectionServicesBase
    {

        public Server Local { get; }



        public ElectionServicesClass(Server server)
        {
            Local = server;
        }
        public override Task<ChooseMasterResponse> ChooseMaster(ChooseMasterRequest request,
            ServerCallContext context)
        {

            Server.Print(Local.Server_id, "I am being elected");

            List<Task> allTasks = new List<Task>();

            foreach (string url in Local.clients)
            {
                allTasks.Add(Task.Run(() =>
                {
                    GrpcChannel c = GrpcChannel.ForAddress(url);
                    try
                    {
                        ElectionServices.ElectionServicesClient Service = new ElectionServices.ElectionServicesClient(c);
                        var reply = Service.AnnounceMaster(new AnnounceMasterRequest { PartitionId = request.PartitionId, ServerId = Local.Server_id }, deadline:DateTime.UtcNow.AddSeconds(5));
                        c.Dispose();
                    }
                    catch(Exception e)
                    {
                        c.Dispose();
                        Server.Print(Local.Server_id, "Client " + url + "unreachable");
                    }
                    

                }));
                
            }
            Task Union = Task.WhenAny(allTasks);
            Union.Wait();
            ChooseMasterResponse response = new ChooseMasterResponse();
            response.Success = Local.UpdateMaster(request.PartitionId, Local.Server_id);
            return Task.FromResult(response);



        }
    }
}
