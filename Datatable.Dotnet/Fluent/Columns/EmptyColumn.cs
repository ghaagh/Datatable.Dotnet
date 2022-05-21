using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Datatable.Dotnet.Fluent.Columns
{
    public class EmptyColumn
    {
        private readonly DatatableColumn _column;
        private readonly MemberInfo _property;
        public EmptyColumn(DatatableColumn column,MemberInfo propertyInfo)
        {
            _column = column;
            _property = propertyInfo;
        }
        public string GetDisplayName()
        {
            var attr = _property.GetCustomAttribute<DisplayAttribute>(false);
            if (attr == null)
            {
                return _property.Name;
            }
            return attr.Name ?? _property.Name;

        }
        public HeaderedColumn WithDefaultHeader()
        {
            _column.HeaderName = GetDisplayName();
            _column.Sort = true;
            _column.HasOwnSearch = true;
            return new HeaderedColumn(_column);
        }
        public HeaderedColumn WithHeader(string headerText, bool search, bool sort)
        {
            _column.HeaderName = headerText;
            _column.HasOwnSearch = search;
            _column.Sort = sort;
            return new HeaderedColumn(_column);
        }
    }
}
