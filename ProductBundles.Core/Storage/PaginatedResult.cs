namespace ProductBundles.Core.Storage
{
    /// <summary>
    /// Represents a paginated result containing items and pagination metadata
    /// </summary>
    /// <typeparam name="T">The type of items in the result</typeparam>
    public class PaginatedResult<T>
    {
        /// <summary>
        /// The items for the current page
        /// </summary>
        public IEnumerable<T> Items { get; }

        /// <summary>
        /// The current page number (1-based)
        /// </summary>
        public int PageNumber { get; }

        /// <summary>
        /// The number of items per page
        /// </summary>
        public int PageSize { get; }

        /// <summary>
        /// Initializes a new instance of the PaginatedResult class
        /// </summary>
        /// <param name="items">The items for the current page</param>
        /// <param name="pageNumber">The current page number (1-based)</param>
        /// <param name="pageSize">The number of items per page</param>
        public PaginatedResult(IEnumerable<T> items, int pageNumber, int pageSize)
        {
            Items = items ?? throw new ArgumentNullException(nameof(items));
            PageNumber = pageNumber;
            PageSize = pageSize;
        }
    }
}
