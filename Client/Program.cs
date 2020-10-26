using Grpc.Core;
using Grpc.Net.Client;
using Server.protos;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Threading;

namespace Client
{
    class Program
    {
        private static Dictionary<int, ServerStorageServices.ServerStorageServicesClient> servers = new Dictionary<int, ServerStorageServices.ServerStorageServicesClient>();
        
        private static ServerStorageServices.ServerStorageServicesClient retrieveServer(int serverId)
        {
            ServerStorageServices.ServerStorageServicesClient res = null;
            if(servers.TryGetValue(serverId, out res))
            {
                return res;
            }
            GrpcChannel channel = GrpcChannel.ForAddress("http://localhost:" + (1000 + serverId).ToString());
            res = new ServerStorageServices.ServerStorageServicesClient(channel);
            servers.Add(serverId, res);
            return res;
        }
        static void Main(string[] args)
        {
            int counter = 0;
            string line;

            // Read the file and display it line by line.  
            System.IO.StreamReader file = new System.IO.StreamReader(@"../../../test.txt");
            while ((line = file.ReadLine()) != null)
            {
                System.Console.WriteLine(line);
                string[] words = line.Split(' ', 4);
                int objectId;
                int partitionId;
                int serverId;
                int rep = 1;
                switch (words[0]) {
                    case "read":
                        
                        if (words.Length == 4 && int.TryParse(words[1], out partitionId) && int.TryParse(words[2], out objectId) && int.TryParse(words[3], out serverId))
                        {
                            var reply = retrieveServer(serverId).ReadObject(new ReadObjectRequest { PartitionId = partitionId, ObjectId = objectId});
                            Console.WriteLine("Object {0} read: {1}", objectId, reply.Value.ToString());
                        }
                        else
                        {
                            Console.WriteLine("Wrong number of args!");
                        }
                        break;
                    case "write":
                        if (words.Length == 4 && int.TryParse(words[1], out partitionId) && int.TryParse(words[2], out objectId))
                        {
                            int masterServerId = partitionId * 3;
                            var reply = retrieveServer(masterServerId).WriteObject(new WriteObjectRequest { PartitionId = partitionId, ObjectId = objectId, Value = words[3] });
                            Console.WriteLine("Write object {0} result: {1}", objectId, reply.WriteResult.ToString());
                        }
                        else
                        {
                            Console.WriteLine("Wrong number of args!");
                        }
                        break;
                    case "listServer":
                        if(words.Length == 2 && int.TryParse(words[1], out serverId))
                        {
                            var reply = retrieveServer(serverId).ListServer(new ListServerRequest { ServerId = serverId });
                            Console.WriteLine("Objects: {0}", reply.StoredObjects);
                        }
                        else
                        {
                            Console.WriteLine("Wrong number of args!");
                        }
                        break;
                    case "listGlobal":
                        if(words.Length == 1)
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
                        if (words.Length == 2 && int.TryParse(words[1], out ms))
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
                        int temprep;
                        if(words.Length == 2 && int.TryParse(words[1], out temprep))
                        {
                            if(temprep > 0)
                            {
                                rep = temprep;
                            }
                            else
                            {
                                Console.WriteLine("Invalid number of repetitions");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Wrong number of args!");
                        }
                        break;
                    case "end-repeat":
                        if(words.Length == 0)
                        {
                            rep = 1;
                        }
                        else
                        {
                            Console.WriteLine("Wrong number of args!");
                        }
                        break;

                }
                counter++;
            }
        }
    }
}
