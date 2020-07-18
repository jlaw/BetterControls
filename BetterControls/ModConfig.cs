using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StardewModdingAPI;

namespace BetterControls
{
    public class KeyMapConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            KeyMap keymap = (KeyMap)value;

            writer.WriteStartObject();
            foreach(var entry in keymap)
            {
                writer.WritePropertyName(entry.Key.ToString());
                writer.WriteValue(entry.Value.ToString());
            }
            writer.WriteEndObject();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            KeyMap keymap = new KeyMap();
            Type type = typeof(SButton);

            try
            {
                JObject jObject = JObject.Load(reader);
                foreach (KeyValuePair<string, JToken> entry in jObject)
                {
                    SButton from = (SButton)type.GetField(entry.Key).GetRawConstantValue();
                    SButton to = SButton.None;
                    if (entry.Value != null && entry.Value.Type != JTokenType.Null)
                    {
                        to = (SButton)type.GetField(entry.Value.ToString()).GetRawConstantValue();
                    }
                    keymap[from] = to;
                }
            }
            catch(JsonReaderException)
            {
                reader.Skip();
            }

            return keymap;
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(KeyMap);
        }
    }

    [JsonConverter(typeof(KeyMapConverter))]
    public class KeyMap : Dictionary<SButton, SButton> { }

    public class KeyMaps
    {
        public KeyMap OverWorld { get; set; } = new KeyMap();
        public KeyMap GameMenu { get; set; } = new KeyMap();
        public KeyMap ItemGrabMenu { get; set; } = new KeyMap();
        public KeyMap TitleMenu { get; set; } = new KeyMap();
    }

    class ModConfig
    {
        public KeyMaps KeyMaps { get; set; } = new KeyMaps();
    }
}
