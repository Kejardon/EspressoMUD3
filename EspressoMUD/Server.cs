using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;

namespace EspressoMUD
{
    public static class TelnetCode
    {
        public const byte BINARY = 0,
            IS = 0,
            ECHO = 1,
            SEND = 1,
            LOGOUT = 18,
            SUPRESS_GO_AHEAD = 3,
            TERMTYPE = 24,
            NAWS = 31,
            TOGGLE_FLOW_CONTROL = 33,
            LINEMODE = 34,
            CHARSET = 42,
            MSDP = 69,
            MSSP = 70,
            MCCPOLD = 85,
            MCCP = 86,
            MSP = 90,
            MXP = 91,
            ATCP = 200,
            GMCP = 201,
            SE = 240,
            AYT = 246,
            EC = 247,
            GA = 249,
            SB = 250,
            WILL = 251,
            WONT = 252,
            DO = 253,
            ANSI = 253,
            DONT = 254,
            IAC = 255;
    }

    public class Server
    {
        private Socket mainSocket;
        //private byte[] data;
        private int packetSize;

        public Server(EndPoint endPoint, int packetSize = 1024)
        {
            //this.data = new byte[packetSize];
            this.packetSize = packetSize;
            this.mainSocket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            this.mainSocket.Bind(endPoint);
            this.mainSocket.Listen(0);
            this.mainSocket.BeginAccept(acceptConnection, null);

        }

        private void acceptConnection(IAsyncResult result)
        {
            Socket newSocket = this.mainSocket.EndAccept(result);
            this.mainSocket.BeginAccept(acceptConnection, null);

            Client newClient = new Client(newSocket, this.packetSize);
        }
    }
}
