using InfoScopeDeveloperToolkit.Core.Abstractions;

namespace InfoScopeDeveloperToolkit.Tools.Sample.Tools;

public sealed class Base64EncodeDecodeTool : ITool
{
    public string Id => "base64-encode-decode";
    public string Name => "BASE64 decode-encode";
    public string Description => "Szöveg BASE64 enkódolása/dekódolása dedikált felületen.";
    public ToolParameterDefinition[] ParameterDefinitions => [];

    public Task RunAsync(ToolExecutionContext context, CancellationToken cancellationToken)
    {
        throw new NotSupportedException("Ez az eszköz dedikált UI felületen futtatható.");
    }
}
