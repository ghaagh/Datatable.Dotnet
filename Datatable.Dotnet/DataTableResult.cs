
using System.Text.Json.Serialization;

namespace Datatable.Dotnet;

public class DataTableResult<T>
{
    [JsonPropertyName("data")]
    public IEnumerable<T> Data { get; set; }
    [JsonPropertyName("draw")]
    public int Draw { get; set; }
    [JsonPropertyName("recordsTotal")]
    public int RecordsTotal { get; set; }
    [JsonPropertyName("recordsFiltered")]
    public int RecordsFiltered { get; set; }
}
