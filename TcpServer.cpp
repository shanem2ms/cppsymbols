#include "Precomp.h"
#include "TcpServer.h"

#include <evpp/tcp_server.h>
#include <evpp/buffer.h>
#include <evpp/tcp_conn.h>


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
    server.SetMessageCallback([&buffer](const evpp::TCPConnPtr& conn,
        evpp::Buffer* msg) {
            if (msg->data()[0] == 1)
            {
                std::string str(msg->data() + 1, msg->data() + msg->size());
                LOG_INFO <<"command line: " << str << conn->remote_addr();

                buffer.Reset();
                int16_t val = 200;
                buffer.Append(&val, sizeof(val));
                conn->Send(&buffer);
            }
            else
            {
                std::string str(msg->data(), msg->data() + msg->size());
                conn->Send(msg);
            }
        });
    server.SetConnectionCallback([](const evpp::TCPConnPtr& conn) {
        if (conn->IsConnected()) {
            //LOG_INFO << "nc" << conn->remote_addr();
        }
        else {
            //LOG_INFO << "lc" << conn->remote_addr();
        }
        });
    server.Init();
    server.Start();
    loop.Run();
}