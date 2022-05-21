
namespace Datatable.Dotnet.Fluent.Columns
{
    public class HeaderedColumn
    {
        private DatatableColumn _column;
        public HeaderedColumn(DatatableColumn column)
        {
            _column = column;
        }
        public DatatableColumn AsString()
        {
            return _column;
        }
        public DatatableColumn AsDate()
        {
            _column.Type = ColumnTypeEnum.Date;
            return _column;
        }
        public IntColumn AsInt() => new(_column);
        public EnumColumn AsEnum() => new(_column);
        public CustomColumn AsCustom() => new(_column);
        public CheckboxColumn AsCheckbox() => new(_column);

    }

}
