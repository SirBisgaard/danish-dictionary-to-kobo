using System.Runtime.InteropServices;
using System.Text;
using Ddtk.Cli.Helpers;
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
}
