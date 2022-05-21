
using Datatable.Dotnet.Format;

namespace Datatable.Dotnet.Fluent.Columns
{
    public class IntColumn
    {
        private readonly DatatableColumn _column;
        public IntColumn(DatatableColumn column)
        {
            _column = column;
            _column.Type = ColumnTypeEnum.Number;
        }
        public DatatableColumn WithDefaultFormat()
        {
            _column.Format = NumberFormat.DefaultInt();
            return _column;
        }
        public DatatableColumn WithFormat(NumberFormat format)
        {
            _column.Format = format;
            return _column;
        }
    }
}
