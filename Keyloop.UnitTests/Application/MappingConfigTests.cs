using Keyloop.Application.Mappings;
using Mapster;

namespace Keyloop.UnitTests.Application;

public sealed class MappingConfigTests
{
    [Fact]
    public void IMapFrom_RegistersConventionMapping()
    {
        var config = MappingConfig.Create(typeof(SampleDto).Assembly);

        var source = new SampleSource { Name = "Keyloop", Count = 3 };
        var dto = source.Adapt<SampleDto>(config);

        Assert.Equal("Keyloop", dto.Name);
        Assert.Equal(3, dto.Count);
    }

    public sealed class SampleSource
    {
        public string Name { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public sealed class SampleDto : IMapFrom<SampleSource>
    {
        public string Name { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}
