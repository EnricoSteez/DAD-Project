using Grpc.Core;
using Server;
using Server.protos;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace PCS
{

    public class PCSServerService : PuppetMasterServices.PuppetMasterServicesBase
    {
        private Dictionary<string, Process> servers;


        public PCSServerService()
        {
            servers = new Dictionary<string, Process>();
        }

        public override Task<ClientResponseObject> ClientRequest(ClientRequestObject request, ServerCallContext context)
        {
            
            Process.Start("..\\..\\..\\..\\Client\\bin\\Debug\\netcoreapp3.1\\Client.exe ", request.Scriptfile + " " + request.ClientUrl + " " + request.Username);
            return Task.FromResult(new ClientResponseObject { Succes = true });
        }

        public override Task<ServerResponseObject> ServerRequest(ServerRequestObject request, ServerCallContext context)
        {
            Console.WriteLine("Going to create a Server");
            String argumentsString = request.ServerId + " " + request.Url + " " + request.MinDelay + " " + request.MaxDelay;
            foreach(PartitionMessage pm in request.Partitions)
            {
                argumentsString += " " + pm.Id + " " + pm.MasterId;
            }
            Process server = Process.Start("..\\..\\..\\..\\Server\\bin\\Debug\\netcoreapp3.1\\Server.exe", argumentsString);
            servers.Add(request.ServerId, server);
            return Task.FromResult(new ServerResponseObject { Success = true });
        }
        public override Task<CrashResponseObject> CrashRequest(CrashRequestObject request, ServerCallContext context)
        {
            Process server;
            if(servers.TryGetValue(request.ServerId, out server))
            {
                server.Kill();
                servers.Remove(request.ServerId);
                return Task.FromResult(new CrashResponseObject { Success = true });
            }
            return Task.FromResult(new CrashResponseObject { Success = false });
        }
    }
    class Program
    {
        static void Main(string[] args)
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            Grpc.Core.Server server = new Grpc.Core.Server
            {
                Services =
                {
                    PuppetMasterServices.BindService(new PCSServerService())
                },
                Ports = { new ServerPort("127.0.0.1", 10000, ServerCredentials.Insecure) }
            };


            try
            {
                server.Start();
                Console.WriteLine("PCS is Running Running");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            Console.ReadLine();
        }
    }
}
