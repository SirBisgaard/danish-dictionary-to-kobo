# This script will create the C shim.
# Run this script from the c-shim directory.
# ubuntu dependencies: build-essential, cmake, git

set -e  # Exit on any error

echo "========================================"
echo "Danish Dictionary to Kobo"
echo "Building libmarisa C shim"
echo "========================================"
echo ""

# Clone the marisa-trie source.
echo "[1/6] Cloning marisa-trie repository..."
if git clone https://github.com/s-yata/marisa-trie.git; then
    echo "✓ Repository cloned successfully"
else
    echo "✗ Failed to clone marisa-trie repository"
    exit 1
fi
echo ""

# Build the Marisa-Trie library.
echo "[2/6] Configuring marisa-trie with CMake..."
cd marisa-trie || exit

mkdir build
cd build || exit

if cmake -S .. -B . \
  -DCMAKE_BUILD_TYPE=Release \
  -DBUILD_SHARED_LIBS=ON \
  -DENABLE_NATIVE_CODE=ON \
  -DCMAKE_POSITION_INDEPENDENT_CODE=ON > /dev/null; then
    echo "✓ CMake configuration successful"
else
    echo "✗ CMake configuration failed"
    exit 1
fi
echo ""

echo "[3/6] Building marisa-trie library..."
if cmake --build . --target marisa; then
    echo "✓ marisa-trie library built successfully"
else
    echo "✗ marisa-trie library build failed"
    exit 1
fi
echo ""

# Create the C shim.
echo "[4/6] Preparing C shim directory..."
cd ../bindings || exit
mkdir c
cd c || exit

# Copy the C shim code from the c-shim directory.
if cp ../../../marisa_c_shim* ./; then
    echo "✓ C shim source files copied"
else
    echo "✗ Failed to copy C shim source files"
    exit 1
fi
echo ""

echo "[5/6] Compiling C shim library..."
if g++ -std=c++17 -fPIC -shared \
  marisa_c_shim.cpp \
  -I../../include \
  -L../../build \
  -l:libmarisa.so \
  -o libmarisa.so; then
    echo "✓ C shim compiled successfully"
else
    echo "✗ C shim compilation failed"
    exit 1
fi
echo ""

# Copy the newly compiled C shim.
echo "[6/6] Copying libmarisa.so to c-shim directory..."
if cp libmarisa.so ../../../; then
    echo "✓ libmarisa.so copied successfully"
else
    echo "✗ Failed to copy libmarisa.so"
    exit 1
fi
echo ""

# Remove the marisa-trie repository.
echo "Cleaning up..."
cd ../../../
rm -r marisa-trie
echo "✓ Cleanup complete"
echo ""

echo "========================================"
echo "✓ BUILD SUCCESSFUL!"
echo "========================================"
echo "Output: libmarisa.so"
echo ""
