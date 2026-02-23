using Moq;

namespace BizFlow.Application.Tests.Common
{
    /// <summary>
    /// Helper class for common mocking patterns and setup
    /// </summary>
    public static class MockHelper
    {
        /// <summary>
        /// Verifies that a method was called exactly once with any arguments
        /// </summary>
        public static void VerifyCalledOnce<T, TResult>(
            Mock<T> mock,
            System.Linq.Expressions.Expression<Func<T, TResult>> expression) where T : class
        {
            mock.Verify(expression, Times.Once);
        }

        /// <summary>
        /// Verifies that a method was never called
        /// </summary>
        public static void VerifyNeverCalled<T, TResult>(
            Mock<T> mock,
            System.Linq.Expressions.Expression<Func<T, TResult>> expression) where T : class
        {
            mock.Verify(expression, Times.Never);
        }

        /// <summary>
        /// Verifies that a method was called exactly N times
        /// </summary>
        public static void VerifyCalledTimes<T, TResult>(
            Mock<T> mock,
            System.Linq.Expressions.Expression<Func<T, TResult>> expression,
            int times) where T : class
        {
            mock.Verify(expression, Times.Exactly(times));
        }
    }

    /// <summary>
    /// Constants for test data
    /// </summary>
    public static class TestConstants
    {
        public const string DefaultRoleName = "Test Role";
        public const string DefaultProductName = "Test Product";
        public const string DefaultLocationName = "Test Location";
        public const string DefaultEmail = "test@example.com";
        
        public static readonly Guid DefaultGuid = Guid.Parse("00000000-0000-0000-0000-000000000001");
        public static readonly long DefaultProductId = 1L;
    }
}
