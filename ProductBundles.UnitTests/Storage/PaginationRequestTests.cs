using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProductBundles.Core.Storage;

namespace ProductBundles.UnitTests
{
    [TestClass]
    public class PaginationRequestTests
    {
        [TestMethod]
        public void Constructor_WithValidParameters_SetsPropertiesCorrectly()
        {
            // Arrange
            int pageNumber = 2;
            int pageSize = 10;

            // Act
            var request = new PaginationRequest(pageNumber, pageSize);

            // Assert
            Assert.AreEqual(pageNumber, request.PageNumber);
            Assert.AreEqual(pageSize, request.PageSize);
            Assert.AreEqual(10, request.Skip); // (2-1) * 10 = 10
        }

        [TestMethod]
        public void Constructor_WithPageNumber1_CalculatesSkipCorrectly()
        {
            // Arrange & Act
            var request = new PaginationRequest(1, 20);

            // Assert
            Assert.AreEqual(0, request.Skip); // (1-1) * 20 = 0
        }

        [TestMethod]
        public void Constructor_WithPageNumber3_CalculatesSkipCorrectly()
        {
            // Arrange & Act
            var request = new PaginationRequest(3, 15);

            // Assert
            Assert.AreEqual(30, request.Skip); // (3-1) * 15 = 30
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Constructor_WithZeroPageNumber_ThrowsArgumentOutOfRangeException()
        {
            // Act
            new PaginationRequest(0, 10);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Constructor_WithNegativePageNumber_ThrowsArgumentOutOfRangeException()
        {
            // Act
            new PaginationRequest(-1, 10);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Constructor_WithZeroPageSize_ThrowsArgumentOutOfRangeException()
        {
            // Act
            new PaginationRequest(1, 0);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Constructor_WithNegativePageSize_ThrowsArgumentOutOfRangeException()
        {
            // Act
            new PaginationRequest(1, -5);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Constructor_WithPageSizeExceedsLimit_ThrowsArgumentOutOfRangeException()
        {
            // Act
            new PaginationRequest(1, 1001);
        }

        [TestMethod]
        public void Constructor_WithMaxAllowedPageSize_CreatesSuccessfully()
        {
            // Arrange & Act
            var request = new PaginationRequest(1, 1000);

            // Assert
            Assert.AreEqual(1000, request.PageSize);
        }
    }
}
