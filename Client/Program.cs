using Grpc.Core;
using Grpc.Net.Client;
using Server.protos;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;


namespace Client
{
    class Program
    {
        private static int inc = 1;
        private static Dictionary<string, GrpcServer> servers = new Dictionary<string, GrpcServer>();

        private static Dictionary<string, List<string>> partitions = new Dictionary<string, List<string>>(); //key - partitionId object - list of servers with that partition

        private static Dictionary<string, int> lastKnownObjects = new Dictionary<string, int>(); //(ObjectIDs,LastKnownVersions)

        private static ServerStorageServices.ServerStorageServicesClient currentServer = null;
        private static string currentServerId = null;



        private static ServerStorageServices.ServerStorageServicesClient RetrieveServer(string serverId)
        {
            GrpcServer res = servers.GetValueOrDefault(serverId, new GrpcServer("http://localhost:" + (1000 + inc++)));

            if (servers[serverId] == null)
            {
                servers[serverId] = res;
            }

            Console.WriteLine("retrieve server will return: " + res.ToString());

            return res.Service;
        }


        private static List<string> TransformCommands(List<string> loopCommands, int reps)
        {
            List<string> commands = new List<string>();
            for (int i = 1; i <= reps; i++)
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
            if (commands.Count == 0)
            {
                return null;
            }
            string res = commands[0];
            commands.RemoveAt(0);
            return res;
        }


        static void Main(string[] args)
        {

            Dictionary<string, string> simpleservers = new Dictionary<string, string>();

            /*variables declarations*/
            string fileName = @"../../../test.txt";
            string url = "localhost";
            string username = "john";
            string partitionsFile = "partitions.binary";
            string serversFile = "servers.binary";

            /*read arguments*/
            if (args.Length == 5)
            {
                fileName = args[0];
                url = args[1];
                username = args[2];
                partitionsFile = args[3];
                serversFile = args[4];
            }


            Console.WriteLine(partitionsFile + " " + serversFile);
            /*parse serialized dictionaries*/

            BinaryFormatter bf = new BinaryFormatter();

            FileStream partfsin = new FileStream(partitionsFile, FileMode.Open, FileAccess.Read, FileShare.None);
            FileStream servfsin = new FileStream(serversFile, FileMode.Open, FileAccess.Read, FileShare.None);

            partitions = (Dictionary<string, List<string>>)bf.Deserialize(partfsin);
            simpleservers = (Dictionary<string, string>)bf.Deserialize(servfsin);  //key - serverId value - server url

            foreach (string ss in simpleservers.Keys)
            {
                servers.Add(ss, new GrpcServer(simpleservers[ss]));
            }

            simpleservers.Clear();            




            List<string> loopCommands = new List<string>();
            List<string> commands = new List<string>();
            // Read the file and display it line by line.  
            StreamReader file = new StreamReader(fileName);
            int counter = 0;
            string line;
            while ((line = GetElement(commands)) != null || (line = file.ReadLine()) != null)
            {
                string[] words = line.Split(' ', 4);
                string objectId;
                string partitionId;
                string serverId;
                int rep = 1;
                switch (words[0])
                {
                    case "read":
                        if (words.Length != 4)
                        {
                            Console.WriteLine("Wrong number of args!");
                            break;
                        }
                        partitionId = words[1];
                        objectId = words[2];
                        serverId = words[3];

                        //here I could also only check != -1 and assume that I never read before being connected to anyone
                        //as it wouldn't make sense to read before inserting any objects
                        //and as soon as I insert an object I'm connected to someone ;)
                        if (serverId != "-1" || currentServer == null)
                        {
                            currentServer = RetrieveServer(serverId);
                            currentServerId = serverId;
                        }

                        ReadObjectResponse readResponse = null;
                        bool retry = true;
                        int attempt = 0;
                        string firstAttempt = currentServerId;

                        int lastKnownVersion = lastKnownObjects.GetValueOrDefault(objectId, 0);

                        ReadObjectRequest readRequest = new ReadObjectRequest
                        {
                            PartitionId = partitionId,
                            ObjectId = objectId,
                            LastVersion = lastKnownVersion
                        };

                        while (retry)
                        {
                            try
                            {
                                readResponse = currentServer.ReadObject(readRequest);
                                retry = false;
                            }
                            catch (RpcException ex) when (ex.StatusCode == StatusCode.Internal)
                            {
                                //just in case it got modified with random return values anyways
                                readResponse = null;

                                if (attempt < partitions[partitionId].Count)
                                {
                                    string newAttemptId = partitions[partitionId][attempt];
                                    //It's geeky but we could avoid trying again the first server that we already know it's unreachable
                                    //we simply skip it when we encounter the id, only if it's not already the last of the list
                                    if (newAttemptId == firstAttempt)
                                    {
                                        //if I can skip it and still find at least another server in the list
                                        if (attempt < partitions[partitionId].Count - 1)
                                        {
                                            attempt++;
                                            newAttemptId = partitions[partitionId][attempt];
                                        }
                                        else //after this (which I know is unreachable) there's nothing else to try
                                        {
                                            Console.WriteLine("No more servers available, sorry!");
                                            break;
                                        }
                                    }

                                    
                                    
                                    Console.WriteLine("Server Unreachable, trying with {0}", newAttemptId);
                                    currentServer = RetrieveServer(newAttemptId);
                                    currentServerId = newAttemptId;
                                    attempt++;
                                    retry = true;
                                }
                                else
                                {
                                    Console.WriteLine("No more servers available, sorry!");
                                    break;
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("Unexpected Error!!\n{0}", ex.Message);
                            }
                        }

                        // print results
                        if (readResponse != null)
                        {
                            if (readResponse.Id == "OLDER VERSION" && readResponse.Value == "OLDER VERSION")
                                Console.WriteLine("The server only has an older version of this object");
                            else if (readResponse.Id == "N/A" && readResponse.Value == "N/A")
                                Console.WriteLine("The server doesn't store the required object");
                            else
                            {
                                Console.WriteLine("Object {0} read: {1}   V{2}", readResponse.Id, readResponse.Value, readResponse.Version);
                                if (lastKnownObjects.ContainsKey(readResponse.Id))
                                {
                                    lastKnownObjects[readResponse.Id] = readResponse.Version;
                                }
                                else
                                {
                                    lastKnownObjects.Add(readResponse.Id, readResponse.Version);
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine("Error getting reply");
                        }

                        break;
                    case "write":

                        if (words.Length != 4)
                        {
                            Console.WriteLine("Wrong number of args!");
                            break;
                        }

                        partitionId = words[1];
                        objectId = words[2];
                        string value = words[3];

                        if (currentServerId != partitions[partitionId][0])
                        {
                            currentServer = RetrieveServer(partitions[partitionId][0]);
                            currentServerId = partitions[partitionId][0];
                        }

                        // the version of the updates is handled server side,
                        // no version in the request
                        WriteObjectRequest writeRequest = new WriteObjectRequest
                        {
                            PartitionId = partitionId,
                            ObjectId = objectId,
                            Value = value
                        };
                        try
                        {
                            WriteObjectResponse writeResponse = currentServer.WriteObject(writeRequest);
                            Console.WriteLine("Write object {0} result: {1}", objectId, writeResponse.WriteResult);

                        }
                        catch (RpcException exx) when (
                                      exx.StatusCode == StatusCode.Unknown ||
                                      exx.StatusCode == StatusCode.Unavailable)
                        {
                            Console.WriteLine("Server {0} Unreachable, say Goodbye before it's too late...", currentServerId);
                            //ADIOS
                            servers.Remove(currentServerId);
                        }
                        catch(RpcException exx) when(exx.StatusCode == StatusCode.DeadlineExceeded)
                        {
                            Console.WriteLine("Server {0} Frozen, will try to elect another server", currentServerId);
                        }
                        break;
                    case "listServer":
                        if (words.Length == 2)
                        {
                            serverId = words[1];

                            if (currentServerId != serverId)
                            {
                                currentServer = RetrieveServer(serverId);
                                currentServerId = serverId;
                            }

                            try
                            {
                                ListServerResponse reply = currentServer.ListServer(new ListServerRequest { });

                                //PRINT RESULTS
                                Console.WriteLine("Objects:");

                                foreach (ListServerResource r in reply.Objects)
                                {
                                    Console.WriteLine("Object: {0} V{1}, master: {2}", r.Id, r.Version, r.IsMasterReplica);
                                }

                            }
                            catch (RpcException exx) when (
                                        exx.StatusCode == StatusCode.Unknown ||
                                        exx.StatusCode == StatusCode.Unavailable ||
                                        exx.StatusCode == StatusCode.DeadlineExceeded)
                            {
                                Console.WriteLine("Server {0} Unreachable, say Goodbye before it's too late...", currentServerId);
                                //farewell champ
                                servers.Remove(currentServerId);
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

                        if (words.Length == 1)
                        {
                            List<Task> tasks = new List<Task>();

                            ListGlobalResponse reply = null;

                            foreach (string serv in servers.Keys)
                            {
                                Console.WriteLine("asking server " + serv);
                                currentServer = RetrieveServer(serv);
                                try
                                {
                                    currentServerId = serv;
                                    reply = currentServer.ListGlobal(new ListGlobalRequest { });
                                    Console.WriteLine("asked " + currentServerId);

                                }
                                catch (RpcException exx) when (
                                    exx.StatusCode == StatusCode.Unknown ||
                                    exx.StatusCode == StatusCode.Unavailable ||
                                    exx.StatusCode == StatusCode.DeadlineExceeded)
                                {
                                    Console.WriteLine("Server {0} Unreachable, say Goodbye before it's too late...", currentServerId);
                                    //goodbye sweetheart
                                    servers.Remove(currentServerId);
                                }
                                catch (Exception exx)
                                {
                                    Console.WriteLine("Unexpected Error!!\n{0}", exx.Message);
                                }


                                Console.WriteLine("Server {0} Partitions:", currentServerId);
                                foreach (PartitionIdentification p in reply.Partitions)
                                {
                                    Console.WriteLine("->{0}", p.PartitionId);
                                    Console.WriteLine("\tObjects:");
                                    for (int i = 0; i < p.ObjectIds.Count; i++)
                                    {
                                        Console.WriteLine("{0}\tV{1}", p.ObjectIds[i], p.Versions[i]);
                                    }
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
                        if (words.Length == 2 && int.TryParse(words[1], out ms))
                        {

                            Console.WriteLine("Waiting {0} ms", words[1]);
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