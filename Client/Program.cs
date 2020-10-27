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
        private static int inc = 1;
        private static Dictionary<string, GrpcServer> servers = new Dictionary<string, GrpcServer>();

        private static Dictionary<string, string> masters = new Dictionary<string, string>(); //key - partitionId object - serverId

        private static ServerStorageServices.ServerStorageServicesClient retrieveServer(string serverId)
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
        private static ServerStorageServices.ServerStorageServicesClient retrieveServer(string serverId, string partitionId)
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





        static void Main(string[] args)
        {
            /* ------- POPULATE ------- */

            

            /* ------------------------*/
            int counter = 0;
            string line;

            // Read the file and display it line by line.  
            System.IO.StreamReader file = new System.IO.StreamReader(@"../../../test.txt");
            while ((line = file.ReadLine()) != null)
            {
                System.Console.WriteLine(line);
                string[] words = line.Split(' ', 4);
                string objectId;
                string partitionId;
                string serverId;
                int rep = 1;
                switch (words[0]) {
                    case "read":
                        
                        if (words.Length == 4)
                        {
                            partitionId = words[1];
                            objectId = words[2];
                            serverId = words[3];
                            var reply = retrieveServer(serverId, partitionId).ReadObject(new ReadObjectRequest { PartitionId = partitionId, ObjectId = objectId});
                            Console.WriteLine("Object {0} read: {1}", objectId, reply.Value.ToString());
                        }
                        else
                        {
                            Console.WriteLine("Wrong number of args!");
                        }
                        break;
                    case "write":
                        
                        if (words.Length == 4)
                        {
                            partitionId = words[1];
                            objectId = words[2];
                            string masterServerId;
                            if(masters.TryGetValue(partitionId, out masterServerId))
                            {
                                var reply = retrieveServer(masterServerId, partitionId).WriteObject(new WriteObjectRequest { PartitionId = partitionId, ObjectId = objectId, Value = words[3] });
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
                        if(words.Length == 2)
                        {
                            serverId = words[1];
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
