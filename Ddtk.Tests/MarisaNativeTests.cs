using System.Text;
using Ddtk.DataAccess;
using FluentAssertions;

namespace Ddtk.Tests;

/// <summary>
/// Integration tests for MarisaNative class.
/// Tests verify that the native libmarisa.so library loads and functions correctly.
/// </summary>
public class MarisaNativeTests : IDisposable
{
    private readonly string testDirectory;

    public MarisaNativeTests()
    {
        // Use test execution directory for temp files
        testDirectory = Path.Combine(AppContext.BaseDirectory, "test-temp");
        Directory.CreateDirectory(testDirectory);
    }

    public void Dispose()
    {
        // Cleanup test directory
        if (Directory.Exists(testDirectory))
        {
            Directory.Delete(testDirectory, recursive: true);
        }
    }

    [Fact]
    public void BindMarisa_ShouldLoadNativeLibrary_WithoutException()
    {
        // Arrange
        // (no setup needed)

        // Act
        Action act = () => MarisaNative.BindMarisa();

        // Assert
        act.Should().NotThrow("the native library should load successfully");
    }

    [Fact]
    public void CreateBuilder_ShouldReturnValidPointer_WhenCalled()
    {
        // Arrange
        MarisaNative.BindMarisa();

        // Act
        var builderPtr = MarisaNative.CreateBuilder();

        // Assert
        builderPtr.Should().NotBe(IntPtr.Zero, "builder pointer should be valid");

        // Cleanup
        MarisaNative.DestroyBuilder(builderPtr);
    }

    [Fact]
    public void PushIntoBuilder_ShouldReturnSuccess_WhenAddingKey()
    {
        // Arrange
        MarisaNative.BindMarisa();
        var builderPtr = MarisaNative.CreateBuilder();
        var key = "testword"u8.ToArray();

        // Act
        var result = MarisaNative.PushIntoBuilder(builderPtr, key, key.Length);

        // Assert
        result.Should().Be(0, "push operation should succeed with return code 0");

        // Cleanup
        MarisaNative.DestroyBuilder(builderPtr);
    }

    [Fact]
    public void PushIntoBuilder_ShouldAcceptMultipleKeys_WhenCalledRepeatedly()
    {
        // Arrange
        MarisaNative.BindMarisa();
        var builderPtr = MarisaNative.CreateBuilder();
        var keys = new[] { "apple", "banana", "cherry", "date" };

        // Act
        var results = keys
            .Select(k => Encoding.UTF8.GetBytes(k))
            .Select(bytes => MarisaNative.PushIntoBuilder(builderPtr, bytes, bytes.Length))
            .ToList();

        // Assert
        results.Should().OnlyContain(r => r == 0, "all push operations should succeed");

        // Cleanup
        MarisaNative.DestroyBuilder(builderPtr);
    }

    [Fact]
    public void BuildBuilder_ShouldReturnValidTrie_WhenKeysAdded()
    {
        // Arrange
        MarisaNative.BindMarisa();
        var builderPtr = MarisaNative.CreateBuilder();
        var key = "word"u8.ToArray();
        MarisaNative.PushIntoBuilder(builderPtr, key, key.Length);

        // Act
        var triePtr = MarisaNative.BuildBuilder(builderPtr);

        // Assert
        triePtr.Should().NotBe(IntPtr.Zero, "trie pointer should be valid after build");

        // Cleanup
        MarisaNative.DestroyTrie(triePtr);
        MarisaNative.DestroyBuilder(builderPtr);
    }

    [Fact]
    public void BuildBuilder_ShouldReturnValidTrie_WhenNoKeysAdded()
    {
        // Arrange
        MarisaNative.BindMarisa();
        var builderPtr = MarisaNative.CreateBuilder();

        // Act
        var triePtr = MarisaNative.BuildBuilder(builderPtr);

        // Assert
        triePtr.Should().NotBe(IntPtr.Zero, "trie pointer should be valid even with empty trie");

        // Cleanup
        MarisaNative.DestroyTrie(triePtr);
        MarisaNative.DestroyBuilder(builderPtr);
    }

    [Fact]
    public void SaveTrie_ShouldCreateFile_WhenTrieIsBuilt()
    {
        // Arrange
        MarisaNative.BindMarisa();
        var builderPtr = MarisaNative.CreateBuilder();
        var key = "savetest"u8.ToArray();
        MarisaNative.PushIntoBuilder(builderPtr, key, key.Length);
        var triePtr = MarisaNative.BuildBuilder(builderPtr);
        var savePath = Path.Combine(testDirectory, "test-trie.bin");

        // Act
        MarisaNative.SaveTrie(triePtr, savePath);

        // Assert
        File.Exists(savePath).Should().BeTrue("trie file should be created");
        new FileInfo(savePath).Length.Should().BeGreaterThan(0, "trie file should not be empty");

        // Cleanup
        MarisaNative.DestroyTrie(triePtr);
        MarisaNative.DestroyBuilder(builderPtr);
    }

    [Fact]
    public void SaveTrie_ShouldCreateValidFile_WithMultipleKeys()
    {
        // Arrange
        MarisaNative.BindMarisa();
        var builderPtr = MarisaNative.CreateBuilder();
        var keys = new[] { "apple", "application", "apply", "banana", "band" };
        
        foreach (var key in keys)
        {
            var bytes = Encoding.UTF8.GetBytes(key);
            MarisaNative.PushIntoBuilder(builderPtr, bytes, bytes.Length);
        }

        var triePtr = MarisaNative.BuildBuilder(builderPtr);
        var savePath = Path.Combine(testDirectory, "multi-key-trie.bin");

        // Act
        MarisaNative.SaveTrie(triePtr, savePath);

        // Assert
        File.Exists(savePath).Should().BeTrue("trie file should be created");
        var fileInfo = new FileInfo(savePath);
        fileInfo.Length.Should().BeGreaterThan(0, "trie file should contain data");

        // Cleanup
        MarisaNative.DestroyTrie(triePtr);
        MarisaNative.DestroyBuilder(builderPtr);
    }

    [Fact]
    public void DestroyBuilder_ShouldNotThrow_WhenCalledWithValidPointer()
    {
        // Arrange
        MarisaNative.BindMarisa();
        var builderPtr = MarisaNative.CreateBuilder();

        // Act
        Action act = () => MarisaNative.DestroyBuilder(builderPtr);

        // Assert
        act.Should().NotThrow("destroying a valid builder should succeed");
    }

    [Fact]
    public void DestroyTrie_ShouldNotThrow_WhenCalledWithValidPointer()
    {
        // Arrange
        MarisaNative.BindMarisa();
        var builderPtr = MarisaNative.CreateBuilder();
        var triePtr = MarisaNative.BuildBuilder(builderPtr);

        // Act
        Action act = () => MarisaNative.DestroyTrie(triePtr);

        // Assert
        act.Should().NotThrow("destroying a valid trie should succeed");

        // Cleanup
        MarisaNative.DestroyBuilder(builderPtr);
    }

    [Fact]
    public void FullWorkflow_ShouldCompleteSuccessfully_WithDanishWords()
    {
        // Arrange
        MarisaNative.BindMarisa();
        var builderPtr = MarisaNative.CreateBuilder();
        var danishWords = new[] { "hund", "kat", "fugil", "fisk", "hest" };
        var savePath = Path.Combine(testDirectory, "danish-trie.bin");

        // Act
        foreach (var word in danishWords)
        {
            var bytes = Encoding.UTF8.GetBytes(word);
            var result = MarisaNative.PushIntoBuilder(builderPtr, bytes, bytes.Length);
            result.Should().Be(0, $"push for word '{word}' should succeed");
        }

        var triePtr = MarisaNative.BuildBuilder(builderPtr);
        MarisaNative.SaveTrie(triePtr, savePath);

        // Assert
        triePtr.Should().NotBe(IntPtr.Zero, "trie should be built successfully");
        File.Exists(savePath).Should().BeTrue("trie file should be saved");

        // Cleanup
        MarisaNative.DestroyTrie(triePtr);
        MarisaNative.DestroyBuilder(builderPtr);
    }

    [Fact]
    public void PushIntoBuilder_ShouldHandleUtf8Characters_InDanishWords()
    {
        // Arrange
        MarisaNative.BindMarisa();
        var builderPtr = MarisaNative.CreateBuilder();
        var danishWordsWithSpecialChars = new[] { "æble", "øl", "år", "bøtte", "grød" };

        // Act & Assert
        foreach (var word in danishWordsWithSpecialChars)
        {
            var bytes = Encoding.UTF8.GetBytes(word);
            var result = MarisaNative.PushIntoBuilder(builderPtr, bytes, bytes.Length);
            result.Should().Be(0, $"push for word '{word}' with UTF-8 chars should succeed");
        }

        // Cleanup
        MarisaNative.DestroyBuilder(builderPtr);
    }

    [Fact]
    public void LoadTrie_ShouldReturnValidPointer_WhenFileExists()
    {
        // Arrange
        MarisaNative.BindMarisa();
        var builderPtr = MarisaNative.CreateBuilder();
        var key = "loadtest"u8.ToArray();
        MarisaNative.PushIntoBuilder(builderPtr, key, key.Length);
        var triePtr = MarisaNative.BuildBuilder(builderPtr);
        var savePath = Path.Combine(testDirectory, "load-test-trie.bin");
        MarisaNative.SaveTrie(triePtr, savePath);
        MarisaNative.DestroyTrie(triePtr);

        // Act
        var loadedTriePtr = MarisaNative.LoadTrie(savePath);

        // Assert
        loadedTriePtr.Should().NotBe(IntPtr.Zero, "loaded trie pointer should be valid");

        // Cleanup
        MarisaNative.DestroyTrie(loadedTriePtr);
        MarisaNative.DestroyBuilder(builderPtr);
    }

    [Fact]
    public void LoadTrie_ShouldReturnZero_WhenFileDoesNotExist()
    {
        // Arrange
        MarisaNative.BindMarisa();
        var nonExistentPath = Path.Combine(testDirectory, "non-existent-trie.bin");

        // Act
        var triePtr = MarisaNative.LoadTrie(nonExistentPath);

        // Assert
        triePtr.Should().Be(IntPtr.Zero, "loading non-existent file should return null pointer");
    }

    [Fact]
    public void GetNumKeys_ShouldReturnCorrectCount_WhenTrieHasKeys()
    {
        // Arrange
        MarisaNative.BindMarisa();
        var builderPtr = MarisaNative.CreateBuilder();
        var keys = new[] { "apple", "banana", "cherry", "date", "elderberry" };
        
        foreach (var key in keys)
        {
            var bytes = Encoding.UTF8.GetBytes(key);
            MarisaNative.PushIntoBuilder(builderPtr, bytes, bytes.Length);
        }

        var triePtr = MarisaNative.BuildBuilder(builderPtr);

        // Act
        var numKeys = MarisaNative.GetNumKeys(triePtr);

        // Assert
        ((int)numKeys).Should().Be(keys.Length, "trie should contain exactly the number of keys added");

        // Cleanup
        MarisaNative.DestroyTrie(triePtr);
        MarisaNative.DestroyBuilder(builderPtr);
    }

    [Fact]
    public void GetNumKeys_ShouldReturnZero_WhenTrieIsEmpty()
    {
        // Arrange
        MarisaNative.BindMarisa();
        var builderPtr = MarisaNative.CreateBuilder();
        var triePtr = MarisaNative.BuildBuilder(builderPtr);

        // Act
        var numKeys = MarisaNative.GetNumKeys(triePtr);

        // Assert
        ((int)numKeys).Should().Be(0, "empty trie should have zero keys");

        // Cleanup
        MarisaNative.DestroyTrie(triePtr);
        MarisaNative.DestroyBuilder(builderPtr);
    }

    [Fact]
    public void CreateAgent_ShouldReturnValidPointer_WhenCalled()
    {
        // Arrange
        MarisaNative.BindMarisa();

        // Act
        var agentPtr = MarisaNative.CreateAgent();

        // Assert
        agentPtr.Should().NotBe(IntPtr.Zero, "agent pointer should be valid");

        // Cleanup
        MarisaNative.DestroyAgent(agentPtr);
    }

    [Fact]
    public void DestroyAgent_ShouldNotThrow_WhenCalledWithValidPointer()
    {
        // Arrange
        MarisaNative.BindMarisa();
        var agentPtr = MarisaNative.CreateAgent();

        // Act
        Action act = () => MarisaNative.DestroyAgent(agentPtr);

        // Assert
        act.Should().NotThrow("destroying a valid agent should succeed");
    }

    [Fact]
    public void LookupKey_ShouldReturnOne_WhenKeyExists()
    {
        // Arrange
        MarisaNative.BindMarisa();
        var builderPtr = MarisaNative.CreateBuilder();
        var keys = new[] { "apple", "banana", "cherry" };
        
        foreach (var key in keys)
        {
            var bytes = Encoding.UTF8.GetBytes(key);
            MarisaNative.PushIntoBuilder(builderPtr, bytes, bytes.Length);
        }

        var triePtr = MarisaNative.BuildBuilder(builderPtr);
        var agentPtr = MarisaNative.CreateAgent();
        var searchKey = "banana"u8.ToArray();

        // Act
        MarisaNative.SetAgentQuery(agentPtr, searchKey, searchKey.Length);
        var result = MarisaNative.LookupKey(triePtr, agentPtr);

        // Assert
        result.Should().Be(1, "lookup should return 1 when key exists in trie");

        // Cleanup
        MarisaNative.DestroyAgent(agentPtr);
        MarisaNative.DestroyTrie(triePtr);
        MarisaNative.DestroyBuilder(builderPtr);
    }

    [Fact]
    public void LookupKey_ShouldReturnZero_WhenKeyDoesNotExist()
    {
        // Arrange
        MarisaNative.BindMarisa();
        var builderPtr = MarisaNative.CreateBuilder();
        var keys = new[] { "apple", "banana", "cherry" };
        
        foreach (var key in keys)
        {
            var bytes = Encoding.UTF8.GetBytes(key);
            MarisaNative.PushIntoBuilder(builderPtr, bytes, bytes.Length);
        }

        var triePtr = MarisaNative.BuildBuilder(builderPtr);
        var agentPtr = MarisaNative.CreateAgent();
        var searchKey = "orange"u8.ToArray();

        // Act
        MarisaNative.SetAgentQuery(agentPtr, searchKey, searchKey.Length);
        var result = MarisaNative.LookupKey(triePtr, agentPtr);

        // Assert
        result.Should().Be(0, "lookup should return 0 when key does not exist in trie");

        // Cleanup
        MarisaNative.DestroyAgent(agentPtr);
        MarisaNative.DestroyTrie(triePtr);
        MarisaNative.DestroyBuilder(builderPtr);
    }

    [Fact]
    public void LookupKey_ShouldWorkWithDanishCharacters_WhenKeyExists()
    {
        // Arrange
        MarisaNative.BindMarisa();
        var builderPtr = MarisaNative.CreateBuilder();
        var danishWords = new[] { "æble", "øl", "år", "bøtte" };
        
        foreach (var word in danishWords)
        {
            var bytes = Encoding.UTF8.GetBytes(word);
            MarisaNative.PushIntoBuilder(builderPtr, bytes, bytes.Length);
        }

        var triePtr = MarisaNative.BuildBuilder(builderPtr);
        var agentPtr = MarisaNative.CreateAgent();
        var searchKey = Encoding.UTF8.GetBytes("øl");

        // Act
        MarisaNative.SetAgentQuery(agentPtr, searchKey, searchKey.Length);
        var result = MarisaNative.LookupKey(triePtr, agentPtr);

        // Assert
        result.Should().Be(1, "lookup should find Danish words with special characters");

        // Cleanup
        MarisaNative.DestroyAgent(agentPtr);
        MarisaNative.DestroyTrie(triePtr);
        MarisaNative.DestroyBuilder(builderPtr);
    }

    [Fact]
    public void ReverseLookup_ShouldReturnSuccess_WhenKeyIdIsValid()
    {
        // Arrange
        MarisaNative.BindMarisa();
        var builderPtr = MarisaNative.CreateBuilder();
        var keys = new[] { "apple", "banana", "cherry" };
        
        foreach (var key in keys)
        {
            var bytes = Encoding.UTF8.GetBytes(key);
            MarisaNative.PushIntoBuilder(builderPtr, bytes, bytes.Length);
        }

        var triePtr = MarisaNative.BuildBuilder(builderPtr);
        var agentPtr = MarisaNative.CreateAgent();

        // Act
        var result = MarisaNative.ReverseLookup(triePtr, agentPtr, UIntPtr.Zero);

        // Assert
        result.Should().Be(0, "reverse lookup should return 0 on success");

        // Cleanup
        MarisaNative.DestroyAgent(agentPtr);
        MarisaNative.DestroyTrie(triePtr);
        MarisaNative.DestroyBuilder(builderPtr);
    }

    [Fact]
    public void GetAgentKey_ShouldRetrieveKey_AfterReverseLookup()
    {
        // Arrange
        MarisaNative.BindMarisa();
        var builderPtr = MarisaNative.CreateBuilder();
        var keys = new[] { "apple", "banana", "cherry" };
        
        foreach (var key in keys)
        {
            var bytes = Encoding.UTF8.GetBytes(key);
            MarisaNative.PushIntoBuilder(builderPtr, bytes, bytes.Length);
        }

        var triePtr = MarisaNative.BuildBuilder(builderPtr);
        var agentPtr = MarisaNative.CreateAgent();
        var buffer = new byte[1024];

        // Act
        MarisaNative.ReverseLookup(triePtr, agentPtr, new UIntPtr(1)); // Get second key (banana)
        var result = MarisaNative.GetAgentKey(agentPtr, buffer, buffer.Length, out var length);
        var retrievedKey = Encoding.UTF8.GetString(buffer, 0, length);

        // Assert
        result.Should().Be(0, "get agent key should return 0 on success");
        retrievedKey.Should().Be("banana", "retrieved key should match the key at index 1");

        // Cleanup
        MarisaNative.DestroyAgent(agentPtr);
        MarisaNative.DestroyTrie(triePtr);
        MarisaNative.DestroyBuilder(builderPtr);
    }

    [Fact]
    public void GetAgentKey_ShouldHandleDanishCharacters_AfterReverseLookup()
    {
        // Arrange
        MarisaNative.BindMarisa();
        var builderPtr = MarisaNative.CreateBuilder();
        var danishWords = new[] { "æble", "øl", "år" };
        
        foreach (var word in danishWords)
        {
            var bytes = Encoding.UTF8.GetBytes(word);
            MarisaNative.PushIntoBuilder(builderPtr, bytes, bytes.Length);
        }

        var triePtr = MarisaNative.BuildBuilder(builderPtr);
        var agentPtr = MarisaNative.CreateAgent();
        var buffer = new byte[1024];

        // Act - retrieve all keys and check they're all valid Danish words
        var retrievedKeys = new List<string>();
        var numKeys = (int)MarisaNative.GetNumKeys(triePtr);
        for (int i = 0; i < numKeys; i++)
        {
            MarisaNative.ReverseLookup(triePtr, agentPtr, new UIntPtr((uint)i));
            MarisaNative.GetAgentKey(agentPtr, buffer, buffer.Length, out var length);
            var key = Encoding.UTF8.GetString(buffer, 0, length);
            retrievedKeys.Add(key);
        }

        // Assert
        retrievedKeys.Should().BeEquivalentTo(danishWords, "all Danish words with UTF-8 characters should be retrievable");

        // Cleanup
        MarisaNative.DestroyAgent(agentPtr);
        MarisaNative.DestroyTrie(triePtr);
        MarisaNative.DestroyBuilder(builderPtr);
    }

    [Fact]
    public void LoadTrie_AndLookup_ShouldFindKeys_AfterSavingAndLoading()
    {
        // Arrange
        MarisaNative.BindMarisa();
        var builderPtr = MarisaNative.CreateBuilder();
        var keys = new[] { "apple", "banana", "cherry", "date" };
        var savePath = Path.Combine(testDirectory, "lookup-test-trie.bin");
        
        foreach (var key in keys)
        {
            var bytes = Encoding.UTF8.GetBytes(key);
            MarisaNative.PushIntoBuilder(builderPtr, bytes, bytes.Length);
        }

        var triePtr = MarisaNative.BuildBuilder(builderPtr);
        MarisaNative.SaveTrie(triePtr, savePath);
        MarisaNative.DestroyTrie(triePtr);

        // Act - Load trie and perform lookup
        var loadedTriePtr = MarisaNative.LoadTrie(savePath);
        var agentPtr = MarisaNative.CreateAgent();
        var searchKey = "cherry"u8.ToArray();
        MarisaNative.SetAgentQuery(agentPtr, searchKey, searchKey.Length);
        var lookupResult = MarisaNative.LookupKey(loadedTriePtr, agentPtr);

        // Assert
        lookupResult.Should().Be(1, "lookup should find key in loaded trie");

        // Cleanup
        MarisaNative.DestroyAgent(agentPtr);
        MarisaNative.DestroyTrie(loadedTriePtr);
        MarisaNative.DestroyBuilder(builderPtr);
    }

    [Fact]
    public void LoadTrie_AndEnumerateAllKeys_ShouldRetrieveAllStoredKeys()
    {
        // Arrange
        MarisaNative.BindMarisa();
        var builderPtr = MarisaNative.CreateBuilder();
        var originalKeys = new[] { "apple", "banana", "cherry", "date", "elderberry" };
        var savePath = Path.Combine(testDirectory, "enumerate-test-trie.bin");
        
        foreach (var key in originalKeys)
        {
            var bytes = Encoding.UTF8.GetBytes(key);
            MarisaNative.PushIntoBuilder(builderPtr, bytes, bytes.Length);
        }

        var triePtr = MarisaNative.BuildBuilder(builderPtr);
        MarisaNative.SaveTrie(triePtr, savePath);
        MarisaNative.DestroyTrie(triePtr);

        // Act - Load trie and extract all keys
        var loadedTriePtr = MarisaNative.LoadTrie(savePath);
        var numKeys = (int)MarisaNative.GetNumKeys(loadedTriePtr);
        var agentPtr = MarisaNative.CreateAgent();
        var buffer = new byte[1024];
        var retrievedKeys = new List<string>();

        for (int i = 0; i < numKeys; i++)
        {
            MarisaNative.ReverseLookup(loadedTriePtr, agentPtr, new UIntPtr((uint)i));
            MarisaNative.GetAgentKey(agentPtr, buffer, buffer.Length, out var length);
            var key = Encoding.UTF8.GetString(buffer, 0, length);
            retrievedKeys.Add(key);
        }

        // Assert
        retrievedKeys.Should().HaveCount(originalKeys.Length, "all keys should be retrieved");
        retrievedKeys.Should().BeEquivalentTo(originalKeys, "retrieved keys should match original keys");

        // Cleanup
        MarisaNative.DestroyAgent(agentPtr);
        MarisaNative.DestroyTrie(loadedTriePtr);
        MarisaNative.DestroyBuilder(builderPtr);
    }

    [Fact]
    public void LoadTrie_AndEnumerateAllKeys_ShouldWorkWithDanishWords()
    {
        // Arrange
        MarisaNative.BindMarisa();
        var builderPtr = MarisaNative.CreateBuilder();
        var danishWords = new[] { "æble", "øl", "år", "bøtte", "grød", "hætte" };
        var savePath = Path.Combine(testDirectory, "danish-enumerate-test-trie.bin");
        
        foreach (var word in danishWords)
        {
            var bytes = Encoding.UTF8.GetBytes(word);
            MarisaNative.PushIntoBuilder(builderPtr, bytes, bytes.Length);
        }

        var triePtr = MarisaNative.BuildBuilder(builderPtr);
        MarisaNative.SaveTrie(triePtr, savePath);
        MarisaNative.DestroyTrie(triePtr);

        // Act - Load trie and extract all keys
        var loadedTriePtr = MarisaNative.LoadTrie(savePath);
        var numKeys = (int)MarisaNative.GetNumKeys(loadedTriePtr);
        var agentPtr = MarisaNative.CreateAgent();
        var buffer = new byte[1024];
        var retrievedWords = new List<string>();

        for (int i = 0; i < numKeys; i++)
        {
            MarisaNative.ReverseLookup(loadedTriePtr, agentPtr, new UIntPtr((uint)i));
            MarisaNative.GetAgentKey(agentPtr, buffer, buffer.Length, out var length);
            var word = Encoding.UTF8.GetString(buffer, 0, length);
            retrievedWords.Add(word);
        }

        // Assert
        retrievedWords.Should().HaveCount(danishWords.Length, "all Danish words should be retrieved");
        retrievedWords.Should().BeEquivalentTo(danishWords, "retrieved Danish words should match original");

        // Cleanup
        MarisaNative.DestroyAgent(agentPtr);
        MarisaNative.DestroyTrie(loadedTriePtr);
        MarisaNative.DestroyBuilder(builderPtr);
    }
}
