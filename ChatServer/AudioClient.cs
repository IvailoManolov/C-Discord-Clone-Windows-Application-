using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ChatServer
{
    class AudioClient
    {
        private const int FLAG_READY_RECEIVE = 1;
        public TcpClient AudioClientSocket { get; set; }

        public PacketReader packetReader { get; set; }

        public AudioClient(TcpClient client)
        {
            AudioClientSocket = client;

            packetReader = new PacketReader(AudioClientSocket.GetStream());

            Task.Run(() => Process());
        }

        private void Process()
        {
            while (true)
            {
                try
                {
                    var buffer = packetReader.ReadAudioMessage();

                    Program.BroadcastAudio(buffer);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception captured AUdio CLient: " + e.Message);
                    throw;
                }
            }
        }
    }
}
