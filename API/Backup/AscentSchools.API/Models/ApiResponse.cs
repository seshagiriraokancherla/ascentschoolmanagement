namespace AscentSchools.API.Models
{
    /// <summary>
    /// Standard response envelope for all API endpoints.
    /// All controllers return this type — never raw objects.
    /// </summary>
    public class ApiResponse<T>
    {
        public bool   Success { get; set; }
        public T      Data    { get; set; }
        public string Message { get; set; }
        public object Errors  { get; set; }

        public static ApiResponse<T> Ok(T data, string message = null)
            => new ApiResponse<T> { Success = true, Data = data, Message = message };

        public static ApiResponse<T> Fail(string message, object errors = null)
            => new ApiResponse<T> { Success = false, Message = message, Errors = errors };
    }

    // Non-generic convenience for responses with no data payload
    public class ApiResponse : ApiResponse<object>
    {
        public static ApiResponse Ok(string message = null)
            => new ApiResponse { Success = true, Message = message };

        public static new ApiResponse Fail(string message, object errors = null)
            => new ApiResponse { Success = false, Message = message, Errors = errors };
    }
}
