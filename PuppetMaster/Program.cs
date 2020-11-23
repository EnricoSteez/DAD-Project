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

namespace PuppetMaster

{


    class PuppetMaster
    {

        
        private static PuppetMasterServices.PuppetMasterServicesClient PuppetMasterServicesClient()
        {


            GrpcChannel channel = GrpcChannel.ForAddress("http://localhost:" + 10000);
            PuppetMasterServices.PuppetMasterServicesClient node = new PuppetMasterServices.PuppetMasterServicesClient(channel);

            return 
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

            // Read the file and display it line by line. (idem Client)
            System.IO.StreamReader file = new System.IO.StreamReader(@"../../../test.txt");
            while ((line = GetElement(commands)) != null || (line = file.ReadLine()) != null)
            {
                string[] words = line.Split(' ', ); // depends how many words to split

                string serverId;
                string URL;
                int minDelay;
                int maxDelay;
                string partitionName;
                string username;
                string scriptFile;
                int r;
                
                

                switch (words[0]) {
                    // configure system
                    case "replicationFactor":
                        if (words.Length == 2 && int.TryParse(words[1], out r))
                        {

                        }
                        else
                        {
                            Console.WriteLine("Wrong number of arguments!");
                        }
                        break;
                // Script starts with Server setup (Server and Partition commands)
                    // create server process
                    case "server":
                        if (words.Length == 5 && int.TryParse(words[3], out minDelay) && int.TryParse(words[4], out maxDelay))
                        {
                            serverId = words[1];
                            URL = words[2];

                            // normal implementation without delays yet

                            GrpcChannel channel = GrpcChannel.ForAddress("http://localhost:" + 2000);
                            PuppetMasterServices.PuppetMasterServicesClient node = new PuppetMasterServices.PuppetMasterServicesClient(channel);

                            ServerRequestObject request = new ServerRequestObject
                            {
                                ServerId = serverId,
                                Url = URL,
                                MaxDelay = maxDelay,
                                MinDelay = minDelay
                            };

                            ServerResponseObject result = node.ServerRequest(request);

                            Console.WriteLine(result);

                            

                        }
                        else
                        {
                            Console.WriteLine("Wrong number of arguments!");
                        }
                        break;
                    // configure system to store partition on given servers
                    case "partition":
                        if (int.TryParse(words[1], out r) && words.Length == r + 3)
                        {
                            partitionName = words[2];
                            // do all r serverIds
                        }
                        else
                        {
                            Console.WriteLine("Wrong number of arguments!");
                        }
                        break;
                    // create client process
                    case "client":
                        if (words.Length == 4)
                        {
                            username = words[1];
                            URL = words[2];
                            scriptFile = words[3];
                            GrpcChannel channel = GrpcChannel.ForAddress("http://localhost:" + 1000);
                            PuppetMasterServices.PuppetMasterServicesClient node = new PuppetMasterServices.PuppetMasterServicesClient(channel);

                            ClientRequestObject request = new ClientRequestObject
                            {
                                ClientUrl = URL,
                                Username = username,
                                Scriptfile = scriptFile

                            };

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
                        if (words.Length == 1)
                        {


                            StatusRequestObject request = new StatusRequestObject
                            {

                            };

                            StatusResponseObject result = StatusRequest(request);
                        }
                        else
                        {
                            Console.WriteLine("Wrong number of arguments!");
                        }
                        break;

                    // DEBUGGING COMMANDS
                    // force process to crash
                    case "crash":
                        if (words.Length == 2)
                        {
                            serverId = words[1];
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
                            serverId = words[1];
                        }
                        else
                        {
                            Console.WriteLine("Wrong number of arguments!");
                        }
                        break;

                                        
                    case "wait":
                        int ms;
                        if (words.Length == 2 && int.TryParse(words[1], out ms))
                        {
                            Process.Sleep(ms);
                            Console.WriteLine("Waiting {0} ms", words[1]);
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
            
