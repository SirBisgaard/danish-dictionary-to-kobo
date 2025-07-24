# This scrip will create the C shim.
# Run the script in a folder that is not a git repository.

cd .

# Clone the source.
git clone https://github.com/SirBisgaard/danish-dictionary-to-kobo.git
git clone https://github.com/s-yata/marisa-trie.git

# Build the Marisa-Trie library.
cd marisa-trie || exit

mkdir build
cd build || exit

cmake -S .. -B . \
  -DCMAKE_BUILD_TYPE=Release \
  -DBUILD_SHARED_LIBS=ON \
  -DENABLE_NATIVE_CODE=ON \
  -DCMAKE_POSITION_INDEPENDENT_CODE=ON
cmake --build . --target marisa

# Create the C shim.
cd ../bindings || exit
mkdir c
cd c || exit

# Copy the C shim code that only includes what is needed to create the Marisa-Trie.
cp ../../../danish-dictionary-to-kobo/c-shim/marisa_c_shim* ./

g++ -std=c++17 -fPIC -shared \
  marisa_c_shim.cpp \
  -I../../build/include         \
  -L../../build                  \
  -l:libmarisa.so                \
  -o libmarisa.so

# Copy the newly compiled C shim.
cp libmarisa.so ../../../

# Remove the repositories repository.
cd ../../../
rm -r marisa-trie
rm -r danish-dictionary-to-kobo
