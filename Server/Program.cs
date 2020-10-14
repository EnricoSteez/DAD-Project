using System;
using Server.protos;

namespace Server
{

    public class ServerReadService : ReadObjectService.ReadObjectServiceBase
    {

    }

    public class ServerWriteService : WriteObjectService.WriteObjectServiceBase
    {

    }

    public class ServerListService : GetInformations.GetInformationsBase
    {

    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
        }
    }
}
