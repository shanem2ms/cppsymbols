#include "Precomp.h"
#include "CPPSourceFile.h"
#include "DbMgr.h"
#include "Node.h"
#include "cppstream.h"
#include "zlib.h"

bool g_fullDbRebuild = false;
bool g_doOptimizeOnStart = false;

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

DbNode::DbNode(const Node &n) :
    key(n.Key),
    compilingFile(n.CompilingFile->Key),
    parentNodeIdx(n.ParentNodeIdx),
    referencedIdx(n.ReferencedIdx),
    kind(n.Kind),
    typeKind(n.TypeKind),
    token(n.token),
    typetoken(n.TypeToken),
    line(n.Line),
    column(n.Column),
    startOffset(n.StartOffset),
    endOffset(n.EndOffset),
    sourceFile(n.SourceFile->Key)
{

}
void DbFile::AddNodes(std::vector<Node>& nodes)
{
    size_t idx = 0;
    for (Node& t : nodes)
    {
        if (t.SourceFile == nullptr)
        {
            idx++;
            continue;
        }
        t.Key = m_dbNodes.size();
        t.ParentNodeIdx = (t.ParentNodeIdx != nullnode) ? t.ParentNodeIdx : 0;
        t.ReferencedIdx = (t.ReferencedIdx != nullnode) ? t.ReferencedIdx : 0;
        m_dbNodes.push_back(DbNode(t));
    }    
}


DbFile::DbFile(const std::string& dbfile) :
    m_dbfile(dbfile)
{
}


void DbFile::UpdateRow(CPPSourceFilePtr node)
{

}

void DbFile::AddRowsPtr(std::vector<ErrorPtr>& range)
{

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

void DbFile::Save()
{
    std::vector<uint8_t> data;
    int64_t maxFileKey = 0;
    for (const auto& kv : m_sourceFiles)
    {
        maxFileKey = std::max(kv.second->Key, maxFileKey);
    }
    std::vector<std::string> orderedSourceFiles(maxFileKey);

    for (const auto& kv : m_sourceFiles)
    {
        orderedSourceFiles[kv.second->Key - 1] = kv.second->FullPath;
    }
    CppVecStreamWriter vecWriter(data);
    CppStream::Write(vecWriter, orderedSourceFiles);
    CppStream::Write(vecWriter, m_dbTokens);
    CppStream::Write(vecWriter, m_dbNodes);

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

    std::ofstream ofstream(m_dbfile, std::ios::out | std::ios::binary);
    ofstream.write((const char*)&decodedCnt, sizeof(uint32_t));
    ofstream.write((const char*)compressedData.data(), encodedCnt);
    ofstream.close();
}

static std::map<CXCursorKind, std::string> sCursorKindMap
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

static std::map<CXTypeKind, std::string> sTypeKindMap
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

void DbFile::Load()
{
    std::ifstream ifstream(m_dbfile, std::ios::in | std::ios::binary);
    ifstream.seekg(0, std::ios::end);
    size_t size = ifstream.tellg();
    std::vector<uint8_t> compressedData(size - sizeof(uint32_t));
    ifstream.seekg(0);

    uLongf decodedCnt;
    ifstream.read((char*)&decodedCnt, sizeof(uint32_t));
    ifstream.read((char *)compressedData.data(), size - sizeof(uint32_t));

    std::vector<uint8_t> data(decodedCnt);

    int decompressStatus = uncompress(
        reinterpret_cast<Bytef*>(data.data()),
        &decodedCnt,
        reinterpret_cast<const Bytef*>(compressedData.data()),
        compressedData.size());

    std::vector<std::string> orderedSourceFiles;
    CppVecStreamReader vecReader(data);
    size_t offset = 0;
    offset = CppStream::Read(vecReader, offset, orderedSourceFiles);
    offset = CppStream::Read(vecReader, offset, m_dbTokens);
    offset = CppStream::Read(vecReader, offset, m_dbNodes);

//    std::set<CXCursorKind> cursorKinds;
    std::set<CXTypeKind> typeKinds;
    for (const auto& node : m_dbNodes)
    {
//        cursorKinds.insert(node.kind);
        typeKinds.insert(node.typeKind);
        if (node.sourceFile == 1)
        {
            std::cout << node.line << " " << sCursorKindMap[node.kind] << " " 
                << sTypeKindMap[node.typeKind] << std::endl;
        }
    }
}