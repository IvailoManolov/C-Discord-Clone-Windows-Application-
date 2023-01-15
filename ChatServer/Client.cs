using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ChatServer
{
    class Client
    {

        const int FLAG_READY_RECEIVE = 1;
        
        const int FLAG_USER_MUTED = 3;

        const int FLAG_USER_MESSAGE = 101;

        const int FLAG_USER_STARTED_SHARING = 50;

        private bool clientConnected = false;

        public string Name { get; set; }

        public Guid ClientID { get; set; }

        public TcpClient ClientSocket { get; set; }

        public PacketReader packetReader { get; set; }

        public Dictionary<TcpClient, Guid> dictHolder = new Dictionary<TcpClient, Guid>();

        public Client(TcpClient client)
        {

            ClientSocket = client;

            ClientID = Guid.NewGuid();

            packetReader = new PacketReader(ClientSocket.GetStream());

            var opCode = packetReader.ReadByte();

            Name = packetReader.ReadMessage();

            Console.WriteLine($"[{DateTime.Now}] : " +
                $"[User Connected] User {Name} has connected");

            clientConnected = true;

            Task.Run(() => Process());
        }

        private void Process()
        {
            while(true)
            {
                try
                {
                    var opCode = packetReader.Read();

                    switch(opCode)
                    {
                        case FLAG_READY_RECEIVE:
                            if(clientConnected)
                            {
                                clientConnected = false;
                                Program.BroadcastConnection();
                            }
                            break;

                        case FLAG_USER_MUTED:
                            var userColor = packetReader.ReadMessage();
                            Program.BroadcastMutedState(userColor,dictHolder[ClientSocket]);
                            break;

                        case FLAG_USER_MESSAGE:
                            var messageToSend = packetReader.ReadMessage();
                            Program.BroadcastMessage(ClientID, messageToSend,this.Name);
                            break;

                        case FLAG_USER_STARTED_SHARING:
                            var screenFlag = packetReader.ReadMessage(); //RECEIVED TRUE
                            Program.BroadcastScreenStatusMessage(ClientID, screenFlag);
                            break;

                        default:
                            break;
                    }
                }
                catch(Exception e)
                {
                    Console.WriteLine("Exception captured : " + e.Message);
                    Program.BroadcastDisconnectedUser(this.ClientID.ToString());
                    ClientSocket.Close();
                    throw;
                }
            }
        }
    }
}
