using Grpc.Core;
using Server;
using Server.protos;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace PCS
{

    public class PCSServerService : PuppetMasterServices.PuppetMasterServicesBase
    {


        public override Task<ClientResponseObject> ClientRequest(ClientRequestObject request, ServerCallContext context)
        {
            
            Process.Start("..\\..\\..\\..\\Client\\bin\\Debug\\netcoreapp3.1\\Client.exe ", request.Scriptfile + " " + request.ClientUrl + " " + request.Username);
            return Task.FromResult(new ClientResponseObject { Success = "true" });
        }

        public override Task<ServerResponseObject> ServerRequest(ServerRequestObject request, ServerCallContext context)
        {
            String argumentsString = request.ServerId + " " + request.Url + " " + request.MinDelay + " " + request.MaxDelay;
            foreach(PartitionMessage pm in request.Partitions)
            {
                argumentsString += " " + pm.Id + " " + pm.MasterId;
            }
            Process.Start("..\\..\\..\\..\\Server\\bin\\Debug\\netcoreapp3.1\\Server.exe", argumentsString);
            return Task.FromResult(new ServerResponseObject { Success = "true" });
        }
        public override Task<StatusResponseObject> StatusRequest(StatusRequestObject request, ServerCallContext context)
        {
            Console.WriteLine("STH");
            return Task.FromResult(new StatusResponseObject { });
        }
    }
    class Program
    {
        static void Main(string[] args)
        {
            Grpc.Core.Server server = new Grpc.Core.Server
            {
                Services =
                {
                    PuppetMasterServices.BindService(new PCSServerService())
                },
                Ports = { new ServerPort("localhost", 10000, ServerCredentials.Insecure) }
            };

            server.Start();
        }
    }
}
