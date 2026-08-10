using AuthService.Application.Features.Admin.Modules;
using AuthService.Domain.Enums;
using AuthService.UnitTests.TestSupport;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AuthService.UnitTests.Admin;

public class ModuleHandlerTests : IDisposable
{
    private readonly TestAuthDbContext _context;
    private readonly FakeAuditLogger _auditLogger = new();
    private readonly FakeDateTimeProvider _clock = new();
    private CreateModuleHandler _createHandler = default!;
    private UpdateModuleHandler _updateHandler = default!;
    private GetModulesHandler _getHandler = default!;
    private DeleteModuleHandler _deleteHandler = default!;

    public ModuleHandlerTests()
    {
        var options = new DbContextOptionsBuilder<TestAuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new TestAuthDbContext(options);
        Setup();
    }

    private void Setup()
    {
        _createHandler = new CreateModuleHandler(_context, _auditLogger, NullLogger<CreateModuleHandler>.Instance);
        _updateHandler = new UpdateModuleHandler(_context, _auditLogger, NullLogger<UpdateModuleHandler>.Instance);
        _getHandler = new GetModulesHandler(_context);
        _deleteHandler = new DeleteModuleHandler(_context, _auditLogger, NullLogger<DeleteModuleHandler>.Instance);
    }

    [Fact]
    public async Task CreateModule_ReturnsNewId()
    {
        var id = await _createHandler.Handle(new CreateModuleCommand("Users", "User management"), CancellationToken.None);
        id.Should().NotBeEmpty();
        _auditLogger.Entries.Should().ContainSingle(e => e.Action == AuditAction.ModuleCreated);
    }

    [Fact]
    public async Task GetModules_ReturnsCreatedModules()
    {
        await _createHandler.Handle(new CreateModuleCommand("Users", "User management"), CancellationToken.None);
        await _createHandler.Handle(new CreateModuleCommand("Buses", "Bus management"), CancellationToken.None);

        var result = await _getHandler.Handle(new GetModulesQuery(), CancellationToken.None);
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task UpdateModule_UpdatesSuccessfully()
    {
        var id = await _createHandler.Handle(new CreateModuleCommand("Users", "User management"), CancellationToken.None);
        await _updateHandler.Handle(new UpdateModuleCommand(id, "Updated description"), CancellationToken.None);

        var result = await _getHandler.Handle(new GetModulesQuery(), CancellationToken.None);
        result.First().Description.Should().Be("Updated description");
    }

    [Fact]
    public async Task DeleteModule_RemovesSuccessfully()
    {
        var id = await _createHandler.Handle(new CreateModuleCommand("Users", "User management"), CancellationToken.None);
        await _deleteHandler.Handle(new DeleteModuleCommand(id), CancellationToken.None);

        var result = await _getHandler.Handle(new GetModulesQuery(), CancellationToken.None);
        result.Should().BeEmpty();
    }

    public void Dispose() => _context.Dispose();
}
