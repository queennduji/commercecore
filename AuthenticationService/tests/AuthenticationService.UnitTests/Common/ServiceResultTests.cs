using AuthenticationService.Application.Common;

namespace AuthenticationService.UnitTests.Common;

public class ServiceResultTests
{
    [Fact]
    public void Success_SetsSucceededTrueValueAndEmptyErrors()
    {
        var result = ServiceResult<string>.Success("value");

        Assert.True(result.Succeeded);
        Assert.Equal("value", result.Value);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Failure_SetsSucceededFalseAndErrors()
    {
        var result = ServiceResult<string>.Failure("error one", "error two");

        Assert.False(result.Succeeded);
        Assert.Null(result.Value);
        Assert.Equal(["error one", "error two"], result.Errors);
    }
}
