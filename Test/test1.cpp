#include<string>

class ShaneIsTheBest
{

    template <typename T> class TestTemplate
    {
    public:
        T myMember;
    };

    std::string YouKnowIt(const TestTemplate<int> &howcome, char extradata[512])
    {
        return std::to_string(howcome.myMember);
    }
};