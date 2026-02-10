using InfoScopeDeveloperToolkit.Core.Services;

namespace InfoScopeDeveloperToolkit.Tests.Core;

public class ErrorThreadSummaryGeneratorTests
{
    [Fact]
    public void BuildSummary_KiemeliHibasThreadeket()
    {
        var input = """
2026-02-08 10:43:55 |19236| Information Start
2026-02-08 10:43:56 |19236| Error Login failed (user not found)
2026-02-08 10:43:56 |19236| Error No user found: erdei.imre@infoscope.hu
2026-02-08 10:43:57 |333| Information Ok
""";

        var output = ErrorThreadSummaryGenerator.BuildSummary(input);

        Assert.Contains("Error count: 2", output);
        Assert.Contains("ThreadId: 19236", output);
        Assert.Contains("Login failed", output);
        Assert.DoesNotContain("ThreadId: 333", output);
    }

    [Fact]
    public void BuildSummary_UnknownThreadetIsFeldolgoz()
    {
        var input = "raw line without thread and with Exception happened";

        var output = ErrorThreadSummaryGenerator.BuildSummary(input);

        Assert.Contains("ThreadId: unknown", output);
        Assert.Contains("Exception", output);
    }
}
