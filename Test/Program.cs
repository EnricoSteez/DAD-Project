using Grpc.Net.Client;
using System;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            GrpcChannel channel = GrpcChannel.ForAddress("http://localhost:10000");

            PuppetMasterServices.PuppetMasterServicesClient node = new PuppetMasterServices.PuppetMasterServicesClient(channel);
        }
    }
}
