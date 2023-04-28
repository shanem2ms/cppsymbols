cmake_minimum_required(VERSION 3.15.0 FATAL_ERROR)
set(CMAKE_SYSTEM_VERSION 10.0 CACHE STRING "" FORCE)
set(CMAKE_EXPORT_COMPILE_COMMANDS on)

include(${VCPKG_DIR}/scripts/buildsystems/vcpkg.cmake)
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
find_package(unofficial-evpp CONFIG REQUIRED)

find_file(CLANGLIB libclang.lib REQUIRED HINTS "${LLVM_DIR}/lib")
find_file(CLANGHDR BuildSystem.h REQUIRED HINTS "${LLVM_DIR}/include/clang-c")


set(Main_Files
	Compiler.cpp
	CPPSourceFile.cpp
	DbMgr.cpp
	Node.cpp
	Precomp.cpp
	symbols.cpp
)

add_executable(${PROJECT_NAME} ${Main_Files})

#string(REPLACE "/O2" "/O10" CMAKE_CXX_FLAGS_RELEASE "${CMAKE_CXX_FLAGS_RELEASE}")
#string(REPLACE "/O2" "/O10" CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS}")

if (DEFINED ${CPPCLR})
	add_compile_options(/clr)
	string(REPLACE "/EHsc" "" CMAKE_CXX_FLAGS_DEBUG "${CMAKE_CXX_FLAGS_DEBUG}")
	string(REPLACE "/RTC1" "" CMAKE_CXX_FLAGS_DEBUG "${CMAKE_CXX_FLAGS_DEBUG}")
endif()

set(VCPKG_INSTALL_PATH ${VCPKG_DIR}/installed/${VCPKG_TARGET_TRIPLET})

target_include_directories(${PROJECT_NAME} PUBLIC
    "${VCPKG_INSTALL_PATH}/include"
	"${LLVM_DIR}/include")
    
target_link_libraries(${PROJECT_NAME} LINK_PUBLIC 
       ${CLANGLIB}
	   unofficial::evpp::evmc_static
	   )

set (LIBNAME lib${PROJECT_NAME})
add_library(${LIBNAME} SHARED  ${Main_Files})

target_include_directories(${LIBNAME} PUBLIC
    "${VCPKG_INSTALL_PATH}/include"
	"${LLVM_DIR}/include")

target_link_options(${LIBNAME} PUBLIC /MAPINFO:EXPORTS /MAP)

target_link_libraries(${LIBNAME} LINK_PUBLIC 
    ${CLANGLIB}
	ZLIB::ZLIB)
find_file(CLANGDLL libclang.dll REQUIRED)