using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizFlow.Application.Common.Models
{
    /// <summary>
    /// Standard API response wrapper
    /// </summary>
    public class ApiResponse
    {
        public bool Success { get; set; }
        public string MessageCode { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public object? Errors { get; set; }
        public List<string>? Warnings { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public static ApiResponse SuccessResponse(string messageCode, string message)
        {
            return new ApiResponse
            {
                Success = true,
                MessageCode = messageCode,
                Message = message
            };
        }

        public static ApiResponse ErrorResponse(string messageCode, string message, object? errors = null)
        {
            return new ApiResponse
            {
                Success = false,
                MessageCode = messageCode,
                Message = message,
                Errors = errors
            };
        }
    }

    /// <summary>
    /// Standard API response wrapper with data
    /// </summary>
    public class ApiResponse<T> : ApiResponse
    {
        public T? Data { get; set; }

        public static ApiResponse<T> SuccessResponse(T data, string messageCode, string message)
        {
            return new ApiResponse<T>
            {
                Success = true,
                MessageCode = messageCode,
                Message = message,
                Data = data
            };
        }

        public new static ApiResponse<T> ErrorResponse(string messageCode, string message, object? errors = null)
        {
            return new ApiResponse<T>
            {
                Success = false,
                MessageCode = messageCode,
                Message = message,
                Errors = errors,
                Data = default
            };
        }
    }
}
