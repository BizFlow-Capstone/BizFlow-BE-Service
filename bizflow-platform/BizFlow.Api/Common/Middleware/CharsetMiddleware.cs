using System.Text;

namespace BizFlow.Api.Common.Middleware
{
    /// <summary>
    /// Middleware to ensure all JSON responses include charset=utf-8 in content-type header.
    /// This fixes Unicode encoding issues with Vietnamese characters.
    /// </summary>
    public class CharsetMiddleware
    {
        private readonly RequestDelegate _next;

        public CharsetMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Capture the original response stream
            var originalBodyStream = context.Response.Body;

            using (var memoryStream = new MemoryStream())
            {
                context.Response.Body = memoryStream;

                try
                {
                    await _next(context);

                    // If response is JSON, ensure charset=utf-8
                    if (context.Response.ContentType?.Contains("application/json") == true)
                    {
                        if (!context.Response.ContentType.Contains("charset"))
                        {
                            context.Response.ContentType = "application/json; charset=utf-8";
                        }
                    }

                    // Copy the captured response back to the original stream
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    await memoryStream.CopyToAsync(originalBodyStream);
                }
                finally
                {
                    context.Response.Body = originalBodyStream;
                }
            }
        }
    }
}
