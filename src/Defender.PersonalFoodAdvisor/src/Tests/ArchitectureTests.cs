using Defender.PersonalFoodAdvisor.Application.Configuration.Options;
using Defender.PersonalFoodAdvisor.Domain.Entities;
using Defender.PersonalFoodAdvisor.Infrastructure.Repositories;

namespace Defender.PersonalFoodAdvisor.Tests;

public class ArchitectureTests
{
    [Fact]
    public void LayerAssemblies_ShouldMatchExpectedNames()
    {
        var applicationAssemblyName = typeof(ServiceOptions).Assembly.GetName().Name;
        var domainAssemblyName = typeof(MenuSession).Assembly.GetName().Name;
        var infrastructureAssemblyName = typeof(MenuSessionRepository).Assembly.GetName().Name;

        Assert.EndsWith(".Application", applicationAssemblyName);
        Assert.EndsWith(".Domain", domainAssemblyName);
        Assert.EndsWith(".Infrastructure", infrastructureAssemblyName);
    }

    [Fact]
    public void LayerTypes_ShouldBePublicAndInPersonalFoodAdvisorNamespace()
    {
        var layerTypes = new[] { typeof(ServiceOptions), typeof(MenuSession), typeof(MenuSessionRepository) };

        Assert.All(layerTypes, type =>
        {
            Assert.True(type.IsPublic, $"{type.FullName} should be public.");
            Assert.NotNull(type.Namespace);
            Assert.StartsWith("Defender.PersonalFoodAdvisor.", type.Namespace!);
        });
    }
}

