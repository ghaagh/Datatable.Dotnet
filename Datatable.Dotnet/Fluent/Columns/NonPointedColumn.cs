
namespace Datatable.Dotnet.Fluent.Columns
{
    public class NonPointedColumn
    {
        private DatatableColumn _column;
        public NonPointedColumn(DatatableColumn column)
        {
            _column = column;
        }
        public HeaderedColumn WithHeader(string headerText)
        {
            _column.HeaderName = headerText;
            _column.HasOwnSearch = false;
            _column.Sort = false;
            return new HeaderedColumn(_column);
        }
    }
}
