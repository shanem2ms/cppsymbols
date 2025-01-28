# Include the vcpkg toolchain file (if needed)
include("c:/vcpkg/scripts/buildsystems/vcpkg.cmake")

# Define the paths to your custom tool
set(CMAKE_C_COMPILER "D:/cppsymbols/build/x64-release/cppsymbols.exe")
set(CMAKE_CXX_COMPILER "D:/cppsymbols/build/x64-release/cppsymbols.exe")

# Disable all compiler checks
set(CMAKE_C_COMPILER_WORKS TRUE)
set(CMAKE_CXX_COMPILER_WORKS TRUE)
set(CMAKE_C_STANDARD_REQUIRED FALSE)
set(CMAKE_CXX_STANDARD_REQUIRED FALSE)

# Pretend the "compiler" is a valid compiler with dummy values
set(CMAKE_C_COMPILER_ID "CustomCompiler")
set(CMAKE_CXX_COMPILER_ID "CustomCompiler")
set(CMAKE_C_COMPILER_VERSION "1.0")
set(CMAKE_CXX_COMPILER_VERSION "1.0")

# Pretend it supports some standard (to satisfy project requirements)
set(CMAKE_C_STANDARD 11)
set(CMAKE_CXX_STANDARD 17)

# Disable detection of features and properties
set(CMAKE_CXX_KNOWN_FEATURES "")
set(CMAKE_C_KNOWN_FEATURES "")
set(CMAKE_C_ABI_COMPILED TRUE)
set(CMAKE_CXX_ABI_COMPILED TRUE)

# Prevent CMake from testing the compiler for validity
set(CMAKE_C_COMPILER_EXTERNAL_TOOLCHAIN "D:/cppsymbols/build/x64-release/cppsymbols.exe")
set(CMAKE_CXX_COMPILER_EXTERNAL_TOOLCHAIN "D:/cppsymbols/build/x64-release/cppsymbols.exe")
