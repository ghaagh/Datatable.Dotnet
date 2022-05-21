using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Datatable.Dotnet.Fluent.Columns
{
    public class CustomColumn
    {
        private readonly DatatableColumn _column;
        public CustomColumn(DatatableColumn datatableColumn)
        {
            _column = datatableColumn;
            _column.Type = ColumnTypeEnum.Custom;
        }
        public DatatableColumn WithRender(string renderFunction)
        {
            _column.RenderFunction = renderFunction;
            return _column;
        }
    }
}
