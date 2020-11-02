﻿using Grpc.Core;
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


        public override Task<ClientResponseObject> ClientRequest(ClientRequestObject request, ServerCallContext context)
        {
            
            Process.Start("..\\..\\..\\..\\Client\\bin\\Debug\\netcoreapp3.1\\Client.exe", request.Username + " " + request.ClientUrl + " " + request.Scriptfile);
            return Task.FromResult(new ClientResponseObject { Success = "true" });
        }

        public override Task<ServerResponseObject> ServerRequest(ServerRequestObject request, ServerCallContext context)
        {
            Process.Start("..\\..\\..\\..\\Server\\bin\\Debug\\netcoreapp3.1\\Server.exe", request.ServerId + " " + request.Url + " " + request.MinDelay + " " + request.MaxDelay);
            return Task.FromResult(new ServerResponseObject { Success = "true" });
        }
        public override Task<StatusResponseObject> StatusRequest(StatusRequestObject request, ServerCallContext context)
        {
            Console.WriteLine("STH");
            return Task.FromResult(new StatusResponseObject { });
        }
    }
    class Program
    {
        static void Main(string[] args)
        {
            PuppetMasterServices.PuppetMasterServicesBase.ClientRequest();
            Process.Start("..\\..\\..\\..\\Client\\bin\\Debug\\netcoreapp3.1\\Client.exe", "D:\\Users\\almof\\Documents\\MEIC\\DAD-Project\\Client\\test.txt");
            //PuppetMasterServices.PuppetMasterServicesBase
            Console.WriteLine("Hello World!");
        }
    }
}
