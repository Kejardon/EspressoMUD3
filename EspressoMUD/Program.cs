using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EspressoMUD
{
    public class Program
    {
        public static Server Server;

        /// <summary>
        /// Flag / Event when the server is shutting down. Only Shutdown() should manually change this, but anything can wait on this.
        /// </summary>
        public static ManualResetEvent ShutdownTrigger = new ManualResetEvent(false);
        /// <summary>
        /// Shuts down the MUD. Pauses everything and sets ShutdownTrigger.
        /// </summary>
        public static void Shutdown()
        {
            ThreadManager.PauseMUD(false);
            ShutdownTrigger.Set();
        }

        static bool AttemptedForceClose = false;
        static void CleanupProgram(object sender, ConsoleCancelEventArgs e)
        {
            
            if(AttemptedForceClose)
            {
                Console.Out.WriteLine("Force-closing the MUD. Warning: This may corrupt your data.");
            }
            else
            {
                AttemptedForceClose = true;
                e.Cancel = true;
                Console.Out.WriteLine("Attempting to shut down the MUD. You may force-close the MUD by sending another cancel event, but this may cause problems.");
                Shutdown();
            }
        }

        static void Main(string[] args)
        {
            Console.CancelKeyPress += CleanupProgram;
            Metadata.Initialize();
            DatabaseManager.Start();
            DatabaseManager.LoadFullType(ObjectType.TypeByClass[typeof(Account)]);

            EndPoint endPoint;
            //TODO: Get EndPoint from configuration.
            endPoint = new IPEndPoint(new IPAddress(new byte[] {192,168,1,123 }), 32943);
            Server = new Server(endPoint);

            ShutdownTrigger.WaitOne();
            //TODO: Cleanup.

        }
    }
}
