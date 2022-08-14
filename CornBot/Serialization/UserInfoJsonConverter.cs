using CornBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace CornBot.Serialization
{
    internal class UserInfoJsonConverter : JsonConverter<UserInfo>
    {

        private readonly IServiceProvider _services;

        public UserInfoJsonConverter(IServiceProvider services)
        {
            _services = services;
        }

        public override UserInfo? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
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

            ulong userId = 0;
            long cornCount = 0;
            bool hasClaimedDaily = false;
            double cornMultiplier = 1.0;
            DateTime cornMultiplierLastEdit = DateTime.UtcNow;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    break;
                else if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    var propertyName = reader.GetString();
                    reader.Read();
                    switch (propertyName)
                    {
                        case "userId":
                            userId = reader.GetUInt64();
                            break;
                        case "cornCount":
                            cornCount = reader.GetInt64();
                            break;
                        case "hasClaimedDaily":
                            hasClaimedDaily = reader.GetBoolean();
                            break;
                        case "cornMultiplier":
                            cornMultiplier = reader.GetDouble();
                            break;
                        case "cornMultiplierLastEdit":
                            cornMultiplierLastEdit = reader.GetDateTime();
                            break;
                    }
                }
            }

            return new(userId, cornCount, hasClaimedDaily, cornMultiplier, cornMultiplierLastEdit, _services);
        }

        public override void Write(Utf8JsonWriter writer, UserInfo value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber("userId", value.UserId);
            writer.WriteNumber("cornCount", value.CornCount);
            writer.WriteBoolean("hasClaimedDaily", value.HasClaimedDaily);
            writer.WriteNumber("cornMultiplier", value.CornMultiplier);
            writer.WriteString("cornMultiplierLastEdit", DateTime.UtcNow.ToString("o"));
            writer.WriteEndObject();
        }

    }
}
