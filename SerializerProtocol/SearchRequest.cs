#nullable enable

using System;
using System.Collections.Generic;
using System.Text;

namespace SerializerProtocol;


public class SearchNodeSerializable
{
    public int Mode { get; set; }
    public object? Reference { get; set; }
    public int Property { get; set; }
    public bool IsOr { get; set; }
    public List<SearchNodeSerializable>? Children { get; set; }
}
public class SortSettingSerializable
{
    public int Property { get; set; }
    public bool IsDescending { get; set; }
}
public class SearchInformationSerializable
{
    public SearchNodeSerializable? Root { get; set; }
    public List<SortSettingSerializable>? SortSettings { get; set; }
    public string? Name { get; set; }
    public string? ThumbnailId { get; set; }

}
public class SearchRequest
{
    public SearchInformationSerializable? Info { get; set; }
    public int Take { get; set; }
    public int Skip { get; set; }

}
public class SearchResult
{
    public List<RecordSerializable>? Records { get; set; }
    public int Offset { get; set; }
    public int Total { get; set; }

}
