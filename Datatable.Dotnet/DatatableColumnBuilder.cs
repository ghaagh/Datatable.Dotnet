
namespace Datatable.Dotnet
{

    public class DatatableColumnBuilder
    {
        public DatatableColumnBuilder()
        {
        }
        private List<DatatableColumn> Columns { get; set; } = new List<DatatableColumn>();
        public IEnumerable<DatatableColumn> Build() => Columns;
        private static DatatableColumn GeneralColumn(string headerName, string field, bool includeInGlobalSearch = true, bool hasOwnSearch = true, bool sort = true)
        {
            return new DatatableColumn()
            {
                HeaderName = headerName,
                Field = field,
                Sort = sort,
                IncludeInGlobalSearch = includeInGlobalSearch,
                HasOwnSearch = hasOwnSearch
            };
        }
        public DatatableColumnBuilder AddTextColumn(string headerName, string field, bool includeInGlobalSearch = true, bool hasOwnSearch = true, bool sort = true)
        {
            var column = GeneralColumn(headerName, field, includeInGlobalSearch, hasOwnSearch, sort);
            column.Type = ColumnTypeEnum.String;
            Columns.Add(column);
            return this;
        }
        public DatatableColumnBuilder AddNumberColumn(string headerName, string field, NumberFormat format, bool includeInGlobalSearch = true, bool hasOwnSearch = true, bool sort = true)
        {
            var column = GeneralColumn(headerName, field, includeInGlobalSearch, hasOwnSearch, sort);
            column.Type = ColumnTypeEnum.Number;
            column.Format = format;
            Columns.Add(column);
            return this;
        }
        public DatatableColumnBuilder AddEnumColumn(string headerName, string field, Dictionary<int, string> dictionary, bool includeInGlobalSearch = true, bool hasOwnSearch = true, bool sort = true)
        {
            var column = GeneralColumn(headerName, field, includeInGlobalSearch, hasOwnSearch, sort);
            column.Type = ColumnTypeEnum.Enum;
            column.EnumDictionary = dictionary;
            Columns.Add(column);
            return this;
        }
        public DatatableColumnBuilder AddCheckBoxColumn(string headerName, string field, bool disabled, string clickFunctionName, bool includeInGlobalSearch = true, bool hasOwnSearch = true, bool sort = true)
        {
            var column = GeneralColumn(headerName, field, includeInGlobalSearch, hasOwnSearch, sort);
            column.Disabled = disabled;
            column.Type = ColumnTypeEnum.CheckBox;
            column.ClickFunctionName = clickFunctionName;
            Columns.Add(column);
            return this;
        }
        public DatatableColumnBuilder AddDateTimeColumn(string headerName, string field, bool includeInGlobalSearch = true, bool hasOwnSearch = true, bool sort = true)
        {
            var column = GeneralColumn(headerName, field, includeInGlobalSearch, hasOwnSearch, sort);
            column.Type = ColumnTypeEnum.Date;
            Columns.Add(column);
            return this;
        }
        public DatatableColumnBuilder AddCustomColumn(string headerName, string field, string renderFunction, bool includeInGlobalSearch = true, bool hasOwnSearch = true, bool sort = true)
        {
            var column = GeneralColumn(headerName, field, includeInGlobalSearch, hasOwnSearch, sort);
            column.RenderFunction = renderFunction;
            column.Type = ColumnTypeEnum.Custom;
            Columns.Add(column);
            return this;
        }
    }
}

