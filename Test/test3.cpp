
class ClassTest
{
public:
    int GetStringLength(char* test);
};



int ClassTest::GetStringLength(char* test)
{
    int idx = 0;
    while (*test != nullptr)
    {
        idx++;
        test++;
    }
    return idx;
}