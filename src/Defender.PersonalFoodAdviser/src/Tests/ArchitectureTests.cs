using Defender.PersonalFoodAdviser.Application.Configuration.Options;
using Defender.PersonalFoodAdviser.Domain.Entities;
using Defender.PersonalFoodAdviser.Infrastructure.Repositories;

namespace Defender.PersonalFoodAdviser.Tests;

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
    public void LayerTypes_ShouldBePublicAndInPersonalFoodAdviserNamespace()
    {
        var layerTypes = new[] { typeof(ServiceOptions), typeof(MenuSession), typeof(MenuSessionRepository) };

        Assert.All(layerTypes, type =>
        {
            Assert.True(type.IsPublic, $"{type.FullName} should be public.");
            Assert.NotNull(type.Namespace);
            Assert.StartsWith("Defender.PersonalFoodAdviser.", type.Namespace!);
        });
    }
}

