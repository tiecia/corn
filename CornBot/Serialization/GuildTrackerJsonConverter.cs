using CornBot.Models;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace CornBot.Serialization
{
    internal class GuildTrackerJsonConverter : JsonConverter<GuildTracker>
    {

        private readonly IServiceProvider _services;

        public GuildTrackerJsonConverter(IServiceProvider services)
        {
            _services = services;
        }

        public override GuildTracker? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            /*
             * This block of code is incredibly cursed. Like, it's something that I would write in Java. Very bad.
             * Is there a way to do this in C# that isn't so convoluted? Probably. Do I know what that solution is?
             * No. So for now, this stays.
             * 
             * I know there are ways to let the JSON Serializer/Deserializer automatically fill in the properties of
             * classes. The big problem here is that the "_services" variable also needs to be inserted into the
             * constructor, which to my knowledge is not possible otherwise. I have seen solutions online that look
             * like they might be close, but to be honest, my knowledge of the JSON framework is incredibly sparse
             * at the moment. Definitely something to look into, but this will do for now.
             */

            var client = _services.GetRequiredService<DiscordSocketClient>();

            Dictionary<ulong, GuildInfo> guilds = new();

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    return new(guilds, _services);
                }
                else if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    var propertyName = reader.GetString();
                    reader.Read();
                    switch (propertyName)
                    {
                        case "data":
                            guilds = DeserializeGuilds(ref reader, options);
                            break;
                    }
                }
            }

            return new(guilds, _services);
        }

        public override void Write(Utf8JsonWriter writer, GuildTracker value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("data");
            SerializeGuilds(writer, value.Guilds, options);
            writer.WriteEndObject();
        }

        private Dictionary<ulong, GuildInfo> DeserializeGuilds(ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            Dictionary<ulong, GuildInfo> guilds = new();
            var guildInfoJsonConverter = (GuildInfoJsonConverter)options.GetConverter(typeof(GuildInfo));

            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.EndArray:
                        return guilds;
                    case JsonTokenType.StartObject:
                        var guild = guildInfoJsonConverter.Read(ref reader, typeof(GuildInfo), options);
                        if (guild != null)
                            guilds.Add(guild.Guild.Id, guild);
                        break;
                }
            }

            return guilds;
        }

        private void SerializeGuilds(Utf8JsonWriter writer, Dictionary<ulong, GuildInfo> guilds, JsonSerializerOptions options)
        {
            var guildInfoJsonConverter = (GuildInfoJsonConverter)options.GetConverter(typeof(GuildInfo));

            writer.WriteStartArray();
            foreach (var guild in guilds.Values)
            {
                guildInfoJsonConverter.Write(writer, guild, options);
            }
            writer.WriteEndArray();
        }

    }
}
