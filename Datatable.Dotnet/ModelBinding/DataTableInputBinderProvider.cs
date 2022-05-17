using Datatable.Dotnet;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

namespace Datatable.Dotnet.ModelBinding;

public class DataTableInputBinderProvider : IModelBinderProvider
{
    public IModelBinder GetBinder(ModelBinderProviderContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (context.Metadata.ModelType == typeof(DataTableInput))
        {
            return new BinderTypeModelBinder(typeof(DatatableInputBinder));
        }

        return null;
    }
}

