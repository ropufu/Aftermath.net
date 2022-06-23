namespace Ropufu.Json;

public class VerboseJson : Verbose<JsonPointer>
{
    protected override JsonPointer Compose(JsonPointer a, JsonPointer b)
        => a + b;
}
