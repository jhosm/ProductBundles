using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProductBundles.Core.Storage;

namespace ProductBundles.UnitTests
{
    [TestClass]
    public class PaginatedResultTests
    {
        [TestMethod]
        public void Constructor_WithValidParameters_SetsPropertiesCorrectly()
        {
            // Arrange
            var items = new List<string> { "item1", "item2", "item3" };
            int pageNumber = 2;
            int pageSize = 5;

            // Act
            var result = new PaginatedResult<string>(items, pageNumber, pageSize);

            // Assert
            Assert.AreEqual(items, result.Items);
            Assert.AreEqual(pageNumber, result.PageNumber);
            Assert.AreEqual(pageSize, result.PageSize);
        }



        [TestMethod]
        public void HasPreviousPage_OnFirstPage_ReturnsFalse()
        {
            // Arrange
            var items = new List<string>();
            var result = new PaginatedResult<string>(items, 1, 10);

            // Act & Assert
            Assert.IsFalse(result.HasPreviousPage);
        }

        [TestMethod]
        public void HasPreviousPage_OnSecondPage_ReturnsTrue()
        {
            // Arrange
            var items = new List<string>();
            var result = new PaginatedResult<string>(items, 2, 10);

            // Act & Assert
            Assert.IsTrue(result.HasPreviousPage);
        }



        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_WithNullItems_ThrowsArgumentNullException()
        {
            // Act
            new PaginatedResult<string>(null!, 1, 10);
        }

        [TestMethod]
        public void Constructor_WithEmptyItems_CreatesSuccessfully()
        {
            // Arrange
            var items = new List<string>();

            // Act
            var result = new PaginatedResult<string>(items, 1, 10);

            // Assert
            Assert.IsNotNull(result.Items);
            Assert.AreEqual(0, result.Items.Count());
        }
    }
}
