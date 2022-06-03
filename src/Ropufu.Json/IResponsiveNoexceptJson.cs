using System.Text.Json;

namespace Ropufu.Json;

public interface IResponsiveNoexceptJson
{
    void OnRequiredPropertyMissing(string jsonPropertyName);

    void OnParsingFailure(string jsonPropertyName, ref Utf8JsonReader propertyJson);

    void OnDeserializing();

    void OnDeserialized();
}
