using BizFlow.Domain.Entities;

namespace BizFlow.Application.Tests.Fixtures
{
    /// <summary>
    /// Test fixtures providing common test data
    /// Customize these based on your domain entities
    /// </summary>
    public static class TestFixtures
    {
        /// <summary>
        /// Creates a test Role entity
        /// </summary>
        public static Role CreateTestRole(Guid? id = null, string name = "Test Role")
        {
            return new Role
            {
                Id = id ?? Guid.NewGuid(),
                Name = name,
                Description = $"{name} Description",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Creates multiple test Role entities
        /// </summary>
        public static List<Role> CreateTestRoles(int count = 3)
        {
            var roles = new List<Role>();
            for (int i = 0; i < count; i++)
            {
                roles.Add(CreateTestRole(null, $"Role {i + 1}"));
            }
            return roles;
        }

        /// <summary>
        /// Creates fixture data for common role names
        /// Useful for testing lookups
        /// </summary>
        public static IEnumerable<Role> GetCommonRoles()
        {
            return new List<Role>
            {
                CreateTestRole(null, "Admin"),
                CreateTestRole(null, "Manager"),
                CreateTestRole(null, "Employee"),
                CreateTestRole(null, "Consultant")
            };
        }
    }
}
