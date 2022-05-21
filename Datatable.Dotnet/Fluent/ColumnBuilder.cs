using Datatable.Dotnet.Fluent.Columns;
using System.Linq.Expressions;
using System.Reflection;

namespace Datatable.Dotnet.Fluent
{
    public class ColumnBuilder<T>
    {
        private static MemberInfo GetProperty(Expression<Func<T, object>> property)
        {
            LambdaExpression lambda = property;
            MemberExpression memberExpression;

            if (lambda.Body is UnaryExpression)
            {
                UnaryExpression unaryExpression = (UnaryExpression)lambda.Body;
                memberExpression = (MemberExpression)unaryExpression.Operand;
            }
            else
            {
                memberExpression = (MemberExpression)lambda.Body;
            }

            return ((MemberInfo)memberExpression.Member);
        }
        private readonly DatatableColumn column = new DatatableColumn();
        /// <summary>
        /// 
        /// </summary>
        /// <param name="lambdaExp"></param>
        /// <param name="search"></param>
        /// <param name="sort"></param>
        /// <returns></returns>
        public EmptyColumn ForMember(Expression<Func<T, object>> lambdaExp)
        {
            column.Field = GetProperty(lambdaExp).Name;
            return new EmptyColumn(column,GetProperty(lambdaExp));
        }
        public NonPointedColumn ForNone()
        {
            column.Field = string.Empty;
            return new NonPointedColumn(column);
        }

    }
}
