using Ddtk.Domain;
using FluentAssertions;

namespace Ddtk.Tests.Domain;

/// <summary>
/// Unit tests for SeedingWordCollection class.
/// Tests verify word collection creation, counting, and new word detection.
/// </summary>
public class SeedingWordCollectionTests
{
    [Fact]
    public void Count_ShouldReturnZero_WhenCreatedWithEmptyCollection()
    {
        // Arrange
        var words = Array.Empty<string>();

        // Act
        var collection = new SeedingWordCollection(words);

        // Assert
        collection.Count.Should().Be(0, "an empty collection should have zero words");
    }

    [Fact]
    public void Count_ShouldReturnCorrectCount_WhenCreatedWithUniqueWords()
    {
        // Arrange
        var words = new[] { "hej", "verden", "tak" };

        // Act
        var collection = new SeedingWordCollection(words);

        // Assert
        collection.Count.Should().Be(3, "collection should contain all unique words");
    }

    [Fact]
    public void Count_ShouldDeduplicateWords_WhenCreatedWithDuplicates()
    {
        // Arrange
        var words = new[] { "hej", "verden", "hej", "tak", "verden" };

        // Act
        var collection = new SeedingWordCollection(words);

        // Assert
        collection.Count.Should().Be(3, "duplicate words should be removed");
    }

    [Fact]
    public void GetNewWordsCount_ShouldReturnZero_WhenOtherIsEmpty()
    {
        // Arrange
        var collection = new SeedingWordCollection(["hej", "verden"]);
        var other = new SeedingWordCollection([]);

        // Act
        var result = collection.GetNewWordsCount(other);

        // Assert
        result.Should().Be(0, "an empty other collection has no new words");
    }

    [Fact]
    public void GetNewWordsCount_ShouldReturnZero_WhenAllWordsAlreadyExist()
    {
        // Arrange
        var collection = new SeedingWordCollection(["hej", "verden", "tak"]);
        var other = new SeedingWordCollection(["hej", "verden"]);

        // Act
        var result = collection.GetNewWordsCount(other);

        // Assert
        result.Should().Be(0, "all words in other already exist in this collection");
    }

    [Fact]
    public void GetNewWordsCount_ShouldReturnCount_WhenAllWordsAreNew()
    {
        // Arrange
        var collection = new SeedingWordCollection(["hej", "verden"]);
        var other = new SeedingWordCollection(["tak", "godmorgen"]);

        // Act
        var result = collection.GetNewWordsCount(other);

        // Assert
        result.Should().Be(2, "all words in other are new");
    }

    [Fact]
    public void GetNewWordsCount_ShouldReturnPartialCount_WhenSomeWordsAreNew()
    {
        // Arrange
        var collection = new SeedingWordCollection(["hej", "verden", "tak"]);
        var other = new SeedingWordCollection(["verden", "godmorgen", "tak", "farvel"]);

        // Act
        var result = collection.GetNewWordsCount(other);

        // Assert
        result.Should().Be(2, "only words not in this collection should be counted");
    }

    [Fact]
    public void GetNewWordsCount_ShouldReturnOtherCount_WhenThisCollectionIsEmpty()
    {
        // Arrange
        var collection = new SeedingWordCollection([]);
        var other = new SeedingWordCollection(["hej", "verden", "tak"]);

        // Act
        var result = collection.GetNewWordsCount(other);

        // Assert
        result.Should().Be(3, "all words should be new when this collection is empty");
    }

    [Fact]
    public void AddWords_ShouldIncreaseCount_WhenAddingNewWords()
    {
        // Arrange
        var collection = new SeedingWordCollection(["hej", "verden"]);

        // Act
        collection.AddWords(["tak", "godmorgen"]);

        // Assert
        collection.Count.Should().Be(4, "new words should be added to the collection");
    }

    [Fact]
    public void AddWords_ShouldNotIncreaseCount_WhenAddingExistingWords()
    {
        // Arrange
        var collection = new SeedingWordCollection(["hej", "verden", "tak"]);

        // Act
        collection.AddWords(["hej", "verden"]);

        // Assert
        collection.Count.Should().Be(3, "existing words should not be added again");
    }

    [Fact]
    public void AddWords_ShouldOnlyAddNewWords_WhenAddingMixOfExistingAndNew()
    {
        // Arrange
        var collection = new SeedingWordCollection(["hej", "verden"]);

        // Act
        collection.AddWords(["verden", "tak", "hej", "farvel"]);

        // Assert
        collection.Count.Should().Be(4, "only new words should be added");
    }

    [Fact]
    public void AddWords_ShouldReturnSameInstance_WhenCalled()
    {
        // Arrange
        var collection = new SeedingWordCollection(["hej"]);

        // Act
        var result = collection.AddWords(["verden"]);

        // Assert
        result.Should().BeSameAs(collection, "AddWords should return the same instance for chaining");
    }

    [Fact]
    public void AddWords_ShouldNotChangeCount_WhenAddingEmptyCollection()
    {
        // Arrange
        var collection = new SeedingWordCollection(["hej", "verden"]);

        // Act
        collection.AddWords([]);

        // Assert
        collection.Count.Should().Be(2, "adding empty collection should not change count");
    }

    [Fact]
    public void AddWords_ShouldAffectGetNewWordsCount_WhenNewWordsAreAdded()
    {
        // Arrange
        var collection = new SeedingWordCollection(["hej"]);
        var other = new SeedingWordCollection(["hej", "verden", "tak"]);

        // Act
        collection.AddWords(["verden"]);
        var result = collection.GetNewWordsCount(other);

        // Assert
        result.Should().Be(1, "only 'tak' should be new after adding 'verden'");
    }
}
