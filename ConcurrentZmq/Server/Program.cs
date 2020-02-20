using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NetMQ;
using NetMQ.Sockets;

namespace Server
{
    class Program
    {
        private static Task StartServer()
        {
            Task taskA = new Task(() =>
            {
                int pedId = 0;

                using (var server = new RouterSocket("@inproc://localhost:6000"))
                {
                    while (true)
                    {
                        var message = server.ReceiveMultipartMessage();
                        pedId++;

                        var messageToRouter = new NetMQMessage();
                        messageToRouter.Append(message[0]);
                        messageToRouter.AppendEmptyFrame();
                        messageToRouter.Append($"PED-{pedId}");

                        server.SendMultipartMessage(messageToRouter);
                    }
                }
            });
            
            taskA.Start();
            
            return taskA;
        }

        static Task[] StartClients(int numberOfClients)
        {
            var tasksClients = new Task[numberOfClients];
            
            for (var i = 0; i < numberOfClients; i++)
            {
                tasksClients[i] = Task.Run(() =>
                {
                    var threadId = Guid.NewGuid().ToString();
                    using (var client = new DealerSocket())
                    {
                        client.Options.Identity = Encoding.UTF8.GetBytes(threadId);

                        Console.WriteLine($"{threadId} calling");

                        var msg = new NetMQMessage();
                        msg.AppendEmptyFrame();
                        msg.Append(threadId);

                        client.Connect("inproc://localhost:6000");
                        client.SendMultipartMessage(msg);
    
                        var result = client.ReceiveMultipartMessage();

                        Console.WriteLine($"{threadId} response {result[1].ConvertToString()}");
                    }
                });
            }

            return tasksClients;
        }
        
        static void Main(string[] args)
        {
            StartServer();
            
            var clients = StartClients(5);
            
            Task.WaitAll(clients);
            
            Console.ReadLine();
        }
    }
}