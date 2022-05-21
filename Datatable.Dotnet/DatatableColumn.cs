using Datatable.Dotnet.Format;

namespace Datatable.Dotnet
{
    public class DatatableColumn
    {
        public DatatableColumn()
        {
            EnumDictionary = new Dictionary<int, string>();
        }
        public string Field { get; set; }
        public NumberFormat Format { get; set; }
        public bool Sort { get; set; } = true;
        public string? ClickFunctionName { get; set; }
        public ColumnTypeEnum Type { get; set; } = ColumnTypeEnum.String;
        public string? HeaderName { get; set; }
        public string? RenderFunction { get; set; }
        public bool IncludeInGlobalSearch { get; set; } = true;
        public bool HasOwnSearch { get; set; } = true;
        public bool Disabled { get; internal set; }
        public Dictionary<int, string> EnumDictionary { get; set; }
    }
}
