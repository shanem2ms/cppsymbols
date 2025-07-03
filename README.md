# C++ Symbols

A powerful command-line tool for parsing C++ source code and generating compressed Abstract Syntax Tree (AST) databases. Built with libClang, C++ Symbols creates `.osy` files that contain serialized representations of your C++ code's structure, enabling efficient code analysis and tooling.

## Features

- **AST Parsing**: Parse C++ source files using libClang to extract complete syntax trees
- **Compressed Storage**: Generate compact `.osy` files with zlib compression
- **Database Export**: Convert AST data to SQLite databases for advanced querying
- **File Merging**: Combine multiple `.osy` files into unified databases
- **Debug Tools**: Dump and validate `.osy` file contents
- **Cross-Platform**: Works on Windows, macOS, and Linux

## Prerequisites

- **CMake** 3.15 or higher
- **libClang** development libraries
- **zlib** compression library
- **C++17** compatible compiler
- **SQLite3** (for database export functionality)

## Building

### Using CMake

```bash
# Clone the repository
git clone https://github.com/shanem2ms/cppsymbols.git
cd cppsymbols

# Create build directory
mkdir build && cd build

# Configure and build
cmake ..
cmake --build . --config Release
```

### Platform-Specific Scripts

**Windows:**
```cmd
bld_win.bat
```

**macOS/Linux:**
```bash
./buildmac.sh
```

## Usage

### Parse C++ Source Files

Generate an OSY database from C++ source code:

```bash
symbols --compile main.cpp --output main.osy --include-directory /usr/include --define DEBUG=1
```

**Options:**
- `--compile <file>`: Specify the C++ source file to parse
- `--output <file>`: Set the output OSY file path
- `--include-directory <path>`: Add include directories (can be used multiple times)
- `--define <macro[=value]>`: Define preprocessor macros (can be used multiple times)

### Merge OSY Files

Combine multiple OSY files into a single database:

```bash
symbols --merge file1.osy file2.osy file3.osy --output merged.osy
```

### Export to SQLite

Convert OSY files to SQLite databases for SQL querying:

```bash
symbols --to-sqlite main.osy main.sqlite
```

### Debug and Validation

Dump OSY file contents for inspection:

```bash
symbols --dump main.osy
```

Validate OSY file structure:

```bash
symbols --validate main.osy
```

### Get Help

Display usage information:

```bash
symbols --help
```

## OSY File Format

The `.osy` format is a compressed binary format that stores:

- **Source Files Table**: Paths to all referenced source files
- **Tokens Table**: Unique identifiers and literals from the code
- **Types Table**: Type definitions with relationships and metadata
- **Nodes Table**: Complete AST nodes with location and relationship data

Each file begins with a 4-byte header indicating uncompressed size, followed by zlib-compressed data containing the serialized tables.

For detailed format specification, see [CPP_Symbols_Analysis.md](CPP_Symbols_Analysis.md).

## SQLite Schema

When exported to SQLite, the database contains these tables:

- `SourceFiles`: File paths and metadata
- `Tokens`: String literals and identifiers  
- `TypeKinds`: libClang type classifications
- `CursorKinds`: libClang cursor classifications
- `Types`: Type definitions with relationships
- `TypeChildren`: Parent-child type relationships
- `Nodes`: Complete AST node data with locations and references

## Examples

### Basic Project Analysis

```bash
# Parse a simple C++ file
symbols --compile hello.cpp --output hello.osy

# View the contents
symbols --dump hello.osy

# Export for SQL analysis
symbols --to-sqlite hello.osy hello.sqlite
```

### Large Project Processing

```bash
# Parse with system includes and defines
symbols --compile src/main.cpp \
  --output project.osy \
  --include-directory /usr/include/c++/11 \
  --include-directory ./include \
  --define NDEBUG \
  --define VERSION=1.0

# Merge multiple translation units
symbols --merge main.osy utils.osy core.osy --output complete.osy
```

### Code Analysis Workflow

```bash
# 1. Parse your codebase
symbols --compile src/*.cpp --output codebase.osy --include-directory ./include

# 2. Export to SQLite for analysis
symbols --to-sqlite codebase.osy analysis.sqlite

# 3. Query with SQL (example)
sqlite3 analysis.sqlite "SELECT COUNT(*) FROM Nodes WHERE KindId = (SELECT Id FROM CursorKinds WHERE Name = 'CXCursor_FunctionDecl')"
```

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is open source. Please check the repository for license details.

## Related Projects

- **API Viewer**: The `apiview/` directory contains a C# WPF application for visualizing OSY files
- **Scripts**: Various analysis scripts for processing the generated databases

## Support

For issues, questions, or contributions, please visit the [GitHub repository](https://github.com/shanem2ms/cppsymbols).
