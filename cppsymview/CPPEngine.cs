using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents.DocumentStructures;
using System.Threading;
using System.Text.Unicode;

namespace cppsymview
{
    public class CPPEngine
    {

        public void RunServer()
        {
            Task.Run(async () => { await ConnectTcp(); });
        }

        SemaphoreSlim sendEvent = new SemaphoreSlim(0, 1);
        enum DataType
        {
            CommandLineArgs = 1,
            Filename = 2,
            SourceCode = 3,
            Shutdown = 100
        };

        DataType dataType;
        string sourceCode = string.Empty;
        string[] commandArgs = { "-DDLLX=",
                                "-DPRId64=\"I64d\"",
                                "-ID:/vq/flash/src/core/.",
                                "-ID:/vq/flash/../vcpkg/installed/x64-windows/include",
                                "-O0",
                                "-g",
                                "-gcodeview",
                                "-std=c++20",
                                "-P",
                                "D:/vq/flash/build/debugclg/StdIncludes.h.pch" };

        static byte[] SerializeStringArray(string[] args)
        {
            int length = 8;
            foreach (var arg in args)
            {
                int byteCount = Encoding.UTF8.GetByteCount(arg);
                length += byteCount + 2;
            }
            
            byte[] outbytes = new byte[length];
            length = 8;
            UInt64 arrayLen = (UInt64)args.Length;
            byte[] bytes = BitConverter.GetBytes(arrayLen);
            Buffer.BlockCopy(bytes, 0, outbytes, 0, bytes.Length);

            foreach (var arg in args)
            {
                int byteCount = Encoding.UTF8.GetByteCount(arg);
                bytes = BitConverter.GetBytes((UInt16)byteCount);
                outbytes[length] = bytes[0];
                outbytes[length + 1] = bytes[1];
                length += 2;
                int numencoded = Encoding.UTF8.GetBytes(arg, new Span<byte>(outbytes, length, byteCount));
                length += byteCount;
            }
            return outbytes;
        }


        async Task<bool> ConnectTcp()
        {
            IPAddress iPAddress = new IPAddress(new byte[4] { 127, 0, 0, 1 });
            IPEndPoint ipEndPoint = new(iPAddress, 9099);

            using Socket client = new(
            ipEndPoint.AddressFamily,
            SocketType.Stream,
            ProtocolType.Tcp);

            await client.ConnectAsync(ipEndPoint);

            {
                // Send command line
                byte[] data = SerializeStringArray(this.commandArgs);
                byte[] fullBytes = new byte[data.Length + 1];
                fullBytes[0] = (int)DataType.CommandLineArgs;
                Buffer.BlockCopy(data, 0, fullBytes, 1, data.Length);
                _ = await client.SendAsync(fullBytes, SocketFlags.None);

                // Receive ack.
                var buffer = new byte[2];
                var received = await client.ReceiveAsync(buffer, SocketFlags.None);
                UInt16 response = BitConverter.ToUInt16(buffer);
                if (response != 200)
                    throw new Exception("Command line error");
            }

            while (true)
            {
                await sendEvent.WaitAsync();
                if (dataType == DataType.Shutdown)
                    break;
                // Send source.
                var localsrc = this.sourceCode;
                this.sourceCode = string.Empty;
                int byteCount = Encoding.UTF8.GetByteCount(localsrc);
                byte []messageBytes = new byte[byteCount + 1];
                int numencoded = Encoding.UTF8.GetBytes(localsrc, new Span<byte>(messageBytes, 1, byteCount));
                messageBytes[0] = (byte)this.dataType;
                _ = await client.SendAsync(messageBytes, SocketFlags.None);

                // Receive ack.
                var buffer = new byte[1_024];
                var received = await client.ReceiveAsync(buffer, SocketFlags.None);
                var response = Encoding.UTF8.GetString(buffer, 0, received);
            }

            client.Shutdown(SocketShutdown.Both);
            return true;
        }


        public void SendSourceCode(string text)
        {
            this.sourceCode = text;
            this.dataType = DataType.SourceCode;
            this.sendEvent.Release();
        }
        public void CompileFile(string text)
        {
            this.sourceCode = text;
            this.dataType = DataType.Filename;
            this.sendEvent.Release();
        }
    }

}
