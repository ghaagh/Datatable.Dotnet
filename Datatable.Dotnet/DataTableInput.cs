using System.Text.Json.Serialization;

namespace Datatable.Dotnet;


public class DataTableInput
{
    public DataTableInput()
    {
        ColumnSearches = new List<ColumnSearch>();
    }
    public int Draw { get; set; }
    public int Start { get; set; }
    public int Length { get; set; }
    public IEnumerable<ColumnSearch> ColumnSearches { get; set; }
    public string Search { get; set; }
    public string[] Columns { get; set; }
    public Order Order { get; set; }
}
public class ColumnSearch
{
    public string Field { get; set; }
    public string Keyword { get; set; }
}

public class Order
{
    public string Column { get; set; }
    public string Dir { get; set; }
}

