#nullable enable

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace SerializerProtocol;

public class DatabaseUpdateRequest
{
    public List<string>? Ids { get; set; }
    public List<int>? TagsToAdd { get; set; }
    public List<int>? TagsToRemove { get; set; }
    public int? Rating { get; set; }

}

[JsonSerializable(typeof(DatabaseUpdateRequest))]
public partial class DatabaseUpdateRequestSerializerContext : JsonSerializerContext { }

