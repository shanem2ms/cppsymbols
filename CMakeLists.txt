cmake_minimum_required(VERSION 3.15.0 FATAL_ERROR)
set(CMAKE_SYSTEM_VERSION 10.0 CACHE STRING "" FORCE)
set(CMAKE_EXPORT_COMPILE_COMMANDS on)

set(CMAKE_EXPORT_COMPILE_COMMANDS on)
set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)

set(CMAKE_CONFIGURATION_TYPES "Debug;Release") 
project(cppsymbols C CXX)

set(PROJECT_NAME cppsymbols)

set(CMAKE_XCODE_ATTRIBUTE_PRODUCT_BUNDLE_IDENTIFIER ${BUNDLE_ID})
set(CMAKE_XCODE_ATTRIBUTE_DEVELOPMENT_TEAM "73CP3TPHE9")

find_package(ZLIB REQUIRED)
find_package(fmt CONFIG REQUIRED)
find_package(Libevent CONFIG REQUIRED)
find_package(unofficial-sqlite3 CONFIG REQUIRED)

find_file(CLANGLIB libclang.lib REQUIRED HINTS "${LLVM_DIR}/lib")
find_file(CLANGHDR BuildSystem.h REQUIRED HINTS "${LLVM_DIR}/include/clang-c")


set(Main_Files
	Compiler.cpp
	CPPSourceFile.cpp
	DbMgr.cpp
	Node.cpp
	Precomp.cpp
	symbols.cpp
	ClangDefs.cpp
	OsyToSqlite.cpp
)

add_executable(${PROJECT_NAME} ${Main_Files})

if (DEFINED ${CPPCLR})
	add_compile_options(/clr)
	string(REPLACE "/EHsc" "" CMAKE_CXX_FLAGS_DEBUG "${CMAKE_CXX_FLAGS_DEBUG}")
	string(REPLACE "/RTC1" "" CMAKE_CXX_FLAGS_DEBUG "${CMAKE_CXX_FLAGS_DEBUG}")
endif()

add_compile_definitions(FMT_UNICODE=0)
set(VCPKG_INSTALL_PATH ${VCPKG_INSTALLED_DIR}/${VCPKG_TARGET_TRIPLET})

target_include_directories(${PROJECT_NAME} PRIVATE
  "${VCPKG_INSTALL_PATH}/include"
	"${LLVM_DIR}/include")
target_link_directories(${PROJECT_NAME} PRIVATE 
	"${VCPKG_INSTALL_PATH}/lib")
    
target_link_libraries(${PROJECT_NAME} LINK_PUBLIC 
	   libevent::core
     ${CLANGLIB}
	   ZLIB::ZLIB
     unofficial::sqlite3::sqlite3
	   )

message(STATUS "ZLIB runtime DLL: ${ZLIB_DLL_PATH}")

find_file(CLANGDLL libclang.dll REQUIRED)

    # Install the shared library
install(TARGETS ${PROJECT_NAME}
    EXPORT ${PROJECT_NAME}Config
    LIBRARY DESTINATION lib
    ARCHIVE DESTINATION lib
    RUNTIME DESTINATION bin
    INCLUDES DESTINATION include
)

# Install export set
install(EXPORT ${PROJECT_NAME}Config
    FILE ${PROJECT_NAME}.cmake
    NAMESPACE cppsym::
    DESTINATION share/cppsymbols
)

# Set the path to symbols.exe
set(SYMBOLS_EXE_PATH ${CMAKE_BINARY_DIR}/${PROJECT_NAME}.exe)
# Set the path to the vcpkg toolchain file
set(VCPKG_TOOLCHAIN_FILE ${CMAKE_TOOLCHAIN_FILE})

# Configure the toolchain file
configure_file(
    ${CMAKE_SOURCE_DIR}/cppsymbols_toolchain.cmake.in
    ${CMAKE_BINARY_DIR}/cppsymbols_toolchain.cmake
    @ONLY
)



install(FILES
  ${CMAKE_BINARY_DIR}/cppsymbols_toolchain.cmake
  DESTINATION share)

install(FILES
  ${CLANGDLL}
  ${CMAKE_BINARY_DIR}/zlib1.dll
  ${CMAKE_BINARY_DIR}/sqlite3.dll
  DESTINATION bin)

  # Generate Config files
include(CMakePackageConfigHelpers)

# Install Config files
install(FILES
    "${CMAKE_CURRENT_BINARY_DIR}/cppsymbols.cmake"
    "${CMAKE_CURRENT_BINARY_DIR}/cppsymbolsVersion.cmake"
    DESTINATION share/rrs
)

configure_package_config_file(
    "${CMAKE_CURRENT_SOURCE_DIR}/cppsymbols.cmake.in"
    "${CMAKE_CURRENT_BINARY_DIR}/cppsymbols.cmake"
    INSTALL_DESTINATION share/${PROJECT_NAME}
)

write_basic_package_version_file(
    "${CMAKE_CURRENT_BINARY_DIR}/cppsymbolsVersion.cmake"
    VERSION 1.0.0
    COMPATIBILITY AnyNewerVersion
)
