using BusService.Domain.Entities;
using BusService.Domain.Enums;
using BusService.Domain.Events;
using BusService.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace BusService.UnitTests.Buses;

public class BusTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 1, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid OperatorId = Guid.NewGuid();
    private static readonly Guid DepotId = Guid.NewGuid();

    [Fact]
    public void Register_CreatesActiveBus_AndRaisesBusRegisteredEvent()
    {
        var bus = Bus.Register(Guid.NewGuid(), OperatorId, "dl-1pc-1234", BusType.AcSleeper, 40, DepotId, "Volvo", "9600", 2022, null, null, null, Now);

        bus.PlateNumber.Should().Be("DL-1PC-1234"); // normalized
        bus.Status.Should().Be(BusStatus.Active);
        bus.DomainEvents.Should().ContainSingle(e => e is BusRegisteredDomainEvent);
    }

    [Theory]
    [InlineData(BusStatus.Active, BusStatus.UnderMaintenance, true)]
    [InlineData(BusStatus.Active, BusStatus.Retired, true)]
    [InlineData(BusStatus.UnderMaintenance, BusStatus.Active, true)]
    [InlineData(BusStatus.UnderMaintenance, BusStatus.Retired, true)]
    [InlineData(BusStatus.Retired, BusStatus.Active, false)]
    [InlineData(BusStatus.Retired, BusStatus.UnderMaintenance, false)]
    [InlineData(BusStatus.Active, BusStatus.Active, false)]
    public void ChangeStatus_EnforcesLifecycleRules(BusStatus from, BusStatus to, bool shouldSucceed)
    {
        var bus = Bus.Register(Guid.NewGuid(), OperatorId, "DL-1PC-1234", BusType.AcSleeper, 40, DepotId, null, null, null, null, null, null, Now);
        bus.ClearDomainEvents();

        if (from != BusStatus.Active)
            bus.ChangeStatus(from, Now); // walk to the starting state first

        bus.ClearDomainEvents();
        var act = () => bus.ChangeStatus(to, Now.AddMinutes(1));

        if (shouldSucceed)
        {
            act.Should().NotThrow();
            bus.Status.Should().Be(to);
            bus.DomainEvents.Should().ContainSingle(e => e is BusStatusChangedDomainEvent);
        }
        else
        {
            act.Should().Throw<InvalidBusStatusTransitionException>();
            bus.Status.Should().Be(from);
        }
    }

    [Fact]
    public void UpdateDetails_ChangesFields_AndRaisesBusDetailsUpdatedEvent()
    {
        var bus = Bus.Register(Guid.NewGuid(), OperatorId, "DL-1PC-1234", BusType.AcSleeper, 40, DepotId, null, null, null, null, null, null, Now);
        bus.ClearDomainEvents();

        var newDepotId = Guid.NewGuid();
        bus.UpdateDetails(BusType.NonAcSeater, 45, newDepotId, "Tata", "Starbus", 2023, Now.AddDays(1));

        bus.BusType.Should().Be(BusType.NonAcSeater);
        bus.TotalSeats.Should().Be(45);
        bus.DepotId.Should().Be(newDepotId);
        bus.DomainEvents.Should().ContainSingle(e => e is BusDetailsUpdatedDomainEvent);
    }
}
