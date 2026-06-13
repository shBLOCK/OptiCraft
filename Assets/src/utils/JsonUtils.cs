using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using UnityEngine;
using Object = UnityEngine.Object;

namespace utils;

public static class JsonUtils {
    public class UnityResourcesConverter<T> : JsonConverter<T> where T : Object {
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            Resources.Load<T>(reader.GetString());

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) =>
            throw new NotSupportedException();
    }
}