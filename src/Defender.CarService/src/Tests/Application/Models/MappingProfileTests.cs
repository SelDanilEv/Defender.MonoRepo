using AutoMapper;
using Defender.CarService.Application.Mappings;
using Microsoft.Extensions.Logging.Abstractions;

namespace Defender.CarService.Tests.Application.Models;

public sealed class MappingProfileTests
{
    [Fact]
    public void MappingProfile_MapsComputedDtoFieldsWithoutInvalidConfiguration()
    {
        var configuration = new MapperConfiguration(
            configuration => configuration.AddProfile<MappingProfile>(),
            new NullLoggerFactory());

        configuration.AssertConfigurationIsValid();
    }
}
