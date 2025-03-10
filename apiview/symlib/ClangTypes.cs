﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Immutable;

namespace symlib
{
    public enum CXCursorKind
    {
        /* Declarations */
        /**
         * A declaration whose specific kind is not exposed via this
         * interface.
         *
         * Unexposed declarations have the same operations as any other kind
         * of declaration; one can extract their location information,
         * spelling, find their definitions, etc. However, the specific kind
         * of the declaration is not reported.
         */
        None = 0,
        UnexposedDecl = 1,
        /** A C or C++ struct. */
        StructDecl = 2,
        /** A C or C++ union. */
        UnionDecl = 3,
        /** A C++ class. */
        ClassDecl = 4,
        /** An enumeration. */
        EnumDecl = 5,
        /**
         * A field (in C) or non-static data member (in C++) in a
         * struct, union, or C++ class.
         */
        FieldDecl = 6,
        /** An enumerator constant. */
        EnumConstantDecl = 7,
        /** A function. */
        FunctionDecl = 8,
        /** A variable. */
        VarDecl = 9,
        /** A function or method parameter. */
        ParmDecl = 10,
        /** An Objective-C \@interface. */
        ObjCInterfaceDecl = 11,
        /** An Objective-C \@interface for a category. */
        ObjCCategoryDecl = 12,
        /** An Objective-C \@protocol declaration. */
        ObjCProtocolDecl = 13,
        /** An Objective-C \@property declaration. */
        ObjCPropertyDecl = 14,
        /** An Objective-C instance variable. */
        ObjCIvarDecl = 15,
        /** An Objective-C instance method. */
        ObjCInstanceMethodDecl = 16,
        /** An Objective-C class method. */
        ObjCClassMethodDecl = 17,
        /** An Objective-C \@implementation. */
        ObjCImplementationDecl = 18,
        /** An Objective-C \@implementation for a category. */
        ObjCCategoryImplDecl = 19,
        /** A typedef. */
        TypedefDecl = 20,
        /** A C++ class method. */
        CXXMethod = 21,
        /** A C++ namespace. */
        Namespace = 22,
        /** A linkage specification, e.g. 'extern "C"'. */
        LinkageSpec = 23,
        /** A C++ constructor. */
        Constructor = 24,
        /** A C++ destructor. */
        Destructor = 25,
        /** A C++ conversion function. */
        ConversionFunction = 26,
        /** A C++ template type parameter. */
        TemplateTypeParameter = 27,
        /** A C++ non-type template parameter. */
        NonTypeTemplateParameter = 28,
        /** A C++ template template parameter. */
        TemplateTemplateParameter = 29,
        /** A C++ function template. */
        FunctionTemplate = 30,
        /** A C++ class template. */
        ClassTemplate = 31,
        /** A C++ class template partial specialization. */
        ClassTemplatePartialSpecialization = 32,
        /** A C++ namespace alias declaration. */
        NamespaceAlias = 33,
        /** A C++ using directive. */
        UsingDirective = 34,
        /** A C++ using declaration. */
        UsingDeclaration = 35,
        /** A C++ alias declaration */
        TypeAliasDecl = 36,
        /** An Objective-C \@synthesize definition. */
        ObjCSynthesizeDecl = 37,
        /** An Objective-C \@dynamic definition. */
        ObjCDynamicDecl = 38,
        /** An access specifier. */
        CXXAccessSpecifier = 39,

        FirstDecl = UnexposedDecl,
        LastDecl = CXXAccessSpecifier,

        /* References */
        FirstRef = 40, /* Decl references */
        ObjCSuperClassRef = 40,
        ObjCProtocolRef = 41,
        ObjCClassRef = 42,
        /**
         * A reference to a type declaration.
         *
         * A type reference occurs anywhere where a type is named but not
         * declared. For example, given:
         *
         * \code
         * typedef unsigned size_type;
         * size_type size;
         * \endcode
         *
         * The typedef is a declaration of size_type (TypedefDecl),
         * while the type of the variable "size" is referenced. The cursor
         * referenced by the type of size is the typedef for size_type.
         */
        TypeRef = 43,
        CXXBaseSpecifier = 44,
        /**
         * A reference to a class template, function template, template
         * template parameter, or class template partial specialization.
         */
        TemplateRef = 45,
        /**
         * A reference to a namespace or namespace alias.
         */
        NamespaceRef = 46,
        /**
         * A reference to a member of a struct, union, or class that occurs in
         * some non-expression context, e.g., a designated initializer.
         */
        MemberRef = 47,
        /**
         * A reference to a labeled statement.
         *
         * This cursor kind is used to describe the jump to "start_over" in the
         * goto statement in the following example:
         *
         * \code
         *   start_over:
         *     ++counter;
         *
         *     goto start_over;
         * \endcode
         *
         * A label reference cursor refers to a label statement.
         */
        LabelRef = 48,

        /**
         * A reference to a set of overloaded functions or function templates
         * that has not yet been resolved to a specific function or function template.
         *
         * An overloaded declaration reference cursor occurs in C++ templates where
         * a dependent name refers to a function. For example:
         *
         * \code
         * template<typename T> void swap(T&, T&);
         *
         * struct X { ... };
         * void swap(X&, X&);
         *
         * template<typename T>
         * void reverse(T* first, T* last) {
         *   while (first < last - 1) {
         *     swap(*first, *--last);
         *     ++first;
         *   }
         * }
         *
         * struct Y { };
         * void swap(Y&, Y&);
         * \endcode
         *
         * Here, the identifier "swap" is associated with an overloaded declaration
         * reference. In the template definition, "swap" refers to either of the two
         * "swap" functions declared above, so both results will be available. At
         * instantiation time, "swap" may also refer to other functions found via
         * argument-dependent lookup (e.g., the "swap" function at the end of the
         * example).
         *
         * The functions \c clang_getNumOverloadedDecls() and
         * \c clang_getOverloadedDecl() can be used to retrieve the definitions
         * referenced by this cursor.
         */
        OverloadedDeclRef = 49,

        /**
         * A reference to a variable that occurs in some non-expression
         * context, e.g., a C++ lambda capture list.
         */
        VariableRef = 50,

        LastRef = VariableRef,

        /* Error conditions */
        FirstInvalid = 70,
        InvalidFile = 70,
        NoDeclFound = 71,
        NotImplemented = 72,
        InvalidCode = 73,
        LastInvalid = InvalidCode,

        /* Expressions */
        FirstExpr = 100,

        /**
         * An expression whose specific kind is not exposed via this
         * interface.
         *
         * Unexposed expressions have the same operations as any other kind
         * of expression; one can extract their location information,
         * spelling, children, etc. However, the specific kind of the
         * expression is not reported.
         */
        UnexposedExpr = 100,

        /**
         * An expression that refers to some value declaration, such
         * as a function, variable, or enumerator.
         */
        DeclRefExpr = 101,

        /**
         * An expression that refers to a member of a struct, union,
         * class, Objective-C class, etc.
         */
        MemberRefExpr = 102,

        /** An expression that calls a function. */
        CallExpr = 103,

        /** An expression that sends a message to an Objective-C
         object or class. */
        ObjCMessageExpr = 104,

        /** An expression that represents a block literal. */
        BlockExpr = 105,

        /** An integer literal.
         */
        IntegerLiteral = 106,

        /** A floating point number literal.
         */
        FloatingLiteral = 107,

        /** An imaginary number literal.
         */
        ImaginaryLiteral = 108,

        /** A string literal.
         */
        StringLiteral = 109,

        /** A character literal.
         */
        CharacterLiteral = 110,

        /** A parenthesized expression, e.g. "(1)".
         *
         * This AST node is only formed if full location information is requested.
         */
        ParenExpr = 111,

        /** This represents the unary-expression's (except sizeof and
         * alignof).
         */
        UnaryOperator = 112,

        /** [C99 6.5.2.1] Array Subscripting.
         */
        ArraySubscriptExpr = 113,

        /** A builtin binary operation expression such as "x + y" or
         * "x <= y".
         */
        BinaryOperator = 114,

        /** Compound assignment such as "+=".
         */
        CompoundAssignOperator = 115,

        /** The ?: ternary operator.
         */
        ConditionalOperator = 116,

        /** An explicit cast in C (C99 6.5.4) or a C-style cast in C++
         * (C++ [expr.cast]), which uses the syntax (Type)expr.
         *
         * For example: (int)f.
         */
        CStyleCastExpr = 117,

        /** [C99 6.5.2.5]
         */
        CompoundLiteralExpr = 118,

        /** Describes an C or C++ initializer list.
         */
        InitListExpr = 119,

        /** The GNU address of label extension, representing &&label.
         */
        AddrLabelExpr = 120,

        /** This is the GNU Statement Expression extension: ({int X=4; X;})
         */
        StmtExpr = 121,

        /** Represents a C11 generic selection.
         */
        GenericSelectionExpr = 122,

        /** Implements the GNU __null extension, which is a name for a null
         * pointer constant that has integral type (e.g., int or long) and is the same
         * size and alignment as a pointer.
         *
         * The __null extension is typically only used by system headers, which define
         * NULL as __null in C++ rather than using 0 (which is an integer that may not
         * match the size of a pointer).
         */
        GNUNullExpr = 123,

        /** C++'s static_cast<> expression.
         */
        CXXStaticCastExpr = 124,

        /** C++'s dynamic_cast<> expression.
         */
        CXXDynamicCastExpr = 125,

        /** C++'s reinterpret_cast<> expression.
         */
        CXXReinterpretCastExpr = 126,

        /** C++'s const_cast<> expression.
         */
        CXXConstCastExpr = 127,

        /** Represents an explicit C++ type conversion that uses "functional"
         * notion (C++ [expr.type.conv]).
         *
         * Example:
         * \code
         *   x = int(0.5);
         * \endcode
         */
        CXXFunctionalCastExpr = 128,

        /** A C++ typeid expression (C++ [expr.typeid]).
         */
        CXXTypeidExpr = 129,

        /** [C++ 2.13.5] C++ Boolean Literal.
         */
        CXXBoolLiteralExpr = 130,

        /** [C++0x 2.14.7] C++ Pointer Literal.
         */
        CXXNullPtrLiteralExpr = 131,

        /** Represents the "this" expression in C++
         */
        CXXThisExpr = 132,

        /** [C++ 15] C++ Throw Expression.
         *
         * This handles 'throw' and 'throw' assignment-expression. When
         * assignment-expression isn't present, Op will be null.
         */
        CXXThrowExpr = 133,

        /** A new expression for memory allocation and constructor calls, e.g:
         * "new CXXNewExpr(foo)".
         */
        CXXNewExpr = 134,

        /** A delete expression for memory deallocation and destructor calls,
         * e.g. "delete[] pArray".
         */
        CXXDeleteExpr = 135,

        /** A unary expression. (noexcept, sizeof, or other traits)
         */
        UnaryExpr = 136,

        /** An Objective-C string literal i.e. @"foo".
         */
        ObjCStringLiteral = 137,

        /** An Objective-C \@encode expression.
         */
        ObjCEncodeExpr = 138,

        /** An Objective-C \@selector expression.
         */
        ObjCSelectorExpr = 139,

        /** An Objective-C \@protocol expression.
         */
        ObjCProtocolExpr = 140,

        /** An Objective-C "bridged" cast expression, which casts between
         * Objective-C pointers and C pointers, transferring ownership in the process.
         *
         * \code
         *   NSString *str = (__bridge_transfer NSString *)CFCreateString();
         * \endcode
         */
        ObjCBridgedCastExpr = 141,

        /** Represents a C++0x pack expansion that produces a sequence of
         * expressions.
         *
         * A pack expansion expression contains a pattern (which itself is an
         * expression) followed by an ellipsis. For example:
         *
         * \code
         * template<typename F, typename ...Types>
         * void forward(F f, Types &&...args) {
         *  f(static_cast<Types&&>(args)...);
         * }
         * \endcode
         */
        PackExpansionExpr = 142,

        /** Represents an expression that computes the length of a parameter
         * pack.
         *
         * \code
         * template<typename ...Types>
         * struct count {
         *   static const unsigned value = sizeof...(Types);
         * };
         * \endcode
         */
        SizeOfPackExpr = 143,

        /* Represents a C++ lambda expression that produces a local function
         * object.
         *
         * \code
         * void abssort(float *x, unsigned N) {
         *   std::sort(x, x + N,
         *             [](float a, float b) {
         *               return std::abs(a) < std::abs(b);
         *             });
         * }
         * \endcode
         */
        LambdaExpr = 144,

        /** Objective-c Boolean Literal.
         */
        ObjCBoolLiteralExpr = 145,

        /** Represents the "self" expression in an Objective-C method.
         */
        ObjCSelfExpr = 146,

        /** OpenMP 5.0 [2.1.5, Array Section].
         */
        OMPArraySectionExpr = 147,

        /** Represents an @available(...) check.
         */
        ObjCAvailabilityCheckExpr = 148,

        /**
         * Fixed point literal
         */
        FixedPointLiteral = 149,

        /** OpenMP 5.0 [2.1.4, Array Shaping].
         */
        OMPArrayShapingExpr = 150,

        /**
         * OpenMP 5.0 [2.1.6 Iterators]
         */
        OMPIteratorExpr = 151,

        /** OpenCL's addrspace_cast<> expression.
         */
        CXXAddrspaceCastExpr = 152,

        /**
         * Expression that references a C++20 concept.
         */
        ConceptSpecializationExpr = 153,

        /**
         * Expression that references a C++20 concept.
         */
        RequiresExpr = 154,

        /**
         * Expression that references a C++20 parenthesized list aggregate
         * initializer.
         */
        CXXParenListInitExpr = 155,

        LastExpr = CXXParenListInitExpr,

        /* Statements */
        FirstStmt = 200,
        /**
         * A statement whose specific kind is not exposed via this
         * interface.
         *
         * Unexposed statements have the same operations as any other kind of
         * statement; one can extract their location information, spelling,
         * children, etc. However, the specific kind of the statement is not
         * reported.
         */
        UnexposedStmt = 200,

        /** A labelled statement in a function.
         *
         * This cursor kind is used to describe the "start_over:" label statement in
         * the following example:
         *
         * \code
         *   start_over:
         *     ++counter;
         * \endcode
         *
         */
        LabelStmt = 201,

        /** A group of statements like { stmt stmt }.
         *
         * This cursor kind is used to describe compound statements, e.g. function
         * bodies.
         */
        CompoundStmt = 202,

        /** A case statement.
         */
        CaseStmt = 203,

        /** A default statement.
         */
        DefaultStmt = 204,

        /** An if statement
         */
        IfStmt = 205,

        /** A switch statement.
         */
        SwitchStmt = 206,

        /** A while statement.
         */
        WhileStmt = 207,

        /** A do statement.
         */
        DoStmt = 208,

        /** A for statement.
         */
        ForStmt = 209,

        /** A goto statement.
         */
        GotoStmt = 210,

        /** An indirect goto statement.
         */
        IndirectGotoStmt = 211,

        /** A continue statement.
         */
        ContinueStmt = 212,

        /** A break statement.
         */
        BreakStmt = 213,

        /** A return statement.
         */
        ReturnStmt = 214,

        /** A GCC inline assembly statement extension.
         */
        GCCAsmStmt = 215,
        AsmStmt = GCCAsmStmt,

        /** Objective-C's overall \@try-\@catch-\@finally statement.
         */
        ObjCAtTryStmt = 216,

        /** Objective-C's \@catch statement.
         */
        ObjCAtCatchStmt = 217,

        /** Objective-C's \@finally statement.
         */
        ObjCAtFinallyStmt = 218,

        /** Objective-C's \@throw statement.
         */
        ObjCAtThrowStmt = 219,

        /** Objective-C's \@synchronized statement.
         */
        ObjCAtSynchronizedStmt = 220,

        /** Objective-C's autorelease pool statement.
         */
        ObjCAutoreleasePoolStmt = 221,

        /** Objective-C's collection statement.
         */
        ObjCForCollectionStmt = 222,

        /** C++'s catch statement.
         */
        CXXCatchStmt = 223,

        /** C++'s try statement.
         */
        CXXTryStmt = 224,

        /** C++'s for (* : *) statement.
         */
        CXXForRangeStmt = 225,

        /** Windows Structured Exception Handling's try statement.
         */
        SEHTryStmt = 226,

        /** Windows Structured Exception Handling's except statement.
         */
        SEHExceptStmt = 227,

        /** Windows Structured Exception Handling's finally statement.
         */
        SEHFinallyStmt = 228,

        /** A MS inline assembly statement extension.
         */
        MSAsmStmt = 229,

        /** The null statement ";": C99 6.8.3p3.
         *
         * This cursor kind is used to describe the null statement.
         */
        NullStmt = 230,

        /** Adaptor class for mixing declarations with statements and
         * expressions.
         */
        DeclStmt = 231,

        /** OpenMP parallel directive.
         */
        OMPParallelDirective = 232,

        /** OpenMP SIMD directive.
         */
        OMPSimdDirective = 233,

        /** OpenMP for directive.
         */
        OMPForDirective = 234,

        /** OpenMP sections directive.
         */
        OMPSectionsDirective = 235,

        /** OpenMP section directive.
         */
        OMPSectionDirective = 236,

        /** OpenMP single directive.
         */
        OMPSingleDirective = 237,

        /** OpenMP parallel for directive.
         */
        OMPParallelForDirective = 238,

        /** OpenMP parallel sections directive.
         */
        OMPParallelSectionsDirective = 239,

        /** OpenMP task directive.
         */
        OMPTaskDirective = 240,

        /** OpenMP master directive.
         */
        OMPMasterDirective = 241,

        /** OpenMP critical directive.
         */
        OMPCriticalDirective = 242,

        /** OpenMP taskyield directive.
         */
        OMPTaskyieldDirective = 243,

        /** OpenMP barrier directive.
         */
        OMPBarrierDirective = 244,

        /** OpenMP taskwait directive.
         */
        OMPTaskwaitDirective = 245,

        /** OpenMP flush directive.
         */
        OMPFlushDirective = 246,

        /** Windows Structured Exception Handling's leave statement.
         */
        SEHLeaveStmt = 247,

        /** OpenMP ordered directive.
         */
        OMPOrderedDirective = 248,

        /** OpenMP atomic directive.
         */
        OMPAtomicDirective = 249,

        /** OpenMP for SIMD directive.
         */
        OMPForSimdDirective = 250,

        /** OpenMP parallel for SIMD directive.
         */
        OMPParallelForSimdDirective = 251,

        /** OpenMP target directive.
         */
        OMPTargetDirective = 252,

        /** OpenMP teams directive.
         */
        OMPTeamsDirective = 253,

        /** OpenMP taskgroup directive.
         */
        OMPTaskgroupDirective = 254,

        /** OpenMP cancellation point directive.
         */
        OMPCancellationPointDirective = 255,

        /** OpenMP cancel directive.
         */
        OMPCancelDirective = 256,

        /** OpenMP target data directive.
         */
        OMPTargetDataDirective = 257,

        /** OpenMP taskloop directive.
         */
        OMPTaskLoopDirective = 258,

        /** OpenMP taskloop simd directive.
         */
        OMPTaskLoopSimdDirective = 259,

        /** OpenMP distribute directive.
         */
        OMPDistributeDirective = 260,

        /** OpenMP target enter data directive.
         */
        OMPTargetEnterDataDirective = 261,

        /** OpenMP target exit data directive.
         */
        OMPTargetExitDataDirective = 262,

        /** OpenMP target parallel directive.
         */
        OMPTargetParallelDirective = 263,

        /** OpenMP target parallel for directive.
         */
        OMPTargetParallelForDirective = 264,

        /** OpenMP target update directive.
         */
        OMPTargetUpdateDirective = 265,

        /** OpenMP distribute parallel for directive.
         */
        OMPDistributeParallelForDirective = 266,

        /** OpenMP distribute parallel for simd directive.
         */
        OMPDistributeParallelForSimdDirective = 267,

        /** OpenMP distribute simd directive.
         */
        OMPDistributeSimdDirective = 268,

        /** OpenMP target parallel for simd directive.
         */
        OMPTargetParallelForSimdDirective = 269,

        /** OpenMP target simd directive.
         */
        OMPTargetSimdDirective = 270,

        /** OpenMP teams distribute directive.
         */
        OMPTeamsDistributeDirective = 271,

        /** OpenMP teams distribute simd directive.
         */
        OMPTeamsDistributeSimdDirective = 272,

        /** OpenMP teams distribute parallel for simd directive.
         */
        OMPTeamsDistributeParallelForSimdDirective = 273,

        /** OpenMP teams distribute parallel for directive.
         */
        OMPTeamsDistributeParallelForDirective = 274,

        /** OpenMP target teams directive.
         */
        OMPTargetTeamsDirective = 275,

        /** OpenMP target teams distribute directive.
         */
        OMPTargetTeamsDistributeDirective = 276,

        /** OpenMP target teams distribute parallel for directive.
         */
        OMPTargetTeamsDistributeParallelForDirective = 277,

        /** OpenMP target teams distribute parallel for simd directive.
         */
        OMPTargetTeamsDistributeParallelForSimdDirective = 278,

        /** OpenMP target teams distribute simd directive.
         */
        OMPTargetTeamsDistributeSimdDirective = 279,

        /** C++2a std::bit_cast expression.
         */
        BuiltinBitCastExpr = 280,

        /** OpenMP master taskloop directive.
         */
        OMPMasterTaskLoopDirective = 281,

        /** OpenMP parallel master taskloop directive.
         */
        OMPParallelMasterTaskLoopDirective = 282,

        /** OpenMP master taskloop simd directive.
         */
        OMPMasterTaskLoopSimdDirective = 283,

        /** OpenMP parallel master taskloop simd directive.
         */
        OMPParallelMasterTaskLoopSimdDirective = 284,

        /** OpenMP parallel master directive.
         */
        OMPParallelMasterDirective = 285,

        /** OpenMP depobj directive.
         */
        OMPDepobjDirective = 286,

        /** OpenMP scan directive.
         */
        OMPScanDirective = 287,

        /** OpenMP tile directive.
         */
        OMPTileDirective = 288,

        /** OpenMP canonical loop.
         */
        OMPCanonicalLoop = 289,

        /** OpenMP interop directive.
         */
        OMPInteropDirective = 290,

        /** OpenMP dispatch directive.
         */
        OMPDispatchDirective = 291,

        /** OpenMP masked directive.
         */
        OMPMaskedDirective = 292,

        /** OpenMP unroll directive.
         */
        OMPUnrollDirective = 293,

        /** OpenMP metadirective directive.
         */
        OMPMetaDirective = 294,

        /** OpenMP loop directive.
         */
        OMPGenericLoopDirective = 295,

        /** OpenMP teams loop directive.
         */
        OMPTeamsGenericLoopDirective = 296,

        /** OpenMP target teams loop directive.
         */
        OMPTargetTeamsGenericLoopDirective = 297,

        /** OpenMP parallel loop directive.
         */
        OMPParallelGenericLoopDirective = 298,

        /** OpenMP target parallel loop directive.
         */
        OMPTargetParallelGenericLoopDirective = 299,

        /** OpenMP parallel masked directive.
         */
        OMPParallelMaskedDirective = 300,

        /** OpenMP masked taskloop directive.
         */
        OMPMaskedTaskLoopDirective = 301,

        /** OpenMP masked taskloop simd directive.
         */
        OMPMaskedTaskLoopSimdDirective = 302,

        /** OpenMP parallel masked taskloop directive.
         */
        OMPParallelMaskedTaskLoopDirective = 303,

        /** OpenMP parallel masked taskloop simd directive.
         */
        OMPParallelMaskedTaskLoopSimdDirective = 304,

        /** OpenMP error directive.
         */
        OMPErrorDirective = 305,

        LastStmt = OMPErrorDirective,

        /**
         * Cursor that represents the translation unit itself.
         *
         * The translation unit cursor exists primarily to act as the root
         * cursor for traversing the contents of a translation unit.
         */
        TranslationUnit = 350,

        /* Attributes */
        FirstAttr = 400,
        /**
         * An attribute whose specific kind is not exposed via this
         * interface.
         */
        UnexposedAttr = 400,

        IBActionAttr = 401,
        IBOutletAttr = 402,
        IBOutletCollectionAttr = 403,
        CXXFinalAttr = 404,
        CXXOverrideAttr = 405,
        AnnotateAttr = 406,
        AsmLabelAttr = 407,
        PackedAttr = 408,
        PureAttr = 409,
        ConstAttr = 410,
        NoDuplicateAttr = 411,
        CUDAConstantAttr = 412,
        CUDADeviceAttr = 413,
        CUDAGlobalAttr = 414,
        CUDAHostAttr = 415,
        CUDASharedAttr = 416,
        VisibilityAttr = 417,
        DLLExport = 418,
        DLLImport = 419,
        NSReturnsRetained = 420,
        NSReturnsNotRetained = 421,
        NSReturnsAutoreleased = 422,
        NSConsumesSelf = 423,
        NSConsumed = 424,
        ObjCException = 425,
        ObjCNSObject = 426,
        ObjCIndependentClass = 427,
        ObjCPreciseLifetime = 428,
        ObjCReturnsInnerPointer = 429,
        ObjCRequiresSuper = 430,
        ObjCRootClass = 431,
        ObjCSubclassingRestricted = 432,
        ObjCExplicitProtocolImpl = 433,
        ObjCDesignatedInitializer = 434,
        ObjCRuntimeVisible = 435,
        ObjCBoxable = 436,
        FlagEnum = 437,
        ConvergentAttr = 438,
        WarnUnusedAttr = 439,
        WarnUnusedResultAttr = 440,
        AlignedAttr = 441,
        LastAttr = AlignedAttr,

        /* Preprocessing */
        PreprocessingDirective = 500,
        MacroDefinition = 501,
        MacroExpansion = 502,
        MacroInstantiation = MacroExpansion,
        InclusionDirective = 503,
        FirstPreprocessing = PreprocessingDirective,
        LastPreprocessing = InclusionDirective,

        /* Extra Declarations */
        /**
         * A module import declaration.
         */
        ModuleImportDecl = 600,
        TypeAliasTemplateDecl = 601,
        /**
         * A static_assert or _Static_assert node
         */
        StaticAssert = 602,
        /**
         * a friend declaration.
         */
        FriendDecl = 603,
        /**
         * a concept declaration.
         */
        ConceptDecl = 604,

        FirstExtraDecl = ModuleImportDecl,
        LastExtraDecl = ConceptDecl,

        /**
         * A code completion overload candidate.
         */
        OverloadCandidate = 700
    }
    public enum CXTypeKind
    {
        /**
         * Represents an invalid type (e.g., where no type is available).
         */
        None = 0,

        /**
         * A type whose specific kind is not exposed via this
         * interface.
         */
        Unexposed = 1,

        /* Builtin types */
        Void = 2,
        Bool = 3,
        Char_U = 4,
        UChar = 5,
        Char16 = 6,
        Char32 = 7,
        UShort = 8,
        UInt = 9,
        ULong = 10,
        ULongLong = 11,
        UInt128 = 12,
        Char_S = 13,
        SChar = 14,
        WChar = 15,
        Short = 16,
        Int = 17,
        Long = 18,
        LongLong = 19,
        Int128 = 20,
        Float = 21,
        Double = 22,
        LongDouble = 23,
        NullPtr = 24,
        Overload = 25,
        Dependent = 26,
        ObjCId = 27,
        ObjCClass = 28,
        ObjCSel = 29,
        Float128 = 30,
        Half = 31,
        Float16 = 32,
        ShortAccum = 33,
        Accum = 34,
        LongAccum = 35,
        UShortAccum = 36,
        UAccum = 37,
        ULongAccum = 38,
        BFloat16 = 39,
        Ibm128 = 40,
        FirstBuiltin = Void,
        LastBuiltin = Ibm128,

        Complex = 100,
        Pointer = 101,
        BlockPointer = 102,
        LValueReference = 103,
        RValueReference = 104,
        Record = 105,
        Enum = 106,
        Typedef = 107,
        ObjCInterface = 108,
        ObjCObjectPointer = 109,
        FunctionNoProto = 110,
        FunctionProto = 111,
        ConstantArray = 112,
        Vector = 113,
        IncompleteArray = 114,
        VariableArray = 115,
        DependentSizedArray = 116,
        MemberPointer = 117,
        Auto = 118,

        /**
         * Represents a type that was referred to using an elaborated type keyword.
         *
         * E.g., struct S, or via a qualified name, e.g., N::M::type, or both.
         */
        Elaborated = 119,

        /* OpenCL PipeType. */
        Pipe = 120,

        /* OpenCL builtin types. */
        OCLImage1dRO = 121,
        OCLImage1dArrayRO = 122,
        OCLImage1dBufferRO = 123,
        OCLImage2dRO = 124,
        OCLImage2dArrayRO = 125,
        OCLImage2dDepthRO = 126,
        OCLImage2dArrayDepthRO = 127,
        OCLImage2dMSAARO = 128,
        OCLImage2dArrayMSAARO = 129,
        OCLImage2dMSAADepthRO = 130,
        OCLImage2dArrayMSAADepthRO = 131,
        OCLImage3dRO = 132,
        OCLImage1dWO = 133,
        OCLImage1dArrayWO = 134,
        OCLImage1dBufferWO = 135,
        OCLImage2dWO = 136,
        OCLImage2dArrayWO = 137,
        OCLImage2dDepthWO = 138,
        OCLImage2dArrayDepthWO = 139,
        OCLImage2dMSAAWO = 140,
        OCLImage2dArrayMSAAWO = 141,
        OCLImage2dMSAADepthWO = 142,
        OCLImage2dArrayMSAADepthWO = 143,
        OCLImage3dWO = 144,
        OCLImage1dRW = 145,
        OCLImage1dArrayRW = 146,
        OCLImage1dBufferRW = 147,
        OCLImage2dRW = 148,
        OCLImage2dArrayRW = 149,
        OCLImage2dDepthRW = 150,
        OCLImage2dArrayDepthRW = 151,
        OCLImage2dMSAARW = 152,
        OCLImage2dArrayMSAARW = 153,
        OCLImage2dMSAADepthRW = 154,
        OCLImage2dArrayMSAADepthRW = 155,
        OCLImage3dRW = 156,
        OCLSampler = 157,
        OCLEvent = 158,
        TemplateName = 159,
        TempalteParam = 160,

        ObjCObject = 161,
        ObjCTypeParam = 162,
        Attributed = 163,

        OCLIntelSubgroupAVCMcePayload = 164,
        OCLIntelSubgroupAVCImePayload = 165,
        OCLIntelSubgroupAVCRefPayload = 166,
        OCLIntelSubgroupAVCSicPayload = 167,
        OCLIntelSubgroupAVCMceResult = 168,
        OCLIntelSubgroupAVCImeResult = 169,
        OCLIntelSubgroupAVCRefResult = 170,
        OCLIntelSubgroupAVCSicResult = 171,
        OCLIntelSubgroupAVCImeResultSingleRefStreamout = 172,
        OCLIntelSubgroupAVCImeResultDualRefStreamout = 173,
        OCLIntelSubgroupAVCImeSingleRefStreamin = 174,

        OCLIntelSubgroupAVCImeDualRefStreamin = 175,

        ExtVector = 176,
        Atomic = 177,
        BTFTagAttributed = 178
    }

    public enum CX_StorageClass
    {
        Invalid = 0,
        None,
        Extern,
        Static,
        PrivateExtern,
        OpenCLWorkGroupLocal,
        Auto,
        Register
    };
    public enum CXXAccessSpecifier
    {
        Invalid = 0,
        Public = 1,
        Protected = 2,
        Private = 3
    };
    public static class ClangTypes
    {
        public static List<CXCursorKind> CursorKindsMRU = new List<CXCursorKind> {
            CXCursorKind.ParmDecl,
            CXCursorKind.TypeRef,
            CXCursorKind.DeclRefExpr,
            CXCursorKind.OverloadedDeclRef,
            CXCursorKind.TemplateTypeParameter,
            CXCursorKind.VarDecl,
            CXCursorKind.TypedefDecl,
            CXCursorKind.FunctionDecl,
            CXCursorKind.CallExpr,
            CXCursorKind.FirstExpr,
            CXCursorKind.MemberRefExpr,
            CXCursorKind.TemplateRef,
            CXCursorKind.CompoundStmt,
            CXCursorKind.CXXMethod,
            CXCursorKind.TypeAliasDecl,
            CXCursorKind.BinaryOperator,
            CXCursorKind.ClassTemplate,
            CXCursorKind.FieldDecl,
            CXCursorKind.IntegerLiteral,
            CXCursorKind.Namespace,
            CXCursorKind.ReturnStmt,
            CXCursorKind.NamespaceRef,
            CXCursorKind.FunctionTemplate,
            CXCursorKind.DeclStmt,
            CXCursorKind.UnaryOperator,
            CXCursorKind.ClassDecl,
            CXCursorKind.ConceptDecl,
            CXCursorKind.FirstDecl,
            CXCursorKind.EnumConstantDecl,
            CXCursorKind.TypeAliasTemplateDecl,
            CXCursorKind.IfStmt,
            CXCursorKind.NonTypeTemplateParameter,
            CXCursorKind.Constructor,
            CXCursorKind.StructDecl,
            CXCursorKind.ParenExpr,
            CXCursorKind.WarnUnusedResultAttr,
            CXCursorKind.CXXAccessSpecifier,
            CXCursorKind.CXXStaticCastExpr,
            CXCursorKind.ArraySubscriptExpr,
            CXCursorKind.EnumDecl,
            CXCursorKind.CXXThisExpr,
            CXCursorKind.CXXBoolLiteralExpr,
            CXCursorKind.InitListExpr,
            CXCursorKind.ClassTemplatePartialSpecialization,
            CXCursorKind.CStyleCastExpr,
            CXCursorKind.MemberRef,
            CXCursorKind.UsingDeclaration,
            CXCursorKind.ConceptSpecializationExpr,
            CXCursorKind.CXXBaseSpecifier,
            CXCursorKind.FirstAttr,
            CXCursorKind.NullStmt,
            CXCursorKind.StaticAssert,
            CXCursorKind.ForStmt,
            CXCursorKind.StringLiteral,
            CXCursorKind.CXXNullPtrLiteralExpr,
            CXCursorKind.FriendDecl,
            CXCursorKind.CompoundAssignOperator,
            CXCursorKind.Destructor,
            CXCursorKind.ConditionalOperator,
            CXCursorKind.BreakStmt,
            CXCursorKind.UnaryExpr,
            CXCursorKind.CharacterLiteral,
            CXCursorKind.CaseStmt,
            CXCursorKind.FloatingLiteral,
            CXCursorKind.PackExpansionExpr,
            CXCursorKind.CXXFunctionalCastExpr,
            CXCursorKind.WhileStmt,
            CXCursorKind.CXXConstCastExpr,
            CXCursorKind.CXXReinterpretCastExpr,
            CXCursorKind.CXXOverrideAttr,
            CXCursorKind.ConversionFunction,
            CXCursorKind.UnionDecl,
            CXCursorKind.RequiresExpr,
            CXCursorKind.LambdaExpr,
            CXCursorKind.DoStmt,
            CXCursorKind.CXXCatchStmt,
            CXCursorKind.CXXTryStmt,
            CXCursorKind.CXXNewExpr,
            CXCursorKind.SwitchStmt,
            CXCursorKind.TemplateTemplateParameter,
            CXCursorKind.LastRef,
            CXCursorKind.DefaultStmt,
            CXCursorKind.NoDeclFound,
            CXCursorKind.CXXDeleteExpr,
            CXCursorKind.CXXThrowExpr,
            CXCursorKind.AlignedAttr,
            CXCursorKind.DLLImport,
            CXCursorKind.ContinueStmt,
            CXCursorKind.SizeOfPackExpr,
            CXCursorKind.FirstStmt,
            CXCursorKind.CXXFinalAttr,
            CXCursorKind.CXXTypeidExpr,
            CXCursorKind.CXXDynamicCastExpr,
            CXCursorKind.UsingDirective,
            CXCursorKind.CXXForRangeStmt,
            CXCursorKind.BuiltinBitCastExpr,
            CXCursorKind.NamespaceAlias,
            CXCursorKind.DLLExport
        };

        public static IEnumerable<CXCursorKind> CursorKinds = Enum.GetValues(typeof(CXCursorKind)).Cast<CXCursorKind>();
        public static IEnumerable<CXTypeKind> TypeKinds => Enum.GetValues(typeof(CXTypeKind)).Cast<CXTypeKind>();

        static Dictionary<CXCursorKind, string> cursorAbbrev = null;
        public static Dictionary<CXCursorKind, string> CursorAbbrev
        {
            get
            {
                if (cursorAbbrev == null)
                {
                    cursorAbbrev = new Dictionary<CXCursorKind, string>();
                    var vals = Enum.GetValues(typeof(CXCursorKind));
                    foreach (CXCursorKind val in vals)
                    {
                        string name = Enum.GetName(typeof(CXCursorKind), val);
                        char []uc = name.Where(c => char.IsUpper(c)).ToArray();
                        cursorAbbrev.TryAdd(val, new string(uc));
                    }
                }
                return cursorAbbrev;
            }
        }
        
    }
}
