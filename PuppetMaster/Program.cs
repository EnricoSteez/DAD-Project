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

namespace PuppetMaster

{


    class PuppetMaster
    {

        // Start with server setup (server and Partition commands)




        // Make Pm able to read a sequence of commands from a Script fileand execute them
        // don't forget 'Wait x_ms' command





        static void Main(string[] args)
        {

            /*int counter = 0;
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
                string minDelay;
                string maxDelay;
                string partitionName;
                string username;
                string scriptFile;
                string r;


                switch (words[0]) {
                    // configure system
                    case "replicationFactor":
                        if (words.Length == 2 && int.TryParse(words[1], out r))
                        {

                        }
                        else
                        {

                        }
                        break;
                    // create server process
                    case "server":
                        if (words.Length == 5 && int.TryParse(words[3], out minDelay) && int.TryParse(words[4], out maxDelay))
                        {
                            serverId = words[1];
                            URL = words[2];

                            if (minDelay == 0 && maxDelay == 0)
                            {

                            }
                        }
                        else
                        {

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

                        }
                        break;
                    // create client process
                    case "client":
                        if (words.Length == 4)
                        {

                            username = words[1];
                            URL = words[2];
                            scriptFile = words[3];
                        }
                        else
                        {

                        }
                        break;
                    // all nodes print current status
                    case "status":
                        if (words.Length == 1)
                        {

                        }
                        else
                        {

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

                        }
                        break;
                    default:
                        Console.WriteLine("Invalid command!");
                        break;
                }

            }
            */
        }
    }
}
            
