using Xunit;

namespace Ropufu.Tests;

public class TextExtenderTest
{
    [Fact]
    public void SnakeCaseChangeInCapitalization()
        => Assert.Equal("_first_name", " firstName".ToSnakeCase());

    [Fact]
    public void SnakeCaseDoubleUnderscore()
        => Assert.Equal("first__name_", "first  name ".ToSnakeCase());

    [Fact]
    public void SnakeCaseCollapseUnderscore()
        => Assert.Equal("first_name_", "first  name ".ToSnakeCase(StringSnakeOptions.CollapseUnderscores));

    [Fact]
    public void SnakeCaseTrim()
        => Assert.Equal("happiness_for_all", " ?% happiness for All !!! ".ToSnakeCase(StringSnakeOptions.Trim));
}
