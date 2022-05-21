
namespace Datatable.Dotnet.Fluent.Columns
{
    public class EnumColumn
    {
        private readonly DatatableColumn _column;
        public EnumColumn(DatatableColumn column)
        {
            _column = column;
            _column.Type = ColumnTypeEnum.Enum;
        }
        public DatatableColumn WithDictionary(Dictionary<int, string> dictionary)
        {
            _column.EnumDictionary = dictionary;
            return _column;
        }

    }
}
