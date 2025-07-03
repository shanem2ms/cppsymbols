# C++ Symbols Project Analysis and OSY File Format Specification

## Project Overview

The C++ Symbols project is a command-line utility developed in C++ using CMake. Its primary function is to parse C++ source code using the `libClang` library and generate a custom binary file with an `.osy` extension. This file contains a compressed, serialized representation of the Abstract Syntax Tree (AST) of the parsed code, effectively creating a database-like structure for the C++ source.

### Core Components

The project consists of several key components:

1.  **`symbols` (Command-Line Tool):** The main executable that orchestrates the parsing and serialization process. It accepts command-line arguments for specifying input files, include paths, defines, and the output file path. It also includes functionality for merging multiple `.osy` files and dumping their contents for debugging.

2.  **`Compiler`:** A singleton class that wraps the `libClang` API. It is responsible for creating a translation unit from a source file and traversing the resulting AST.

3.  **In-Memory AST Representation (`Node`, `TypeNode`):** A set of C++ classes that represent the AST in memory. These structures are designed to be easily converted into a serializable format by using integer indices for relationships (e.g., parent, type) instead of direct pointers.

4.  **Serializable Data Structures (`DbNode`, `DbType`, `DbToken`):** A collection of plain C++ structs that represent the on-disk format of the AST data. These structs are designed for direct binary serialization.

5.  **Database Manager (`DbFile`):** A class responsible for managing the collection of serializable data structures and handling the serialization to and from the `.osy` file format. It uses a custom streaming library (`cppstream.h`) for this purpose.

6.  **Serialization Library (`cppstream.h`):** A header-only library providing a simple, efficient mechanism for serializing fundamental C++ types, strings, and standard library containers into a binary stream.

### Workflow

The process of generating an `.osy` file follows these steps:

1.  **Invocation:** The user runs the `symbols` executable, providing the path to a C++ source file and other necessary compiler flags (includes, defines).

2.  **Parsing:** The `Compiler` class invokes `libClang` to parse the source code and generate a complete AST.

3.  **AST Traversal:** The tool traverses the `libClang` AST. For each cursor (node) in the tree, it creates a corresponding in-memory `Node` object, capturing information such as the cursor kind, source location, type, and relationships to other nodes.

4.  **Data Aggregation:** All created `Node`, `TypeNode`, and `Token` objects are collected within a `DbFile` instance. This instance converts them into their serializable `DbNode`, `DbType`, and `DbToken` counterparts.

5.  **Serialization:** The `DbFile::Save` method is called. It serializes its internal data tables (source files, tokens, types, and nodes) into a single byte buffer using the `CppStream` library.

6.  **Compression:** The resulting byte buffer is compressed using the `zlib` library to reduce file size.

7.  **File Write:** The final compressed data is written to the specified output `.osy` file, prefixed with a 4-byte header indicating the size of the *uncompressed* data.

### Command-Line Usage

The tool supports several modes of operation:

```bash
# Generate OSY file from C++ source
symbols --compile input.cpp --output output.osy --include-directory include_path --define MACRO_DEFINE

# Merge multiple OSY files
symbols --merge file1.osy file2.osy --output merged.osy

# Dump OSY file contents for debugging
symbols --dump file.osy

# Convert OSY file to a SQLite database
symbols --to-sqlite file.osy file.sqlite

# Validate OSY file structure
symbols --validate file.osy

# Show help information
symbols --help
```

## OSY File Format Specification

The `.osy` file is a compressed binary format designed for efficient storage and retrieval of C++ AST data. To read the file, one must first decompress the entire file payload using `zlib`.

### File Structure Overview

```
[4-byte Header] [Compressed Data Block]
```

### Header

The file begins with a 4-byte header that specifies the size of the uncompressed data.

| Offset | Size (bytes) | Type       | Description                                           |
|:-------|:-------------|:-----------|:------------------------------------------------------|
| 0      | 4            | `uint32_t` | The total size of the uncompressed data that follows |

### Body (Compressed Data)

The remainder of the file after the header is a `zlib`-compressed data block. After decompression, the body consists of four contiguous data sections, written in the following order:

1.  **Source Files Table**
2.  **Tokens Table**
3.  **Types Table**
4.  **Nodes Table**

The layout of these sections is determined by the `CppStream` serialization of `std::vector`. Each section is prefixed by its element count.

### Data Sections

#### 1. Source Files Table

This section contains a list of all source file paths referenced in the AST.

- **Format:** `std::vector<std::string>`
- **Layout:**
  - `uint64_t`: Number of source file strings
  - A sequence of string records. Each string is prefixed with a `uint16_t` length specifier

#### 2. Tokens Table

This section contains all unique identifier and literal strings from the source code.

- **Format:** `std::vector<DbToken>`
- **Layout:**
  - `uint64_t`: Number of `DbToken` records
  - A sequence of `DbToken` records

**`DbToken` Record Structure:**

| Field  | Size (bytes) | Type          | Description                                              |
|:-------|:-------------|:--------------|:---------------------------------------------------------|
| `key`  | 8            | `int64_t`     | The unique key/index of the token                        |
| `text` | variable     | `std::string` | The token string (prefixed with `uint16_t` length)      |

#### 3. Types Table

This section defines all types encountered in the source code.

- **Format:** `std::vector<DbType>`
- **Layout:**
  - `uint64_t`: Number of `DbType` records
  - A sequence of `DbType` records

**`DbType` Record Structure:**

| Field      | Size (bytes) | Type                   | Description                                                                                             |
|:-----------|:-------------|:-----------------------|:--------------------------------------------------------------------------------------------------------|
| `key`      | 8            | `int64_t`              | The unique key/index of the type                                                                        |
| `hash`     | 8            | `int64_t`              | A hash of the type's properties for quick comparison                                                    |
| `children` | variable     | `std::vector<int64_t>` | A vector of keys pointing to child types (e.g., template arguments). Prefixed with `uint64_t` count   |
| `token`    | 8            | `int64_t`              | A key referencing a record in the **Tokens Table** (e.g., the name of a `struct` or `class`)          |
| `kind`     | 4            | `CXTypeKind` (enum)    | The `libClang` kind of the type (e.g., `CXType_Pointer`, `CXType_Record`)                              |
| `isconst`  | 1            | `uint8_t`              | A boolean flag (0 or 1) indicating if the type has a `const` qualifier                                 |

#### 4. Nodes Table

This is the main section of the file, containing the serialized AST nodes.

- **Format:** `std::vector<DbNode>`
- **Layout:**
  - `uint64_t`: Number of `DbNode` records
  - A sequence of `DbNode` records

**`DbNode` Record Structure (80 bytes total):**

| Field           | Size (bytes) | Type                   | Description
|:----------------|:-------------|:-----------------------|:--------------------------------------------------------------------------------------------------------------------------------------------------------------
| `key`           | 8            | `int64_t`              | The unique key/index of this node.
| `compilingFile` | 8            | `int64_t`              | A key referencing the source file in the **Source Files Table** that was being compiled to generate this node.
| `parentNodeIdx` | 8            | `int64_t`              | The key of the parent node in this table. `nullnode` (-1) for root nodes.
| `referencedIdx` | 8            | `int64_t`              | The key of another node that this node references (e.g., a function call referencing a function declaration). `nullnode` (-1) if not applicable.
| `kind`          | 4            | `CXCursorKind` (enum)  | The `libClang` kind of the cursor (e.g., `CXCursor_FunctionDecl`, `CXCursor_VarDecl`).
| `flags`         | 4            | `int32_t`              | A bitfield storing multiple boolean and enum flags: `AccessSpecifier` (bits 0-3), `isAbstract` (bit 4), `StorageClass` (bits 5-8), `isDeleted` (bit 9).
| `typeIdx`       | 8            | `int64_t`              | A key referencing a record in the **Types Table**. `nullnode` (-1) if the node has no type.
| `token`         | 8            | `int64_t`              | A key referencing a record in the **Tokens Table** (e.g., the name of a function or variable).
| `line`          | 4            | `unsigned int`         | The line number in the source file where the node begins.
| `column`        | 4            | `unsigned int`         | The column number in the source file where the node begins.
| `startOffset`   | 4            | `unsigned int`         | The starting byte offset of the node in the source file.
| `endOffset`     | 4            | `unsigned int`         | The ending byte offset of the node in the source file.
| `sourceFile`    | 8            | `int64_t`              | A key referencing the source file in the **Source Files Table** where this node is defined.

## SQLite Database Schema

When using the `-to-sqlite` command, the utility generates a SQLite database with the following schema. This provides a relational view of the AST data, making it easier to query and analyze.

### `SourceFiles` Table

Stores the paths of all source files referenced in the project.

| Column | Type    | Description                               |
|:-------|:--------|:------------------------------------------|
| `Id`   | INTEGER | The primary key of the source file.       |
| `Path` | TEXT    | The full path to the source file.         |

### `Tokens` Table

Stores all unique identifier and literal strings from the source code.

| Column | Type    | Description                               |
|:-------|:--------|:------------------------------------------|
| `Id`   | INTEGER | The primary key of the token.             |
| `Text` | TEXT    | The text of the token string.             |

### `TypeKinds` Table

Stores the different types of `CXTypeKind` enum values used by libClang for type classification.

| Column | Type    | Description                                                                 |
|:-------|:--------|:----------------------------------------------------------------------------|
| `Id`   | INTEGER | The primary key, corresponding to the `CXTypeKind` enum value.              |
| `Name` | TEXT    | The `libClang` kind name of the type (e.g., `CXType_Pointer`, `CXType_Record`). |

### `CursorKinds` Table

Stores the different types of `CXCursorKind` enum values used by libClang for cursor classification.

| Column | Type    | Description                                                                 |
|:-------|:--------|:----------------------------------------------------------------------------|
| `Id`   | INTEGER | The primary key, corresponding to the `CXCursorKind` enum value.            |
| `Name` | TEXT    | The `libClang` kind name of the cursor (e.g., `CXCursor_FunctionDecl`, `CXCursor_VarDecl`). |

### `Types` Table

Defines all types encountered in the source code.

| Column    | Type    | Description                                                                 |
|:----------|:--------|:----------------------------------------------------------------------------|
| `Id`      | INTEGER | The primary key of the type.                                                |
| `Hash`    | INTEGER | A hash of the type's properties for quick comparison.                       |
| `TokenId` | INTEGER | A foreign key to the `Tokens` table, representing the type's name.          |
| `Kind`    | INTEGER | A foreign key to the `TypeKinds` table, representing the type's kind.       |
| `IsConst` | INTEGER | A boolean flag (0 or 1) indicating if the type has a `const` qualifier.     |

### `TypeChildren` Table

A linking table to represent the parent-child relationships between types (e.g., for template arguments).

| Column    | Type    | Description                                      |
|:----------|:--------|:-------------------------------------------------|
| `TypeId`  | INTEGER | A foreign key to the `Types` table (the parent). |
| `ChildId` | INTEGER | A foreign key to the `Types` table (the child).  |

### `Nodes` Table

This is the main table, containing the serialized AST nodes.

| Column          | Type    | Description                                                                 |
|:----------------|:--------|:----------------------------------------------------------------------------|
| `Id`            | INTEGER | The primary key of the node.                                                |
| `CompilingFileId`| INTEGER | A foreign key to the `SourceFiles` table.                                   |
| `ParentId`      | INTEGER | A foreign key to this table, representing the parent node.                  |
| `ReferencedId`  | INTEGER | A foreign key to this table, representing a referenced node.                |
| `KindId`        | INTEGER | A foreign key to the `CursorKinds` table, representing the cursor kind.     |
| `Flags`         | INTEGER | A bitfield for various node properties.                                     |
| `TypeId`        | INTEGER | A foreign key to the `Types` table.                                         |
| `TokenId`       | INTEGER | A foreign key to the `Tokens` table.                                        |
| `Line`          | INTEGER | The line number in the source file.                                         |
| `Column`        | INTEGER | The column number in the source file.                                       |
| `StartOffset`   | INTEGER | The starting byte offset in the source file.                                |
| `EndOffset`     | INTEGER | The ending byte offset in the source file.                                  |
| `SourceFileId`  | INTEGER | A foreign key to the `SourceFiles` table where the node is defined.         |
