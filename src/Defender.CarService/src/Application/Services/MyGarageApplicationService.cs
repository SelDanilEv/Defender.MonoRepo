using Defender.CarService.Application.Common;
using Defender.CarService.Application.Common.Exceptions;
using Defender.CarService.Application.Common.Interfaces.Repositories;
using Defender.CarService.Application.Common.Interfaces.Services;
using Defender.CarService.Application.DTOs;
using Defender.CarService.Application.Requests.History;
using Defender.CarService.Application.Requests.Insurance;
using Defender.CarService.Application.Requests.Maintenance;
using Defender.CarService.Application.Requests.Vehicles;
using Defender.CarService.Domain.Entities;
using Defender.CarService.Domain.Enums;
using Defender.CarService.Domain.Exceptions;
using Defender.CarService.Domain.Services;
using Defender.CarService.Domain.ValueObjects;
using Defender.Common.Errors;
using Defender.Common.Exceptions;
using Defender.Common.Interfaces;
using MediatR;

namespace Defender.CarService.Application.Services;

public sealed class MyGarageApplicationService : IMyGarageApplicationService
{
    private readonly IVehicleRepository vehicleRepository;
    private readonly IMaintenanceItemRepository maintenanceItemRepository;
    private readonly IServiceHistoryRepository serviceHistoryRepository;
    private readonly IInsurancePolicyRepository insurancePolicyRepository;
    private readonly ICarTransactionCoordinator transactionCoordinator;
    private readonly ICurrentAccountAccessor currentAccountAccessor;
    private readonly TimeProvider timeProvider;
    private readonly MaintenanceDueCalculator dueCalculator;

    public MyGarageApplicationService(
        IVehicleRepository vehicleRepository,
        IMaintenanceItemRepository maintenanceItemRepository,
        IServiceHistoryRepository serviceHistoryRepository,
        IInsurancePolicyRepository insurancePolicyRepository,
        ICarTransactionCoordinator transactionCoordinator,
        ICurrentAccountAccessor currentAccountAccessor,
        TimeProvider timeProvider)
    {
        this.vehicleRepository = vehicleRepository;
        this.maintenanceItemRepository = maintenanceItemRepository;
        this.serviceHistoryRepository = serviceHistoryRepository;
        this.insurancePolicyRepository = insurancePolicyRepository;
        this.transactionCoordinator = transactionCoordinator;
        this.currentAccountAccessor = currentAccountAccessor;
        this.timeProvider = timeProvider;
        dueCalculator = new MaintenanceDueCalculator(timeProvider);
    }

    public Task<IReadOnlyList<VehicleSummaryDto>> GetVehiclesAsync(bool includeArchived, CancellationToken cancellationToken)
    {
        var userId = currentAccountAccessor.GetAccountId();
        return TranslateAsync(() => GetVehiclesCoreAsync(userId, includeArchived, cancellationToken));
    }

    public Task<VehicleDetailDto> GetVehicleAsync(Guid vehicleId, CancellationToken cancellationToken)
    {
        var userId = currentAccountAccessor.GetAccountId();
        return TranslateAsync(() => GetVehicleCoreAsync(userId, vehicleId, cancellationToken));
    }

    public Task<VehicleDto> CreateVehicleAsync(CreateVehicleCommand request, CancellationToken cancellationToken)
    {
        var userId = currentAccountAccessor.GetAccountId();
        return TranslateAsync(async () =>
        {
            var vehicle = Vehicle.Create(
                userId,
                request.DisplayName,
                request.Make,
                request.Model,
                request.Year,
                request.Plate,
                request.Vin,
                timeProvider);
            await vehicleRepository.AddAsync(userId, vehicle, cancellationToken: cancellationToken);
            return MapVehicle(vehicle);
        });
    }

    public Task<VehicleDto> UpdateVehicleAsync(UpdateVehicleCommand request, CancellationToken cancellationToken)
    {
        var userId = currentAccountAccessor.GetAccountId();
        return TranslateAsync(async () =>
        {
            var vehicle = await GetVehicleOrThrowAsync(userId, request.VehicleId, null, cancellationToken);
            var expectedVersion = vehicle.Version;
            vehicle.Update(request.DisplayName, request.Make, request.Model, request.Year, request.Plate, request.Vin, timeProvider);
            if (!await vehicleRepository.ReplaceAsync(userId, vehicle, expectedVersion, cancellationToken: cancellationToken))
            {
                throw new CarApplicationException(CarDomainErrorCodes.ConcurrencyConflict);
            }

            return MapVehicle(vehicle);
        });
    }

    public Task<VehicleDto> ArchiveVehicleAsync(Guid vehicleId, CancellationToken cancellationToken)
        => ChangeArchiveStateAsync(vehicleId, archive: true, cancellationToken);

    public Task<VehicleDto> UnarchiveVehicleAsync(Guid vehicleId, CancellationToken cancellationToken)
        => ChangeArchiveStateAsync(vehicleId, archive: false, cancellationToken);

    public Task<IReadOnlyList<MaintenanceItemDto>> GetMaintenanceItemsAsync(Guid vehicleId, CancellationToken cancellationToken)
    {
        var userId = currentAccountAccessor.GetAccountId();
        return TranslateAsync<IReadOnlyList<MaintenanceItemDto>>(async () =>
        {
            var vehicle = await GetVehicleOrThrowAsync(userId, vehicleId, null, cancellationToken);
            var items = await maintenanceItemRepository.GetForVehicleAsync(userId, vehicleId, cancellationToken: cancellationToken);
            var linkedIds = await GetLinkedMaintenanceIdsAsync(userId, vehicleId, cancellationToken);
            return items.Select(item => MapMaintenance(item, vehicle, linkedIds.Contains(item.Id))).ToArray();
        });
    }

    public Task<MaintenanceItemDto> CreateMaintenanceItemAsync(CreateMaintenanceItemCommand request, CancellationToken cancellationToken)
    {
        var userId = currentAccountAccessor.GetAccountId();
        return TranslateAsync(() => transactionCoordinator.ExecuteAsync(
            async context =>
            {
                var vehicle = await GetVehicleOrThrowAsync(userId, request.VehicleId, context, cancellationToken);
                vehicle.EnsureActive();
                var expectedVehicleVersion = vehicle.Version;
                var item = MaintenanceItem.Create(
                    vehicle,
                    request.Name,
                    request.IntervalMonths,
                    request.IntervalThousandKm,
                    request.ManualBaselineDate ?? request.LastDate,
                    request.ManualBaselineOdometerKm ?? request.LastOdometerKm,
                    timeProvider);
                await maintenanceItemRepository.AddAsync(userId, request.VehicleId, item, context, cancellationToken);
                vehicle.Touch(timeProvider);
                if (!await vehicleRepository.ReplaceAsync(userId, vehicle, expectedVehicleVersion, context, cancellationToken))
                {
                    throw new CarApplicationException(CarDomainErrorCodes.ConcurrencyConflict);
                }

                return MapMaintenance(item, vehicle);
            },
            cancellationToken));
    }

    public Task<MaintenanceItemDto> UpdateMaintenanceItemAsync(UpdateMaintenanceItemCommand request, CancellationToken cancellationToken)
    {
        var userId = currentAccountAccessor.GetAccountId();
        return TranslateAsync(() => transactionCoordinator.ExecuteAsync(
            context => UpdateMaintenanceItemInTransactionAsync(userId, request, context, cancellationToken),
            cancellationToken));
    }

    public Task<Unit> DeleteMaintenanceItemAsync(DeleteMaintenanceItemCommand request, CancellationToken cancellationToken)
    {
        var userId = currentAccountAccessor.GetAccountId();
        return TranslateAsync(() => transactionCoordinator.ExecuteAsync(
            async context =>
            {
                var vehicle = await GetVehicleOrThrowAsync(userId, request.VehicleId, context, cancellationToken);
                vehicle.EnsureActive();
                var item = await maintenanceItemRepository.GetByIdAsync(userId, request.VehicleId, request.MaintenanceItemId, context, cancellationToken);
                if (item is null)
                {
                    throw new CarApplicationException(CarDomainErrorCodes.MaintenanceNotFound);
                }

                var expectedVehicleVersion = vehicle.Version;
                if (!await maintenanceItemRepository.DeleteAsync(userId, request.VehicleId, item.Id, context, cancellationToken))
                {
                    throw new CarApplicationException(CarDomainErrorCodes.MaintenanceNotFound);
                }

                vehicle.Touch(timeProvider);
                if (!await vehicleRepository.ReplaceAsync(userId, vehicle, expectedVehicleVersion, context, cancellationToken))
                {
                    throw new CarApplicationException(CarDomainErrorCodes.ConcurrencyConflict);
                }

                return Unit.Value;
            },
            cancellationToken));
    }

    public Task<ServiceHistoryPageDto> GetHistoryAsync(GetHistoryQuery request, CancellationToken cancellationToken)
    {
        if (request.Page < 0 || request.PageSize is < 1 or > 100)
        {
            throw new CarApplicationException(CarApplicationErrorCodes.HistoryPaginationInvalid);
        }

        var userId = currentAccountAccessor.GetAccountId();
        return TranslateAsync(async () =>
        {
            await GetVehicleOrThrowAsync(userId, request.VehicleId, null, cancellationToken);
            var page = await serviceHistoryRepository.GetPageForVehicleAsync(
                userId,
                request.VehicleId,
                request.Page,
                request.PageSize,
                cancellationToken: cancellationToken);
            var totalItems = page.TotalItemsCount;
            var items = page.Items.Select(MapHistory).ToArray();
            return new ServiceHistoryPageDto
            {
                Items = items,
                TotalItemsCount = totalItems,
                CurrentPage = request.Page,
                PageSize = request.PageSize,
                TotalPagesCount = totalItems == 0 ? 0 : (int)Math.Ceiling((double)totalItems / request.PageSize),
            };
        });
    }

    public Task<ServiceHistoryRecordDto> CreateHistoryAsync(CreateHistoryCommand request, CancellationToken cancellationToken)
    {
        var userId = currentAccountAccessor.GetAccountId();
        return TranslateAsync(() => transactionCoordinator.ExecuteAsync(
            context => CreateHistoryInTransactionAsync(userId, request, context, cancellationToken),
            cancellationToken));
    }

    public Task<ServiceHistoryRecordDto> UpdateHistoryAsync(UpdateHistoryCommand request, CancellationToken cancellationToken)
    {
        var userId = currentAccountAccessor.GetAccountId();
        return TranslateAsync(() => transactionCoordinator.ExecuteAsync(
            context => UpdateHistoryInTransactionAsync(userId, request, context, cancellationToken),
            cancellationToken));
    }

    public Task<Unit> DeleteHistoryAsync(DeleteHistoryCommand request, CancellationToken cancellationToken)
    {
        var userId = currentAccountAccessor.GetAccountId();
        return TranslateAsync(() => transactionCoordinator.ExecuteAsync(
            context => DeleteHistoryInTransactionAsync(userId, request, context, cancellationToken),
            cancellationToken));
    }

    public Task<IReadOnlyList<InsurancePolicyDto>> GetInsurancePoliciesAsync(Guid vehicleId, CancellationToken cancellationToken)
    {
        var userId = currentAccountAccessor.GetAccountId();
        return TranslateAsync<IReadOnlyList<InsurancePolicyDto>>(async () =>
        {
            await GetVehicleOrThrowAsync(userId, vehicleId, null, cancellationToken);
            var policies = await insurancePolicyRepository.GetForVehicleAsync(userId, vehicleId, cancellationToken: cancellationToken);
            return policies.Select(MapInsurance).ToArray();
        });
    }

    public Task<InsurancePolicyDto> CreateInsurancePolicyAsync(CreateInsurancePolicyCommand request, CancellationToken cancellationToken)
    {
        var userId = currentAccountAccessor.GetAccountId();
        return TranslateAsync(() => transactionCoordinator.ExecuteAsync(
            async context =>
            {
                var vehicle = await GetVehicleOrThrowAsync(userId, request.VehicleId, context, cancellationToken);
                var expectedVehicleVersion = vehicle.Version;
                var policy = InsurancePolicy.Create(vehicle, request.Provider, request.PolicyNumber, request.CoverageType, request.StartDate, request.EndDate, request.Notes, timeProvider);
                await insurancePolicyRepository.AddAsync(userId, request.VehicleId, policy, context, cancellationToken);
                vehicle.Touch(timeProvider);
                if (!await vehicleRepository.ReplaceAsync(userId, vehicle, expectedVehicleVersion, context, cancellationToken))
                {
                    throw new CarApplicationException(CarDomainErrorCodes.ConcurrencyConflict);
                }

                return MapInsurance(policy);
            },
            cancellationToken));
    }

    public Task<InsurancePolicyDto> UpdateInsurancePolicyAsync(UpdateInsurancePolicyCommand request, CancellationToken cancellationToken)
    {
        var userId = currentAccountAccessor.GetAccountId();
        return TranslateAsync(() => transactionCoordinator.ExecuteAsync(
            context => UpdateInsurancePolicyInTransactionAsync(userId, request, context, cancellationToken),
            cancellationToken));
    }

    private async Task<InsurancePolicyDto> UpdateInsurancePolicyInTransactionAsync(
        Guid userId,
        UpdateInsurancePolicyCommand request,
        ICarTransactionContext context,
        CancellationToken cancellationToken)
    {
        var vehicle = await GetVehicleOrThrowAsync(userId, request.VehicleId, context, cancellationToken);
        var policy = await insurancePolicyRepository.GetByIdAsync(userId, request.VehicleId, request.InsurancePolicyId, context, cancellationToken);
        if (policy is null)
        {
            throw new CarApplicationException(CarDomainErrorCodes.InsuranceNotFound);
        }

        var expectedVehicleVersion = vehicle.Version;
        policy.Update(vehicle, request.Provider, request.PolicyNumber, request.CoverageType, request.StartDate, request.EndDate, request.Notes, timeProvider);
        if (!await insurancePolicyRepository.ReplaceAsync(userId, request.VehicleId, policy, context, cancellationToken))
        {
            throw new CarApplicationException(CarDomainErrorCodes.InsuranceNotFound);
        }

        vehicle.Touch(timeProvider);
        if (!await vehicleRepository.ReplaceAsync(userId, vehicle, expectedVehicleVersion, context, cancellationToken))
        {
            throw new CarApplicationException(CarDomainErrorCodes.ConcurrencyConflict);
        }

        return MapInsurance(policy);
    }

    private async Task<IReadOnlyList<VehicleSummaryDto>> GetVehiclesCoreAsync(Guid userId, bool includeArchived, CancellationToken cancellationToken)
    {
        var vehicles = await vehicleRepository.GetForUserAsync(userId, includeArchived, cancellationToken: cancellationToken);
        var summaries = new List<VehicleSummaryDto>(vehicles.Count);
        foreach (var vehicle in vehicles)
        {
            var items = await maintenanceItemRepository.GetForVehicleAsync(userId, vehicle.Id, cancellationToken: cancellationToken);
            var policies = await insurancePolicyRepository.GetForVehicleAsync(userId, vehicle.Id, cancellationToken: cancellationToken);
            summaries.Add(MapVehicleSummary(vehicle, items, policies));
        }

        return summaries;
    }

    private async Task<VehicleDetailDto> GetVehicleCoreAsync(Guid userId, Guid vehicleId, CancellationToken cancellationToken)
    {
        var vehicle = await GetVehicleOrThrowAsync(userId, vehicleId, null, cancellationToken);
        var items = await maintenanceItemRepository.GetForVehicleAsync(userId, vehicleId, cancellationToken: cancellationToken);
        var policies = await insurancePolicyRepository.GetForVehicleAsync(userId, vehicleId, cancellationToken: cancellationToken);
        var linkedIds = await GetLinkedMaintenanceIdsAsync(userId, vehicleId, cancellationToken);
        return new VehicleDetailDto
        {
            Vehicle = MapVehicle(vehicle),
            MaintenanceItems = items.Select(item => MapMaintenance(item, vehicle, linkedIds.Contains(item.Id))).ToArray(),
            InsurancePolicies = policies.Select(MapInsurance).ToArray(),
        };
    }

    private Task<VehicleDto> ChangeArchiveStateAsync(Guid vehicleId, bool archive, CancellationToken cancellationToken)
    {
        var userId = currentAccountAccessor.GetAccountId();
        return TranslateAsync(async () =>
        {
            var vehicle = await GetVehicleOrThrowAsync(userId, vehicleId, null, cancellationToken);
            var expectedVersion = vehicle.Version;
            if (archive)
            {
                vehicle.Archive(timeProvider);
            }
            else
            {
                vehicle.Unarchive(timeProvider);
            }

            if (!await vehicleRepository.ReplaceAsync(userId, vehicle, expectedVersion, cancellationToken: cancellationToken))
            {
                throw new CarApplicationException(CarDomainErrorCodes.ConcurrencyConflict);
            }

            return MapVehicle(vehicle);
        });
    }

    private async Task<MaintenanceItemDto> UpdateMaintenanceItemInTransactionAsync(
        Guid userId,
        UpdateMaintenanceItemCommand request,
        ICarTransactionContext context,
        CancellationToken cancellationToken)
    {
        var vehicle = await GetVehicleOrThrowAsync(userId, request.VehicleId, context, cancellationToken);
        vehicle.EnsureActive();
        var item = await maintenanceItemRepository.GetByIdAsync(userId, request.VehicleId, request.MaintenanceItemId, context, cancellationToken);
        if (item is null)
        {
            throw new CarApplicationException(CarDomainErrorCodes.MaintenanceNotFound);
        }

        var linkedHistory = await serviceHistoryRepository.GetLinkedToMaintenanceAsync(userId, request.VehicleId, item.Id, context, cancellationToken);
        var expectedVehicleVersion = vehicle.Version;
        item.SetManualBaseline(
            vehicle,
            request.ManualBaselineDate ?? request.LastDate,
            request.ManualBaselineOdometerKm ?? request.LastOdometerKm,
            linkedHistory.Count > 0,
            timeProvider);
        item.Update(vehicle, request.Name, request.IntervalMonths, request.IntervalThousandKm, timeProvider);
        if (!await maintenanceItemRepository.ReplaceAsync(userId, request.VehicleId, item, context, cancellationToken))
        {
            throw new CarApplicationException(CarDomainErrorCodes.MaintenanceNotFound);
        }

        vehicle.Touch(timeProvider);
        if (!await vehicleRepository.ReplaceAsync(userId, vehicle, expectedVehicleVersion, context, cancellationToken))
        {
            throw new CarApplicationException(CarDomainErrorCodes.ConcurrencyConflict);
        }

        return MapMaintenance(item, vehicle, linkedHistory.Count > 0);
    }

    private async Task<ServiceHistoryRecordDto> CreateHistoryInTransactionAsync(
        Guid userId,
        CreateHistoryCommand request,
        ICarTransactionContext context,
        CancellationToken cancellationToken)
    {
        var vehicle = await GetVehicleOrThrowAsync(userId, request.VehicleId, context, cancellationToken);
        vehicle.EnsureActive();
        var links = ValidateLinks(request.LinkedMaintenanceItemIds);
        await EnsureMaintenanceLinksAsync(userId, request.VehicleId, links, context, cancellationToken);
        var expectedVehicleVersion = vehicle.Version;
        var history = ServiceHistoryRecord.Create(
            vehicle,
            request.Date,
            request.OdometerKm,
            request.Type,
            request.Title,
            request.Notes,
            links,
            request.CostAmountMinor,
            request.CostCurrency,
            timeProvider);
        var added = await serviceHistoryRepository.AddAsync(userId, request.VehicleId, history, context, cancellationToken);
        var allHistory = await GetHistoryIncludingAsync(userId, request.VehicleId, added, context, cancellationToken);
        VehicleOdometerCalculator.ValidateChronologicalSequence(allHistory);
        vehicle.SetCurrentOdometer(VehicleOdometerCalculator.CalculateCurrentOdometer(allHistory), timeProvider);
        await RecalculateBaselinesAsync(userId, request.VehicleId, links, allHistory, vehicle, context, cancellationToken);
        if (!await vehicleRepository.ReplaceAsync(userId, vehicle, expectedVehicleVersion, context, cancellationToken))
        {
            throw new CarApplicationException(CarDomainErrorCodes.ConcurrencyConflict);
        }

        return MapHistory(added);
    }

    private async Task<ServiceHistoryRecordDto> UpdateHistoryInTransactionAsync(
        Guid userId,
        UpdateHistoryCommand request,
        ICarTransactionContext context,
        CancellationToken cancellationToken)
    {
        var vehicle = await GetVehicleOrThrowAsync(userId, request.VehicleId, context, cancellationToken);
        vehicle.EnsureActive();
        var history = await serviceHistoryRepository.GetByIdAsync(userId, request.VehicleId, request.HistoryId, context, cancellationToken);
        if (history is null)
        {
            throw new CarApplicationException(CarDomainErrorCodes.HistoryNotFound);
        }

        var oldLinks = history.LinkedMaintenanceItemIds.ToArray();
        var newLinks = ValidateLinks(request.LinkedMaintenanceItemIds);
        await EnsureMaintenanceLinksAsync(userId, request.VehicleId, newLinks, context, cancellationToken);
        var expectedVehicleVersion = vehicle.Version;
        history.Update(vehicle, request.Date, request.OdometerKm, request.Type, request.Title, request.Notes, newLinks, request.CostAmountMinor, request.CostCurrency, timeProvider);
        if (!await serviceHistoryRepository.ReplaceAsync(userId, request.VehicleId, history, context, cancellationToken))
        {
            throw new CarApplicationException(CarDomainErrorCodes.HistoryNotFound);
        }

        var allHistory = await GetHistoryIncludingAsync(userId, request.VehicleId, history, context, cancellationToken);
        VehicleOdometerCalculator.ValidateChronologicalSequence(allHistory);
        vehicle.SetCurrentOdometer(VehicleOdometerCalculator.CalculateCurrentOdometer(allHistory), timeProvider);
        await RecalculateBaselinesAsync(userId, request.VehicleId, oldLinks.Union(newLinks), allHistory, vehicle, context, cancellationToken);
        if (!await vehicleRepository.ReplaceAsync(userId, vehicle, expectedVehicleVersion, context, cancellationToken))
        {
            throw new CarApplicationException(CarDomainErrorCodes.ConcurrencyConflict);
        }

        return MapHistory(history);
    }

    private async Task<Unit> DeleteHistoryInTransactionAsync(
        Guid userId,
        DeleteHistoryCommand request,
        ICarTransactionContext context,
        CancellationToken cancellationToken)
    {
        var vehicle = await GetVehicleOrThrowAsync(userId, request.VehicleId, context, cancellationToken);
        vehicle.EnsureActive();
        var history = await serviceHistoryRepository.GetByIdAsync(userId, request.VehicleId, request.HistoryId, context, cancellationToken);
        if (history is null)
        {
            throw new CarApplicationException(CarDomainErrorCodes.HistoryNotFound);
        }

        var expectedVehicleVersion = vehicle.Version;
        if (!await serviceHistoryRepository.DeleteAsync(userId, request.VehicleId, history.Id, context, cancellationToken))
        {
            throw new CarApplicationException(CarDomainErrorCodes.HistoryNotFound);
        }

        var allHistory = (await serviceHistoryRepository.GetForVehicleAsync(userId, request.VehicleId, context, cancellationToken)).ToArray();
        VehicleOdometerCalculator.ValidateChronologicalSequence(allHistory);
        vehicle.SetCurrentOdometer(VehicleOdometerCalculator.CalculateCurrentOdometer(allHistory), timeProvider);
        await RecalculateBaselinesAsync(userId, request.VehicleId, history.LinkedMaintenanceItemIds, allHistory, vehicle, context, cancellationToken);
        if (!await vehicleRepository.ReplaceAsync(userId, vehicle, expectedVehicleVersion, context, cancellationToken))
        {
            throw new CarApplicationException(CarDomainErrorCodes.ConcurrencyConflict);
        }

        return Unit.Value;
    }

    private async Task<IReadOnlyList<ServiceHistoryRecord>> GetHistoryIncludingAsync(
        Guid userId,
        Guid vehicleId,
        ServiceHistoryRecord current,
        ICarTransactionContext context,
        CancellationToken cancellationToken)
    {
        var records = (await serviceHistoryRepository.GetForVehicleAsync(userId, vehicleId, context, cancellationToken)).ToList();
        if (records.All(record => record.Id != current.Id))
        {
            records.Add(current);
        }

        return records;
    }

    private async Task EnsureMaintenanceLinksAsync(
        Guid userId,
        Guid vehicleId,
        IReadOnlyList<Guid> links,
        ICarTransactionContext context,
        CancellationToken cancellationToken)
    {
        foreach (var maintenanceId in links)
        {
            if (await maintenanceItemRepository.GetByIdAsync(userId, vehicleId, maintenanceId, context, cancellationToken) is null)
            {
                throw new CarApplicationException(CarDomainErrorCodes.HistoryLinkInvalid);
            }
        }
    }

    private async Task RecalculateBaselinesAsync(
        Guid userId,
        Guid vehicleId,
        IEnumerable<Guid> maintenanceIds,
        IReadOnlyList<ServiceHistoryRecord> allHistory,
        Vehicle vehicle,
        ICarTransactionContext context,
        CancellationToken cancellationToken)
    {
        foreach (var maintenanceId in maintenanceIds.Distinct())
        {
            var item = await maintenanceItemRepository.GetByIdAsync(userId, vehicleId, maintenanceId, context, cancellationToken);
            if (item is null)
            {
                throw new CarApplicationException(CarDomainErrorCodes.MaintenanceNotFound);
            }

            var linkedRecords = allHistory
                .Where(record => record.LinkedMaintenanceItemIds.Contains(maintenanceId))
                .ToArray();
            var baseline = item.CalculateEffectiveBaseline(linkedRecords);
            item.RecalculateEffectiveBaseline(vehicle, linkedRecords, timeProvider);
            if (!await maintenanceItemRepository.ReplaceEffectiveBaselineAsync(userId, vehicleId, maintenanceId, baseline, context, cancellationToken))
            {
                throw new CarApplicationException(CarDomainErrorCodes.MaintenanceNotFound);
            }
        }
    }

    private async Task<Vehicle> GetVehicleOrThrowAsync(
        Guid userId,
        Guid vehicleId,
        ICarTransactionContext? context,
        CancellationToken cancellationToken)
    {
        var vehicle = await vehicleRepository.GetByIdAsync(userId, vehicleId, context, cancellationToken);
        return vehicle ?? throw new CarApplicationException(CarDomainErrorCodes.VehicleNotFound);
    }

    private static IReadOnlyList<Guid> ValidateLinks(IReadOnlyList<Guid>? links)
    {
        if (links is null || links.Any(link => link == Guid.Empty) || links.Distinct().Count() != links.Count)
        {
            throw new CarApplicationException(CarDomainErrorCodes.HistoryLinkInvalid);
        }

        return links.ToArray();
    }

    private async Task<HashSet<Guid>> GetLinkedMaintenanceIdsAsync(Guid userId, Guid vehicleId, CancellationToken cancellationToken)
    {
        var history = await serviceHistoryRepository.GetForVehicleAsync(userId, vehicleId, cancellationToken: cancellationToken) ?? [];
        return history
            .SelectMany(record => record.LinkedMaintenanceItemIds)
            .ToHashSet();
    }

    private MaintenanceItemDto MapMaintenance(MaintenanceItem item, Vehicle vehicle, bool hasLinkedHistory = false)
        => new()
        {
            Id = item.Id,
            VehicleId = item.VehicleId,
            Name = item.Name,
            IntervalMonths = item.IntervalMonths,
            IntervalThousandKm = item.IntervalThousandKm,
            LastDate = item.LastDate,
            LastOdometerKm = item.LastOdometerKm,
            ManualBaselineDate = item.ManualBaselineDate,
            ManualBaselineOdometerKm = item.ManualBaselineOdometerKm,
            NextDate = dueCalculator.CalculateNextDate(item),
            NextOdometerKm = dueCalculator.CalculateNextOdometerKm(item),
            Status = dueCalculator.CalculateStatusForVehicle(item, vehicle),
            HasLinkedHistory = hasLinkedHistory,
        };

    private InsurancePolicyDto MapInsurance(InsurancePolicy policy)
        => new()
        {
            Id = policy.Id,
            VehicleId = policy.VehicleId,
            Provider = policy.Provider,
            PolicyNumber = policy.PolicyNumber,
            CoverageType = policy.CoverageType,
            StartDate = policy.StartDate,
            EndDate = policy.EndDate,
            Notes = policy.Notes,
            Status = policy.GetStatus(timeProvider),
        };

    private static ServiceHistoryRecordDto MapHistory(ServiceHistoryRecord history)
        => new()
        {
            Id = history.Id,
            VehicleId = history.VehicleId,
            Date = history.Date,
            OdometerKm = history.OdometerKm,
            Type = history.Type,
            Title = history.Title,
            Notes = history.Notes,
            LinkedMaintenanceItemIds = history.LinkedMaintenanceItemIds.ToArray(),
            CostAmountMinor = history.CostAmountMinor,
            CostCurrency = history.CostCurrency,
        };

    private static VehicleDto MapVehicle(Vehicle vehicle)
        => new()
        {
            Id = vehicle.Id,
            DisplayName = vehicle.DisplayName,
            Make = vehicle.Make,
            Model = vehicle.Model,
            Year = vehicle.Year,
            Plate = vehicle.Plate,
            Vin = vehicle.Vin,
            Archived = vehicle.Archived,
            CurrentOdometerKm = vehicle.CurrentOdometerKm,
            Version = vehicle.Version,
            CreatedAtUtc = vehicle.CreatedAtUtc,
            UpdatedAtUtc = vehicle.UpdatedAtUtc,
        };

    private VehicleSummaryDto MapVehicleSummary(
        Vehicle vehicle,
        IReadOnlyList<MaintenanceItem> items,
        IReadOnlyList<InsurancePolicy> policies)
    {
        var statuses = items
            .Select(item => dueCalculator.CalculateStatusForVehicle(item, vehicle))
            .ToArray();
        return new VehicleSummaryDto
        {
            Id = vehicle.Id,
            DisplayName = vehicle.DisplayName,
            Make = vehicle.Make,
            Model = vehicle.Model,
            Year = vehicle.Year,
            Plate = vehicle.Plate,
            Vin = vehicle.Vin,
            Archived = vehicle.Archived,
            CurrentOdometerKm = vehicle.CurrentOdometerKm,
            MaintenanceCounts = new MaintenanceStatusCountsDto
            {
                Overdue = statuses.Count(status => status == MaintenanceStatus.Overdue),
                DueSoon = statuses.Count(status => status == MaintenanceStatus.DueSoon),
                Upcoming = statuses.Count(status => status == MaintenanceStatus.Upcoming),
                NotStarted = statuses.Count(status => status == MaintenanceStatus.NotStarted),
            },
            InsuranceStatus = MaintenanceDueCalculator.CalculateInsuranceStatus(policies, DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime)),
        };
    }

    private static async Task<T> TranslateAsync<T>(Func<Task<T>> operation)
    {
        try
        {
            return await operation();
        }
        catch (CarApplicationException)
        {
            throw;
        }
        catch (CarDomainException exception)
        {
            throw new CarApplicationException(exception.Code, exception);
        }
        catch (ServiceException exception) when (exception.IsErrorCode(ErrorCode.CM_DatabaseIssue))
        {
            throw new CarApplicationException(CarDomainErrorCodes.DatabaseUnavailable, exception);
        }
    }
}
