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
    int thread_num = 4;
    evpp::EventLoop loop;
    evpp::TCPServer server(&loop, addr, "TCPEchoServer", thread_num);
    server.SetMessageCallback([](const evpp::TCPConnPtr& conn,
        evpp::Buffer* msg) {
            std::string str(msg->data(), msg->data() + msg->size());
            
            LOG_INFO << str << conn->remote_addr();
            conn->Send(msg);
        });
    server.SetConnectionCallback([](const evpp::TCPConnPtr& conn) {
        if (conn->IsConnected()) {
            LOG_INFO << "nc" << conn->remote_addr();
        }
        else {
            LOG_INFO << "lc" << conn->remote_addr();
        }
        });
    server.Init();
    server.Start();
    loop.Run();
}