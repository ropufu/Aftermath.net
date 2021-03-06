namespace Ropufu.Json;

public sealed class JsonLogger
    : Logger<JsonPointer>
{
    protected override JsonPointer Compose(JsonPointer a, JsonPointer b)
        => a + b;
}
