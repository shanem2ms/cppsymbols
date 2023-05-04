#include "Precomp.h"
#include "TcpServer.h"
#include "cppstream.h"

#include <evpp/tcp_server.h>
#include <evpp/buffer.h>
#include <evpp/tcp_conn.h>

#include "Compiler.h"

void TcpServer::Start()
{
#ifdef _WIN32
    WSADATA wsaData;
    WORD wVersionRequested = MAKEWORD(2, 2);
    int err = WSAStartup(wVersionRequested, &wsaData);
    if (0 != err) {
        return;
    }
#endif
    std::string addr = "0.0.0.0:9099";
    evpp::Buffer buffer(1024);
    int thread_num = 4;
    evpp::EventLoop loop;
    evpp::TCPServer server(&loop, addr, "CPPSymbols", 1);

    server.SetMessageCallback([this, &buffer](const evpp::TCPConnPtr& conn,
        evpp::Buffer* msg) {
            int8_t command = msg->ReadInt8();
            evpp::Slice slice = msg->ToSlice();
            if (command == 1)  // Send command line
            {
                buffer.Reset();
                
                std::vector<uint8_t> data(slice.data(), slice.data() + slice.size());
                CppVecStreamReader vecReader(data);
                std::vector<std::string> args;
                CppStream::Read(vecReader, 0, args);
                m_args = args;
                buffer.Reset();
                int16_t val = 200;
                buffer.Append(&val, sizeof(val));
                conn->Send(&buffer);
            }
            if (command == 2) // send source filename
            {
                buffer.Reset();
                std::string filename = slice.ToString();
                std::vector<std::string> cargs = m_args;
                std::vector<uint8_t> data = 
                    Compiler::Inst()->CompileWithArgs(filename, m_args, true);
                buffer.Append(data.data(), data.size());
                conn->Send(&buffer);
            }
            else
            {
                conn->Send(msg);
            }
        });
    server.SetConnectionCallback([](const evpp::TCPConnPtr& conn) {
        if (conn->IsConnected()) {
        }
        else {
        }
        });
    server.Init();
    server.Start();
    loop.Run();
}