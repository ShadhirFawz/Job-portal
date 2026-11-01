namespace Recruitment.Infrastructure.Supabase;

public interface ISupabaseContext
{
    Task<T?> GetSingleAsync<T>(string tableQuery);
    Task<List<T>> GetListAsync<T>(string tableQuery);
    Task<(List<T> Items, int TotalCount)> GetPagedListAsync<T>(string tableQuery, int page, int pageSize);
    Task<T> InsertAsync<T>(string table, object payload);
    Task<T?> UpdateAsync<T>(string tableQuery, object payload);
    Task DeleteAsync(string tableQuery);
    Task<T?> RpcAsync<T>(string functionName, object? payload = null);
    Task RpcVoidAsync(string functionName, object? payload = null);
    Task<string?> CreateSignedUrlAsync(string bucket, string path, int expiresIn = 3600);
    Task DeleteStorageObjectAsync(string bucket, string path);
}

