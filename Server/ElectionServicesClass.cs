using Grpc.Core;
using Grpc.Net.Client;
using Server.protos;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
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


        public override Task<AnnounceMasterResponse> AnnounceMaster(AnnounceMasterRequest request, ServerCallContext context)
        {
            

            return Task.FromResult(AM(request));

        }

        private AnnounceMasterResponse AM(AnnounceMasterRequest request)
        {
           
            if (Local.new_masters == null)
            {
                Local.new_masters = new Dictionary<string, string>();
            }
            Monitor.Enter(Local.new_masters);
            if (Local.new_masters.ContainsKey(request.PartitionId))
            {
                Local.new_masters[request.PartitionId] = request.ServerId;
            }
            else
            {
                Local.new_masters.Add(request.PartitionId, request.ServerId);
            }
            Monitor.Exit(Local.new_masters);

            Server.Print(Local.Server_id, "new masters:");

            foreach (string partitionId in Local.new_masters.Keys)
            {
                Server.Print(Local.Server_id, "paritition Id: " + partitionId + " masterId: " + Local.new_masters[partitionId]);
            }

            return new AnnounceMasterResponse { Success = true };

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
                        var reply = Service.AnnounceMaster(new AnnounceMasterRequest { PartitionId = request.PartitionId, ServerId = Local.Server_id }, deadline:DateTime.UtcNow.AddSeconds(8));
                        c.Dispose();
                    }
                    catch(Exception e)
                    {
                        c.Dispose();
                        Server.Print(Local.Server_id, "Client " + url + "unreachable");
                    }
                    

                }));
                
            }


            foreach (ServerIdentification s in Local.SystemNodes.Values)
            {
                allTasks.Add(Task.Run(() =>
                {
                    if(!s.Id.Equals(Local.Server_id))
                    {
                        GrpcChannel c = GrpcChannel.ForAddress(s.Ip);
                        try
                        {
                            ElectionServices.ElectionServicesClient Service = new ElectionServices.ElectionServicesClient(c);
                            var reply = Service.AnnounceMaster(new AnnounceMasterRequest { PartitionId = request.PartitionId, ServerId = Local.Server_id }, deadline: DateTime.UtcNow.AddSeconds(8));
                            c.Dispose();
                        }
                        catch (Exception e)
                        {
                            c.Dispose();
                            Server.Print(Local.Server_id, "Server " + s.Id + "unreachable");
                        }
                    }

                }));
            }
            Task Union = Task.WhenAny(allTasks);
            Union.Wait();

            if (Local.new_masters == null)
            {
                Local.new_masters = new Dictionary<string, string>();
            }
            Monitor.Enter(Local.new_masters);
            if (Local.new_masters.ContainsKey(request.PartitionId))
            {
                Local.new_masters[request.PartitionId] = Local.Server_id;
            }
            else
            {
                Local.new_masters.Add(request.PartitionId, Local.Server_id);
            }
            Monitor.Exit(Local.new_masters);

            ChooseMasterResponse response = new ChooseMasterResponse();
            response.Success = Local.UpdateMaster(request.PartitionId, Local.Server_id);
            return Task.FromResult(response);



        }
    }
}
