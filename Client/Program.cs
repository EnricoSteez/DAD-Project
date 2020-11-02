using Grpc.Core;
using Grpc.Net.Client;
using Server.protos;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
        private static ServerStorageServices.ServerStorageServicesClient RetrieveServer(string serverId, string partitionId)
        {
            GrpcServer res = null;
            if (servers.TryGetValue(serverId, out res))
            {
                res.Partition_id = partitionId;
                return res.Service;
            }
                res = new GrpcServer(partitionId, "http://localhost:" + (1000 + inc++));
                servers.Add(serverId, res);
                return res.Service;
        }


        private static List<string> TransformCommands(List<string> loopCommands, int reps)
        {
            
            List<string> commands = new List<string>();
            for(int i = reps; i > 0; i--)
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
            int counter = 0;
            string line;

            List<string> loopCommands = new List<string>();
            List<string> commands = new List<string>();

            // Read the file and display it line by line.  
            System.IO.StreamReader file = new System.IO.StreamReader(@"../../../test.txt");
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
                            var reply = RetrieveServer(serverId, partitionId).ReadObject(new ReadObjectRequest { PartitionId = partitionId, ObjectId = objectId});
                            Console.WriteLine("Object {0} read: {1}", objectId, reply.Value.ToString());
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
                                var reply = RetrieveServer(masterServerId, partitionId).WriteObject(new WriteObjectRequest { PartitionId = partitionId, ObjectId = objectId, Value = words[3] });
                                Console.WriteLine("Write object {0} result: {1}", objectId, reply.WriteResult.ToString());

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
                            var reply = RetrieveServer(serverId).ListServer(new ListServerRequest { ServerId = serverId });
                            Console.WriteLine("Objects: {0}", reply.StoredObjects);
                        }
                        else
                        {
                            Console.WriteLine("Wrong number of args!");
                        }
                        break;
                    case "listGlobal":
                        if(words.Length == 1 )
                        {
                            // TODO: list global
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

                            Thread.Sleep(ms);
                            Console.WriteLine("Wainting {0} ms", words[1]);
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
