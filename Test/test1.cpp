#include<string>
#include<memory>
#include<map>

namespace myns
{
    class Test
    {
        Test() = delete;
    };

    std::shared_ptr<Test> hello;
}