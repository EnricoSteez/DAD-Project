using Grpc.Net.Client;
using Server.protos;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Client
{
    class Program
    {
        static void Main(string[] args)
        {
            GrpcChannel channel = GrpcChannel.ForAddress("http://localhost:1001");
            ServerStorageServices.ServerStorageServicesClient client = new ServerStorageServices.ServerStorageServicesClient(channel);
            int counter = 0;
            string line;

            // Read the file and display it line by line.  
            System.IO.StreamReader file = new System.IO.StreamReader(@"../../../test.txt");
            while ((line = file.ReadLine()) != null)
            {
                System.Console.WriteLine(line);
                string[] words = line.Split(' ');
                switch(words[0]) {
                    case "read":
                        int objectId;
                        int partitionId;
                        int serverId;
                        if (words.Length == 4 && int.TryParse(words[1], out partitionId) && int.TryParse(words[2], out objectId) && int.TryParse(words[3], out serverId))
                        {
                            var reply = client.ReadObject(new ReadObjectRequest { PartitionId = partitionId, ObjectId = objectId});
                            Console.WriteLine(reply.Value.ToString());
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
