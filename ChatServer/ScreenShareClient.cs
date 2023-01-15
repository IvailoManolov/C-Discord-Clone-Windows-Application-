using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace ChatServer
{
    class ScreenShareClient
    {
        private const int FLAG_READY_RECEIVE = 1;
        public TcpClient ScreenClientSocket { get; set; }

        public PacketReader packetReader { get; set; }

        public ScreenShareClient(TcpClient client)
        {
            ScreenClientSocket = client;

            packetReader = new PacketReader(ScreenClientSocket.GetStream());

            Task.Run(() => Process());
        }

        private void Process()
        {
            while (true)
            {
                try
                {
                    var buffer = packetReader.ReadScreenPicture();

                    Program.BroadcastScreenImage(buffer);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception captured : " + e.Message);
                    //ScreenClientSocket.Close();
                    //throw;
                }
            }
        }
    }
}

