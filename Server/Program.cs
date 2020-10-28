using System;
using System.Collections.Generic;
using System.Net;
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

        public event EventHandler Unlock;


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

        public Server(string server_id, string partition_id, bool isMasterReplica, string ip)
        {
            Server_id = server_id;
            Partition_id = partition_id;
            IsMasterReplica = isMasterReplica;
            Ip = IPAddress.Parse(ip);
            Storage = new Dictionary<string, Resource>();
            SystemNodes = new Dictionary<string, ServerIdentification>();
        }

        internal int AddObject(string id, Resource value)
        {
            int result = 0;
            if (Storage.ContainsKey(id))
            {
                Storage.TryGetValue(id, out Resource curVal);
                if (curVal.Locked)
                {
                    //subscribe to the unlock event
                    //TODO WAIT FOR UNLOCK
                }
                else
                {
                    lock (Storage[id]) { Storage[id] = value; }
                }
            }
            else
            {
                Storage.Add(id, value);
            }

            return result;
        }

        internal string RetrieveObject(string id)
        {
            if (Storage.ContainsKey(id))
            {
                if (Storage[id].Locked == false)
                    return Storage[id].Value;
                else //if the resource is locked
                {
                    //TODO WAIT FOR UNLOCK
                }
            }
            return "N/A"; //no such resource on this server
        }

        internal bool LockObject(string objectId)
        {
            
            if(Storage.ContainsKey(objectId) && Storage[objectId].Locked == false)
            {
                lock (Storage[objectId])
                {
                    Storage[objectId].Locked = true;
                    return true;
                }
            } else
            {
                //TODO WAIT FOR THE RESOURCE TO UNLOCK BEFORE LOCKING MYSELF
                //subscribe to the unlock event
                
            }
            return false;
        }

        internal bool UnlockObject(string objectId)
        {
            if (Storage.ContainsKey(objectId) && Storage[objectId].Locked == true)
            {
                lock (Storage[objectId]) { Storage[objectId].Locked = false; }
                    
                //TODO trigger unlock event
                return true;
            }

            return false;
        }

    }

    

    class Program
    { 

        static void Main(string[] args)
        {
            Server local = new Server("1", "A", true, "127.0.0.1");
            Server anotherOne = new Server("2", "A", false, "127.0.0.1");

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
