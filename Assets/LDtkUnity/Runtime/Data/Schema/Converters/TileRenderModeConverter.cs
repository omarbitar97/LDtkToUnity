﻿using System;
using Newtonsoft.Json;

namespace LDtkUnity.Data
{
    internal class TileRenderModeConverter : JsonConverter
    {
        public override bool CanConvert(Type t) => t == typeof(TileRenderMode) || t == typeof(TileRenderMode?);

        public override object ReadJson(JsonReader reader, Type t, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;
            var value = serializer.Deserialize<string>(reader);
            switch (value)
            {
                case "Crop":
                    return TileRenderMode.Crop;
                case "Stretch":
                    return TileRenderMode.Stretch;
            }
            throw new Exception("Cannot unmarshal type TileRenderMode");
        }

        public override void WriteJson(JsonWriter writer, object untypedValue, JsonSerializer serializer)
        {
            if (untypedValue == null)
            {
                serializer.Serialize(writer, null);
                return;
            }
            var value = (TileRenderMode)untypedValue;
            switch (value)
            {
                case TileRenderMode.Crop:
                    serializer.Serialize(writer, "Crop");
                    return;
                case TileRenderMode.Stretch:
                    serializer.Serialize(writer, "Stretch");
                    return;
            }
            throw new Exception("Cannot marshal type TileRenderMode");
        }

        public static readonly TileRenderModeConverter Singleton = new TileRenderModeConverter();
    }
}