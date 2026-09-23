using Defender.CarService.Application.Common.Interfaces.Repositories;
using Defender.CarService.Application.Common.Exceptions;
using Defender.CarService.Application.DTOs;
using Defender.CarService.Application.Requests.History;
using Defender.CarService.Application.Requests.Vehicles;
using Defender.CarService.Application.Requests.Maintenance;
using Defender.CarService.Application.Requests.Insurance;
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
    public async Task GetVehicle_WhenLinkedHistoryAppearsAfterFirstHundred_ReturnsManualAndEffectiveBaselinesSeparately()
    {
        var vehicle = Vehicle.Create(UserId, "Garage", "BMW", "E46", 2002, "ABC-123", timeProvider: FixedTimeProvider(), id: VehicleId);
        var maintenance = MaintenanceItem.Create(
            UserId,
            VehicleId,
            "Oil",
            12,
            null,
            new DateOnly(2025, 1, 2),
            40_000,
            FixedTimeProvider(),
            FirstMaintenanceId);
        var history = ServiceHistoryRecord.Create(
            UserId,
            VehicleId,
            new DateOnly(2026, 1, 3),
            50_000,
            HistoryType.Maintenance,
            "Oil change",
            linkedMaintenanceItemIds: [FirstMaintenanceId],
            timeProvider: FixedTimeProvider());
        maintenance.ApplyEffectiveBaseline(
            vehicle,
            new EffectiveBaseline(history.Date, history.OdometerKm),
            FixedTimeProvider());
        var vehicles = new Mock<IVehicleRepository>();
        var maintenanceRepository = new Mock<IMaintenanceItemRepository>();
        var histories = new Mock<IServiceHistoryRepository>();
        var insurance = new Mock<IInsurancePolicyRepository>();
        vehicles.Setup(repository => repository.GetByIdAsync(UserId, VehicleId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vehicle);
        maintenanceRepository.Setup(repository => repository.GetForVehicleAsync(UserId, VehicleId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([maintenance]);
        var unlinkedHistory = Enumerable.Range(0, 100)
            .Select(index => ServiceHistoryRecord.Create(
                UserId,
                VehicleId,
                new DateOnly(2024, 1, 1),
                index,
                HistoryType.Other,
                $"Other {index}",
                timeProvider: FixedTimeProvider()))
            .ToArray();
        histories.Setup(repository => repository.GetForVehicleAsync(UserId, VehicleId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(unlinkedHistory.Append(history).ToArray());
        insurance.Setup(repository => repository.GetForVehicleAsync(UserId, VehicleId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        var service = CreateService(vehicles, maintenanceRepository, histories, insurance: insurance);

        var result = await service.GetVehicleAsync(VehicleId, CancellationToken.None);

        var item = Assert.Single(result.MaintenanceItems);
        Assert.Equal(new DateOnly(2025, 1, 2), item.ManualBaselineDate);
        Assert.Equal(40_000, item.ManualBaselineOdometerKm);
        Assert.Equal(new DateOnly(2026, 1, 3), item.LastDate);
        Assert.Equal(50_000, item.LastOdometerKm);
        Assert.True(item.HasLinkedHistory);
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
        vehicles.Setup(repository => repository.ReplaceAsync(UserId, vehicle, 0, context, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
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

    [Fact]
    public async Task CreateMaintenance_WhenVehicleVersionCompareFails_ReturnsConcurrencyCode()
    {
        var vehicle = Vehicle.Create(UserId, "Garage", "BMW", "E46", 2002, "ABC-123", timeProvider: FixedTimeProvider(), id: VehicleId);
        var item = MaintenanceItem.Create(UserId, VehicleId, "Oil", 12, null, id: FirstMaintenanceId, timeProvider: FixedTimeProvider());
        var context = new Mock<ICarTransactionContext>().Object;
        var vehicles = new Mock<IVehicleRepository>();
        var maintenance = new Mock<IMaintenanceItemRepository>();
        var coordinator = new Mock<ICarTransactionCoordinator>();
        vehicles.Setup(repository => repository.GetByIdAsync(UserId, VehicleId, context, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vehicle);
        vehicles.Setup(repository => repository.ReplaceAsync(UserId, vehicle, 0, context, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        maintenance.Setup(repository => repository.AddAsync(UserId, VehicleId, It.IsAny<MaintenanceItem>(), context, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);
        coordinator.Setup(transaction => transaction.ExecuteAsync(It.IsAny<Func<ICarTransactionContext, Task<MaintenanceItemDto>>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<ICarTransactionContext, Task<MaintenanceItemDto>> operation, CancellationToken _) => operation(context));
        var service = CreateService(vehicles: vehicles, maintenance: maintenance, coordinator: coordinator);

        var exception = await Assert.ThrowsAsync<CarApplicationException>(() => service.CreateMaintenanceItemAsync(
            new CreateMaintenanceItemCommand
            {
                VehicleId = VehicleId,
                Name = "Oil",
                IntervalMonths = 12,
            },
            CancellationToken.None));

        Assert.Equal("CAR_CONCURRENCY_CONFLICT", exception.Code);
    }

    [Fact]
    public async Task DeleteMaintenance_WhenVehicleArchiveWins_ReturnsConcurrencyCode()
    {
        var vehicle = Vehicle.Create(UserId, "Garage", "BMW", "E46", 2002, "ABC-123", timeProvider: FixedTimeProvider(), id: VehicleId);
        var item = MaintenanceItem.Create(UserId, VehicleId, "Oil", 12, null, id: FirstMaintenanceId, timeProvider: FixedTimeProvider());
        var context = new Mock<ICarTransactionContext>().Object;
        var vehicles = new Mock<IVehicleRepository>();
        var maintenance = new Mock<IMaintenanceItemRepository>();
        var coordinator = new Mock<ICarTransactionCoordinator>();
        vehicles.Setup(repository => repository.GetByIdAsync(UserId, VehicleId, context, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vehicle);
        vehicles.Setup(repository => repository.ReplaceAsync(UserId, vehicle, 0, context, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        maintenance.Setup(repository => repository.GetByIdAsync(UserId, VehicleId, FirstMaintenanceId, context, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);
        maintenance.Setup(repository => repository.DeleteAsync(UserId, VehicleId, FirstMaintenanceId, context, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        coordinator.Setup(transaction => transaction.ExecuteAsync(It.IsAny<Func<ICarTransactionContext, Task<Unit>>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<ICarTransactionContext, Task<Unit>> operation, CancellationToken _) => operation(context));
        var service = CreateService(vehicles: vehicles, maintenance: maintenance, coordinator: coordinator);

        var exception = await Assert.ThrowsAsync<CarApplicationException>(() => service.DeleteMaintenanceItemAsync(
            new DeleteMaintenanceItemCommand { VehicleId = VehicleId, MaintenanceItemId = FirstMaintenanceId },
            CancellationToken.None));

        Assert.Equal("CAR_CONCURRENCY_CONFLICT", exception.Code);
    }

    [Fact]
    public async Task CreateInsurance_WhenVehicleArchiveWins_ReturnsConcurrencyCode()
    {
        var vehicle = Vehicle.Create(UserId, "Garage", "BMW", "E46", 2002, "ABC-123", timeProvider: FixedTimeProvider(), id: VehicleId);
        var policy = InsurancePolicy.Create(UserId, VehicleId, "Provider", null, null, new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31), timeProvider: FixedTimeProvider());
        var context = new Mock<ICarTransactionContext>().Object;
        var vehicles = new Mock<IVehicleRepository>();
        var insurance = new Mock<IInsurancePolicyRepository>();
        var coordinator = new Mock<ICarTransactionCoordinator>();
        vehicles.Setup(repository => repository.GetByIdAsync(UserId, VehicleId, context, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vehicle);
        vehicles.Setup(repository => repository.ReplaceAsync(UserId, vehicle, 0, context, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        insurance.Setup(repository => repository.AddAsync(UserId, VehicleId, It.IsAny<InsurancePolicy>(), context, It.IsAny<CancellationToken>()))
            .ReturnsAsync(policy);
        coordinator.Setup(transaction => transaction.ExecuteAsync(It.IsAny<Func<ICarTransactionContext, Task<InsurancePolicyDto>>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<ICarTransactionContext, Task<InsurancePolicyDto>> operation, CancellationToken _) => operation(context));
        var service = CreateService(vehicles: vehicles, insurance: insurance, coordinator: coordinator);

        var exception = await Assert.ThrowsAsync<CarApplicationException>(() => service.CreateInsurancePolicyAsync(
            new CreateInsurancePolicyCommand
            {
                VehicleId = VehicleId,
                Provider = "Provider",
                StartDate = policy.StartDate,
                EndDate = policy.EndDate,
            },
            CancellationToken.None));

        Assert.Equal("CAR_CONCURRENCY_CONFLICT", exception.Code);
    }

    [Fact]
    public async Task UpdateInsurance_WhenVehicleArchiveWins_ReturnsConcurrencyCode()
    {
        var vehicle = Vehicle.Create(UserId, "Garage", "BMW", "E46", 2002, "ABC-123", timeProvider: FixedTimeProvider(), id: VehicleId);
        var policyId = Guid.Parse("00000000-0000-0000-0000-000000000005");
        var policy = InsurancePolicy.Create(UserId, VehicleId, "Provider", null, null, new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31), id: policyId, timeProvider: FixedTimeProvider());
        var context = new Mock<ICarTransactionContext>().Object;
        var vehicles = new Mock<IVehicleRepository>();
        var insurance = new Mock<IInsurancePolicyRepository>();
        var coordinator = new Mock<ICarTransactionCoordinator>();
        vehicles.Setup(repository => repository.GetByIdAsync(UserId, VehicleId, context, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vehicle);
        vehicles.Setup(repository => repository.ReplaceAsync(UserId, vehicle, 0, context, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        insurance.Setup(repository => repository.GetByIdAsync(UserId, VehicleId, policyId, context, It.IsAny<CancellationToken>()))
            .ReturnsAsync(policy);
        insurance.Setup(repository => repository.ReplaceAsync(UserId, VehicleId, policy, context, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        coordinator.Setup(transaction => transaction.ExecuteAsync(It.IsAny<Func<ICarTransactionContext, Task<InsurancePolicyDto>>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<ICarTransactionContext, Task<InsurancePolicyDto>> operation, CancellationToken _) => operation(context));
        var service = CreateService(vehicles: vehicles, insurance: insurance, coordinator: coordinator);

        var exception = await Assert.ThrowsAsync<CarApplicationException>(() => service.UpdateInsurancePolicyAsync(
            new UpdateInsurancePolicyCommand
            {
                VehicleId = VehicleId,
                InsurancePolicyId = policyId,
                Provider = "Updated",
                StartDate = policy.StartDate,
                EndDate = policy.EndDate,
            },
            CancellationToken.None));

        Assert.Equal("CAR_CONCURRENCY_CONFLICT", exception.Code);
    }

    [Fact]
    public async Task CreateHistory_WhenLinkedMaintenanceIdsAreNull_ReturnsLinkErrorCode()
    {
        var vehicle = Vehicle.Create(UserId, "Garage", "BMW", "E46", 2002, "ABC-123", timeProvider: FixedTimeProvider(), id: VehicleId);
        var context = new Mock<ICarTransactionContext>().Object;
        var vehicles = new Mock<IVehicleRepository>();
        var coordinator = new Mock<ICarTransactionCoordinator>();
        vehicles.Setup(repository => repository.GetByIdAsync(UserId, VehicleId, context, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vehicle);
        coordinator.Setup(transaction => transaction.ExecuteAsync(It.IsAny<Func<ICarTransactionContext, Task<ServiceHistoryRecordDto>>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<ICarTransactionContext, Task<ServiceHistoryRecordDto>> operation, CancellationToken _) => operation(context));
        var service = CreateService(vehicles: vehicles, coordinator: coordinator);

        var exception = await Assert.ThrowsAsync<CarApplicationException>(() => service.CreateHistoryAsync(
            new CreateHistoryCommand
            {
                VehicleId = VehicleId,
                Date = new DateOnly(2026, 1, 1),
                OdometerKm = 100,
                Type = HistoryType.Maintenance,
                Title = "Service",
                LinkedMaintenanceItemIds = null!,
            },
            CancellationToken.None));

        Assert.Equal("CAR_HISTORY_LINK_INVALID", exception.Code);
    }

    [Fact]
    public async Task GetHistory_WhenPageOffsetExceedsIntRange_UsesBoundedRepositoryPage()
    {
        var vehicle = Vehicle.Create(UserId, "Garage", "BMW", "E46", 2002, "ABC-123", timeProvider: FixedTimeProvider(), id: VehicleId);
        var vehicles = new Mock<IVehicleRepository>();
        var histories = new Mock<IServiceHistoryRepository>();
        vehicles.Setup(repository => repository.GetByIdAsync(UserId, VehicleId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vehicle);
        histories.Setup(repository => repository.GetPageForVehicleAsync(UserId, VehicleId, int.MaxValue, 100, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Array.Empty<ServiceHistoryRecord>(), 1));
        var service = CreateService(vehicles: vehicles, histories: histories);

        var result = await service.GetHistoryAsync(
            new GetHistoryQuery { VehicleId = VehicleId, Page = int.MaxValue, PageSize = 100 },
            CancellationToken.None);

        Assert.Empty(result.Items);
        Assert.Equal(1, result.TotalItemsCount);
        histories.Verify(repository => repository.GetPageForVehicleAsync(UserId, VehicleId, int.MaxValue, 100, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateHistory_WhenCoordinatorReportsDatabaseFailure_ReturnsStableCode()
    {
        var coordinator = new Mock<ICarTransactionCoordinator>();
        coordinator.Setup(item => item.ExecuteAsync(It.IsAny<Func<ICarTransactionContext, Task<ServiceHistoryRecordDto>>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Defender.Common.Exceptions.ServiceException(Defender.Common.Errors.ErrorCode.CM_DatabaseIssue));
        var service = CreateService(coordinator: coordinator);

        var exception = await Assert.ThrowsAsync<CarApplicationException>(() => service.CreateHistoryAsync(
            new CreateHistoryCommand { VehicleId = VehicleId },
            CancellationToken.None));

        Assert.Equal("CAR_DATABASE_UNAVAILABLE", exception.Code);
    }

    [Fact]
    public async Task CreateMaintenance_WhenVehicleIsArchived_ReturnsArchiveCode()
    {
        var vehicle = Vehicle.Create(UserId, "Garage", "BMW", "E46", 2002, "ABC-123", timeProvider: FixedTimeProvider(), id: VehicleId);
        vehicle.Archive(FixedTimeProvider());
        var context = new Mock<ICarTransactionContext>().Object;
        var vehicles = new Mock<IVehicleRepository>();
        var coordinator = new Mock<ICarTransactionCoordinator>();
        vehicles.Setup(repository => repository.GetByIdAsync(UserId, VehicleId, context, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vehicle);
        coordinator.Setup(transaction => transaction.ExecuteAsync(It.IsAny<Func<ICarTransactionContext, Task<MaintenanceItemDto>>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<ICarTransactionContext, Task<MaintenanceItemDto>> operation, CancellationToken _) => operation(context));
        var service = CreateService(vehicles: vehicles, coordinator: coordinator);

        var exception = await Assert.ThrowsAsync<CarApplicationException>(() => service.CreateMaintenanceItemAsync(
            new CreateMaintenanceItemCommand { VehicleId = VehicleId, Name = "Oil", IntervalMonths = 12 },
            CancellationToken.None));

        Assert.Equal("CAR_VEHICLE_ARCHIVED", exception.Code);
    }

    [Fact]
    public async Task CreateMaintenance_WithoutBaselineReturnsBlankBaseline()
    {
        var vehicle = Vehicle.Create(UserId, "Garage", "BMW", "E46", 2002, "ABC-123", timeProvider: FixedTimeProvider(), id: VehicleId);
        var item = MaintenanceItem.Create(UserId, VehicleId, "Oil", 12, null, id: FirstMaintenanceId, timeProvider: FixedTimeProvider());
        var context = new Mock<ICarTransactionContext>().Object;
        var vehicles = new Mock<IVehicleRepository>();
        var maintenance = new Mock<IMaintenanceItemRepository>();
        var coordinator = new Mock<ICarTransactionCoordinator>();
        vehicles.Setup(repository => repository.GetByIdAsync(UserId, VehicleId, context, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vehicle);
        vehicles.Setup(repository => repository.ReplaceAsync(UserId, vehicle, 0, context, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        maintenance.Setup(repository => repository.AddAsync(UserId, VehicleId, It.IsAny<MaintenanceItem>(), context, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);
        coordinator.Setup(transaction => transaction.ExecuteAsync(It.IsAny<Func<ICarTransactionContext, Task<MaintenanceItemDto>>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<ICarTransactionContext, Task<MaintenanceItemDto>> operation, CancellationToken _) => operation(context));
        var service = CreateService(vehicles: vehicles, maintenance: maintenance, coordinator: coordinator);

        var result = await service.CreateMaintenanceItemAsync(
            new CreateMaintenanceItemCommand { VehicleId = VehicleId, Name = "Oil", IntervalMonths = 12 },
            CancellationToken.None);

        Assert.Null(result.LastDate);
        Assert.Null(result.LastOdometerKm);
    }

    [Fact]
    public async Task UpdateMaintenance_WithLinkedHistory_PreservesManualBaselineAndReportsReference()
    {
        var vehicle = Vehicle.Create(UserId, "Garage", "BMW", "E46", 2002, "ABC-123", timeProvider: FixedTimeProvider(), id: VehicleId);
        var item = MaintenanceItem.Create(
            UserId,
            VehicleId,
            "Oil",
            12,
            null,
            new DateOnly(2025, 1, 2),
            40_000,
            FixedTimeProvider(),
            FirstMaintenanceId);
        var history = ServiceHistoryRecord.Create(
            UserId,
            VehicleId,
            new DateOnly(2026, 1, 3),
            50_000,
            HistoryType.Maintenance,
            "Oil change",
            linkedMaintenanceItemIds: [FirstMaintenanceId],
            timeProvider: FixedTimeProvider());
        item.ApplyEffectiveBaseline(vehicle, new EffectiveBaseline(history.Date, history.OdometerKm), FixedTimeProvider());
        var context = new Mock<ICarTransactionContext>().Object;
        var vehicles = new Mock<IVehicleRepository>();
        var maintenance = new Mock<IMaintenanceItemRepository>();
        var histories = new Mock<IServiceHistoryRepository>();
        var coordinator = new Mock<ICarTransactionCoordinator>();
        vehicles.Setup(repository => repository.GetByIdAsync(UserId, VehicleId, context, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vehicle);
        vehicles.Setup(repository => repository.ReplaceAsync(UserId, vehicle, 0, context, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        maintenance.Setup(repository => repository.GetByIdAsync(UserId, VehicleId, FirstMaintenanceId, context, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);
        maintenance.Setup(repository => repository.ReplaceAsync(UserId, VehicleId, item, context, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        histories.Setup(repository => repository.GetLinkedToMaintenanceAsync(UserId, VehicleId, FirstMaintenanceId, context, It.IsAny<CancellationToken>()))
            .ReturnsAsync([history]);
        coordinator.Setup(transaction => transaction.ExecuteAsync(It.IsAny<Func<ICarTransactionContext, Task<MaintenanceItemDto>>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<ICarTransactionContext, Task<MaintenanceItemDto>> operation, CancellationToken _) => operation(context));
        var service = CreateService(vehicles: vehicles, maintenance: maintenance, histories: histories, coordinator: coordinator);

        var result = await service.UpdateMaintenanceItemAsync(
            new UpdateMaintenanceItemCommand
            {
                VehicleId = VehicleId,
                MaintenanceItemId = FirstMaintenanceId,
                Name = "Oil updated",
                IntervalMonths = 12,
                ManualBaselineDate = new DateOnly(2025, 1, 2),
                ManualBaselineOdometerKm = 40_000,
            },
            CancellationToken.None);

        Assert.Equal(new DateOnly(2025, 1, 2), result.ManualBaselineDate);
        Assert.Equal(40_000, result.ManualBaselineOdometerKm);
        Assert.Equal(new DateOnly(2026, 1, 3), result.LastDate);
        Assert.Equal(50_000, result.LastOdometerKm);
        Assert.True(result.HasLinkedHistory);
    }

    [Fact]
    public async Task CreateHistory_WhenVehicleVersionIsStale_ReturnsConcurrencyCode()
    {
        var vehicle = Vehicle.Create(UserId, "Garage", "BMW", "E46", 2002, "ABC-123", timeProvider: FixedTimeProvider(), id: VehicleId);
        var history = ServiceHistoryRecord.Create(UserId, VehicleId, new DateOnly(2026, 1, 1), 100, HistoryType.Repair, "Repair", timeProvider: FixedTimeProvider());
        var context = new Mock<ICarTransactionContext>().Object;
        var vehicles = new Mock<IVehicleRepository>();
        var histories = new Mock<IServiceHistoryRepository>();
        var coordinator = new Mock<ICarTransactionCoordinator>();
        vehicles.Setup(repository => repository.GetByIdAsync(UserId, VehicleId, context, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vehicle);
        vehicles.Setup(repository => repository.ReplaceAsync(UserId, vehicle, 0, context, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        histories.Setup(repository => repository.AddAsync(UserId, VehicleId, It.IsAny<ServiceHistoryRecord>(), context, It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);
        histories.Setup(repository => repository.GetForVehicleAsync(UserId, VehicleId, context, It.IsAny<CancellationToken>()))
            .ReturnsAsync([history]);
        coordinator.Setup(transaction => transaction.ExecuteAsync(It.IsAny<Func<ICarTransactionContext, Task<ServiceHistoryRecordDto>>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<ICarTransactionContext, Task<ServiceHistoryRecordDto>> operation, CancellationToken _) => operation(context));
        var service = CreateService(vehicles: vehicles, histories: histories, coordinator: coordinator);

        var exception = await Assert.ThrowsAsync<CarApplicationException>(() => service.CreateHistoryAsync(
            new CreateHistoryCommand
            {
                VehicleId = VehicleId,
                Date = history.Date,
                OdometerKm = history.OdometerKm,
                Type = history.Type,
                Title = history.Title,
            },
            CancellationToken.None));

        Assert.Equal("CAR_CONCURRENCY_CONFLICT", exception.Code);
    }

    [Fact]
    public async Task CreateHistory_WhenChronologyDecreases_ReturnsSequenceCode()
    {
        var vehicle = Vehicle.Create(UserId, "Garage", "BMW", "E46", 2002, "ABC-123", timeProvider: FixedTimeProvider(), id: VehicleId);
        var existing = ServiceHistoryRecord.Create(UserId, VehicleId, new DateOnly(2026, 1, 10), 200, HistoryType.Repair, "Later", timeProvider: FixedTimeProvider());
        var added = ServiceHistoryRecord.Create(UserId, VehicleId, new DateOnly(2026, 1, 5), 300, HistoryType.Repair, "Earlier", timeProvider: FixedTimeProvider());
        var context = new Mock<ICarTransactionContext>().Object;
        var vehicles = new Mock<IVehicleRepository>();
        var histories = new Mock<IServiceHistoryRepository>();
        var coordinator = new Mock<ICarTransactionCoordinator>();
        vehicles.Setup(repository => repository.GetByIdAsync(UserId, VehicleId, context, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vehicle);
        histories.Setup(repository => repository.AddAsync(UserId, VehicleId, It.IsAny<ServiceHistoryRecord>(), context, It.IsAny<CancellationToken>()))
            .ReturnsAsync(added);
        histories.Setup(repository => repository.GetForVehicleAsync(UserId, VehicleId, context, It.IsAny<CancellationToken>()))
            .ReturnsAsync([existing, added]);
        coordinator.Setup(transaction => transaction.ExecuteAsync(It.IsAny<Func<ICarTransactionContext, Task<ServiceHistoryRecordDto>>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<ICarTransactionContext, Task<ServiceHistoryRecordDto>> operation, CancellationToken _) => operation(context));
        var service = CreateService(vehicles: vehicles, histories: histories, coordinator: coordinator);

        var exception = await Assert.ThrowsAsync<CarApplicationException>(() => service.CreateHistoryAsync(
            new CreateHistoryCommand
            {
                VehicleId = VehicleId,
                Date = added.Date,
                OdometerKm = added.OdometerKm,
                Type = added.Type,
                Title = added.Title,
            },
            CancellationToken.None));

        Assert.Equal("CAR_ODOMETER_SEQUENCE_INVALID", exception.Code);
    }

    [Fact]
    public async Task UpdateHistory_RecalculatesOldAndNewMaintenanceBaselines()
    {
        var vehicle = Vehicle.Create(UserId, "Garage", "BMW", "E46", 2002, "ABC-123", timeProvider: FixedTimeProvider(), id: VehicleId);
        var historyId = Guid.Parse("00000000-0000-0000-0000-000000000006");
        var oldHistory = ServiceHistoryRecord.Create(UserId, VehicleId, new DateOnly(2026, 1, 1), 100, HistoryType.Maintenance, "Service", linkedMaintenanceItemIds: [FirstMaintenanceId], timeProvider: FixedTimeProvider(), id: historyId);
        var updatedHistory = ServiceHistoryRecord.Create(UserId, VehicleId, new DateOnly(2026, 1, 2), 200, HistoryType.Maintenance, "Service", linkedMaintenanceItemIds: [SecondMaintenanceId], timeProvider: FixedTimeProvider(), id: historyId);
        var first = MaintenanceItem.Create(UserId, VehicleId, "Oil", 12, null, id: FirstMaintenanceId, timeProvider: FixedTimeProvider());
        var second = MaintenanceItem.Create(UserId, VehicleId, "Brakes", null, 20, id: SecondMaintenanceId, timeProvider: FixedTimeProvider());
        var context = new Mock<ICarTransactionContext>().Object;
        var vehicles = new Mock<IVehicleRepository>();
        var maintenance = new Mock<IMaintenanceItemRepository>();
        var histories = new Mock<IServiceHistoryRepository>();
        var coordinator = new Mock<ICarTransactionCoordinator>();
        vehicles.Setup(repository => repository.GetByIdAsync(UserId, VehicleId, context, It.IsAny<CancellationToken>())).ReturnsAsync(vehicle);
        vehicles.Setup(repository => repository.ReplaceAsync(UserId, vehicle, 0, context, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        maintenance.Setup(repository => repository.GetByIdAsync(UserId, VehicleId, FirstMaintenanceId, context, It.IsAny<CancellationToken>())).ReturnsAsync(first);
        maintenance.Setup(repository => repository.GetByIdAsync(UserId, VehicleId, SecondMaintenanceId, context, It.IsAny<CancellationToken>())).ReturnsAsync(second);
        maintenance.Setup(repository => repository.ReplaceEffectiveBaselineAsync(UserId, VehicleId, It.IsAny<Guid>(), It.IsAny<EffectiveBaseline>(), context, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        histories.Setup(repository => repository.GetByIdAsync(UserId, VehicleId, historyId, context, It.IsAny<CancellationToken>())).ReturnsAsync(oldHistory);
        histories.Setup(repository => repository.ReplaceAsync(UserId, VehicleId, oldHistory, context, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        histories.Setup(repository => repository.GetForVehicleAsync(UserId, VehicleId, context, It.IsAny<CancellationToken>())).ReturnsAsync([updatedHistory]);
        coordinator.Setup(transaction => transaction.ExecuteAsync(It.IsAny<Func<ICarTransactionContext, Task<ServiceHistoryRecordDto>>>(), It.IsAny<CancellationToken>())).Returns((Func<ICarTransactionContext, Task<ServiceHistoryRecordDto>> operation, CancellationToken _) => operation(context));
        var service = CreateService(vehicles: vehicles, maintenance: maintenance, histories: histories, coordinator: coordinator);

        await service.UpdateHistoryAsync(
            new UpdateHistoryCommand
            {
                VehicleId = VehicleId,
                HistoryId = historyId,
                Date = updatedHistory.Date,
                OdometerKm = updatedHistory.OdometerKm,
                Type = updatedHistory.Type,
                Title = updatedHistory.Title,
                LinkedMaintenanceItemIds = [SecondMaintenanceId],
            },
            CancellationToken.None);

        maintenance.Verify(repository => repository.ReplaceEffectiveBaselineAsync(UserId, VehicleId, FirstMaintenanceId, It.IsAny<EffectiveBaseline>(), context, It.IsAny<CancellationToken>()), Times.Once);
        maintenance.Verify(repository => repository.ReplaceEffectiveBaselineAsync(UserId, VehicleId, SecondMaintenanceId, It.IsAny<EffectiveBaseline>(), context, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteHistory_RecalculatesLinkedMaintenanceBaselineAfterDelete()
    {
        var vehicle = Vehicle.Create(UserId, "Garage", "BMW", "E46", 2002, "ABC-123", timeProvider: FixedTimeProvider(), id: VehicleId);
        var historyId = Guid.Parse("00000000-0000-0000-0000-000000000007");
        var history = ServiceHistoryRecord.Create(UserId, VehicleId, new DateOnly(2026, 1, 1), 100, HistoryType.Maintenance, "Service", linkedMaintenanceItemIds: [FirstMaintenanceId], timeProvider: FixedTimeProvider(), id: historyId);
        var first = MaintenanceItem.Create(UserId, VehicleId, "Oil", 12, null, new DateOnly(2025, 1, 1), 10, FixedTimeProvider(), FirstMaintenanceId);
        var context = new Mock<ICarTransactionContext>().Object;
        var vehicles = new Mock<IVehicleRepository>();
        var maintenance = new Mock<IMaintenanceItemRepository>();
        var histories = new Mock<IServiceHistoryRepository>();
        var coordinator = new Mock<ICarTransactionCoordinator>();
        vehicles.Setup(repository => repository.GetByIdAsync(UserId, VehicleId, context, It.IsAny<CancellationToken>())).ReturnsAsync(vehicle);
        vehicles.Setup(repository => repository.ReplaceAsync(UserId, vehicle, 0, context, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        maintenance.Setup(repository => repository.GetByIdAsync(UserId, VehicleId, FirstMaintenanceId, context, It.IsAny<CancellationToken>())).ReturnsAsync(first);
        maintenance.Setup(repository => repository.ReplaceEffectiveBaselineAsync(UserId, VehicleId, FirstMaintenanceId, It.IsAny<EffectiveBaseline>(), context, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        histories.Setup(repository => repository.GetByIdAsync(UserId, VehicleId, historyId, context, It.IsAny<CancellationToken>())).ReturnsAsync(history);
        histories.Setup(repository => repository.DeleteAsync(UserId, VehicleId, historyId, context, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        histories.Setup(repository => repository.GetForVehicleAsync(UserId, VehicleId, context, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        coordinator.Setup(transaction => transaction.ExecuteAsync(It.IsAny<Func<ICarTransactionContext, Task<Unit>>>(), It.IsAny<CancellationToken>())).Returns((Func<ICarTransactionContext, Task<Unit>> operation, CancellationToken _) => operation(context));
        var service = CreateService(vehicles: vehicles, maintenance: maintenance, histories: histories, coordinator: coordinator);

        await service.DeleteHistoryAsync(
            new DeleteHistoryCommand { VehicleId = VehicleId, HistoryId = historyId },
            CancellationToken.None);

        maintenance.Verify(repository => repository.ReplaceEffectiveBaselineAsync(UserId, VehicleId, FirstMaintenanceId, It.Is<EffectiveBaseline>(baseline => baseline.Date == new DateOnly(2025, 1, 1) && baseline.OdometerKm == 10), context, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetVehicles_WhenMultipleVehiclesReturned_UsesBatchRepositoryMethodsOnce()
    {
        var firstVehicle = Vehicle.Create(UserId, "Garage", "BMW", "E46", 2002, "ABC-123", timeProvider: FixedTimeProvider(), id: VehicleId);
        var secondVehicleId = Guid.Parse("00000000-0000-0000-0000-000000000008");
        var secondVehicle = Vehicle.Create(UserId, "Garage", "Audi", "A4", 2010, "XYZ-987", timeProvider: FixedTimeProvider(), id: secondVehicleId);
        var vehicles = new Mock<IVehicleRepository>();
        var maintenance = new Mock<IMaintenanceItemRepository>();
        var insurance = new Mock<IInsurancePolicyRepository>();
        vehicles.Setup(repository => repository.GetPageForUserAsync(UserId, false, 0, 25, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new[] { firstVehicle, secondVehicle }, 2));
        maintenance.Setup(repository => repository.GetForVehiclesAsync(UserId, It.Is<IReadOnlyList<Guid>>(ids => ids.Contains(VehicleId) && ids.Contains(secondVehicleId)), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        insurance.Setup(repository => repository.GetForVehiclesAsync(UserId, It.Is<IReadOnlyList<Guid>>(ids => ids.Contains(VehicleId) && ids.Contains(secondVehicleId)), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        var service = CreateService(vehicles: vehicles, maintenance: maintenance, insurance: insurance);

        var result = await service.GetVehiclesAsync(new GetVehiclesQuery(), CancellationToken.None);

        Assert.Equal(2, result.Items.Count);
        maintenance.Verify(repository => repository.GetForVehicleAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<ICarTransactionContext>(), It.IsAny<CancellationToken>()), Times.Never);
        insurance.Verify(repository => repository.GetForVehicleAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<ICarTransactionContext>(), It.IsAny<CancellationToken>()), Times.Never);
        maintenance.Verify(repository => repository.GetForVehiclesAsync(UserId, It.IsAny<IReadOnlyList<Guid>>(), null, It.IsAny<CancellationToken>()), Times.Once);
        insurance.Verify(repository => repository.GetForVehiclesAsync(UserId, It.IsAny<IReadOnlyList<Guid>>(), null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetVehicles_WhenPageIsNegative_ThrowsPaginationInvalid()
    {
        var service = CreateService();

        var exception = await Assert.ThrowsAsync<CarApplicationException>(() => service.GetVehiclesAsync(
            new GetVehiclesQuery { Page = -1, PageSize = 25 },
            CancellationToken.None));

        Assert.Equal("CAR_VEHICLES_PAGINATION_INVALID", exception.Code);
    }

    [Fact]
    public async Task GetVehicles_WhenPageSizeExceedsMaximum_ThrowsPaginationInvalid()
    {
        var service = CreateService();

        var exception = await Assert.ThrowsAsync<CarApplicationException>(() => service.GetVehiclesAsync(
            new GetVehiclesQuery { Page = 0, PageSize = 101 },
            CancellationToken.None));

        Assert.Equal("CAR_VEHICLES_PAGINATION_INVALID", exception.Code);
    }

    [Fact]
    public async Task GetVehicles_WhenRequested_ReturnsPagedSummaryWithRepositoryTotals()
    {
        var vehicle = Vehicle.Create(UserId, "Garage", "BMW", "E46", 2002, "ABC-123", timeProvider: FixedTimeProvider(), id: VehicleId);
        var vehicles = new Mock<IVehicleRepository>();
        var maintenance = new Mock<IMaintenanceItemRepository>();
        var insurance = new Mock<IInsurancePolicyRepository>();
        vehicles.Setup(repository => repository.GetPageForUserAsync(UserId, false, 1, 10, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new[] { vehicle }, 11));
        maintenance.Setup(repository => repository.GetForVehiclesAsync(UserId, It.IsAny<IReadOnlyList<Guid>>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        insurance.Setup(repository => repository.GetForVehiclesAsync(UserId, It.IsAny<IReadOnlyList<Guid>>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        var service = CreateService(vehicles: vehicles, maintenance: maintenance, insurance: insurance);

        var result = await service.GetVehiclesAsync(
            new GetVehiclesQuery { Page = 1, PageSize = 10 },
            CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal(11, result.TotalItemsCount);
        Assert.Equal(1, result.CurrentPage);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(2, result.TotalPagesCount);
    }

    [Fact]
    public async Task DeleteInsurance_WhenPolicyExists_DeletesAndTouchesVehicle()
    {
        var vehicle = Vehicle.Create(UserId, "Garage", "BMW", "E46", 2002, "ABC-123", timeProvider: FixedTimeProvider(), id: VehicleId);
        var policyId = Guid.Parse("00000000-0000-0000-0000-000000000009");
        var policy = InsurancePolicy.Create(UserId, VehicleId, "Provider", null, null, new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31), id: policyId, timeProvider: FixedTimeProvider());
        var context = new Mock<ICarTransactionContext>().Object;
        var vehicles = new Mock<IVehicleRepository>();
        var insurance = new Mock<IInsurancePolicyRepository>();
        var coordinator = new Mock<ICarTransactionCoordinator>();
        vehicles.Setup(repository => repository.GetByIdAsync(UserId, VehicleId, context, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vehicle);
        vehicles.Setup(repository => repository.ReplaceAsync(UserId, vehicle, 0, context, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        insurance.Setup(repository => repository.GetByIdAsync(UserId, VehicleId, policyId, context, It.IsAny<CancellationToken>()))
            .ReturnsAsync(policy);
        insurance.Setup(repository => repository.DeleteAsync(UserId, VehicleId, policyId, context, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        coordinator.Setup(transaction => transaction.ExecuteAsync(It.IsAny<Func<ICarTransactionContext, Task<Unit>>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<ICarTransactionContext, Task<Unit>> operation, CancellationToken _) => operation(context));
        var service = CreateService(vehicles: vehicles, insurance: insurance, coordinator: coordinator);

        await service.DeleteInsurancePolicyAsync(
            new DeleteInsurancePolicyCommand { VehicleId = VehicleId, InsuranceId = policyId },
            CancellationToken.None);

        insurance.Verify(repository => repository.DeleteAsync(UserId, VehicleId, policyId, context, It.IsAny<CancellationToken>()), Times.Once);
        vehicles.Verify(repository => repository.ReplaceAsync(UserId, vehicle, 0, context, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteInsurance_WhenPolicyNotFound_ReturnsInsuranceNotFoundCode()
    {
        var vehicle = Vehicle.Create(UserId, "Garage", "BMW", "E46", 2002, "ABC-123", timeProvider: FixedTimeProvider(), id: VehicleId);
        var policyId = Guid.Parse("00000000-0000-0000-0000-00000000000a");
        var context = new Mock<ICarTransactionContext>().Object;
        var vehicles = new Mock<IVehicleRepository>();
        var insurance = new Mock<IInsurancePolicyRepository>();
        var coordinator = new Mock<ICarTransactionCoordinator>();
        vehicles.Setup(repository => repository.GetByIdAsync(UserId, VehicleId, context, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vehicle);
        insurance.Setup(repository => repository.GetByIdAsync(UserId, VehicleId, policyId, context, It.IsAny<CancellationToken>()))
            .ReturnsAsync((InsurancePolicy?)null);
        coordinator.Setup(transaction => transaction.ExecuteAsync(It.IsAny<Func<ICarTransactionContext, Task<Unit>>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<ICarTransactionContext, Task<Unit>> operation, CancellationToken _) => operation(context));
        var service = CreateService(vehicles: vehicles, insurance: insurance, coordinator: coordinator);

        var exception = await Assert.ThrowsAsync<CarApplicationException>(() => service.DeleteInsurancePolicyAsync(
            new DeleteInsurancePolicyCommand { VehicleId = VehicleId, InsuranceId = policyId },
            CancellationToken.None));

        Assert.Equal("CAR_INSURANCE_NOT_FOUND", exception.Code);
        insurance.Verify(repository => repository.DeleteAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<ICarTransactionContext>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteInsurance_WhenPolicyBelongsToDifferentUser_ReturnsInsuranceNotFoundCode()
    {
        var vehicle = Vehicle.Create(UserId, "Garage", "BMW", "E46", 2002, "ABC-123", timeProvider: FixedTimeProvider(), id: VehicleId);
        var otherUserPolicyId = Guid.Parse("00000000-0000-0000-0000-00000000000b");
        var context = new Mock<ICarTransactionContext>().Object;
        var vehicles = new Mock<IVehicleRepository>();
        var insurance = new Mock<IInsurancePolicyRepository>();
        var coordinator = new Mock<ICarTransactionCoordinator>();
        vehicles.Setup(repository => repository.GetByIdAsync(UserId, VehicleId, context, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vehicle);
        insurance.Setup(repository => repository.GetByIdAsync(UserId, VehicleId, otherUserPolicyId, context, It.IsAny<CancellationToken>()))
            .ReturnsAsync((InsurancePolicy?)null);
        coordinator.Setup(transaction => transaction.ExecuteAsync(It.IsAny<Func<ICarTransactionContext, Task<Unit>>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<ICarTransactionContext, Task<Unit>> operation, CancellationToken _) => operation(context));
        var service = CreateService(vehicles: vehicles, insurance: insurance, coordinator: coordinator);

        var exception = await Assert.ThrowsAsync<CarApplicationException>(() => service.DeleteInsurancePolicyAsync(
            new DeleteInsurancePolicyCommand { VehicleId = VehicleId, InsuranceId = otherUserPolicyId },
            CancellationToken.None));

        Assert.Equal("CAR_INSURANCE_NOT_FOUND", exception.Code);
    }

    [Fact]
    public async Task DeleteInsurance_WhenVehicleVersionCompareFails_ReturnsConcurrencyCode()
    {
        var vehicle = Vehicle.Create(UserId, "Garage", "BMW", "E46", 2002, "ABC-123", timeProvider: FixedTimeProvider(), id: VehicleId);
        var policyId = Guid.Parse("00000000-0000-0000-0000-00000000000c");
        var policy = InsurancePolicy.Create(UserId, VehicleId, "Provider", null, null, new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31), id: policyId, timeProvider: FixedTimeProvider());
        var context = new Mock<ICarTransactionContext>().Object;
        var vehicles = new Mock<IVehicleRepository>();
        var insurance = new Mock<IInsurancePolicyRepository>();
        var coordinator = new Mock<ICarTransactionCoordinator>();
        vehicles.Setup(repository => repository.GetByIdAsync(UserId, VehicleId, context, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vehicle);
        vehicles.Setup(repository => repository.ReplaceAsync(UserId, vehicle, 0, context, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        insurance.Setup(repository => repository.GetByIdAsync(UserId, VehicleId, policyId, context, It.IsAny<CancellationToken>()))
            .ReturnsAsync(policy);
        insurance.Setup(repository => repository.DeleteAsync(UserId, VehicleId, policyId, context, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        coordinator.Setup(transaction => transaction.ExecuteAsync(It.IsAny<Func<ICarTransactionContext, Task<Unit>>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<ICarTransactionContext, Task<Unit>> operation, CancellationToken _) => operation(context));
        var service = CreateService(vehicles: vehicles, insurance: insurance, coordinator: coordinator);

        var exception = await Assert.ThrowsAsync<CarApplicationException>(() => service.DeleteInsurancePolicyAsync(
            new DeleteInsurancePolicyCommand { VehicleId = VehicleId, InsuranceId = policyId },
            CancellationToken.None));

        Assert.Equal("CAR_CONCURRENCY_CONFLICT", exception.Code);
    }

    [Fact]
    public async Task DeleteInsurance_WhenVehicleIsArchived_ReturnsArchiveCode()
    {
        var vehicle = Vehicle.Create(UserId, "Garage", "BMW", "E46", 2002, "ABC-123", timeProvider: FixedTimeProvider(), id: VehicleId);
        vehicle.Archive(FixedTimeProvider());
        var policyId = Guid.Parse("00000000-0000-0000-0000-00000000000d");
        var context = new Mock<ICarTransactionContext>().Object;
        var vehicles = new Mock<IVehicleRepository>();
        var insurance = new Mock<IInsurancePolicyRepository>();
        var coordinator = new Mock<ICarTransactionCoordinator>();
        vehicles.Setup(repository => repository.GetByIdAsync(UserId, VehicleId, context, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vehicle);
        coordinator.Setup(transaction => transaction.ExecuteAsync(It.IsAny<Func<ICarTransactionContext, Task<Unit>>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<ICarTransactionContext, Task<Unit>> operation, CancellationToken _) => operation(context));
        var service = CreateService(vehicles: vehicles, insurance: insurance, coordinator: coordinator);

        var exception = await Assert.ThrowsAsync<CarApplicationException>(() => service.DeleteInsurancePolicyAsync(
            new DeleteInsurancePolicyCommand { VehicleId = VehicleId, InsuranceId = policyId },
            CancellationToken.None));

        Assert.Equal("CAR_VEHICLE_ARCHIVED", exception.Code);
        insurance.Verify(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<ICarTransactionContext>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetVehicles_WhenRepositoryReturnsEmptyPage_ReturnsEmptyItemsWithoutBatchLookup()
    {
        var vehicles = new Mock<IVehicleRepository>();
        var maintenance = new Mock<IMaintenanceItemRepository>();
        var insurance = new Mock<IInsurancePolicyRepository>();
        vehicles.Setup(repository => repository.GetPageForUserAsync(UserId, false, 0, 25, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Array.Empty<Vehicle>(), 0));
        var service = CreateService(vehicles: vehicles, maintenance: maintenance, insurance: insurance);

        var result = await service.GetVehiclesAsync(new GetVehiclesQuery(), CancellationToken.None);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalItemsCount);
        Assert.Equal(0, result.TotalPagesCount);
        maintenance.Verify(repository => repository.GetForVehiclesAsync(It.IsAny<Guid>(), It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<ICarTransactionContext>(), It.IsAny<CancellationToken>()), Times.Never);
        insurance.Verify(repository => repository.GetForVehiclesAsync(It.IsAny<Guid>(), It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<ICarTransactionContext>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private MyGarageApplicationService CreateService(
        Mock<IVehicleRepository>? vehicles = null,
        Mock<IMaintenanceItemRepository>? maintenance = null,
        Mock<IServiceHistoryRepository>? histories = null,
        Mock<ICarTransactionCoordinator>? coordinator = null,
        Mock<IInsurancePolicyRepository>? insurance = null)
    {
        var currentAccountAccessor = new Mock<ICurrentAccountAccessor>();
        currentAccountAccessor.Setup(accessor => accessor.GetAccountId()).Returns(UserId);
        return new MyGarageApplicationService(
            vehicles?.Object ?? new Mock<IVehicleRepository>().Object,
            maintenance?.Object ?? new Mock<IMaintenanceItemRepository>().Object,
            histories?.Object ?? new Mock<IServiceHistoryRepository>().Object,
            insurance?.Object ?? new Mock<IInsurancePolicyRepository>().Object,
            coordinator?.Object ?? new Mock<ICarTransactionCoordinator>().Object,
            currentAccountAccessor.Object,
            FixedTimeProvider());
    }

    private static TimeProvider FixedTimeProvider()
        => new TestTimeProvider(new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero));
}
