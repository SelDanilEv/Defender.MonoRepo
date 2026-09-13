using Defender.CarService.Application.Common.Interfaces.Repositories;
using Defender.CarService.Application.Common.Exceptions;
using Defender.CarService.Application.DTOs;
using Defender.CarService.Application.Requests.History;
using Defender.CarService.Application.Requests.Vehicles;
using Defender.CarService.Application.Requests.Maintenance;
using Defender.CarService.Application.Services;
using Defender.CarService.Domain.Entities;
using Defender.CarService.Domain.Enums;
using Defender.CarService.Domain.ValueObjects;
using Defender.Common.Interfaces;
using Moq;
using Defender.CarService.Tests.Domain;
using MediatR;

namespace Defender.CarService.Tests.Application.Services;

public sealed class MyGarageApplicationServiceTests
{
    private static readonly Guid UserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid VehicleId = Guid.Parse("00000000-0000-0000-0000-000000000002");
    private static readonly Guid FirstMaintenanceId = Guid.Parse("00000000-0000-0000-0000-000000000003");
    private static readonly Guid SecondMaintenanceId = Guid.Parse("00000000-0000-0000-0000-000000000004");

    [Fact]
    public async Task GetVehicle_WhenRepositoryScopesAnotherUser_ReturnsNotFoundCode()
    {
        var vehicles = new Mock<IVehicleRepository>();
        vehicles.Setup(repository => repository.GetByIdAsync(UserId, VehicleId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Vehicle?)null);
        var service = CreateService(vehicles: vehicles);

        var exception = await Assert.ThrowsAsync<CarApplicationException>(
            () => service.GetVehicleAsync(VehicleId, CancellationToken.None));

        Assert.Equal("CAR_VEHICLE_NOT_FOUND", exception.Code);
        vehicles.Verify(repository => repository.GetByIdAsync(UserId, VehicleId, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateHistory_WithManyLinks_RecalculatesOdometerAndBothBaselinesInOneTransaction()
    {
        var vehicle = Vehicle.Create(UserId, "Garage", "BMW", "E46", 2002, "ABC-123", timeProvider: FixedTimeProvider(), id: VehicleId);
        var first = MaintenanceItem.Create(UserId, VehicleId, "Oil", 12, null, id: FirstMaintenanceId, timeProvider: FixedTimeProvider());
        var second = MaintenanceItem.Create(UserId, VehicleId, "Brakes", null, 20, id: SecondMaintenanceId, timeProvider: FixedTimeProvider());
        var history = ServiceHistoryRecord.Create(
            UserId,
            VehicleId,
            new DateOnly(2026, 1, 1),
            42_000,
            HistoryType.Maintenance,
            "Annual service",
            linkedMaintenanceItemIds: [FirstMaintenanceId, SecondMaintenanceId],
            timeProvider: FixedTimeProvider());
        var vehicles = new Mock<IVehicleRepository>();
        var maintenance = new Mock<IMaintenanceItemRepository>();
        var histories = new Mock<IServiceHistoryRepository>();
        var coordinator = new Mock<ICarTransactionCoordinator>();
        var context = new Mock<ICarTransactionContext>().Object;

        vehicles.Setup(repository => repository.GetByIdAsync(UserId, VehicleId, context, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vehicle);
        vehicles.Setup(repository => repository.ReplaceAsync(UserId, vehicle, 0, context, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        maintenance.Setup(repository => repository.GetByIdAsync(UserId, VehicleId, FirstMaintenanceId, context, It.IsAny<CancellationToken>()))
            .ReturnsAsync(first);
        maintenance.Setup(repository => repository.GetByIdAsync(UserId, VehicleId, SecondMaintenanceId, context, It.IsAny<CancellationToken>()))
            .ReturnsAsync(second);
        maintenance.Setup(repository => repository.GetForVehicleAsync(UserId, VehicleId, context, It.IsAny<CancellationToken>()))
            .ReturnsAsync([first, second]);
        maintenance.Setup(repository => repository.ReplaceEffectiveBaselineAsync(UserId, VehicleId, It.IsAny<Guid>(), It.IsAny<EffectiveBaseline>(), context, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        histories.Setup(repository => repository.AddAsync(UserId, VehicleId, It.IsAny<ServiceHistoryRecord>(), context, It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);
        histories.Setup(repository => repository.GetForVehicleAsync(UserId, VehicleId, context, It.IsAny<CancellationToken>()))
            .ReturnsAsync([history]);
        histories.Setup(repository => repository.GetLinkedToMaintenanceAsync(UserId, VehicleId, FirstMaintenanceId, context, It.IsAny<CancellationToken>()))
            .ReturnsAsync([history]);
        histories.Setup(repository => repository.GetLinkedToMaintenanceAsync(UserId, VehicleId, SecondMaintenanceId, context, It.IsAny<CancellationToken>()))
            .ReturnsAsync([history]);
        coordinator.Setup(item => item.ExecuteAsync(It.IsAny<Func<ICarTransactionContext, Task<ServiceHistoryRecordDto>>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<ICarTransactionContext, Task<ServiceHistoryRecordDto>> operation, CancellationToken _) => operation(context));

        var service = CreateService(vehicles, maintenance, histories, coordinator);
        var result = await service.CreateHistoryAsync(
            new CreateHistoryCommand
            {
                VehicleId = VehicleId,
                Date = history.Date,
                OdometerKm = history.OdometerKm,
                Type = history.Type,
                Title = history.Title,
                LinkedMaintenanceItemIds = [FirstMaintenanceId, SecondMaintenanceId],
            },
            CancellationToken.None);

        Assert.Equal(history.Id, result.Id);
        Assert.Equal(42_000, vehicle.CurrentOdometerKm);
        Assert.Equal(new DateOnly(2026, 1, 1), first.LastDate);
        Assert.Equal(new DateOnly(2026, 1, 1), second.LastDate);
        maintenance.Verify(repository => repository.ReplaceEffectiveBaselineAsync(UserId, VehicleId, FirstMaintenanceId, It.IsAny<EffectiveBaseline>(), context, It.IsAny<CancellationToken>()), Times.Once);
        maintenance.Verify(repository => repository.ReplaceEffectiveBaselineAsync(UserId, VehicleId, SecondMaintenanceId, It.IsAny<EffectiveBaseline>(), context, It.IsAny<CancellationToken>()), Times.Once);
        vehicles.Verify(repository => repository.ReplaceAsync(UserId, vehicle, 0, context, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateHistory_WhenTransactionFails_PropagatesFailureWithoutReturningSuccess()
    {
        var coordinator = new Mock<ICarTransactionCoordinator>();
        coordinator.Setup(item => item.ExecuteAsync(It.IsAny<Func<ICarTransactionContext, Task<ServiceHistoryRecordDto>>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("transaction failed"));
        var service = CreateService(coordinator: coordinator);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateHistoryAsync(new CreateHistoryCommand { VehicleId = VehicleId }, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateVehicle_WhenVersionCompareFails_ReturnsConcurrencyCode()
    {
        var vehicle = Vehicle.Create(UserId, "Garage", "BMW", "E46", 2002, "ABC-123", timeProvider: FixedTimeProvider(), id: VehicleId);
        var vehicles = new Mock<IVehicleRepository>();
        vehicles.Setup(repository => repository.GetByIdAsync(UserId, VehicleId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vehicle);
        vehicles.Setup(repository => repository.ReplaceAsync(UserId, vehicle, 0, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var service = CreateService(vehicles: vehicles);

        var exception = await Assert.ThrowsAsync<CarApplicationException>(() => service.UpdateVehicleAsync(
            new UpdateVehicleCommand
            {
                VehicleId = VehicleId,
                DisplayName = "Updated",
                Make = "BMW",
                Model = "E46",
                Year = 2002,
                Plate = "ABC-123",
            },
            CancellationToken.None));

        Assert.Equal("CAR_CONCURRENCY_CONFLICT", exception.Code);
    }

    [Fact]
    public async Task DeleteMaintenance_UsesTransactionContextForReferenceCheck()
    {
        var vehicle = Vehicle.Create(UserId, "Garage", "BMW", "E46", 2002, "ABC-123", timeProvider: FixedTimeProvider(), id: VehicleId);
        var item = MaintenanceItem.Create(UserId, VehicleId, "Oil", 12, null, id: FirstMaintenanceId, timeProvider: FixedTimeProvider());
        var context = new Mock<ICarTransactionContext>().Object;
        var vehicles = new Mock<IVehicleRepository>();
        var maintenance = new Mock<IMaintenanceItemRepository>();
        var coordinator = new Mock<ICarTransactionCoordinator>();
        vehicles.Setup(repository => repository.GetByIdAsync(UserId, VehicleId, context, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vehicle);
        maintenance.Setup(repository => repository.GetByIdAsync(UserId, VehicleId, FirstMaintenanceId, context, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);
        maintenance.Setup(repository => repository.DeleteAsync(UserId, VehicleId, FirstMaintenanceId, context, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        coordinator.Setup(transaction => transaction.ExecuteAsync(It.IsAny<Func<ICarTransactionContext, Task<Unit>>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<ICarTransactionContext, Task<Unit>> operation, CancellationToken _) => operation(context));
        var service = CreateService(vehicles: vehicles, maintenance: maintenance, coordinator: coordinator);

        await service.DeleteMaintenanceItemAsync(
            new DeleteMaintenanceItemCommand { VehicleId = VehicleId, MaintenanceItemId = FirstMaintenanceId },
            CancellationToken.None);

        maintenance.Verify(repository => repository.DeleteAsync(UserId, VehicleId, FirstMaintenanceId, context, It.IsAny<CancellationToken>()), Times.Once);
    }

    private MyGarageApplicationService CreateService(
        Mock<IVehicleRepository>? vehicles = null,
        Mock<IMaintenanceItemRepository>? maintenance = null,
        Mock<IServiceHistoryRepository>? histories = null,
        Mock<ICarTransactionCoordinator>? coordinator = null)
    {
        var currentAccountAccessor = new Mock<ICurrentAccountAccessor>();
        currentAccountAccessor.Setup(accessor => accessor.GetAccountId()).Returns(UserId);
        return new MyGarageApplicationService(
            vehicles?.Object ?? new Mock<IVehicleRepository>().Object,
            maintenance?.Object ?? new Mock<IMaintenanceItemRepository>().Object,
            histories?.Object ?? new Mock<IServiceHistoryRepository>().Object,
            new Mock<IInsurancePolicyRepository>().Object,
            coordinator?.Object ?? new Mock<ICarTransactionCoordinator>().Object,
            currentAccountAccessor.Object,
            FixedTimeProvider());
    }

    private static TimeProvider FixedTimeProvider()
        => new TestTimeProvider(new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero));
}
