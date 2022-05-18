using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Datatable.Dotnet.ModelBinding;

public class DatatableInputBinder : IModelBinder
{
    private readonly HttpContext _httpContext;

    public DatatableInputBinder(IHttpContextAccessor accessor)
    {
        _httpContext = accessor.HttpContext;
    }

    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        if (bindingContext == null)
        {
            throw new ArgumentNullException(nameof(bindingContext));
        }

        var keys = GetKeys(_httpContext);
        var searchResult = GetSearch(_httpContext, keys);
        var model = new DataTableInput
        {
            Draw = int.Parse(_httpContext.Request.Query["draw"]),
            Start = int.Parse(_httpContext.Request.Query["start"]),
            Search = _httpContext.Request.Query["search[value]"],
            Length = int.Parse(_httpContext.Request.Query["length"]),
            ColumnSearches = searchResult.Select(c => new ColumnSearch() { Field = c.Key, Keyword = c.Value }),
            Order = GetOrder(_httpContext)
        };
        bindingContext.Result = ModelBindingResult.Success(model);
        return Task.CompletedTask;
    }

    public static Order GetOrder(HttpContext context)
    {
        var orderField = context.Request.Query["order[0][column]"];
        if (!string.IsNullOrEmpty(orderField))
            return new Order()
            {
                Column = context.Request.Query[$"columns[{orderField}][data]"] + " ",
                Dir = context.Request.Query["order[0][dir]"]
            };
        return null;
    }

    private static Dictionary<string, string> GetSearch(HttpContext context, Dictionary<string, int> keyDictionary)
    {
        var result = new Dictionary<string, string>();
        foreach (var item in keyDictionary)
        {
            var searchValue = context.Request.Query[$"columns[{item.Value}][search][value]"];
            if (!string.IsNullOrEmpty(searchValue))
                result.TryAdd(item.Key, searchValue);
        }
        return result;
    }
    private static Dictionary<string, int> GetKeys(HttpContext context)
    {
        var requestQuery = context.Request.Query.Where(c => c.Key.StartsWith("columns[") && c.Key.Contains("[data]"));
        var columnKeys = new Dictionary<string, int>();
        foreach (var item in requestQuery)
        {
            var key = item.Value;
            var value = item.Key.Replace("columns[", "").Replace("][data]", "");
            columnKeys.TryAdd(key, int.Parse(value));
        }
        return columnKeys;
    }
}
