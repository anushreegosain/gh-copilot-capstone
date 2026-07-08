using Api.Data;
using Api.Domain.Entities;
using Api.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Api.Tests.Repositories;

/// <summary>
/// Unit tests for UserRepository data access operations.
/// Uses EF Core InMemory database for isolation and AAA pattern testing.
/// </summary>
public class UserRepositoryTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly UserRepository _repository;

    public UserRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        _context = new AppDbContext(options);
        _repository = new UserRepository(_context);
    }

    #region GetByUsernameAsync Tests

    [Fact]
    public async Task GetByUsernameAsync_WithExistingUsername_ReturnsUser()
    {
        // Arrange
        var username = "testuser";
        var user = new User
        {
            Username = username,
            PasswordHash = "hash123",
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };
        await SeedTestData(user);

        // Act
        var result = await _repository.GetByUsernameAsync(username, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(username, result.Username);
        Assert.Equal("hash123", result.PasswordHash);
    }

    [Fact]
    public async Task GetByUsernameAsync_WithNonExistentUsername_ReturnsNull()
    {
        // Arrange
        var existingUser = new User
        {
            Username = "existinguser",
            PasswordHash = "hash123",
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };
        await SeedTestData(existingUser);

        // Act
        var result = await _repository.GetByUsernameAsync("nonexistent", CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByUsernameAsync_WithCaseInsensitiveSearch_ReturnsUser()
    {
        // Arrange
        var username = "TestUser";
        var user = new User
        {
            Username = username,
            PasswordHash = "hash123",
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };
        await SeedTestData(user);

        // Act
        var resultLower = await _repository.GetByUsernameAsync("testuser", CancellationToken.None);
        var resultUpper = await _repository.GetByUsernameAsync("TESTUSER", CancellationToken.None);
        var resultMixed = await _repository.GetByUsernameAsync("TeStUsEr", CancellationToken.None);

        // Assert
        Assert.NotNull(resultLower);
        Assert.NotNull(resultUpper);
        Assert.NotNull(resultMixed);
        Assert.Equal("TestUser", resultLower.Username);
        Assert.Equal("TestUser", resultUpper.Username);
        Assert.Equal("TestUser", resultMixed.Username);
    }

    [Fact]
    public async Task GetByUsernameAsync_WithWhitespaceUsername_ReturnsNull()
    {
        // Arrange
        var user = new User
        {
            Username = "testuser",
            PasswordHash = "hash123",
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };
        await SeedTestData(user);

        // Act
        var result = await _repository.GetByUsernameAsync("   ", CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ReturnsUser()
    {
        // Arrange
        var user = new User
        {
            Username = "testuser",
            PasswordHash = "hash123",
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };
        var createdUser = await _repository.CreateAsync(user, CancellationToken.None);

        // Act
        var result = await _repository.GetByIdAsync(createdUser.Id, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(createdUser.Id, result.Id);
        Assert.Equal("testuser", result.Username);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentId_ReturnsNull()
    {
        // Arrange
        var user = new User
        {
            Username = "testuser",
            PasswordHash = "hash123",
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };
        await SeedTestData(user);

        // Act
        var result = await _repository.GetByIdAsync(9999, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_WithZeroId_ReturnsNull()
    {
        // Arrange - no setup needed

        // Act
        var result = await _repository.GetByIdAsync(0, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidUser_PersistsAndReturnsUser()
    {
        // Arrange
        var user = new User
        {
            Username = "newuser",
            PasswordHash = "hashedpassword",
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };

        // Act
        var createdUser = await _repository.CreateAsync(user, CancellationToken.None);

        // Assert
        Assert.NotEqual(0, createdUser.Id);
        Assert.Equal("newuser", createdUser.Username);
        Assert.Equal("hashedpassword", createdUser.PasswordHash);
        
        // Verify persistence by querying database
        var retrievedUser = await _repository.GetByIdAsync(createdUser.Id, CancellationToken.None);
        Assert.NotNull(retrievedUser);
        Assert.Equal(createdUser.Id, retrievedUser.Id);
    }

    [Fact]
    public async Task CreateAsync_WithMultipleUsers_AssignsUniqueIds()
    {
        // Arrange
        var user1 = new User
        {
            Username = "user1",
            PasswordHash = "hash1",
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };
        var user2 = new User
        {
            Username = "user2",
            PasswordHash = "hash2",
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };

        // Act
        var created1 = await _repository.CreateAsync(user1, CancellationToken.None);
        var created2 = await _repository.CreateAsync(user2, CancellationToken.None);

        // Assert
        Assert.NotEqual(0, created1.Id);
        Assert.NotEqual(0, created2.Id);
        Assert.NotEqual(created1.Id, created2.Id);
    }

    #endregion

    #region UsernameExistsAsync Tests

    [Fact]
    public async Task UsernameExistsAsync_WithExistingUsername_ReturnsTrue()
    {
        // Arrange
        var username = "existinguser";
        var user = new User
        {
            Username = username,
            PasswordHash = "hash123",
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };
        await SeedTestData(user);

        // Act
        var result = await _repository.UsernameExistsAsync(username, CancellationToken.None);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task UsernameExistsAsync_WithNonExistentUsername_ReturnsFalse()
    {
        // Arrange
        var user = new User
        {
            Username = "existinguser",
            PasswordHash = "hash123",
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };
        await SeedTestData(user);

        // Act
        var result = await _repository.UsernameExistsAsync("nonexistent", CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task UsernameExistsAsync_WithCaseInsensitiveSearch_ReturnsTrue()
    {
        // Arrange
        var username = "TestUser";
        var user = new User
        {
            Username = username,
            PasswordHash = "hash123",
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };
        await SeedTestData(user);

        // Act
        var resultLower = await _repository.UsernameExistsAsync("testuser", CancellationToken.None);
        var resultUpper = await _repository.UsernameExistsAsync("TESTUSER", CancellationToken.None);

        // Assert
        Assert.True(resultLower);
        Assert.True(resultUpper);
    }

    [Fact]
    public async Task UsernameExistsAsync_WithEmptyDatabase_ReturnsFalse()
    {
        // Arrange - empty database

        // Act
        var result = await _repository.UsernameExistsAsync("anyuser", CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task UsernameExistsAsync_WithMultipleUsers_IdentifiesCorrectOne()
    {
        // Arrange
        var user1 = new User
        {
            Username = "user1",
            PasswordHash = "hash1",
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };
        var user2 = new User
        {
            Username = "user2",
            PasswordHash = "hash2",
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };
        await SeedTestData(user1, user2);

        // Act
        var existsUser1 = await _repository.UsernameExistsAsync("user1", CancellationToken.None);
        var existsUser2 = await _repository.UsernameExistsAsync("user2", CancellationToken.None);
        var existsNonexistent = await _repository.UsernameExistsAsync("user3", CancellationToken.None);

        // Assert
        Assert.True(existsUser1);
        Assert.True(existsUser2);
        Assert.False(existsNonexistent);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Seeds the test database with user entities.
    /// </summary>
    private async Task SeedTestData(params User[] users)
    {
        _context.Users.AddRange(users);
        await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context?.Dispose();
    }

    #endregion
}
