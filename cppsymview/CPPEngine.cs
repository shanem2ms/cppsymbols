using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace cppsymview
{
    public class CPPEngine
    {

        public void RunServer()
        {
            Task.Run(async () => { await ConnectTcp(); });
        }

        string commandline = "-DDLLX= -DPRId64=\"I64d\" -ID:/vq/flash/src/core/. -ID:/vq/flash/../vcpkg/installed/x64-windows/include -O0 -g -gcodeview -std=c++20 -c D:/vq/flash/src/core/geo/SphericalProjection.cpp -P D:/vq/flash/build/debugclg/StdIncludes.h.pch";
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
                byte[] clbytes = Encoding.UTF8.GetBytes(commandline);
                byte[] messageBytes = new byte[clbytes.Length + 1];
                Array.Copy(clbytes, 0, messageBytes, 1, clbytes.Length);
                messageBytes[0] = 1;

                _ = await client.SendAsync(messageBytes, SocketFlags.None);

                // Receive ack.
                var buffer = new byte[2];
                var received = await client.ReceiveAsync(buffer, SocketFlags.None);
                UInt16 response = BitConverter.ToUInt16(buffer);
                if (response != 200)
                    throw new Exception("Command line error");
            }

            while (true)
            {
                // Send message.
                var message = "Hi friends <|EOM|>";
                var messageBytes = Encoding.UTF8.GetBytes(message);
                _ = await client.SendAsync(messageBytes, SocketFlags.None);
                Console.WriteLine($"Socket client sent message: \"{message}\"");

                // Receive ack.
                var buffer = new byte[1_024];
                var received = await client.ReceiveAsync(buffer, SocketFlags.None);
                var response = Encoding.UTF8.GetString(buffer, 0, received);
                if (response == "<|ACK|>")
                {
                    Console.WriteLine(
                        $"Socket client received acknowledgment: \"{response}\"");
                    break;
                }
            }

            client.Shutdown(SocketShutdown.Both);
            return true;
        }
    }

}
