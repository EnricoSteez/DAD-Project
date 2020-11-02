using Grpc.Core;
using Server;
using Server.protos;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace PCS
{

    public class PCSServerService : PuppetMasterServices.PuppetMasterServicesBase
    {

        public static void ThreadProc()
        {
            //Server.Program.Main(new string[0]);
        }

        public override Task<ServerResponseObject> ServerRequest(ServerRequestObject request, ServerCallContext context)
        {
            new Thread(new ThreadStart(ThreadProc));
            return Task.FromResult(new ServerResponseObject { Success = "true" });
        }
    }
    class Program
    {
        static void Main(string[] args)
        {
            Process.Start("..\\..\\..\\..\\Client\\bin\\Debug\\netcoreapp3.1\\Client.exe", "D:\\Users\\almof\\Documents\\MEIC\\DAD-Project\\Client\\test.txt");
            //PuppetMasterServices.PuppetMasterServicesBase
            Console.WriteLine("Hello World!");
        }
    }
}
