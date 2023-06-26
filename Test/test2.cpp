#include<string>
#include<vector>

int SimpleIntReturn()
{
    return 5;
}

class ShaneIsTheBest
{

    template <typename T> class TestTemplate
    {
    public:
        T myMember;
    };

    std::vector<uint32_t> YouKnowIt(TestTemplate<int> howcome)
    {
        std::vector<uint32_t> myvec;
        return myvec;
    }


    int SimpleIntReturn()
    {
        return 5;
    }

public:
    int testParam;


    static bool IsThis100OrBigger(int val)
    {
        return val > 100;
    }
};