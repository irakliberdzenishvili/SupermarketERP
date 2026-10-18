namespace DotNetApiTemplate.Models.Response;

public class ApiResponse
{
    public bool IsSuccess { get; set; } = true;
    public string Message { get; set; } = null!;
}

public class ApiResponse<T> : ApiResponse
{
    public T Data { get; set; } = default(T)!;

    public ApiResponse()
    {

    }

    public ApiResponse(T data)
    {
        Data = data;
    }

    public ApiResponse(bool isSuccess, string message, T data)
    {
        IsSuccess = isSuccess;
        Message = message;
        Data = data;
    }
}

public class PagedApiResponse<T> : ApiResponse<IEnumerable<T>>
{
    public int TotalCount { get; set; }
    public int PageSize { get; set; }
    public int PageNumber { get; set; }

    public PagedApiResponse(IEnumerable<T> data, int totalCount, int pageSize, int pageNumber)
        : base(data)
    {
        TotalCount = totalCount;
        PageSize = pageSize;
        PageNumber = pageNumber;
    }
}