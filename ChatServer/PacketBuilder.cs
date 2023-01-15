using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Client.Net.IO
{
    class PacketBuilder
    {
        private MemoryStream ms;
        private object locker = new object();
        public PacketBuilder()
        {
            ms = new MemoryStream();
        }
        public void WriteOpCode(byte opCode)
        {
            lock (locker)
            {
                ms.WriteByte(opCode);
            }
        }
        public void WriteString(string msg)
        {
            lock (locker)
            {
                var msgLength = msg.Length;

                ms.Write(BitConverter.GetBytes(msgLength), 0, BitConverter.GetBytes(msgLength).Length);

                ms.Write(Encoding.ASCII.GetBytes(msg), 0, msgLength);
            }
        }
        public void WriteAudioMessage(byte[] msg, int startingIndex, int endingIndex)
        {
            lock (locker)
            {
                var msgLength = msg.Length;

                ms.Write(BitConverter.GetBytes(msgLength), 0, BitConverter.GetBytes(msg.Length).Length);

                ms.Write(msg, 0, msgLength);
            }
        }
        public byte[] GetPacketBytes()
        {
            lock (locker)
            {
                var sendPacket = ms.ToArray();
                Clear(ms);
                return sendPacket;
            }
        }
        public void Clear(MemoryStream source)
        {
            lock (locker)
            {
                byte[] buffer = source.GetBuffer();
                Array.Clear(buffer, 0, buffer.Length);
                source.Position = 0;
                source.SetLength(0);
            }
        }
        public void WriteScreenImageMessage(byte[] bitmap)
        {
            lock (locker)
            {
                int imageLength = bitmap.Length;

                ms.Write(BitConverter.GetBytes(imageLength), 0, BitConverter.GetBytes(imageLength).Length);

                ms.Write(bitmap, 0, imageLength);
            }
        }
    }
}
