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
    evpp::TCPServer server(&loop, addr, "CPPSymbols", thread_num);

    server.SetMessageCallback([this, &buffer](const evpp::TCPConnPtr& conn,
        evpp::Buffer* msg) {
            size_t sz = msg->size();
            const char* pdata = msg->data();
            if (pdata[0] == 1)  // Send command line
            {
                buffer.Reset();
                std::vector<uint8_t> data(pdata + 1, pdata + sz);
                CppVecStreamReader vecReader(data);
                std::vector<std::string> args;
                CppStream::Read(vecReader, 0, args);
                m_args = args;
                buffer.Reset();
                int16_t val = 200;
                buffer.Append(&val, sizeof(val));
                conn->Send(&buffer);
            }
            if (pdata[0] == 2) // send source filename
            {
                buffer.Reset();
                std::string filename(pdata + 1, pdata + sz);

                std::vector<std::string> cargs = m_args;
                Compiler::Inst()->CompileWithArgs(filename, m_args, true);
                int16_t val = 200;
                buffer.Append(&val, sizeof(val));
                conn->Send(&buffer);
            }
            else
            {
                std::string str(pdata, pdata + sz);
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