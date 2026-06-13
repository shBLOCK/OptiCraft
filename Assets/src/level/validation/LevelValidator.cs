using System.Text.Json.Serialization;

namespace level.validation;

[JsonDerivedType(typeof(MSEImageValidator))]
public abstract class LevelValidator {
}