using AuthService.Application.Features.Admin.Permissions;
using AuthService.Domain.Enums;
using AuthService.UnitTests.TestSupport;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AuthService.UnitTests.Admin;

public class PermissionHandlerTests : IDisposable
{
    private readonly TestAuthDbContext _context;
    private readonly FakeAuditLogger _auditLogger = new();
    private readonly FakeDateTimeProvider _clock = new();
    private CreatePermissionHandler _createHandler = default!;
    private UpdatePermissionHandler _updateHandler = default!;
    private GetPermissionsHandler _getHandler = default!;
    private DeletePermissionHandler _deleteHandler = default!;

    public PermissionHandlerTests()
    {
        var options = new DbContextOptionsBuilder<TestAuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new TestAuthDbContext(options);
        Setup();
    }

    private void Setup()
    {
        _createHandler = new CreatePermissionHandler(_context, _auditLogger, NullLogger<CreatePermissionHandler>.Instance);
        _updateHandler = new UpdatePermissionHandler(_context, _auditLogger, NullLogger<UpdatePermissionHandler>.Instance);
        _getHandler = new GetPermissionsHandler(_context);
        _deleteHandler = new DeletePermissionHandler(_context, _auditLogger, NullLogger<DeletePermissionHandler>.Instance);
    }

    [Fact]
    public async Task CreatePermission_ReturnsNewId()
    {
        var id = await _createHandler.Handle(new CreatePermissionCommand("users.create", "Create users", "Users"), CancellationToken.None);
        id.Should().NotBeEmpty();
        _auditLogger.Entries.Should().ContainSingle(e => e.Action == AuditAction.PermissionCreated);
    }

    [Fact]
    public async Task GetPermissions_ReturnsCreatedPermissions()
    {
        await _createHandler.Handle(new CreatePermissionCommand("users.create", "Create users", "Users"), CancellationToken.None);
        await _createHandler.Handle(new CreatePermissionCommand("users.read", "Read users", "Users"), CancellationToken.None);

        var result = await _getHandler.Handle(new GetPermissionsQuery(null), CancellationToken.None);
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetPermissions_FilterByModule_ReturnsFiltered()
    {
        await _createHandler.Handle(new CreatePermissionCommand("users.create", "Create users", "Users"), CancellationToken.None);
        await _createHandler.Handle(new CreatePermissionCommand("buses.read", "Read buses", "Buses"), CancellationToken.None);

        var result = await _getHandler.Handle(new GetPermissionsQuery("Users"), CancellationToken.None);
        result.Should().HaveCount(1);
        result.First().Name.Should().Be("users.create");
    }

    [Fact]
    public async Task UpdatePermission_UpdatesSuccessfully()
    {
        var id = await _createHandler.Handle(new CreatePermissionCommand("users.create", "Create users", "Users"), CancellationToken.None);
        await _updateHandler.Handle(new UpdatePermissionCommand(id, "Updated description", "Users"), CancellationToken.None);

        var result = await _getHandler.Handle(new GetPermissionsQuery(null), CancellationToken.None);
        result.First().Description.Should().Be("Updated description");
    }

    [Fact]
    public async Task DeletePermission_RemovesSuccessfully()
    {
        var id = await _createHandler.Handle(new CreatePermissionCommand("users.create", "Create users", "Users"), CancellationToken.None);
        await _deleteHandler.Handle(new DeletePermissionCommand(id), CancellationToken.None);

        var result = await _getHandler.Handle(new GetPermissionsQuery(null), CancellationToken.None);
        result.Should().BeEmpty();
    }

    public void Dispose() => _context.Dispose();
}
