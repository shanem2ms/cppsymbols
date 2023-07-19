mkdir build
cd build
cmake -G Ninja -D "MAKE_SYMBOLS"="true" -D "VCPKG_DIR":FILEPATH="%FLASHROOT%/../vcpkg" -D "LLVM_DIR"="c:/Program Files/LLVM" -D "VCPKG_TARGET_TRIPLET"="x64-windows" -D "CMAKE_BUILD_TYPE"="Release" ..
cmake --build .
copy /Y cppsymbols.exe c:\flash\install
copy /Y zlib1.dll c:\flash\install