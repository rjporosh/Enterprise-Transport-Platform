using AuthService.Application.Features.Admin.Roles;
using AuthService.Domain.Enums;
using AuthService.Domain.Entities;
using AuthService.Domain.Exceptions;
using AuthService.UnitTests.TestSupport;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AuthService.UnitTests.Admin;

public class RoleHandlerTests : IDisposable
{
    private readonly TestAuthDbContext _context;
    private readonly FakeAuditLogger _auditLogger = new();
    private readonly FakeDateTimeProvider _clock = new();
    private CreateRoleHandler _createHandler = default!;
    private UpdateRoleHandler _updateHandler = default!;
    private GetRolesHandler _getHandler = default!;
    private AssignPermissionHandler _assignPermissionHandler = default!;
    private RemovePermissionHandler _removePermissionHandler = default!;
    private AssignRoleToUserHandler _assignRoleToUserHandler = default!;
    private RemoveRoleFromUserHandler _removeRoleFromUserHandler = default!;

    public RoleHandlerTests()
    {
        var options = new DbContextOptionsBuilder<TestAuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new TestAuthDbContext(options);
        Setup();
    }

    private void Setup()
    {
        _createHandler = new CreateRoleHandler(_context, _auditLogger, NullLogger<CreateRoleHandler>.Instance);
        _updateHandler = new UpdateRoleHandler(_context, _auditLogger, NullLogger<UpdateRoleHandler>.Instance);
        _getHandler = new GetRolesHandler(_context);
        _assignPermissionHandler = new AssignPermissionHandler(_context, _auditLogger, NullLogger<AssignPermissionHandler>.Instance);
        _removePermissionHandler = new RemovePermissionHandler(_context, _auditLogger, NullLogger<RemovePermissionHandler>.Instance);
        _assignRoleToUserHandler = new AssignRoleToUserHandler(_context, _auditLogger, NullLogger<AssignRoleToUserHandler>.Instance);
        _removeRoleFromUserHandler = new RemoveRoleFromUserHandler(_context, _auditLogger, NullLogger<RemoveRoleFromUserHandler>.Instance);
    }

    [Fact]
    public async Task CreateRole_ReturnsNewId()
    {
        var id = await _createHandler.Handle(new CreateRoleCommand("Manager", "Department manager"), CancellationToken.None);
        id.Should().NotBeEmpty();
        _auditLogger.Entries.Should().ContainSingle(e => e.Action == AuditAction.RoleCreated);
    }

    [Fact]
    public async Task GetRoles_ReturnsCreatedRoles()
    {
        await _createHandler.Handle(new CreateRoleCommand("Manager", "Department manager"), CancellationToken.None);
        await _createHandler.Handle(new CreateRoleCommand("Staff", "Regular staff"), CancellationToken.None);

        var result = await _getHandler.Handle(new GetRolesQuery(), CancellationToken.None);
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task AssignPermissionToRole_Succeeds()
    {
        var roleId = await _createHandler.Handle(new CreateRoleCommand("Manager", "Department manager"), CancellationToken.None);
        var permission = new AuthService.Domain.Entities.Permission(Guid.NewGuid(), "users.read", "Read users", "Users");
        _context.Permissions.Add(permission);
        await _context.SaveChangesAsync();

        await _assignPermissionHandler.Handle(new AssignPermissionCommand(roleId, permission.Id), CancellationToken.None);

        var rolePermission = await _context.RolePermissions.FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permission.Id);
        rolePermission.Should().NotBeNull();
    }

    [Fact]
    public async Task AssignRoleToUser_Succeeds()
    {
        var roleId = await _createHandler.Handle(new CreateRoleCommand("Manager", "Department manager"), CancellationToken.None);
        var user = User.Register(Guid.NewGuid(), "user@example.com", "hash", "User", "Test", null, _clock.UtcNow);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        await _assignRoleToUserHandler.Handle(new AssignRoleToUserCommand(user.Id, roleId), CancellationToken.None);

        var userRole = await _context.UserRoles.FirstOrDefaultAsync(ur => ur.UserId == user.Id && ur.RoleId == roleId);
        userRole.Should().NotBeNull();
    }

    public void Dispose() => _context.Dispose();
}
