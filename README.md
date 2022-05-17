# Datatable.Dotnet
Datatable js is the most popular table generator with pagination support in front-end world.


Here is a customizable implementation of Datatable js with built-in ajax support, tag helper and a sound customization which can be enhanced by you.
You can use it, copy it, fork it, I really don't care.

## Problem
I had a mission to create a .net core project for bootstraping my friend's future projects. For his previous projects he used .Net Framework with 
telerik for the tables. Telerik is a well thought library and it is easy to use. It is hard to convice someone to stop using that and write javascript! 
So the solution was developing a tag helper with absolute minimum of javascript writing. 

## Starting the job
I Add Datatable javascript and css style with bootstrap theme to the layout page of my project. You can use the selected theme or add a custom style.

## Binding the Datatable Request.

For the start, I added a simple class for Datatable request that can be found here: 
[DataTableInput.cs](https://github.com/ghaagh/Datatable.Dotnet/blob/master/Datatable.Dotnet/DataTableInput.cs).\
As you can see below, it contains the global search keyword, column search, ordering and pagination data which will be send by datatable.js.\
```
using System.Text.Json.Serialization;

namespace Datatable.Dotnet;


public class DataTableInput
{
    public DataTableInput()
    {
        ColumnSearches = new List<ColumnSearch>();
    }
    public int Draw { get; set; }
    public int Start { get; set; }
    public int Length { get; set; }
    public IEnumerable<ColumnSearch> ColumnSearches { get; set; }
    public string Search { get; set; }
    public string[] Columns { get; set; }
    public Order Order { get; set; }
}
public class ColumnSearch
{
    public string Field { get; set; }
    public string Keyword { get; set; }
}
public class DataTableResult<T>
{
    [JsonPropertyName("data")]
    public IEnumerable<T> Data { get; set; }
    [JsonPropertyName("draw")]
    public int Draw { get; set; }
    [JsonPropertyName("recordsTotal")]
    public int RecordsTotal { get; set; }
    [JsonPropertyName("recordsFiltered")]
    public int RecordsFiltered { get; set; }
}
public class Order
{
    public string Column { get; set; }
    public string Dir { get; set; }
}
public class Search
{
    [JsonPropertyName("value")]
    public string Value { get; set; }
}
```
Ok, mow, As you probably know the way datatable.js is requesting the data is pretty messed up!. Dotnet model binding was not going to help me here.

There is an option to get the data from IHttpContextAccessor in the controller/Page but it is not pretty. So I decided to add a custom model binding to convert 
this data to the destination class. This binder can be found here:
[DataTableInputBinder](https://github.com/ghaagh/Datatable.Dotnet/blob/master/Datatable.Dotnet/ModelBinding/DatatableInputBinder.cs).
In this class I am getting the keys from the datatable.js request and then getting the value for every field and filling my input with these values.
```
using Datatable.Dotnet;
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
```
For adding this custom binding to a controller/page input, There is two options.
I could use [ModelBinder(BinderType = typeof(DatatableInputBinder))] at the top of 
the  class or object, or we could separate the logic into a BinderProvider file. 
Because I wanted to keep the controllers and pages as clean and minimal as possible, I added a Binder Provider to handle the binding 
job outside the controller and without using annotation.
the code for provider is here 
[DatatableInputBinderProvider](https://github.com/ghaagh/Datatable.Dotnet/blob/master/Datatable.Dotnet/ModelBinding/DataTableInputBinderProvider.cs).
The code is pretty self explainatory. I am returning the previusly written Binder if the type of the input is DatatbleInput.
```
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

```

Ok, Now it is better. The last thing I did to finish this part was adding the Provider to my Program.cs File. I added this code to both Mvc and Razor Page 
so it can be used in both controller methods and razor page handlers.
```
builder.Services.AddRazorPages().AddMvcOptions(options => {
    options.ModelBinderProviders.Insert(1, new DataTableInputBinderProvider());
});
builder.Services.AddControllersWithViews().AddMvcOptions(options => {
    options.ModelBinderProviders.Insert(1, new DataTableInputBinderProvider());
});
```

## ColumnBuilder
Now that the request part of the code is over, I moved to real part. Response!. I added a 
[ColumnTypeEnum](https://github.com/ghaagh/Datatable.Dotnet/blob/master/Datatable.Dotnet/ColumnTypeEnum.cs) 
```
namespace Datatable.Dotnet;

public enum ColumnTypeEnum
{
    String,
    CheckBox,
    Date,
    Enum,
    Custom
}
```
as it is abvious, Based on my need these are the only column types I need for creating a custom datatable. the string is the main one which represent every normal 
'JUST-SHOW-IT' field from the server. Custom, on the other hand, is completely open-ended and can be used for every custom display like buttons etc. Date is like
string with an exeption that is its search. I wanted to search inside the fields with a plugin like datepicker. Others are self-explainatory.

Here is the Column class that contains  information about how to customize the column in datatable.
```
namespace Datatable.Dotnet
{
    public class DatatableColumn
    {
        public DatatableColumn()
        {
            EnumDictionary = new Dictionary<int, string>();
        }
        public string Field { get; set; }
        public bool Sort { get; set; } = true;
        public string? ClickFunctionName { get; set; }
        public ColumnTypeEnum Type { get; set; } = ColumnTypeEnum.String;
        public string? HeaderName { get; set; }
        public string? RenderFunction { get; set; }
        public bool IncludeInGlobalSearch { get; set; } = true;
        public bool HasOwnSearch { get; set; } = true;
        public bool Disabled { get; internal set; }
        public Dictionary<int, string> EnumDictionary { get; set; }
    }
}
```
Field: the name of the field. for example if I am getting a list of products from the server, 'ProductName' is the field. this is field name not its value.
Sort: The column has sort buttons or not.
ClickFunctionName: specific to chekbox type. It can carry a javascript function to be called after a click on checkbox. for example
it can be usefull for instant enabling and disabling
records.
HeaderName: the th string for the column in datatable.
RenderFunction: specific to custom type. It can be a whole javascript function or just its name.
Disabled: also specific for checkbox type.
EnumDictionary: to show a user friendly enum text instead of just number or joint strings.


Ok, The column definition is done. But there are a lot of specific fields for specific column types. So I added a column Builder to help the client to create a list.
```

namespace Datatable.Dotnet
{

    public class DatatableColumnBuilder
    {
        public DatatableColumnBuilder()
        {
        }
        private List<DatatableColumn> Columns { get; set; } = new List<DatatableColumn>();
        public IEnumerable<DatatableColumn> Build() => Columns;
        private static DatatableColumn GeneralColumn(string headerName, string field, bool includeInGlobalSearch = true, 
        bool hasOwnSearch = true, bool sort = true)
        {
            return new DatatableColumn()
            {
                HeaderName = headerName,
                Field = field,
                Sort = sort,
                IncludeInGlobalSearch = includeInGlobalSearch,
                HasOwnSearch = hasOwnSearch
            };
        }
        public DatatableColumnBuilder AddTextColumn(string headerName, string field, bool includeInGlobalSearch = true, 
        bool hasOwnSearch = true, bool sort = true)
        {
            var column = GeneralColumn(headerName, field, includeInGlobalSearch, hasOwnSearch, sort);
            column.Type = ColumnTypeEnum.String;
            Columns.Add(column);
            return this;
        }
        public DatatableColumnBuilder AddEnumColumn(string headerName, string field, Dictionary<int, string> dictionary, 
        bool includeInGlobalSearch = true, bool hasOwnSearch = true, bool sort = true)
        {
            var column = GeneralColumn(headerName, field, includeInGlobalSearch, hasOwnSearch, sort);
            column.Type = ColumnTypeEnum.Enum;
            column.EnumDictionary = dictionary;
            Columns.Add(column);
            return this;
        }
        public DatatableColumnBuilder AddCheckBoxColumn(string headerName, string field, bool disabled, string clickFunctionName, 
        bool includeInGlobalSearch = true, bool hasOwnSearch = true, bool sort = true)
        {
            var column = GeneralColumn(headerName, field, includeInGlobalSearch, hasOwnSearch, sort);
            column.Disabled = disabled;
            column.Type = ColumnTypeEnum.CheckBox;
            column.ClickFunctionName = clickFunctionName;
            Columns.Add(column);
            return this;
        }
        public DatatableColumnBuilder AddDateTimeColumn(string headerName, string field, bool includeInGlobalSearch = true, 
        bool hasOwnSearch = true, bool sort = true)
        {
            var column = GeneralColumn(headerName, field, includeInGlobalSearch, hasOwnSearch, sort);
            column.Type = ColumnTypeEnum.Date;
            Columns.Add(column);
            return this;
        }
        public DatatableColumnBuilder AddCustomColumn(string headerName, string field, string renderFunction, bool includeInGlobalSearch = true, 
        bool hasOwnSearch = true, bool sort = true)
        {
            var column = GeneralColumn(headerName, field, includeInGlobalSearch, hasOwnSearch, sort);
            column.RenderFunction = renderFunction;
            column.Type = ColumnTypeEnum.Custom;
            Columns.Add(column);
            return this;
        }
    }
}

```
With this, If a user want to create a list of complex columns, it is as simple as this code.
```
        IEnumarable<Columns> input = new DatatableColumnBuilder()
            .AddTextColumn("Identification", nameof(ProductViewModel.Id))
            .AddTextColumn("ProductName", nameof(ProductViewModel.Name))
            .AddTextColumn("Description", nameof(ProductViewModel.Desciption))
            .AddTextColumn("Tags", nameof(ProductViewModel.ProductTags), true, false, false)
            .AddEnumColumn("Type", nameof(ProductViewModel.ProductType),
            new Dictionary<int, string>
            {
                {(int)ProductTypeEnum.Book,"Books" },
                {(int)ProductTypeEnum.Tools,"Writing Tools" },
                {(int) ProductTypeEnum.Other,"Other Tools" }
            })
            .AddDateTimeColumn("Date", nameof(ProductViewModel.Date))
            .AddCheckBoxColumn("Visible in Website", nameof(ProductViewModel.Visible), false, "onVisibleClick")
            .AddCustomColumn("Operations", null, "renderButtons", false, false, false)
            .Build();
```
as you can see, I added a bunch of strings, a checkbox, an enum with custom dictionary and a button group to the input and then I call Build() to
return a column List. with this the response part is done!

## Setting

There are a lot of possible customization . But for the first version. I thought these are enough. 
So I added these to my appsettings.config. 

```
  "DataTableSetting": {
    "DefaultPageSize": 25,
    "Header": {
      "OrderAscHtml": "↑",
      "OrderDescHtml": "↓",
      "All": "Show All",
      "DateColumnPluginCall": "$('.date-picker').datepicker();",
      "Checked": "Selected",
      "Unchecked": "Not Selected",
      "OwnSearch": "Search In {0}"
    },
    "Language": {
    "decimal":        "",
    "emptyTable":     "No data available in table",
    "info":           "Showing _START_ to _END_ of _TOTAL_ entries",
    "infoEmpty":      "Showing 0 to 0 of 0 entries",
    "infoFiltered":   "(filtered from _MAX_ total entries)",
    "infoPostFix":    "",
    "thousands":      ",",
    "lengthMenu":     "Show _MENU_ entries",
    "loadingRecords": "Loading...",
    "processing":     "",
    "search":         "Search:",
    "zeroRecords":    "No matching records found",
    "paginate": {
        "first":      "First",
        "last":       "Last",
        "next":       "Next",
        "previous":   "Previous"
    },
    "aria": {
        "sortAscending":  ": activate to sort column ascending",
        "sortDescending": ": activate to sort column descending"
    }

    }
  },
```
The class representation of the json is

```
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Datatable.Dotnet.Setting
{
    public class DatatableSetting
    {
        public int DefaultPageSize { get; set; }
        public Language Language { get; set; }
        public Header Header { get; set; }

    }
    public class Header
    {
        public string DateColumnPluginCall { get; set; }
        public string OwnSearch { get; set; }
        public string All { get; set; }
        public string Checked { get; set; }
        public string Unchecked { get; set; }
        public string OrderDescHtml { get; set; }
        public string OrderAscHtml { get; set; }
    }
    public class Aria
    {

        public string sortAscending { get; set; }

        public string sortDescending { get; set; }
    }

    public class Paginate
    {
        public string first { get; set; }
        public string last { get; set; }
        public string next { get; set; }
        public string previous { get; set; }
    }

    public class Language
    {
        public string @decimal { get; set; }
        public string emptyTable { get; set; }
        public string info { get; set; }
        public string infoEmpty { get; set; }
        public string infoFiltered { get; set; }
        public string infoPostFix { get; set; }
        public string thousands { get; set; }
        public string lengthMenu { get; set; }
        public string loadingRecords { get; set; }
        public string processing { get; set; }
        public string search { get; set; }
        public string zeroRecords { get; set; }

        public Paginate paginate { get; set; }

        public Aria aria { get; set; }
    }
}

```

And finally I added this line to program.cs
```
builder.Services.Configure<DatatableSetting>(builder.Configuration.GetSection("DatatableSetting"));
```
## Tag Helper
The tag helper code can be found here: [DataTableTagHelper](https://github.com/ghaagh/Datatable.Dotnet/blob/master/Datatable.Dotnet/TagHelper/DataTableHelper.cs)
All I did is to create a custom string and replace it with my custom settings in appsettings.config.
```
using Datatable.Dotnet.Setting;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Text;

namespace Datatable.Dotnet.TagHelpers;

[HtmlTargetElement("datatable-helper")]
public class DateTableHelper : TagHelper
{
    private readonly DatatableSetting _setting;

    public DateTableHelper(IOptions<DatatableSetting> options)
    {
        _setting = options.Value;
    }
    public IEnumerable<DatatableColumn> For { get; set; }
    public string TableId { get; set; }
    public string AjaxAddress { get; set; }
    public int PageSize { get; set; }
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "script";
        output.TagMode = TagMode.StartTagAndEndTag;

        var sb = new StringBuilder();
        var serializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };
        var index = 0;
        var stringBuilder = new StringBuilder();
        foreach (var item in For.Where(c => c.Type == ColumnTypeEnum.Enum))
        {
            stringBuilder.AppendFormat(@"
let {0}Map=new Map([{1}]);", char.ToLower(item.Field[0]) + item.Field[1..], string.Join(',', item.EnumDictionary.Select(c => "[" + c.Key + ",'" + c.Value + "']")));
        }
        stringBuilder.AppendFormat(@"
let tbl;
let table = document.getElementById('{0}');
let tableHeader = document.createElement('thead');
tableHeader.innerHTML += `", TableId);
        foreach (var item in For)
        {
            switch (item.Type)
            {
                case ColumnTypeEnum.String:
                case ColumnTypeEnum.Date:
                    var stringHeaderBuilder = new StringBuilder();
                    var additionalClass = item.Type == ColumnTypeEnum.Date ? "date-picker" : "";

                    stringHeaderBuilder.AppendFormat(@"<th>
<label>{0}</label>
", item.HeaderName);
                    if (item.HasOwnSearch)
                    {
                        stringHeaderBuilder.AppendFormat(@"<input class=""column-search {0}""  type=""text"" data-column=""{2}"" placeholder=""{1}"" />
", additionalClass, string.Format(_setting.Header.OwnSearch,item.HeaderName), index);
                    }
                    if (item.Sort)
                    {
                        stringHeaderBuilder.AppendFormat(@"<span class=""sort-box""><a class=""asc"" data-column=""{0}"">↑</a><a class=""desc"" data-column=""{0}"">↓</a></span>", index);
                    }
                    stringHeaderBuilder.Append("</th>");
                    stringBuilder.Append(stringHeaderBuilder);
                    index++;
                    break;
                case ColumnTypeEnum.Enum:
                case ColumnTypeEnum.CheckBox:
                case ColumnTypeEnum.Custom:
                    var customHeaderBuilder = new StringBuilder();
                    customHeaderBuilder.Append(@"
<th>
<label></label>
</th>");
                    index++;
                    customHeaderBuilder.AppendFormat(@"<th>
<label>{0}</label>
", item.HeaderName);
                    if (item.HasOwnSearch)
                    {
                        if (item.Type == ColumnTypeEnum.Enum)
                        {
                            customHeaderBuilder.AppendFormat(@"
<select data-column=""{0}"">
<option>{1}</option>
{2}
</select>
", index - 1,_setting.Header.All, String.Join(',', item.EnumDictionary.Select(c => "<option value=\"" + c.Key + "\">" + c.Value + "</option>")));
                        }
                        else if (item.Type == ColumnTypeEnum.CheckBox)
                        {
                            customHeaderBuilder.AppendFormat(@"
<select data-column=""{0}"">
<option value=""null"">{1}</option>
<option value=""false"">{2}</option>
<option value=""true"">{3}</option>
</select>
", index - 1,_setting.Header.All,_setting.Header.Unchecked,_setting.Header.Checked);
                        }
                        else
                        {
                            customHeaderBuilder.AppendFormat(@"
<input class=""column-search""  type=""text"" data-column=""{0}"" placeholder=""{1}"" />
", index - 1, "جستجو در" + item.HeaderName);
                        }

                    }
                    if (item.Sort)
                    {
                        customHeaderBuilder.AppendFormat(@"
<span class=""sort-box"">
<a class=""asc"" data-column=""{0}"">{1}</a>
<a class=""desc"" data-column=""{0}"">{2}</a>
</span>
", index, _setting.Header.OrderAscHtml, _setting.Header.OrderDescHtml);
                    }
                    customHeaderBuilder.Append("</th>");
                    stringBuilder.Append(customHeaderBuilder);
                    index++;
                    break;
            }

        }
        stringBuilder.Append(@"</tr>`;
table.appendChild(tableHeader)
");
        index = 0;

        stringBuilder.AppendFormat(@"
$(function(){{
		tbl = $('#{0}').DataTable({{
		proccessing: true,
		search: true,
		serverSide: true,
		ajax: '{1}',
		columnDefs : [
", TableId, AjaxAddress);
        foreach (var item in For)
        {
            switch (item.Type)
            {
                case ColumnTypeEnum.String:
                case ColumnTypeEnum.Date:
                    stringBuilder.AppendFormat(@"
{{'data':'{0}','targets':{1}}},
", char.ToLower(item.Field[0]) + item.Field[1..], index);
                    index++;
                    break;
                case ColumnTypeEnum.Enum:
                    stringBuilder.AppendFormat(@"
{{'data':'{0}','targets':{1},'visible':false}},
", char.ToLower(item.Field[0]) + item.Field[1..], index);
                    index++;
                    stringBuilder.AppendFormat(@"
{{'data':'{0}','targets':{1},render:function(data){{
return {0}Map.get(data)
		}}
		}},
", char.ToLower(item.Field[0]) + item.Field[1..], index);
                    index++;
                    break;
                case ColumnTypeEnum.Custom:
                    stringBuilder.AppendFormat(@"
{{'data':null,'targets':{0},'visible':false}},
", index);
                    index++;
                    stringBuilder.AppendFormat(@"
{{'targets': {0},
			data: null,
			render: {1}
			}},
", index, item.RenderFunction);
                    index++;
                    break;
                case ColumnTypeEnum.CheckBox:
                    stringBuilder.AppendFormat(@"
{{'data':'{0}','targets':{1},'visible':false}},
", char.ToLower(item.Field[0]) + item.Field[1..], index);
                    index++;
                    stringBuilder.AppendFormat(@"
{{'targets': {0},'data': '{1}', 
render: function(data, type, row, meta) 
{{let checked = data ? 'checked=' : '';return `<input ${{checked}} onclick=""{2}(${{row.id}},this)"" type=""checkbox""  />`}}
}},
", index, char.ToLower(item.Field[0]) + item.Field[1..], item.ClickFunctionName);
                    index++;
                    break;
            }
        }

        stringBuilder.AppendFormat(@"
{{targets: ""_all"",orderable: false}}],
		pageLength: {0},
		language: {1}
}});
		tbl.columns().every(function() {{
		let column = this;
		let header = column.header();
		$(header).on('click', 'a.asc', function() {{
			tbl.order([parseInt($(this).attr('data-column')), 'asc']).draw();
		}});
		$(header).on('click', 'a.desc', function() {{
			tbl.order([parseInt($(this).attr('data-column')), 'desc']).draw();
		}});
		$(header).on('keyup', 'input', function() {{
			tbl.column($(this).attr('data-column')).search(this.value);
			tbl.draw();
		}});
		$(header).on('change', '.date-picker', function() {{
console.log('changed');
			tbl.column($(this).attr('data-column')).search(this.value);
			tbl.draw();
		}});
		$(header).on('change', 'select', function() {{
			tbl.column($(this).attr('data-column')).search(this.value=='null'?'':this.value);
tbl.draw();
		}});
    }});
{2}
	}});

", PageSize == 0 ? _setting.DefaultPageSize : PageSize, JsonConvert.SerializeObject(_setting.Language),_setting.Header.DateColumnPluginCall);
        output.PreContent.SetHtmlContent(stringBuilder.ToString());
    }
}
```
And finally, I referenced the tag helper inside my ViewImport.cshtml file like this:
```
@addTagHelper "*, Datatable.Dotnet"
```
Please note that this is the assembly name(Project name), not the namespace.
The parameters I used for this tag helper is:
```
    public string TableId { get; set; }
    public string AjaxAddress { get; set; }
    public int PageSize { get; set; }
```
TableId for an empty html table element. other fields are obvious.

## Ajax call
For the ajax part, I first created a simple generic class for returning to datatable.js as a response.

```
public class DataTableResult<T>
{
    [JsonPropertyName("data")]
    public IEnumerable<T> Data { get; set; }
    [JsonPropertyName("draw")]
    public int Draw { get; set; }
    [JsonPropertyName("recordsTotal")]
    public int RecordsTotal { get; set; }
    [JsonPropertyName("recordsFiltered")]
    public int RecordsFiltered { get; set; }
}
```
and at the end I created a PageHandler inside my product Razor Page. Here is an gist of it:
```
        //Create your query as you want. do not ToList() or AsEnumarable Here yet!
        var records = _db.Products
            .Include(c => c.ProductTags)
            .ThenInclude(c => c.Tag)
            .AsQueryable();

        //Get the total count before filtering. it is needed by datatable
        var totalRecords = await records.CountAsync(); ;

        //Order and search globally and by column
        if (!string.IsNullOrEmpty(datatableRequest.Search))
            records = _queryHelper.ApplyGlobalSearch(records, datatableRequest.Search, globalSearchColumns);
        foreach (var item in datatableRequest.ColumnSearches)
        {
            records = _queryHelper.ApplySearch(records, item.Keyword, item.Field);
        }

        if (datatableRequest.Order != null)
        {
            records = _queryHelper.ApplySort(records, $"{datatableRequest.Order.Column} {datatableRequest.Order.Dir} ");
        }

        //get the total count after filtering and sorting
        var filterdRecordCount = await records.CountAsync();
        //convert your query to a list of <PageSize> item
        var pagedRecord = await _queryHelper.ApplyPaginationAsync(records, datatableRequest.Start, datatableRequest.Length);
        //mappped the Product to ProductViewModel via automapper.
        var mapped = pagedRecord.Records.Select(record => _mapper.Map<ProductViewModel>(record));
        //return the list and some other info as json.
        var result = new JsonResult(new DataTableResult<ProductViewModel>()
        {
            Data = mapped,
            Draw = datatableRequest.Draw,
            RecordsFiltered = filterdRecordCount,
            RecordsTotal = totalRecords
        });
        return result;
    }
```
Note that the _queryHelper is using dynamic linq in my case. you can implement with any other library you want.

### Html Part
```
        <table id="example" class="display">
        </table>
```
### Tag Helper Part
```
<datatable-helper for="@Model.Input" ajax-address="./Index?handler=PagedRecords" page-size="25" table-id="example"></datatable-helper>

<script>
    function renderButtons(data, type, row, meta){
       return  `<div class="btn-group btn-group-sm" role="group" aria-label="Basic example"">
             <button class="btn btn-danger btn-sm">حذف</button>
      <button class="btn btn-secondary btn-sm">یه چی دیگه</button>
            <button  class="btn btn-primary btn-sm" onclick="console.log(${row.id})">ویرایش</button>
    </div>`
    }
    function onVisibleClick(row,sender){
        console.log(sender);
        console.log('visibled clicked:'+ row);
    }
</script>
```

Based on your response type and your ColumnBuilder Input you should see something like this:
![Final view](https://github.com/ghaagh/Datatable.Dotnet/blob/master/Datatable.Dotnet.PNG)

 The javascript part is as minimal as possible. Happy coding/ copying/ changing/ complaining/ whatever.





