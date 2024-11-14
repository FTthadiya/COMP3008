using ChatServiceInterface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Text;
using System.Threading.Tasks;

namespace LobbyServer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting Game Lobby Server...");

            ServiceHost serviceHost = new ServiceHost(typeof(LobbyServer));
            var tcpBinding = new NetTcpBinding
            {
                MaxReceivedMessageSize = 52428800, // 50 MB
                MaxBufferSize = 52428800, // 50 MB
                MaxBufferPoolSize = 52428800 // 50 MB
            };

            serviceHost.AddServiceEndpoint(typeof(LobbyServerInterface), tcpBinding, "net.tcp://localhost:8000/LobbyService");
            var behavior = new ServiceMetadataBehavior { HttpGetEnabled = false };
            serviceHost.Description.Behaviors.Add(behavior);

            serviceHost.Open();

            Console.WriteLine("Game Lobby Server Online!");
            Console.WriteLine("Press any key to exit...");
            Console.ReadLine();
            
            serviceHost.Close();
        }
    }
}
