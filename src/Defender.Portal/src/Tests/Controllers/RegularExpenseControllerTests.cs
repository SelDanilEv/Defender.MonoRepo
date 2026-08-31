using System.Reflection;
using Defender.Common.Attributes;
using Defender.Common.Consts;
using Defender.Portal.WebUI.Controllers.V1;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Defender.Portal.Tests.Controllers;

public class RegularExpenseControllerTests
{
    [Fact]
    public void BudgetTrackerController_WhenRegularExpenseRoutesInspected_ExposesAuthenticatedContract()
    {
        var expected = new Dictionary<string, (Type Verb, string Template)>
        {
            [nameof(BudgetTrackerController.GetRegularExpenses)] = (typeof(HttpGetAttribute), "regular-expenses/expenses"),
            [nameof(BudgetTrackerController.CreateRegularExpense)] = (typeof(HttpPostAttribute), "regular-expenses/expense"),
            [nameof(BudgetTrackerController.UpdateRegularExpense)] = (typeof(HttpPutAttribute), "regular-expenses/expense"),
            [nameof(BudgetTrackerController.DeleteRegularExpense)] = (typeof(HttpDeleteAttribute), "regular-expenses/expense/{Id}"),
            [nameof(BudgetTrackerController.GetRegularExpenseReviews)] = (typeof(HttpGetAttribute), "regular-expenses/reviews"),
            [nameof(BudgetTrackerController.GetRegularExpenseReviewsByDateRange)] = (typeof(HttpGetAttribute), "regular-expenses/reviews/by-date-range"),
            [nameof(BudgetTrackerController.GetRegularExpenseReviewTemplate)] = (typeof(HttpGetAttribute), "regular-expenses/review/template"),
            [nameof(BudgetTrackerController.PublishRegularExpenseReview)] = (typeof(HttpPostAttribute), "regular-expenses/review"),
            [nameof(BudgetTrackerController.DeleteRegularExpenseReview)] = (typeof(HttpDeleteAttribute), "regular-expenses/review/{Id}"),
            [nameof(BudgetTrackerController.GetRegularExpenseDiagramSetup)] = (typeof(HttpGetAttribute), "regular-expenses/diagram-setup"),
            [nameof(BudgetTrackerController.UpdateRegularExpenseDiagramSetup)] = (typeof(HttpPostAttribute), "regular-expenses/diagram-setup"),
        };

        foreach (var (methodName, contract) in expected)
        {
            var method = typeof(BudgetTrackerController).GetMethod(methodName)!;
            var http = method.GetCustomAttributes<HttpMethodAttribute>().Single();
            var auth = method.GetCustomAttribute<AuthAttribute>();

            Assert.Equal(contract.Verb, http.GetType());
            Assert.Equal(contract.Template, http.Template);
            Assert.NotNull(auth);
            Assert.Equal(Roles.User, auth!.Roles);
        }
    }
}
