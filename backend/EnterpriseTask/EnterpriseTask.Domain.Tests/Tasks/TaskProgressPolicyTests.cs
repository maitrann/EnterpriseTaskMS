using EnterpriseTask.Domain.Tasks;
using Xunit;

namespace EnterpriseTask.Domain.Tests.Tasks;

public sealed class TaskProgressPolicyTests
{
    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, 0)]
    [InlineData(50, 50)]
    [InlineData(100, 100)]
    [InlineData(101, 100)]
    public void Normalize_ClampsProgressToValidRange(int input, int expected)
    {
        var normalized = TaskProgressPolicy.Normalize(input);

        Assert.Equal(expected, normalized);
    }
}
