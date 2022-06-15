using System.Text.Json;

namespace Ropufu.Json;

public sealed class Schema
    : BasicSchema<Schema>
{
    protected override bool IsMatchOverride(ref JsonElement element)
        => true;

    protected override void OnDeserializedOverride()
    {
    }

    public static implicit operator Schema(bool trivialValue)
        => trivialValue ? Schema.TrivialTrue : Schema.TrivialFalse;
}
