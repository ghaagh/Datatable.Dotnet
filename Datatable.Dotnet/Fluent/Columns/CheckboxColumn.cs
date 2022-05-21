using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Datatable.Dotnet.Fluent.Columns
{
    public class CheckboxColumn
    {
        private readonly DatatableColumn _column;
        public CheckboxColumn(DatatableColumn column)
        {
            _column = column;
            _column.Type = ColumnTypeEnum.CheckBox;
        }
        public DatatableColumn WithNoClickFunction()
        {

            return _column;
        }
        public DatatableColumn WithClickFunction(string function)
        {
            _column.ClickFunctionName = function;
            return _column;
        }
    }
}
