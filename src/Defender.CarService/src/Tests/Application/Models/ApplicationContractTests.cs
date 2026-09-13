using Defender.CarService.Application.DTOs;
using Defender.CarService.Domain.Enums;

namespace Defender.CarService.Tests.Application.Models;

public sealed class ApplicationContractTests
{
    [Fact]
    public void ServiceHistoryPage_UsesDateOnlyAndNullableCostPair()
    {
        var item = new ServiceHistoryRecordDto
        {
            Date = new DateOnly(2026, 1, 1),
            CostAmountMinor = null,
            CostCurrency = null,
            Type = HistoryType.Tire,
        };
        var page = new ServiceHistoryPageDto
        {
            Items = [item],
            TotalItemsCount = 1,
            CurrentPage = 0,
            PageSize = 25,
            TotalPagesCount = 1,
        };

        Assert.Equal(new DateOnly(2026, 1, 1), page.Items[0].Date);
        Assert.Null(page.Items[0].CostAmountMinor);
        Assert.Null(page.Items[0].CostCurrency);
        Assert.Equal(HistoryType.Tire, page.Items[0].Type);
    }
}
