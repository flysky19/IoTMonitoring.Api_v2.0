// DTOs/CommonDtos.cs
namespace IoTMonitoring.Api.DTOs
{
    // 페이징 결과 래퍼 DTO
    public class PagedResultDto<T>
    {
        public List<T> Items { get; set; }
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }

    // API 응답 래퍼 DTO
    public class ApiResponseDto<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }
        public List<string> Errors { get; set; }

        public static ApiResponseDto<T> SuccessResponse(T data, string message = null)
        {
            return new ApiResponseDto<T>
            {
                Success = true,
                Message = message ?? "Operation completed successfully",
                Data = data,
                Errors = null
            };
        }

        public static ApiResponseDto<T> ErrorResponse(string message, List<string> errors = null)
        {
            return new ApiResponseDto<T>
            {
                Success = false,
                Message = message,
                Data = default,
                Errors = errors
            };
        }
    }

    // 필터 기본 DTO
    public class FilterDto
    {
        public int? PageNumber { get; set; } = 1;
        public int? PageSize { get; set; } = 50;
        public string SortBy { get; set; }
        public bool SortDescending { get; set; } = false;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}