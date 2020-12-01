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
        private static Dictionary<string, Process> servers;
        private static Dictionary<string, Server.ServerIdentification> serversDetails;
        private static List<ServerRequestObject> serverRequests;


        public PCSServerService()
        {
            servers = new Dictionary<string, Process>();
            serverRequests = new List<ServerRequestObject>();
            serversDetails = new Dictionary<string, Server.ServerIdentification>();
        }

        public static void Print(string s)
        {
            Console.BackgroundColor = ConsoleColor.Blue;
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("PCS:   " + s);
        }

        public override Task<ClientResponseObject> ClientRequest(ClientRequestObject request, ServerCallContext context)
        {
            CreateServers();
            Thread.Sleep(2000);
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
                Print(e.ToString());
            }

            Process.Start("..\\..\\..\\..\\Client\\bin\\Debug\\netcoreapp3.1\\Client.exe ", arguments);
            return Task.FromResult(new ClientResponseObject { Succes = true });
        }

        public override Task<ServerResponseObject> ServerRequest(ServerRequestObject request, ServerCallContext context)
        {
            Print("Going to store Server");
            serverRequests.Add(request);
            
            return Task.FromResult(new ServerResponseObject { Success = true });
        }

        private static void CreateServers()
        {
            if(serverRequests.Count == 0 )
            {
                return;
            }

            

            foreach(ServerRequestObject request in serverRequests)
            {
                List<string> serverspartitions = new List<string>();
                foreach (PartitionDetails pd in request.Partitions)
                {
                    serverspartitions.Add(pd.Id);
                }
                serversDetails.Add(request.ServerId, new ServerIdentification(request.ServerId, serverspartitions, request.Url));



            }
            BinaryFormatter bf = new BinaryFormatter();



            foreach (string sid in serversDetails.Keys)
            {
                try
                {
                    FileStream serversout = new FileStream("servers_" + sid + ".binary", FileMode.Create, FileAccess.Write, FileShare.None);

                    using (serversout)
                    {
                        bf.Serialize(serversout, serversDetails);
                        serversout.Close();
                    }
                }
                catch (Exception e)
                {
                    Print(e.ToString());
                }
            }
            
            




            foreach (ServerRequestObject request in serverRequests)
            {
                Dictionary<string, List<string>> partitions = new Dictionary<string, List<string>>(); //key - partitionId object - list of servers with that partition
                foreach (PartitionDetails pd in request.Partitions)
                {
                    List<string> aux = new List<string>();
                    if (pd.MasterId != null && pd.MasterId.Length > 0)
                    {
                        aux.Add(pd.MasterId);
                    }
                    foreach (string part in pd.Replicas)
                    {
                        aux.Add(part);
                    }
                    partitions.Add(pd.Id, aux);
                }
                try
                {
                    FileStream partitionsout = new FileStream("partitions_" + request.ServerId + ".binary", FileMode.Create, FileAccess.Write, FileShare.None);

                    using (partitionsout)
                    {
                        bf.Serialize(partitionsout, partitions);
                        partitionsout.Close();
                    }
                }
                catch (Exception e)
                {
                    Print(e.ToString());
                }

                Print("will launch server " + request.ServerId); 

                string argumentsString = request.ServerId + " " + request.Url + " " + request.MinDelay + " " + request.MaxDelay + " servers_" + request.ServerId + ".binary partitions_" + request.ServerId + ".binary";
                Process server = Process.Start("..\\..\\..\\..\\Server\\bin\\Debug\\netcoreapp3.1\\Server.exe", argumentsString);
                servers.Add(request.ServerId, server);
            }

            serversDetails.Clear();
        }
        public override Task<CrashResponseObject> CrashRequest(CrashRequestObject request, ServerCallContext context)
        {
            CreateServers();
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
                PCSServerService.Print("PCS is Running Running");
            }
            catch (Exception e)
            {
                PCSServerService.Print(e.Message);
            }
            Console.ReadLine();
        }
    }
}
