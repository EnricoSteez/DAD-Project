using Grpc.Core;
using Server.protos;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks.Dataflow;
using Grpc.Net.Client;
using System.Linq;

namespace PuppetMaster

{


    class PuppetMaster
    {

        
        private static PuppetMasterServices.PuppetMasterServicesClient PuppetMasterServicesClient()
        {

            GrpcChannel channel = GrpcChannel.ForAddress("http://localhost:" + 10000);
            PuppetMasterServices.PuppetMasterServicesClient node = new PuppetMasterServices.PuppetMasterServicesClient(channel);

            return node; 
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
            
            string line;

            List<string> loopCommands = new List<string>();
            List<string> commands = new List<string>();

            /*----*/
            List<Server.ServerIdentification> servers = new List<Server.ServerIdentification>();
            Dictionary<String, List<String>> partitions = new Dictionary<String, List<string>>(); 
            //IMPORTANT: mapping partition Id to a list of server Ids where the partition is replicated (first is master)
            //IMPORTANT: ->>> when you create a server, search for that server id in the list of every entry in the dictionary
            //the master Id will be the first element of the list.

            /*----*/

            // Read the file and display it line by line. (idem Client)
            System.IO.StreamReader file = new System.IO.StreamReader(@"../../../test.txt");
            while ((line = GetElement(commands)) != null || (line = file.ReadLine()) != null)
            {
                string[] words = line.Split(' '); // depends how many words to split

                string serverId;
                string URL;



                string partitionName;
                string username;
                string scriptFile;
                int r;


                switch (words[0]) {
                    // configure system
                    case "replicationFactor":
                        if (words.Length == 2 && int.TryParse(words[1], out r))
                        {
                            ///???
                        }
                        else
                        {
                            Console.WriteLine("Wrong number of arguments!");
                        }
                        break;
                // Script starts with Server setup (Server and Partition commands)
                    // create server process
                    case "server":
                        int minDelay;
                        int maxDelay;
                        //TODO: send information about which partitions to store (this information must be created according to the Partition command)
                        if (words.Length == 5 )
                        {
                            serverId = words[1];
                            URL = words[2];

                            int.TryParse(words[3], out minDelay);
                            int.TryParse(words[4], out maxDelay);

                            string address = String.Join(':', words[2].Split(':').SkipLast(1).ToArray());
                            GrpcChannel channel = GrpcChannel.ForAddress( address + ':' + 10000);
                           
                            PuppetMasterServices.PuppetMasterServicesClient node = new PuppetMasterServices.PuppetMasterServicesClient(channel);

                            ServerRequestObject request = new ServerRequestObject
                            {
                                ServerId = serverId,
                                Url = URL,
                                MaxDelay = maxDelay,
                                MinDelay = minDelay
                            };

                            foreach(string partition in partitions.Keys)
                            {
                                if (partitions[partition].Contains(serverId)){
                                    
                                    PartitionMessage p = new PartitionMessage();
                                    p.Id = partition;
                                    p.MasterId = partitions[partition][0];
                                    request.Partitions.Add(p);
                                }
                            }

                            
                            ServerResponseObject result = node.ServerRequest(request);

                            Console.WriteLine(result);

                            if (result.Success)
                            {
                                servers.Add(new Server.ServerIdentification(serverId, URL));
                            }
                        }
                        else
                        {
                            Console.WriteLine("Wrong number of arguments!");
                        }
                        break;
                    // configure system to store partition on given servers
                    case "partition":
                        //TODO: store locally the partition
                        if (int.TryParse(words[1], out r) && words.Length == r + 3)
                        {
                            partitionName = words[2];
                            // do all r serverIds

                            List<string> serverlist = new List<string>();
                            for (int i=0; i < r; i += 1)
                            {
                                serverlist.Add(words[i + 3]);
                            }
                            partitions.Add(partitionName,serverlist);
                        }
                        else
                        {
                            Console.WriteLine("Wrong number of arguments!");
                        }
                        break;
                    // create client process
                    case "client":
                        //TODO: PASS EVERY PARTITION (ID, MASTERID) IN THE DICTIONARY TO THE EVERYPARTITION PARAMETER IN THE REQUEST
                        if (words.Length == 4)
                        {
                            username = words[1];
                            URL = words[2];
                            scriptFile = words[3];

                            string address = String.Join(':', words[2].Split(':').SkipLast(1).ToArray());
                            GrpcChannel channel = GrpcChannel.ForAddress(address + ':' + 10000);

                            PuppetMasterServices.PuppetMasterServicesClient node = new PuppetMasterServices.PuppetMasterServicesClient(channel);

                            ClientRequestObject request = new ClientRequestObject
                            {
                                ClientUrl = URL,
                                Username = username,
                                Scriptfile = scriptFile

                            };

                            foreach(string partition in partitions.Keys)
                            {
                                PartitionMessage p = new PartitionMessage();
                                p.Id = partition;
                                p.MasterId = partitions[partition][0];
                                // all servers later
                                request.Everypartition.Add(p);
                            }

                            ClientResponseObject result = node.ClientRequest(request);

                            Console.WriteLine(result);
                        }
                        else
                        {
                            Console.WriteLine("Wrong number of arguments!");
                        }
                        break;
                    // all nodes print current status
                    case "status":
                        //TODO: loop through all the servers and send status request
                        if (words.Length == 1)
                        {


                            StatusRequestObject request = new StatusRequestObject
                            {

                            };

                            //StatusResponseObject result = StatusRequest(request);

                            // iterate and send to server itself (like freeze and unfreeze)
                        }
                        else
                        {
                            Console.WriteLine("Wrong number of arguments!");
                        }
                        break;

                    // DEBUGGING COMMANDS
                    // force process to crash
                    case "crash":
                        //send the request to the correspondent PCS
                        if (words.Length == 2)
                        {
                            serverId = words[1];

                            // check server address
                            // send crash request to that address + port 10000
                            
                        }
                        else
                        {
                            Console.WriteLine("Wrong number of arguments!");
                        }
                        break;
                    // simulate delay in process (stops processing messages until unfreeze received)
                    case "freeze":
                        if (words.Length == 2)
                        {
                            serverId = words[1];
                            // iterate over all the servers and send them freeze req
                        }
                        else
                        {
                            Console.WriteLine("Wrong number of arguments!");
                        }
                        break;
                    // process back to normal operation
                    case "unfreeze":
                        if (words.Length == 2)
                        {
                                // idem freeze req
                            serverId = words[1];
                        }
                        else
                        {
                            Console.WriteLine("Wrong number of arguments!");
                        }
                        break;

                    default:
                        Console.WriteLine("Invalid command!");
                        break;
                }

            }
            }
    }
}
            
