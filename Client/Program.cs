using Grpc.Core;
using Grpc.Net.Client;
using Server.protos;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks.Dataflow;


namespace Client
{
     

    class Program
    {
        private static int inc = 1;
        private static Dictionary<string, GrpcServer> servers = new Dictionary<string, GrpcServer>();

        private static Dictionary<string, string> masters = new Dictionary<string, string>(); //key - partitionId object - serverId
        
        private static Dictionary<string, List<string>> partitions =  new Dictionary<string, List<string>>(); //key - partitionId object - list of servers with that partition
        private static ServerStorageServices.ServerStorageServicesClient RetrieveServer(string serverId)
        {
            GrpcServer res = null;
            if (servers.TryGetValue(serverId, out res))
            {
                return res.Service;
            }
            res = new GrpcServer("http://localhost:" + (1000 + inc++));
            servers.Add(serverId, res);
            return res.Service;
        }


        private static List<string> TransformCommands(List<string> loopCommands, int reps)
        {
            
            List<string> commands = new List<string>();
            for(int i = 1; i <= reps; i++)
            {
                foreach (string s in loopCommands)
                {
                    commands.Add(Regex.Replace(s, @"\$i", i.ToString(), RegexOptions.None));
                }
            }
            return commands;
        }

        private static string GetElement(List<string> commands)
        {
            if(commands.Count == 0)
            {
                return null;
            }
            string res = commands[0];
            commands.RemoveAt(0);
            return res;
        }


        static void Main(string[] args)
        {
            servers.Add("1", new GrpcServer("http://127.0.0.1:1001"));
            servers.Add("2", new GrpcServer("http://127.0.0.1:1002"));
            servers.Add("3", new GrpcServer("http://127.0.0.1:1003"));
            masters.Add("p1", "1");
            masters.Add("p2", "2");
            partitions.Add("p1", new List<string>(new string[] { "1", "2", "3" }));
            partitions.Add("p2", new List<string>(new string[] { "1", "2", "3" }));
            string fileName = @"../../../test.txt";
            if (args.Length == 1)
            {
                fileName = args[0];
            }
            int counter = 0;
            string line;

            List<string> loopCommands = new List<string>();
            List<string> commands = new List<string>();

            // Read the file and display it line by line.  
            System.IO.StreamReader file = new System.IO.StreamReader(fileName);
            while ((line = GetElement(commands)) != null || (line = file.ReadLine()) != null)
            {
                string[] words = line.Split(' ', 4);
                string objectId;
                string partitionId;
                string serverId;
                int rep = 1;
                switch (words[0]) {
                    case "read":
                        
                        if (words.Length == 4 )
                        {
                            
                            partitionId = words[1];
                            objectId = words[2];
                            serverId = words[3];
                            if(serverId == "-1")
                            {
                                List<string> partitionservers;
                                if(partitions.TryGetValue(partitionId, out partitionservers))
                                {
                                    if(partitionservers.Count > 0)
                                    {
                                        serverId = partitionservers[0];
                                    }
                                }
                            }
                            ReadObjectResponse reply = null;
                            try
                            {
                                reply = RetrieveServer(serverId).ReadObject(new ReadObjectRequest { PartitionId = partitionId, ObjectId = objectId });
                            }
                            catch (RpcException ex) when (ex.StatusCode == StatusCode.Internal)
                            {
                                Console.WriteLine("server unreachable");
                                List<string> otherservers;
                                if(partitions.TryGetValue(partitionId, out otherservers))
                                {
                                    foreach (string s in otherservers)
                                    {
                                        if(s != serverId)
                                        {
                                            Console.WriteLine("Trying other server...");
                                            try
                                            {
                                                reply = RetrieveServer(s).ReadObject(new ReadObjectRequest { PartitionId = partitionId, ObjectId = objectId });
                                            }
                                            catch(RpcException exx) when (exx.StatusCode == StatusCode.Internal)
                                            {
                                                Console.WriteLine("server unreachable");
                                            }
                                            catch (Exception exx)
                                            {
                                                Console.WriteLine("Unexpected Error!!\n{0}", exx.Message);
                                                break;
                                            }
                                        }
                                    }
                                }
                                
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("Unexpected Error!!\n{0}", ex.Message);
                            }
                            if(reply != null)
                            {
                                Console.WriteLine("Object {0} read: {1}", objectId, reply.Value.ToString());
                            }
                            else
                            {
                                Console.WriteLine("Error getting reply");
                            }
                            
                        }
                        else
                        {
                            Console.WriteLine("Wrong number of args!");
                        }
                        break;
                    case "write":
                        
                        if (words.Length == 4 )
                        {
                            partitionId = words[1];
                            objectId = words[2];
                            string masterServerId;
                            if(masters.TryGetValue(partitionId, out masterServerId))
                            {
                                ServerStorageServices.ServerStorageServicesClient s = RetrieveServer(masterServerId);
                                var reply = s.WriteObject(new WriteObjectRequest { PartitionId = partitionId, ObjectId = objectId, Value = words[3] });
                                
                                Console.WriteLine("Write object {0} result: {1}", objectId, reply.WriteResult);

                            }
                            else
                            {
                                Console.WriteLine("Unkown master server.");
                            }

                        }
                        else
                        {
                            Console.WriteLine("Wrong number of args!");
                        }
                        break;
                    case "listServer":
                        if(words.Length == 2 )
                        {
                            serverId = words[1];
                            try
                            {
                                ListServerResponse reply = RetrieveServer(serverId).ListServer(new ListServerRequest { });
                                Console.WriteLine("Objects:");
                                IEnumerator<string> objects = reply.StoredObjects.GetEnumerator();
                                IEnumerator<bool> isMasterReplica = reply.IsMasterReplica.GetEnumerator();
                                foreach (string s in reply.StoredObjects)
                                {
                                    isMasterReplica.MoveNext();
                                    Console.WriteLine("Object: {0}, is server {1} master replica? {2}", s, serverId,isMasterReplica.Current);
                                }
                            }
                            catch (RpcException exx) when (exx.StatusCode == StatusCode.Internal)
                            {
                                Console.WriteLine("server unreachable");
                            }
                            catch (Exception exx)
                            {
                                Console.WriteLine("Unexpected Error!!\n{0}", exx.Message);
                            }
                        }
                        else
                        {
                            Console.WriteLine("Wrong number of args!");
                        }
                        break;
                    case "listGlobal":
                        if(words.Length == 1 )
                        {
                            foreach(string serv in servers.Keys)
                            {
                                try
                                {
                                    ListGlobalResponse reply = RetrieveServer(serv).ListGlobal(new ListGlobalRequest { });
                                    Console.WriteLine("Partitions:");
                                    foreach (PartitionIdentification p in reply.Partitions)
                                    {
                                        Console.WriteLine("->{0}", p.PartitionId);
                                        Console.WriteLine("\tObjects:");
                                        foreach(string oid in p.ObjectIds)
                                        {
                                            Console.WriteLine("\t\t{0}", oid);
                                        }
                                    }
                                    break;
                                }
                                catch (RpcException exx) when (exx.StatusCode == StatusCode.Internal)
                                {
                                    Console.WriteLine("server unreachable");
                                }
                                catch (Exception exx)
                                {
                                    Console.WriteLine("Unexpected Error!!\n{0}", exx.Message);
                                }
                            }
                            
                        }
                        else
                        {
                            Console.WriteLine("Wrong number of args!");
                        }
                        break;
                    case "wait":
                        int ms;
                        if (words.Length == 2 && int.TryParse(words[1], out ms) )
                        {

                            Console.WriteLine("Wainting {0} ms", words[1]);
                            Thread.Sleep(ms);
                        }
                        else
                        {
                            Console.WriteLine("Wrong number of args!");
                        }
                        break;
                    case "begin-repeat":
                        if (commands.Count == 0 && words.Length == 2 && int.TryParse(words[1], out rep))
                        {                            
                            if (rep > 0)
                            {
                                string lineaux = file.ReadLine();
                                while (lineaux != null && lineaux != "end-repeat")
                                {
                                    loopCommands.Add(lineaux);
                                    lineaux = file.ReadLine();
                                }
                                commands = TransformCommands(loopCommands, rep);
                            }
                            else
                            {
                                rep = 0;
                                Console.WriteLine("Invalid number of repetitions");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Invalid repetition!");
                        }
                        break;
                    default:
                        Console.WriteLine("Invalid command!");
                        break;

                }
                counter++;
            }
        }
    }
}
