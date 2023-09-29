#nullable enable

using System;
using System.Collections.Generic;
using System.Text;

namespace SerializerProtocol;


public class RecordSerializable
{
    public string? Id { get; set; }
    public long DateCreated { get; set; }
    public long DateModified { get; set; }
    public long DateRegistered { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public long Size { get; set; }
    public List<int>? Tags { get; set; }
    public int Rating { get; set; }
    public string? Directory { get; set; }
    public bool IsGroup { get; set; }
}
