#include "Precomp.h"
#include "CPPSourceFile.h"
#include "DbMgr.h"
#include "Node.h"
#include "cppstream.h"
#include "zlib.h"

bool g_fullDbRebuild = false;
bool g_doOptimizeOnStart = false;

template <class _InIt>
inline size_t HashFunc(_InIt _Begin, _InIt _End, size_t _Val = 2166136261U)
{	// hash range of elements;

    while (_Begin != _End)
        _Val = 16777619U * _Val ^ (size_t)*_Begin++;
    return (_Val);
}

template<typename T, typename U> constexpr size_t offsetOf(U T::* member)
{
    return (char*)&((T*)nullptr->*member) - (char*)nullptr;
}

size_t DbNode::GetHashVal(size_t parentHashVal) const
{
    size_t* bgn = (size_t*)&kind;
    size_t* end = (size_t*)(((int64_t*)&sourceFile) + 1);
    return HashFunc(bgn, end, parentHashVal);
}


bool DbNode::operator == (const DbNode& other) const
{
    return kind == other.kind &&
        typeIdx == other.typeIdx &&
        token == other.token &&
        line == other.line &&
        column == other.column &&
        startOffset == other.startOffset &&
        endOffset == other.endOffset &&
        flags == other.flags &&
        sourceFile == other.sourceFile;
}

CPPSourceFilePtr DbFile::GetOrInsertFile(const std::string& commitName, const std::string& fileName)
{
    CPPSourceFilePtr sf = nullptr;
    {
        auto itSrcFile = m_sourceFiles.find(commitName);
        if (itSrcFile == m_sourceFiles.end())
        {
            if (fileName.empty())
                return nullptr;

            sf = new CPPSourceFile(fileName);
            m_sourceFiles[commitName] = sf;
        }
        else
            sf = itSrcFile->second;
    }
    return sf;
}

DbNode::DbNode(const Node& n) :
    key(n.Key),
    compilingFile(n.CompilingFile->Key),
    parentNodeIdx(n.ParentNodeIdx),
    referencedIdx(n.ReferencedIdx),
    kind(n.Kind),
    typeIdx(n.TypeIdx),
    token(n.token),
    line(n.Line),
    column(n.Column),
    startOffset(n.StartOffset),
    endOffset(n.EndOffset),
    // Bits (0-3) - AccessSpecifier, 4 - IsAbstract, (5-8) - StorageClass, 9 - IsDeleted
    flags(((n.AcessSpecifier) & 0x03) | (n.isAbstract ? 4 : 0) | (n.StorageClass << 3) |
        (n.isDeleted ? (1 << 9) : 0)),
    sourceFile(n.SourceFile != nullptr ? n.SourceFile->Key : nullnode)
{

}

void DbFile::AddNodes(std::vector<Node>& nodes)
{
    size_t idx = 0;
    for (Node& t : nodes)
    {
        t.Key = m_dbNodes.size();
        t.ParentNodeIdx = t.ParentNodeIdx;
        t.ReferencedIdx = t.ReferencedIdx;
        m_dbNodes.push_back(DbNode(t));
    }

    RemoveDuplicates();
}


DbFile::DbFile()
{
}


void DbFile::UpdateRow(CPPSourceFilePtr node)
{

}

void DbFile::AddRowsPtr(std::vector<ErrorPtr>& range)
{

}

int64_t DbFile::AddRows(std::vector<TypeNode>& types)
{
    size_t keyIdx = 0;
    
    m_dbTypes.reserve(m_dbTypes.size() + types.size());
    for (TypeNode& typ : types)
    {
        std::vector<int64_t> children;
        children.reserve(typ.children.size());
        for (auto& child : typ.children)
        {
            children.push_back(child.idx);
        }
        m_dbTypes.push_back(DbType(typ.Key, typ.hash, children, typ.tokenIdx, typ.TypeKind, typ.isConst));
    }
    return 0;
}

int64_t DbFile::AddRows(std::vector<Token>& range)
{
    size_t keyIdx = 0;
    m_dbTokens.reserve(m_dbTokens.size() + range.size());
    for (Token& token : range)
    {
        m_dbTokens.push_back(DbToken(keyIdx++, token.Text));
    }
    return 0;
}

void DbFile::WriteStream(std::vector<uint8_t>& data)
{
    static_assert(80 == sizeof(DbNode));
    CppVecStreamWriter vecWriter(data);
    CppStream::Write(vecWriter, m_dbSourceFiles);
    CppStream::Write(vecWriter, m_dbTokens);
    CppStream::Write(vecWriter, m_dbTypes);
    CppStream::Write(vecWriter, m_dbNodes);
}

void DbFile::CommitSourceFiles()
{
    int64_t maxFileKey = 0;
    for (const auto& kv : m_sourceFiles)
    {
        maxFileKey = std::max(kv.second->Key, maxFileKey);
    }
    m_dbSourceFiles.resize(maxFileKey);

    for (const auto& kv : m_sourceFiles)
    {
        m_dbSourceFiles[kv.second->Key - 1] = kv.second->FullPath;
    }
}

void DbFile::Save(const std::string& dbfile)
{
    std::vector<uint8_t> data;
    WriteStream(data);
    // Decoded data size (in bytes).
    const uLongf decodedCnt = (uLongf)data.size();

    static_assert(sizeof(uLongf) == sizeof(uint32_t));
    std::vector<uint8_t> compressedData(data.size());
    // Encoded data size (in bytes).
    uLongf encodedCnt = (uLongf)compressedData.size();

    int compressStatus = compress2(
        reinterpret_cast<Bytef*>(compressedData.data()),
        &encodedCnt,
        reinterpret_cast<const Bytef*>(data.data()),
        decodedCnt,
        Z_DEFAULT_COMPRESSION);

    std::ofstream ofstream(dbfile, std::ios::out | std::ios::binary);
    ofstream.write((const char*)&decodedCnt, sizeof(uint32_t));
    ofstream.write((const char*)compressedData.data(), encodedCnt);
    ofstream.close();
}

static std::unordered_map<CXCursorKind, std::string> sCursorKindMap
{
    { CXCursor_UnexposedDecl, "UnexposedDecl" },
    { CXCursor_StructDecl, "StructDecl" },
    { CXCursor_UnionDecl, "UnionDecl" },
    { CXCursor_ClassDecl, "ClassDecl" },
    { CXCursor_EnumDecl, "EnumDecl" },
    { CXCursor_FieldDecl, "FieldDecl" },
    { CXCursor_EnumConstantDecl, "EnumConstantDecl" },
    { CXCursor_FunctionDecl, "FunctionDecl" },
    { CXCursor_VarDecl, "VarDecl" },
    { CXCursor_ParmDecl, "ParmDecl" },
    { CXCursor_TypedefDecl, "TypedefDecl" },
    { CXCursor_CXXMethod, "CXXMethod" },
    { CXCursor_Namespace, "Namespace" },
    { CXCursor_Constructor, "Constructor" },
    { CXCursor_Destructor, "Destructor" },
    { CXCursor_ConversionFunction, "ConversionFunction" },
    { CXCursor_TemplateTypeParameter, "TemplateTypeParameter" },
    { CXCursor_NonTypeTemplateParameter, "NonTypeTemplateParameter" },
    { CXCursor_TemplateTemplateParameter, "TemplateTemplateParameter" },
    { CXCursor_FunctionTemplate, "FunctionTemplate" },
    { CXCursor_ClassTemplate, "ClassTemplate" },
    { CXCursor_ClassTemplatePartialSpecialization, "ClassTemplatePartialSpecialization" },
    { CXCursor_NamespaceAlias, "NamespaceAlias" },
    { CXCursor_UsingDirective, "UsingDirective" },
    { CXCursor_UsingDeclaration, "UsingDeclaration" },
    { CXCursor_TypeAliasDecl, "TypeAliasDecl" },
    { CXCursor_CXXAccessSpecifier, "CXXAccessSpecifier" },
    { CXCursor_TypeRef, "TypeRef" },
    { CXCursor_CXXBaseSpecifier, "CXXBaseSpecifier" },
    { CXCursor_TemplateRef, "TemplateRef" },
    { CXCursor_NamespaceRef, "NamespaceRef" },
    { CXCursor_MemberRef, "MemberRef" },
    { CXCursor_OverloadedDeclRef, "OverloadedDeclRef" },
    { CXCursor_VariableRef, "VariableRef" },
    { CXCursor_FirstExpr, "FirstExpr" },
    { CXCursor_DeclRefExpr, "DeclRefExpr" },
    { CXCursor_MemberRefExpr, "MemberRefExpr" },
    { CXCursor_CallExpr, "CallExpr" },
    { CXCursor_IntegerLiteral, "IntegerLiteral" },
    { CXCursor_FloatingLiteral, "FloatingLiteral" },
    { CXCursor_StringLiteral, "StringLiteral" },
    { CXCursor_CharacterLiteral, "CharacterLiteral" },
    { CXCursor_ParenExpr, "ParenExpr" },
    { CXCursor_UnaryOperator, "UnaryOperator" },
    { CXCursor_ArraySubscriptExpr, "ArraySubscriptExpr" },
    { CXCursor_BinaryOperator, "BinaryOperator" },
    { CXCursor_CompoundAssignOperator, "CompoundAssignOperator" },
    { CXCursor_ConditionalOperator, "ConditionalOperator" },
    { CXCursor_CStyleCastExpr, "CStyleCastExpr" },
    { CXCursor_InitListExpr, "InitListExpr" },
    { CXCursor_CXXStaticCastExpr, "CXXStaticCastExpr" },
    { CXCursor_CXXDynamicCastExpr, "CXXDynamicCastExpr" },
    { CXCursor_CXXReinterpretCastExpr, "CXXReinterpretCastExpr" },
    { CXCursor_CXXConstCastExpr, "CXXConstCastExpr" },
    { CXCursor_CXXFunctionalCastExpr, "CXXFunctionalCastExpr" },
    { CXCursor_CXXTypeidExpr, "CXXTypeidExpr" },
    { CXCursor_CXXBoolLiteralExpr, "CXXBoolLiteralExpr" },
    { CXCursor_CXXNullPtrLiteralExpr, "CXXNullPtrLiteralExpr" },
    { CXCursor_CXXThisExpr, "CXXThisExpr" },
    { CXCursor_CXXThrowExpr, "CXXThrowExpr" },
    { CXCursor_CXXNewExpr, "CXXNewExpr" },
    { CXCursor_CXXDeleteExpr, "CXXDeleteExpr" },
    { CXCursor_UnaryExpr, "UnaryExpr" },
    { CXCursor_PackExpansionExpr, "PackExpansionExpr" },
    { CXCursor_SizeOfPackExpr, "SizeOfPackExpr" },
    { CXCursor_LambdaExpr, "LambdaExpr" },
    { CXCursor_ConceptSpecializationExpr, "ConceptSpecializationExpr" },
    { CXCursor_RequiresExpr, "RequiresExpr" },
    { CXCursor_FirstStmt, "FirstStmt" },
    { CXCursor_CompoundStmt, "CompoundStmt" },
    { CXCursor_CaseStmt, "CaseStmt" },
    { CXCursor_DefaultStmt, "DefaultStmt" },
    { CXCursor_IfStmt, "IfStmt" },
    { CXCursor_SwitchStmt, "SwitchStmt" },
    { CXCursor_WhileStmt, "WhileStmt" },
    { CXCursor_DoStmt, "DoStmt" },
    { CXCursor_ForStmt, "ForStmt" },
    { CXCursor_ContinueStmt, "ContinueStmt" },
    { CXCursor_BreakStmt, "BreakStmt" },
    { CXCursor_ReturnStmt, "ReturnStmt" },
    { CXCursor_CXXCatchStmt, "CXXCatchStmt" },
    { CXCursor_CXXTryStmt, "CXXTryStmt" },
    { CXCursor_CXXForRangeStmt, "CXXForRangeStmt" },
    { CXCursor_NullStmt, "NullStmt" },
    { CXCursor_DeclStmt, "DeclStmt" },
    { CXCursor_BuiltinBitCastExpr, "BuiltinBitCastExpr" },
    { CXCursor_FirstAttr, "FirstAttr" },
    { CXCursor_CXXFinalAttr, "CXXFinalAttr" },
    { CXCursor_CXXOverrideAttr, "CXXOverrideAttr" },
    { CXCursor_DLLImport, "DLLImport" },
    { CXCursor_WarnUnusedResultAttr, "WarnUnusedResultAttr" },
    { CXCursor_AlignedAttr, "AlignedAttr" },
    { CXCursor_TypeAliasTemplateDecl, "TypeAliasTemplateDecl" },
    { CXCursor_StaticAssert, "StaticAssert" },
    { CXCursor_FriendDecl, "FriendDecl" },
    { CXCursor_ConceptDecl, "ConceptDecl" }
};

static std::unordered_map<CXTypeKind, std::string> sTypeKindMap
{
{ CXType_Invalid, "Invalid" },
{ CXType_Unexposed, "Unexposed" },
{ CXType_Void, "Void" },
{ CXType_Bool, "Bool" },
{ CXType_UChar, "UChar" },
{ CXType_Char16, "Char16" },
{ CXType_Char32, "Char32" },
{ CXType_UShort, "UShort" },
{ CXType_UInt, "UInt" },
{ CXType_ULong, "ULong" },
{ CXType_ULongLong, "ULongLong" },
{ CXType_Char_S, "Char_S" },
{ CXType_SChar, "SChar" },
{ CXType_WChar, "WChar" },
{ CXType_Short, "Short" },
{ CXType_Int, "Int" },
{ CXType_Long, "Long" },
{ CXType_LongLong, "LongLong" },
{ CXType_Float, "Float" },
{ CXType_Double, "Double" },
{ CXType_LongDouble, "LongDouble" },
{ CXType_NullPtr, "NullPtr" },
{ CXType_Overload, "Overload" },
{ CXType_Dependent, "Dependent" },
{ CXType_Pointer, "Pointer" },
{ CXType_LValueReference, "LValueReference" },
{ CXType_RValueReference, "RValueReference" },
{ CXType_Record, "Record" },
{ CXType_Enum, "Enum" },
{ CXType_Typedef, "Typedef" },
{ CXType_FunctionProto, "FunctionProto" },
{ CXType_ConstantArray, "ConstantArray" },
{ CXType_IncompleteArray, "IncompleteArray" },
{ CXType_DependentSizedArray, "DependentSizedArray" },
{ CXType_MemberPointer, "MemberPointer" },
{ CXType_Auto, "Auto" },
{ CXType_Elaborated, "Elaborated" }
};

void DbFile::Load(const std::string& dbfile)
{
    std::ifstream ifstream(dbfile, std::ios::in | std::ios::binary);
    ifstream.seekg(0, std::ios::end);
    size_t size = ifstream.tellg();
    std::vector<uint8_t> compressedData(size - sizeof(uint32_t));
    ifstream.seekg(0);

    uLongf decodedCnt;
    ifstream.read((char*)&decodedCnt, sizeof(uint32_t));
    ifstream.read((char*)compressedData.data(), size - sizeof(uint32_t));

    std::vector<uint8_t> data(decodedCnt);

    int decompressStatus = uncompress(
        reinterpret_cast<Bytef*>(data.data()),
        &decodedCnt,
        reinterpret_cast<const Bytef*>(compressedData.data()),
        compressedData.size());

    CppVecStreamReader vecReader(data);
    size_t offset = 0;
    offset = CppStream::Read(vecReader, offset, m_dbSourceFiles);
    offset = CppStream::Read(vecReader, offset, m_dbTokens);
    offset = CppStream::Read(vecReader, offset, m_dbTypes);
    offset = CppStream::Read(vecReader, offset, m_dbNodes);
}

void DbFile::ConsoleDump()
{
}

inline void SetNodeHash(std::vector<size_t>& nodeHashes, const std::vector<DbNode>& dbNodes, int64_t nodeIdx)
{
    const DbNode& nodeCur = dbNodes[nodeIdx];

    size_t parenthash = 0;
    if (nodeCur.parentNodeIdx != nullnode)
    {
        parenthash = nodeHashes[nodeCur.parentNodeIdx];
        if (parenthash == 0)
            SetNodeHash(nodeHashes, dbNodes, nodeCur.parentNodeIdx);
        parenthash = nodeHashes[nodeCur.parentNodeIdx];
    }

    nodeHashes[nodeIdx] = nodeCur.GetHashVal(parenthash);
}

void DbFile::RemoveDuplicates()
{
    std::unordered_map<size_t, int64_t> nodesMap;
    nodesMap.reserve(m_dbNodes.size());
    std::vector<size_t> nodesTreeHash0(m_dbNodes.size());
    for (size_t idx = 0; idx < m_dbNodes.size(); ++idx)
    {
        SetNodeHash(nodesTreeHash0, m_dbNodes, idx);
    }

    std::vector<DbNode> newNodes;
    std::vector<int64_t> nodeRemapping(m_dbNodes.size());
    newNodes.reserve(m_dbNodes.size());
    for (size_t idx = 0; idx < m_dbNodes.size(); ++idx)
    {
        DbNode& nodeCur = m_dbNodes[idx];
        if (nodeCur.parentNodeIdx != nullnode && nodeCur.parentNodeIdx >= idx)
            __debugbreak();
        size_t hash_val = nodesTreeHash0[idx];
        /*
        if (nodeCur.referencedIdx != nullnode)
        {
            size_t hash0 = nodesTreeHash0[nodeCur.referencedIdx];
            hash_val = HashFunc(&hash0, &hash0 + 1, nodesTreeHash0[idx]);
        }*/

        auto itNode = nodesMap.find(hash_val);
        if (itNode == nodesMap.end())
        {
            nodesMap.insert(std::make_pair(hash_val, newNodes.size()));
            nodeRemapping[idx] = newNodes.size();
            newNodes.push_back(nodeCur);
        }
        else
        {
            nodeRemapping[idx] = itNode->second;
            if (nodeCur.referencedIdx != nullnode &&
                newNodes[nodeRemapping[itNode->second]].referencedIdx == nullnode &&
                nodeCur.referencedIdx != itNode->second)
            {
                newNodes[nodeRemapping[itNode->second]].referencedIdx = nodeCur.referencedIdx;
            }
        }
    }

    for (size_t idx = 0; idx < newNodes.size(); ++idx)
    {
        DbNode& nodeCur = newNodes[idx];
        if (nodeCur.parentNodeIdx != nullnode)
        {
            size_t remapped = nodeRemapping[nodeCur.parentNodeIdx];
            if (remapped >= idx)
                throw;
            nodeCur.parentNodeIdx = remapped;
        }
        if (nodeCur.referencedIdx != nullnode)
            nodeCur.referencedIdx = nodeRemapping[nodeCur.referencedIdx];
    }
    m_dbNodes = newNodes;
}

void DbFile::Merge(const DbFile& other)
{
    std::vector<int64_t> srcFileRemapping;
    std::unordered_map<std::string, int64_t> sourceMap;
    {
        int64_t srcIdx = 1;
        for (const auto& srcfile : m_dbSourceFiles)
        {
            sourceMap.insert(std::make_pair(srcfile, srcIdx++));
        }

        srcFileRemapping.push_back(0);
        for (const auto& srcfile : other.m_dbSourceFiles)
        {
            auto itSrc = sourceMap.find(srcfile);
            if (itSrc == sourceMap.end())
            {
                itSrc = sourceMap.insert(std::make_pair(srcfile, srcIdx++)).first;
                m_dbSourceFiles.push_back(srcfile);
            }

            srcFileRemapping.push_back(itSrc->second);
        }
    }

    std::vector<int64_t> tokenRemapping;
    std::vector<int64_t> typeRemapping;

    std::unordered_map<std::string, int64_t> tokenMap;
    tokenMap.reserve(m_dbTokens.size());
    {
        int64_t tokIdx = 0;
        for (const auto& token : m_dbTokens)
        {
            tokenMap.insert(std::make_pair(token.text, tokIdx++));
        }

        for (const auto& token : other.m_dbTokens)
        {
            auto itTok = tokenMap.find(token.text);
            if (itTok == tokenMap.end())
            {
                itTok = tokenMap.insert(std::make_pair(token.text, tokIdx++)).first;
                DbToken t = token;
                t.key = m_dbTokens.size();
                m_dbTokens.push_back(t);
            }

            tokenRemapping.push_back(itTok->second);
        }
    }


    // DbType merging and remapping
    {
        std::unordered_map<int64_t, int64_t> uidTypeMap;
        uidTypeMap.reserve(m_dbTypes.size());
        size_t typeIdx = 0;
        for (auto& ctype : m_dbTypes)
        {
            if (ctype.hash == 0)
                continue;
            auto ittok = uidTypeMap.find(ctype.hash);
            if (ittok != uidTypeMap.end())
                dbgbreak();
            uidTypeMap.insert(std::make_pair(ctype.hash, typeIdx));
            typeIdx++;
        }
        typeIdx = 0;
        typeRemapping.resize(other.m_dbTypes.size());
        for (auto& otype : other.m_dbTypes)
        {
            int64_t tokenIdx = tokenRemapping[otype.token];
            auto itFoundType = uidTypeMap.find(otype.hash);
            if (itFoundType == uidTypeMap.end())
            {
                DbType tn = otype;
                tn.key = m_dbTypes.size();
                tn.token = tokenIdx;
                for (int64_t &child : tn.children)
                {
                    child = typeRemapping[child];
                }
                uidTypeMap.insert(std::make_pair(tn.hash, tn.key));
                m_dbTypes.push_back(tn);
                typeRemapping[typeIdx] = tn.key;
            }
            else
                typeRemapping[typeIdx] = itFoundType->second;
            typeIdx++;
        }
    }
    
    std::vector<DbNode> otherNodes = other.m_dbNodes;
    {
        size_t idx = 0;
        for (const DbNode& node : otherNodes)
        {
            if (node.referencedIdx == idx)
                __debugbreak();
            idx++;
        }
    }
    for (auto& dbNode : otherNodes)
    {
        dbNode.compilingFile = srcFileRemapping[dbNode.compilingFile];
        dbNode.sourceFile = dbNode.sourceFile != nullnode ? 
            srcFileRemapping[dbNode.sourceFile] : nullnode;
        int64_t oldToken = dbNode.token;
        int64_t typeTok = dbNode.typeIdx;
        dbNode.token = tokenRemapping[dbNode.token];

        dbNode.typeIdx = dbNode.typeIdx != nullnode ? 
            typeRemapping[dbNode.typeIdx] : nullnode;
        if (dbNode.typeIdx >= (int64_t)m_dbTypes.size())
            throw;
    }

    for (size_t idx = 0; idx < m_dbNodes.size(); ++idx)
    {
        if (m_dbNodes[idx].parentNodeIdx != nullnode &&
            m_dbNodes[idx].parentNodeIdx >= idx)
        {
            throw;
        }
    }

    std::unordered_map<size_t, int64_t> nodesMap;
    nodesMap.reserve(m_dbNodes.size());
    {
        std::vector<size_t> nodesTreeHash0(m_dbNodes.size());
        for (size_t idx = 0; idx < m_dbNodes.size(); ++idx)
        {
            SetNodeHash(nodesTreeHash0, m_dbNodes, idx);
        }

        for (size_t idx = 0; idx < m_dbNodes.size(); ++idx)
        {
            DbNode& nodeCur = m_dbNodes[idx];
            size_t hash_val = nodesTreeHash0[idx];
            if (nodeCur.referencedIdx != nullnode)
            {
                size_t hash0 = nodesTreeHash0[nodeCur.referencedIdx];
                hash_val = HashFunc(&hash0, &hash0 + 1, nodesTreeHash0[idx]);
            }

            auto itNode = nodesMap.find(hash_val);
            if (itNode == nodesMap.end())
                nodesMap.insert(std::make_pair(hash_val, idx));
        }
    }
    {
        std::vector<size_t> nodesTreeHash0(otherNodes.size());
        std::vector<size_t> nodesTreeHash1(otherNodes.size());
        for (size_t idx = 0; idx < otherNodes.size(); ++idx)
        {
            SetNodeHash(nodesTreeHash0, otherNodes, idx);
        }

        for (size_t idx = 0; idx < otherNodes.size(); ++idx)
        {
            DbNode& nodeCur = otherNodes[idx];
            size_t hash_val = nodesTreeHash0[idx];
//            if (idx == 369906)
                //__debugbreak();
            if (nodeCur.referencedIdx != nullnode)
            {
                size_t hash0 = nodesTreeHash0[nodeCur.referencedIdx];
                hash_val = HashFunc(&hash0, &hash0 + 1, nodesTreeHash0[idx]);
            }
            nodesTreeHash1[idx] = hash_val;
            auto itNode = nodesMap.find(hash_val);
            if (itNode == nodesMap.end())
            {
                DbNode newNode = otherNodes[idx];
                if (newNode.parentNodeIdx != nullnode)
                {
                    size_t parentHash = nodesTreeHash1[newNode.parentNodeIdx];
                    auto itparent = nodesMap.find(parentHash);
                    newNode.parentNodeIdx = itparent->second;
                }
                if (newNode.referencedIdx != nullnode)
                {
                    size_t refHash = nodesTreeHash1[newNode.referencedIdx];
                    newNode.referencedIdx = nodesMap[refHash];
                }
                size_t newIdx = m_dbNodes.size();
                m_dbNodes.push_back(newNode);
                nodesMap.insert(std::make_pair(hash_val, newIdx));
            }
        }
    }

    size_t nullnodes = 0;
    for (DbNode& node : m_dbNodes)
    {
        if (node.parentNodeIdx == nullnode)
            nullnodes++;
    }
    RemoveDuplicates();
}

inline std::string tolower(const std::string& src)
{
    std::string data = src;
    std::transform(data.begin(), data.end(), data.begin(),
        [](unsigned char c) { return std::tolower(c); });
    return data;
}

size_t DbFile::QueryNodes(const std::string& filename)
{
    std::string fnlower = tolower(filename);
    for (auto& src : m_dbSourceFiles)
    {
        if (tolower(src) == fnlower)
        {
            return 1;
        }
    }
    return 0;
}