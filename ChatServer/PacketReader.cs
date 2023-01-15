using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ChatServer
{
    class PacketReader : BinaryReader
    {
        private NetworkStream ns;

        public PacketReader(NetworkStream ns) : base(ns)
        {
            this.ns = ns;
        }

        public string ReadMessage()
        {
            byte[] msgBuffer;

            var length = ReadInt32();

            msgBuffer = new byte[length];

            ns.Read(msgBuffer, 0, length);

            var msg = Encoding.ASCII.GetString(msgBuffer);

            ns.Flush();

            return msg;
        }

        public byte[] ReadAudioMessage()
        {
            byte[] msgBuffer;

            var length = ReadInt32();

            msgBuffer = new byte[length];

            ns.Read(msgBuffer, 0, length);

            return msgBuffer;
        }

        public byte[] ReadScreenPicture()
        {
            byte[] msgBuffer;

            var length = ReadInt32();

            msgBuffer = new byte[length];

            Console.WriteLine("Received " + length);

            var opa = ns.Read(msgBuffer, 0, length);

            Console.WriteLine("Read " + opa);

            byte[] copiedBuffer = new byte[opa];

            Array.Copy(msgBuffer, 0, copiedBuffer, 0, opa);

            Console.WriteLine("Returned array " + copiedBuffer.Length);

            return copiedBuffer;
        }
    }
}
