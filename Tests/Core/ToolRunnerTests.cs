using InfoScopeDeveloperToolkit.Core.Abstractions;
using InfoScopeDeveloperToolkit.Core.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace InfoScopeDeveloperToolkit.Tests.Core;

public class ToolRunnerTests
{
    [Fact]
    public async Task RunAsync_LogolEsHaladastJelent()
    {
        var runner = new ToolRunner(new NullLogger<ToolRunner>());
        var fake = new FakeTool();
        var progressValues = new List<ToolProgressUpdate>();
        var logs = new List<ToolLogEvent>();

        await runner.RunAsync(fake, new Dictionary<string, string>(), new Progress<ToolProgressUpdate>(p => progressValues.Add(p)), logs.Add, CancellationToken.None);

        Assert.NotEmpty(progressValues);
        Assert.True(logs.Count >= 2);
    }

    private sealed class FakeTool : ITool
    {
        public string Id => "fake";
        public string Name => "Fake";
        public string Description => "Fake";
        public ToolParameterDefinition[] ParameterDefinitions => [];

        public Task RunAsync(ToolExecutionContext context, CancellationToken cancellationToken)
        {
            context.Progress.Report(new ToolProgressUpdate(100, "OK"));
            return Task.CompletedTask;
        }
    }
}
