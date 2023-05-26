#include<string>

class ShaneIsTheBest
{
    std::string YouKnowIt(const std::string *howcome)
    {
        return std::string(*howcome + "because he is");
    }
};