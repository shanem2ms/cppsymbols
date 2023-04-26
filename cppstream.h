/// Supported: VS, Linux
////////////////////////////////////////////////////////////////////////////////////////////////////
//  COPYRIGHT 2018 by WSI Corporation
////////////////////////////////////////////////////////////////////////////////////////////////////
/// \author Shane Morrison
#pragma once
#include <type_traits>

inline void CppStreamDbgError()
{
}

class ICppStreamPos
{
public:
    virtual size_t GetPos() const = 0;
    virtual void SetPos(size_t) = 0;
};

class ICppStreamWriter : public ICppStreamPos
{
public:
    virtual void AppendBytes(const uint8_t* pBegin, size_t len) = 0;
};

class ICppStreamReader : public ICppStreamPos
{
public:
    virtual void ReadBytes(uint8_t* pOutBytes, size_t count) const = 0;
};

class CppStreamable
{
public:
    virtual void WriteBinaryData(ICppStreamWriter& data, void* pUserContext) const = 0;
    virtual size_t ReadBinaryData(const ICppStreamReader& data, size_t offset, void* pUserContext) = 0;
protected:
};


class CppVecStreamReader : public ICppStreamReader
{
    const std::vector<uint8_t>& m_vec;
    mutable size_t m_offset;
public:

    CppVecStreamReader(const std::vector<uint8_t>& vec, size_t offset = 0) :
        m_vec(vec),
        m_offset(offset) {}

    void ReadBytes(uint8_t* pOutBytes, size_t count) const override
    {
        memcpy(pOutBytes, &m_vec[m_offset], count);
        m_offset += count;
    }
    size_t GetPos() const override
    {
        return m_offset;
    }
    virtual void SetPos(size_t offset) override
    {
        m_offset = offset;
    }
};

class CppVecStreamWriter : public ICppStreamWriter
{
    std::vector<uint8_t>& m_vec;
    size_t m_offset;
public:

    CppVecStreamWriter(std::vector<uint8_t>& vec) :
        m_vec(vec),
        m_offset(0) {}

    void AppendBytes(const uint8_t* pBegin, size_t len) override
    {
        m_vec.insert(m_vec.end(), pBegin, pBegin + len);
    }

    size_t GetPos() const override
    {
        return m_offset;
    }
    virtual void SetPos(size_t offset) override
    {
        m_offset = offset;
    }
};


class CppFile;
class CppFileStreamWriter : public ICppStreamWriter
{
    std::shared_ptr<std::fstream> m_rFile;
public:

    void AppendBytes(const uint8_t* pBegin, size_t len) override;

    CppFileStreamWriter(std::shared_ptr<std::fstream> rFile) :
        m_rFile(rFile)
    {}

    void SetPos(size_t offset);

    size_t GetPos() const;
};


class CppFileStreamReader : public ICppStreamReader
{
    std::shared_ptr<std::fstream> m_rFile;
    mutable size_t m_bufferOffset;
    mutable std::vector<uint8_t> m_buffer;
    mutable size_t m_offset;
    static inline const size_t sBufferBytes = 1 << 20;


    void BufferFile() const;
public:

    void ReadBytes(uint8_t* pOutBytes, size_t count) const override
    {
        if (m_offset < m_bufferOffset)
            BufferFile();

        if (m_offset - m_bufferOffset + count > sBufferBytes)
        {
            int64_t bytesFromCurBuffer = sBufferBytes - (m_offset - m_bufferOffset);
            if (m_offset - m_bufferOffset < sBufferBytes)
            {
                memcpy(pOutBytes, &m_buffer[sBufferBytes - bytesFromCurBuffer], bytesFromCurBuffer);
                m_offset += bytesFromCurBuffer;
                pOutBytes += bytesFromCurBuffer;
                count -= bytesFromCurBuffer;
            }
            BufferFile();
        }

        memcpy(pOutBytes, &m_buffer[m_offset - m_bufferOffset],
            count);
        m_offset += count;
    }

    CppFileStreamReader(std::shared_ptr<std::fstream> rFile) :
        CppFileStreamReader()
    {
        SetFile(rFile);
    }
    CppFileStreamReader() :
        m_offset(0),
        m_bufferOffset(0),
        m_buffer(sBufferBytes)
    {
    }

    void SetFile(std::shared_ptr<std::fstream> rFile)
    {
        m_offset = 0;
        m_bufferOffset = 0;
        m_rFile = rFile;
        BufferFile();
    }

    void SetPos(size_t offset)
    {
        m_offset = offset;
    }

    size_t GetPos() const
    {
        return m_offset;
    }
};

template <typename T> class Cppshallowvec;
namespace CppStream
{

    inline void AppendBytes(ICppStreamWriter& data, const uint8_t* pBegin, const uint8_t* pEnd)
    {
        data.AppendBytes(pBegin, pEnd - pBegin);
    }

    inline void AppendBytes(std::vector<uint8_t>& data, const uint8_t* pBegin, const uint8_t* pEnd)
    {
        data.insert(data.end(), pBegin, pEnd);
    }

    inline void WriteString(ICppStreamWriter& data, const std::string& string)
    {
        uint16_t length = string.size();
        AppendBytes(data, (uint8_t*)&length, ((uint8_t*)&length) + sizeof(uint16_t));
        AppendBytes(data, (uint8_t*)string.c_str(), (uint8_t*)string.c_str() + length);
    }

    inline size_t DataSizeOf(const std::string& string)
    {
        return sizeof(uint16_t) + string.size();
    }

    template<typename T> void Write(ICppStreamWriter& data, const T& val)
    {
        AppendBytes(data, (uint8_t*)&val, ((uint8_t*)&val) + sizeof(T));
    }

    inline void Write(ICppStreamWriter& data, const std::string& val)
    {
        WriteString(data, val);
    }

    template<typename T> void Write(ICppStreamWriter& data, const T& val, void* pUserContext);
    template<typename T> void WritePtr(ICppStreamWriter& data, const T* pObj, void* pUserContext);
    template<typename T> void Write(ICppStreamWriter& data, const std::shared_ptr<T>& val, void* pUserContext);
    template<typename T> void Write(ICppStreamWriter& data, const std::vector<T>& vec, void* pUserContext = nullptr);
    template<typename T, typename U> void Write(ICppStreamWriter& data, const std::map<T, U>& map, void* pUserContext = nullptr);
    template<typename T, size_t N> void WriteArray(ICppStreamWriter& data, const T val[], void* pUserContext);

    inline void WriteSerial(ICppStreamWriter& data, const CppStreamable* pObj, void* pUserContext)
    {
        pObj->WriteBinaryData(data, pUserContext);
    }

    template<typename T, typename U> void Write(ICppStreamWriter& data, const std::pair<T, U>& val,
        void* pUserContext)
    {
        Write(data, val.first, pUserContext);
        Write(data, val.second, pUserContext);
    }

    template<typename T> void Write(ICppStreamWriter& data, const T& val, void* pUserContext)
    {
#ifndef NOCppSTREAM // some projects don't have c++17 support
        if constexpr (std::is_base_of<CppStreamable, T>::value)
        {
            WriteSerial(data, &val, pUserContext);
        }
        else if constexpr (std::is_pointer<T>::value)
        {
            WritePtr(data, val, pUserContext);
        }
        else if constexpr (std::is_array<T>::value)
        {
            WriteArray<typename std::remove_all_extents<T>::type,
                std::extent<T>::value>(data, val, pUserContext);
        }
        else
            Write(data, val);
#endif
    }

    template<typename T> void WritePtr(ICppStreamWriter& data, const T* pObj, void* pUserContext)
    {
        bool isValid = pObj != nullptr;
        CppStream::Write(data, isValid);
        if (isValid)
        {
            Write(data, *pObj, pUserContext);
        }
    }

    template<typename T> void Write(ICppStreamWriter& data, const std::shared_ptr<T>& val, void* pUserContext)
    {
        bool isValid = val != nullptr;
        CppStream::Write(data, isValid);
        if (isValid)
        {
            Write(data, *val.GetPointer(), pUserContext);
        }
    }

    template<typename T> void Write(ICppStreamWriter& data, const std::vector<T>& vec, void* pUserContext)
    {
        Write(data, vec.size());
#ifndef NOCppSTREAM // some projects don't have c++17 support
        if constexpr (std::is_same<T, uint8_t>::value) // optimization for vector of bytes
        {
            AppendBytes(data, &vec[0], &vec[0] + vec.size());
        }
        else
        {
            for (size_t idx = 0; idx < vec.size(); ++idx)
            {
                const T& t = vec[idx];
                Write(data, t, pUserContext);
            }
        }
#endif
    }

    template<typename T, typename U> void Write(ICppStreamWriter& data, const std::map<T, U>& map, void* pUserContext)
    {
        Write(data, map.size());
        for (auto itMap = map.begin(); itMap != map.end(); ++itMap)
        {
            Write(data, itMap->first, pUserContext);
            Write(data, itMap->second, pUserContext);
        }
    }


    template<typename T, size_t N> void WriteArray(ICppStreamWriter& data, const T val[], void* pUserContext)
    {
        for (size_t idx = 0; idx < N; ++idx)
        {
            Write(data, val[idx], pUserContext);
        }
    }

    template<typename T> void Write(std::vector<uint8_t>& vec, T val)
    {
        CppVecStreamWriter vs(vec);
        Write(vs, val);
    }

    template<typename T> void WriteVectorCnt(ICppStreamWriter& data, const std::vector<T>& vec, size_t cnt, void* pUserContext)
    {
        Write(data, cnt);
        for (size_t idx = 0; idx < cnt; ++idx)
        {
            const T& t = vec[idx];
            Write(data, t, pUserContext);
        }
    }

    template<typename T> void WriteShallowVector(ICppStreamWriter& data, const Cppshallowvec<T>& vec)
    {
        Write(data, vec.size());
        for (const T& t : vec)
            Write(data, t);
    }

    inline size_t ReadString(const ICppStreamReader& data, size_t offset, std::string& string)
    {
        uint16_t len;
        data.ReadBytes((uint8_t*)&len, sizeof(len));
        std::vector<uint8_t> vec(len);
        if (len > 0)
        {
            data.ReadBytes(&vec[0], vec.size());
            string.assign((char*)&vec[0], (char*)&vec[0] + vec.size());
        }
        return sizeof(len) + vec.size();
    }

    template<typename T> size_t Read(const ICppStreamReader& data, size_t offset, T& val)
    {
        data.ReadBytes((uint8_t*)&val, sizeof(T));
        return offset + sizeof(T);
    }

    inline size_t Read(const ICppStreamReader& data, size_t offset, std::string& val)
    {
        return ReadString(data, offset, val);
    }

    inline size_t ReadSerial(const ICppStreamReader& data, size_t offset, CppStreamable* pObj, void* pUserContext)
    {
        offset = pObj->ReadBinaryData(data, offset, pUserContext);
        return offset;
    }

    template<typename T> size_t Read(const ICppStreamReader& data, size_t offset, T& val, void* pUserContext);
    template<typename T> size_t ReadPtr(const ICppStreamReader& data, size_t offset, T** pObj, void* pUserContext);
    template<typename T> size_t Read(const ICppStreamReader& data, size_t offset, std::shared_ptr<T>& val, void* pUserContext);
    template<typename T> size_t Read(const ICppStreamReader& data, size_t offset, std::vector<T>& vec, void* pUserContext = nullptr, size_t sizeLimit = 0);
    template<typename T, typename U> size_t Read(const ICppStreamReader& data, size_t offset, std::map<T, U>& map, void* pUserContext = nullptr);
    template<typename T, size_t N> size_t ReadArray(const ICppStreamReader& data, size_t offset, T val[], void* pUserContext);

    template<typename T> size_t Read(const ICppStreamReader& data, size_t offset, T& val, void* pUserContext)
    {
#ifndef NOCppSTREAM // some projects don't have c++17 support
        if constexpr (std::is_base_of<CppStreamable, T>::value)
        {
            offset = ReadSerial(data, offset, &val, pUserContext);
        }
        else if constexpr (std::is_pointer<T>::value)
        {
            offset = ReadPtr(data, offset, &val, pUserContext);
        }
        else if constexpr (std::is_array<T>::value)
        {
            offset = ReadArray<typename std::remove_all_extents<T>::type,
                std::extent<T>::value>(data, offset, val, pUserContext);
        }
        else
            offset = Read(data, offset, val);
#endif
        return offset;
    }

    template<typename T> size_t ReadPtr(const ICppStreamReader& data, size_t offset, T** pObj, void* pUserContext)
    {
        bool isValid = false;
        offset = Read(data, offset, isValid);
        if (isValid)
        {
            if constexpr (std::is_base_of<CppStreamable, T>::value)
                *pObj = T::CreateFromCppStream(data, offset, pUserContext);
            else
                offset = Read(data, offset, **pObj, pUserContext);
        }

        return offset;
    }
    template<typename T, typename U> size_t
        Read(const ICppStreamReader& data, size_t offset, std::pair<T, U>& pair, void* pUserContext)
    {
        offset = Read(data, offset, pair.first, pUserContext);
        offset = Read(data, offset, pair.second, pUserContext);
        return offset;
    }

    template<typename T> size_t Read(const ICppStreamReader& data, size_t offset, std::vector<T>& vec, void* pUserContext, size_t sizeLimit)
    {
        size_t vecSize;
        offset = Read(data, offset, vecSize);
        if (sizeLimit > 0 && vecSize > sizeLimit)
        {
            return 0;
        }
        vec.resize(vecSize);
        for (size_t idx = 0; idx < vecSize; ++idx)
            offset = Read(data, offset, vec[idx], pUserContext);
        return offset;
    }

    template<typename T> size_t Read(const ICppStreamReader& data, size_t offset, std::shared_ptr<T>& val, void* pUserContext)
    {
        bool isValid = false;
        offset = Read(data, offset, isValid);
#ifndef NOCppSTREAM // some projects don't have c++17 support
        if (isValid)
        {
            if constexpr (std::is_base_of<CppStreamable, T>::value)
                val = T::CreateFromCppStream(data, offset, pUserContext);
            else
                offset = Read(data, offset, *val.GetPointer(), pUserContext);
        }
#endif
        return offset;
    }

    template<typename T, typename U> size_t Read(const ICppStreamReader& data,
        size_t offset, std::map<T, U>& map, void* pUserContext)
    {
        size_t mapSize;
        offset = Read(data, offset, mapSize);
        for (size_t idx = 0; idx < mapSize; ++idx)
        {
            T key;
            offset = Read(data, offset, key, pUserContext);
            typename std::map<T, U>::iterator itNew = map.insert(std::make_pair(key, U())).first;
            offset = Read(data, offset, itNew->second, pUserContext);
        }

        return offset;
    }

    template<typename T, size_t N> size_t ReadArray(const ICppStreamReader& data, size_t offset, T val[], void* pUserContext)
    {
        for (size_t idx = 0; idx < N; ++idx)
        {
            offset = Read(data, offset, val[idx], pUserContext);
        }
        return offset;
    }


    template<typename T> size_t ReadShallowVector(const ICppStreamReader& data, size_t offset, Cppshallowvec<T>& vec)
    {
        size_t vecSize;
        offset = Read(data, offset, vecSize);
        vec.resize(vecSize);
        for (size_t idx = 0; idx < vecSize; ++idx)
            offset = Read(data, offset, vec[idx]);
        return offset;
    }

    inline void DbgWriteOffset(ICppStreamWriter& data)
    {
        size_t dbgOffset = data.GetPos();
        CppStream::Write(data, dbgOffset);
    }

    inline size_t DbgCheckOffset(const ICppStreamReader& data, size_t offset)
    {
        size_t dbgOffset, testOffset = offset;
        offset = CppStream::Read(data, offset, dbgOffset);
        if (dbgOffset != testOffset)
        {
            CppStreamDbgError();
        }
        return offset;
    }
};