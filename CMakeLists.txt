cmake_minimum_required(VERSION 3.15.0 FATAL_ERROR)
set(CMAKE_SYSTEM_VERSION 10.0 CACHE STRING "" FORCE)

include(${VCPKG_DIR}/scripts/buildsystems/vcpkg.cmake)
set(CMAKE_EXPORT_COMPILE_COMMANDS on)
set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)

set(CMAKE_CONFIGURATION_TYPES "Debug;Release") 
project(symbols CXX)

set(PROJECT_NAME symbols)

set(CMAKE_XCODE_ATTRIBUTE_PRODUCT_BUNDLE_IDENTIFIER ${BUNDLE_ID})
set(CMAKE_XCODE_ATTRIBUTE_DEVELOPMENT_TEAM "73CP3TPHE9")

find_package(Clang CONFIG REQUIRED)
find_package(unofficial-sqlite3 CONFIG REQUIRED)
find_package(sqliteOrm CONFIG REQUIRED)
find_package(pugixml CONFIG REQUIRED)
find_package(cppcoro CONFIG REQUIRED)
find_package(fmt CONFIG REQUIRED)

set(Main_Files
	Compiler.cpp
	CPPSourceFile.cpp
	DbMgr.cpp
	Node.cpp
	Precomp.cpp
	symbols.cpp
	VCProject.cpp
)

add_executable(${PROJECT_NAME} WIN32 ${Main_Files})

add_compile_options(/Od)
message("${COMPILE_OPTIONS}")

set_target_properties(${PROJECT_NAME} PROPERTIES COMPILE_OPTIONS "/Od")
set_target_properties(${PROJECT_NAME} PROPERTIES
LINK_FLAGS /SUBSYSTEM:CONSOLE
)

#string(REPLACE "/O2" "/O10" CMAKE_CXX_FLAGS_RELEASE "${CMAKE_CXX_FLAGS_RELEASE}")
#string(REPLACE "/O2" "/O10" CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS}")
#message(${CMAKE_CXX_FLAGS})
#message(${CMAKE_CXX_FLAGS_RELEASE})

target_include_directories(${PROJECT_NAME} PUBLIC
    "${VCPKG_INSTALL_PATH}/include")
    

target_link_libraries(${PROJECT_NAME} LINK_PUBLIC 
       libclang
	   cppcoro
	   unofficial::sqlite3::sqlite3
	   sqlite_orm::sqlite_orm
	   pugixml pugixml::shared pugixml::pugixml
		Synchronization.lib)