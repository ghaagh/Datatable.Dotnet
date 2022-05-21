using System.Linq.Expressions;

namespace Datatable.Dotnet.Fluent
{
    /// <summary>
    /// For generating a script content for an ajax datatable.
    /// </summary>
    /// <typeparam name="T">Type of your view model</typeparam>
    public interface IDatatableBuilder<T>
    {
        public IDatatableBuilder<T> AddColumn(Expression<Func<ColumnBuilder<T>, DatatableColumn>> lambdaExp);
        string BuildAjaxTable(string tableId, string ajaxAddress);
        string BuildAjaxTable(string tableId, string ajaxAddress, int pageSize);
    }
}
