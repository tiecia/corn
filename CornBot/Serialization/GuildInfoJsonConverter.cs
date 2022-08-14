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
    internal class GuildInfoJsonConverter : JsonConverter<GuildInfo>
    {

        private readonly IServiceProvider _services;

        public GuildInfoJsonConverter(IServiceProvider services)
        {
            _services = services;
        }

        public override GuildInfo? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
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

            SocketGuild? guild = null;
            Dictionary<ulong, UserInfo>? users = null;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    if (guild is not null && users is not null)
                        return new(guild, users, _services);
                    else
                        return null;
                }
                else if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    var propertyName = reader.GetString();
                    reader.Read();
                    switch (propertyName)
                    {
                        case "guildId":
                            guild = client.GetGuild(reader.GetUInt64());
                            break;
                        case "users":
                            users = DeserializeUsers(ref reader, options);
                            break;
                    }
                }
            }

            return null;
        }

        public override void Write(Utf8JsonWriter writer, GuildInfo value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber("guildId", value.Guild.Id);
            writer.WritePropertyName("users");
            SerializeUsers(writer, value.Users, options);
            writer.WriteEndObject();
        }

        private Dictionary<ulong, UserInfo> DeserializeUsers(ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            Dictionary<ulong, UserInfo> users = new();
            var userInfoJsonConverter = (UserInfoJsonConverter)options.GetConverter(typeof(UserInfo));

            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.EndArray:
                        return users;
                    case JsonTokenType.StartObject:
                        var user = userInfoJsonConverter.Read(ref reader, typeof(UserInfo), options)!;
                        users.Add(user.UserId, user);
                        break;
                }
            }

            return users;
        }

        private void SerializeUsers(Utf8JsonWriter writer, Dictionary<ulong, UserInfo> users, JsonSerializerOptions options)
        {
            var userInfoJsonConverter = (UserInfoJsonConverter)options.GetConverter(typeof(UserInfo));

            writer.WriteStartArray();
            foreach (var user in users.Values)
            {
                userInfoJsonConverter.Write(writer, user, options);
            }
            writer.WriteEndArray();
        }

    }
}
