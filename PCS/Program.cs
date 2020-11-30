using Grpc.Core;
using Grpc.Net.Client;
using Server;
using Server.protos;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

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
            Dictionary<string, List<string>> partitions = new Dictionary<string, List<string>>(); //key - partitionId object - list of servers with that partition
            Dictionary<string, string> servers = new Dictionary<string, string>(); //key - serverId value - server url
            string arguments = request.Scriptfile + " " + request.ClientUrl + " " + request.Username + " partitions.binary servers.binary";

            foreach(ServerDetails sd in request.EveryServer)
            {
                servers.Add(sd.Id, sd.Url);
            }
            foreach (PartitionDetails pd in request.Everypartition)
            {
                List<string> aux = new List<string>();                
                if(pd.MasterId != null && pd.MasterId.Length > 0)
                {
                    aux.Add(pd.MasterId);
                }
                foreach(string part in pd.Replicas)
                {
                    aux.Add(part);
                }
                partitions.Add(pd.Id, aux);
            }

            BinaryFormatter bf = new BinaryFormatter();
            try
            {
                FileStream partitionsout = new FileStream("partitions.binary", FileMode.Create, FileAccess.Write, FileShare.None);
                FileStream serversout = new FileStream("servers.binary", FileMode.Create, FileAccess.Write, FileShare.None);

                using (partitionsout)
                {
                    bf.Serialize(partitionsout, partitions);
                    partitionsout.Close();
                }


                using (serversout)
                {
                    bf.Serialize(serversout, servers);
                    serversout.Close();
                }
            }
            catch(Exception e)
            {
                Console.WriteLine("FODA-SE" + e);
            }

            Process.Start("..\\..\\..\\..\\Client\\bin\\Debug\\netcoreapp3.1\\Client.exe ", arguments);
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
