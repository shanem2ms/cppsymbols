rmdir /s /q build
rmdir /s /q install
mkdir build
pushd build
cmake --fresh --preset x64-release -G Ninja ..
cmake --build ./x64-release
cmake --install ./x64-release
popd