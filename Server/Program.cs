using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;
using Server.protos;

namespace Server
{

    public class Server
    {
        private Dictionary<string, Resource> Storage { get; }
        public Dictionary<string, ServerIdentification> SystemNodes { get; set; }
        public string Server_id { get; }
        public string Partition_id { get; }
        public bool IsMasterReplica { get; }
        public  IPAddress Ip { get; }
        public int MinDelay;
        public int MaxDelay;


        public Server() //dummy implementation for debugging with just 1 server at localhost
        {
            Server_id = "1";
            Partition_id = "A";
            IsMasterReplica = true;
            Ip = IPAddress.Parse("127.0.0.1");
            //empty storage at startup
            Storage = new Dictionary<string, Resource>();
            //empty dictionary at startup
            SystemNodes = new Dictionary<string, ServerIdentification>();
        }

        public Server(string server_id, string partition_id, bool isMasterReplica, string ip, int minDelay, int maxDelay)
        {
            Server_id = server_id;
            Partition_id = partition_id;
            IsMasterReplica = isMasterReplica;
            Ip = IPAddress.Parse(ip);
            Storage = new Dictionary<string, Resource>();
            SystemNodes = new Dictionary<string, ServerIdentification>();
            MinDelay = minDelay;
            MaxDelay = maxDelay;
        }

        internal int AddObject(string id, Resource value)
        {
            if (Storage.ContainsKey(id))
            {
                if (Storage[id].Locked)
                {
                    Console.WriteLine("Object {0} is locked, waiting for unlock ...", Storage[id].Value);
                    Monitor.Wait(Storage[id]);
                }
                else
                {
                    Monitor.Enter(Storage[id]);
                    Storage[id] = value;
                    Monitor.Exit(Storage[id]);
                }
            }
            else
            {
                Storage.Add(id, value);
                //just in case someone is passing a locked resource
                Monitor.Enter(Storage[id]);
                Storage[id].Locked = false;
                Monitor.Exit(Storage[id]);
            }

            return 0;
        }

        internal string RetrieveObject(string id)
        {
            if (Storage.ContainsKey(id))
            {
                if (Storage[id].Locked == false)
                    return Storage[id].Value;
                else //if the resource is locked I wait for a pulse on this resource from the Unlock function 
                {
                    Monitor.Wait(Storage[id]);
                    return Storage[id].Value;
                }
            }
            return "N/A"; //no such resource on this server
        }

        internal bool LockObject(string id)
        {
            
            if(Storage.ContainsKey(id) && Storage[id].Locked == false)
            {
                Monitor.Enter(Storage[id]);

                Console.WriteLine("Thread {0} just got the permission for locking resource {1}",
                    Thread.CurrentThread.Name, id);

                Storage[id].Locked = true;

                Monitor.Exit(Storage[id]);

                return true;
            } else //resource is already locked by someone else
                //so I wait for it to release the lock and then I lock it myself
            {
                do
                {
                    Monitor.Wait(Storage[id]);
                } while (Storage[id].Locked==true);


                Monitor.Enter(Storage[id]);
                Storage[id].Locked = true;
                Monitor.Exit(Storage[id]);


                return true;
            }
        }

        internal bool UnlockObject(string objectId)
        {
            if (Storage.ContainsKey(objectId) && Storage[objectId].Locked == true)
            {
                Monitor.Enter(Storage[objectId]);
                Storage[objectId].Locked = false;
                Monitor.Exit(Storage[objectId]);

                //awake the processes sleeping on this object
                Monitor.Pulse(Storage[objectId]);
                
                return true;
            }

            return false;
        }

    }

    

    class Program
    { 

        static void Main(string[] args)
        {
            Server local = new Server("1", "A", true, "127.0.0.1",0,0);
            Server anotherOne = new Server("2", "A", false, "127.0.0.1",0,0);

            List<Server> servers = new List<Server>
            {
                { local },
                { anotherOne }
            };

            //knowledge of all other nodes. This should be initialized by the Puppet Master in the future
            foreach(Server s in servers)
            {
                foreach (Server s2 in servers)
                {
                    //serverPool contains an entry for every other node in the system
                    //with its complete identity: ServerIdentification
                    if(s2.Server_id != s.Server_id)
                    {
                        s.SystemNodes.Add(s2.Server_id, new ServerIdentification(s2.Server_id, s2.Partition_id, s2.Ip));
                    }
                    
                }
            }

            Grpc.Core.Server server = new Grpc.Core.Server
            {
                Services =
            {
                ServerStorageServices.BindService(new ServerClientService(local)),
                ServerCoordinationServices.BindService(new ServerServerService(local))
            },

                Ports = { new ServerPort("127.0.0.1", 1000 + int.Parse(local.Partition_id), ServerCredentials.Insecure) }
            };

            server.Start();

            Console.WriteLine("Hello World!");
        }
    }
}
