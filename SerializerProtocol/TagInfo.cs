#nullable enable

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace SerializerProtocol;


public class TagInfo
{
    public int Id { get; set; }
    public string? Name { get; set; }
}

public class TagList
{
    public List<TagInfo>? Tags { get; set; }
}

[JsonSerializable(typeof(TagInfo))]
public partial class TagInfoSerializerContext : JsonSerializerContext { }

[JsonSerializable(typeof(TagList))]
public partial class TagListSerializerContext : JsonSerializerContext { }
