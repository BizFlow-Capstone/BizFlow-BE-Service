using BizFlow.Application.DTOs.AccountingBook;

namespace BizFlow.Application.Interfaces.Services;

public interface IAccountingBookService
{
    Task<CreateBooksResponse> CreateBooksAsync(int locationId, Guid userId, CreateBooksRequest request);
    Task<List<BookListItemDto>> ListBooksAsync(int locationId, Guid userId, long periodId);
    Task<BookSummaryResponse> GetBookSummaryAsync(int locationId, Guid userId, long bookId);
    Task<BookRowsResponse> GetBookRowsAsync(int locationId, Guid userId, long bookId, string? cursor, int batchSize = 200);
}
