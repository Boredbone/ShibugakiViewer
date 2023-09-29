#nullable enable

using System;
using System.Collections.Generic;
using System.Text;

namespace SerializerProtocol;

public class DatabaseUpdateRequest
{
    public List<string>? Ids { get; set; }
    public List<int>? TagsToAdd { get; set; }
    public List<int>? TagsToRemove { get; set; }
    public int? Rating { get; set; }

}
