namespace ProductBundles.Core.Storage
{
    /// <summary>
    /// Represents pagination parameters for data retrieval
    /// </summary>
    public class PaginationRequest
    {
        /// <summary>
        /// The page number (1-based)
        /// </summary>
        public int PageNumber { get; }

        /// <summary>
        /// The number of items per page
        /// </summary>
        public int PageSize { get; }

        /// <summary>
        /// The zero-based skip count for the query
        /// </summary>
        public int Skip => (PageNumber - 1) * PageSize;

        /// <summary>
        /// Initializes a new instance of the PaginationRequest class
        /// </summary>
        /// <param name="pageNumber">The page number (1-based)</param>
        /// <param name="pageSize">The number of items per page</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when pageNumber or pageSize are invalid</exception>
        public PaginationRequest(int pageNumber, int pageSize)
        {
            if (pageNumber < 1)
                throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be 1 or greater");

            if (pageSize < 1)
                throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be 1 or greater");

            if (pageSize > 1000)
                throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size cannot exceed 1000 items");

            PageNumber = pageNumber;
            PageSize = pageSize;
        }
    }
}
