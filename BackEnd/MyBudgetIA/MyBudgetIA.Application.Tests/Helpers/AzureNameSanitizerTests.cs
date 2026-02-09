using MyBudgetIA.Application.Helpers;

namespace MyBudgetIA.Application.Tests.Helpers
{
    /// <summary>
    /// Unit tests for the <see cref="AzureNameSanitizer"/> class.
    /// </summary>
    [TestFixture]
    public class AzureNameSanitizerTests
    {
        #region RemoveDiacritics Tests

        [TestCase("café", "cafe")]
        [TestCase("résumé", "resume")]
        [TestCase("naïve", "naive")]
        [TestCase("crème", "creme")]
        [TestCase("brûlée", "brulee")]
        public void AzureNameSanitizer_RemoveDiacritics_French_ShouldRemove(string input, string expectedWithoutDiacritics)
        {
            // Act - On teste via SanitizeBlobName qui appelle AzureNameSanitizer
            var result = AzureNameSanitizer.RemoveDiacritics(input);

            // Assert
            Assert.That(result, Is.EqualTo(expectedWithoutDiacritics));
        }

        [TestCase("ñoño", "nono")]
        [TestCase("José", "Jose")]
        [TestCase("Málaga", "Malaga")]
        public void AzureNameSanitizer_RemoveDiacritics_Spanish_ShouldRemove(string input, string expectedWithoutDiacritics)
        {
            // Act
            var result = AzureNameSanitizer.RemoveDiacritics(input);

            // Assert
            Assert.That(result, Is.EqualTo(expectedWithoutDiacritics));
        }

        [TestCase("über", "uber")]
        [TestCase("Müller", "Muller")]
        [TestCase("Größe", "Grosse")]
        [TestCase("Straße", "Strasse")]
        public void AzureNameSanitizer_RemoveDiacritics_German_ShouldRemove(string input, string expectedWithoutDiacritics)
        {
            // Act
            var result = AzureNameSanitizer.RemoveDiacritics(input);

            // Assert
            Assert.That(result, Is.EqualTo(expectedWithoutDiacritics));
        }

        [TestCase("Æble", "AEble")]
        [TestCase("Øl", "Ol")]
        [TestCase("Å", "A")]
        public void AzureNameSanitizer_RemoveDiacritics_Nordish_ShouldRemove(string input, string expectedWithoutDiacritics)
        {
            // Act
            var result = AzureNameSanitizer.RemoveDiacritics(input);

            // Assert
            Assert.That(result, Is.EqualTo(expectedWithoutDiacritics));
        }

        [TestCase("čeština", "cestina")]
        [TestCase("Łódź", "Lodz")]
        [TestCase("Škoda", "Skoda")]
        public void AzureNameSanitizer_RemoveDiacritics_Slavish_ShouldRemove(string input, string expectedWithoutDiacritics)
        {
            // Act
            var result = AzureNameSanitizer.RemoveDiacritics(input);

            // Assert
            Assert.That(result, Is.EqualTo(expectedWithoutDiacritics));
        }

        [TestCase("àáâãäåæçèéêëìíîïðñòóôõöøùúûüýþÿ", "aaaaaaaeceeeeiiiidnoooooouuuuythy")]
        public void AzureNameSanitizer_RemoveDiacritics_AllDiacritics_ShouldRemove(string input, string expected)
        {
            // Act
            var result = AzureNameSanitizer.RemoveDiacritics(input);

            // Assert
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void AzureNameSanitizer_RemoveDiacritics_WithoutDiacritics_ShouldNotCHange()
        {
            // Arrange
            var input = "simple_text_without_accents";

            // Act
            var result = AzureNameSanitizer.RemoveDiacritics(input);

            // Assert
            Assert.That(result, Is.EqualTo(input));
        }

        #endregion

        #region SanitizeBlobName Tests

        [TestCase(null, "unnamed_file")]
        [TestCase("", "unnamed_file")]
        [TestCase("   ", "unnamed_file")]
        public void AzureNameSanitizer_SanitizeBlobName_NullOrWhitespace_ReturnsDefault(string? input, string expected)
        {
            var result = AzureNameSanitizer.SanitizeBlobName(input!);
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void AzureNameSanitizer_SanitizeBlobName_Should_Remove_Diacritics_And_Replace_Invalid_Characters()
        {
            var result = AzureNameSanitizer.SanitizeBlobName("café#1?.png");
            Assert.That(result, Is.EqualTo("cafe_1_.png"));
        }

        [Test]
        public void AzureNameSanitizer_SanitizeBlobName_Should_Collapse_Multiple_Slashes_And_Trim_Slashes()
        {
            var result = AzureNameSanitizer.SanitizeBlobName("///a////b///c///");
            Assert.That(result, Is.EqualTo("a/b/c"));
        }

        [Test]
        public void AzureNameSanitizer_SanitizeBlobName_Should_Collapse_Multiple_Dots_And_Trim_Dots()
        {
            var result = AzureNameSanitizer.SanitizeBlobName("..a...b....c..");
            Assert.That(result, Is.EqualTo("a.b.c"));
        }

        [Test]
        public void AzureNameSanitizer_SanitizeBlobName_Should_Collapse_Multiple_Underscores()
        {
            var result = AzureNameSanitizer.SanitizeBlobName("a____b__c");
            Assert.That(result, Is.EqualTo("a_b_c"));
        }

        [Test]
        public void AzureNameSanitizer_SanitizeBlobName_Should_Use_DefaultName_When_Result_Becomes_Empty()
        {
            var result = AzureNameSanitizer.SanitizeBlobName("....////____");
            Assert.That(result, Is.EqualTo("unnamed_file"));
        }

        [Test]
        public void AzureNameSanitizer_SanitizeBlobName_When_Too_Long_Should_Preserve_Extension_When_Possible()
        {
            var input = new string('a', 120) + ".png";
            var result = AzureNameSanitizer.SanitizeBlobName(input, maxLength: 20);

            Assert.That(result, Has.Length.EqualTo(20));
            Assert.That(result, Does.EndWith(".png"));
        }

        [Test]
        public void AzureNameSanitizer_SanitizeBlobName_When_Extension_Longer_Than_MaxLength_Should_Truncate_Hard()
        {
            var input = "file.veryverylongextension";
            var result = AzureNameSanitizer.SanitizeBlobName(input, maxLength: 5);

            Assert.That(result, Has.Length.EqualTo(5));
        }

        [Test]
        public void AzureNameSanitizer_SanitizeBlobName_Should_Not_Return_Whitespace()
        {
            var result = AzureNameSanitizer.SanitizeBlobName("   ###   ");
            Assert.That(result, Is.Not.Null.And.Not.Empty);
            Assert.That(result, Is.Not.EqualTo(" "));
        }
    }

    #endregion
}