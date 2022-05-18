# Datatable.Dotnet
Datatable js is the most popular table generator with pagination support in the front-end world.


Here is a customizable implementation of Datatable js with built-in ajax support, tag helper, and sound customization which can be enhanced by you.

## Problem
I had a mission to create a .net core project for bootstrapping my friend's future projects. For his previous projects, he used .Net Framework with 
Telerik for the tables. Telerik is a well-thought library and it is easy to use. It is hard to convince someone to stop using that and write javascript! 
So the solution was to develop a tag helper with the absolute minimum of javascript writing. 

## Starting the job
I Add Datatable javascript and CSS style with Bootstrap theme to the layout page of my project. You can use the selected theme or add a custom style.

## Binding the Datatable Request.

For the start, I added a simple class for Datatable requests that can be found here: 
[DataTableInput.cs](https://github.com/ghaagh/Datatable.Dotnet/blob/master/Datatable.Dotnet/DataTableInput.cs).\
As you can see below, it contains the global search keyword, column search, ordering, and pagination data which will be sent by datatable.js.
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
Ok. Now, As you probably know the way datatable.js is requesting the data is pretty messed up! Dotnet model binding was not going to help me here.

There is an option to get the data from IHttpContextAccessor in the controller/Page but it is not pretty. So I decided to add a custom model binding to convert 
this data to the destination class. This binder can be found here:
[DataTableInputBinder](https://github.com/ghaagh/Datatable.Dotnet/blob/master/Datatable.Dotnet/ModelBinding/DatatableInputBinder.cs).

In this class, I am getting the keys from the datatable.js request and then getting the value for every field and filling my input with these values.
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
For adding this custom binding to a controller/page input, There are two options.
I could use **[ModelBinder(BinderType = typeof(DatatableInputBinder))]** at the top of 
my class or object, or I could separate the logic into a **BinderProvider** file. 

Because I wanted to keep the controllers and pages as clean and minimal as possible, I added a Binder Provider to handle the binding 
job outside the controller and without using annotation.
the code for the provider is here 
[DatatableInputBinderProvider](https://github.com/ghaagh/Datatable.Dotnet/blob/master/Datatable.Dotnet/ModelBinding/DataTableInputBinderProvider.cs).


The code is pretty self-explanatory. I am returning the previously written **DatatableInputBinder** if the type of the input is DatatbleInput.
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

Ok, now it is better. The last thing I did to finish this part was add the Provider to my Program.cs File. I added this code to both MVC and Razor Page 
so it can be used in both controller methods and razor page handlers.
```
builder.Services.AddRazorPages().AddMvcOptions(options => {
    options.ModelBinderProviders.Insert(1, new DataTableInputBinderProvider());
});
builder.Services.AddControllersWithViews().AddMvcOptions(options => {
    options.ModelBinderProviders.Insert(1, new DataTableInputBinderProvider());
});
```

## Generating Column List
Now that the request part of the code is over, I moved to the real part. Generating the script with C#. First, 

1. I added a [ColumnTypeEnum](https://github.com/ghaagh/Datatable.Dotnet/blob/master/Datatable.Dotnet/ColumnTypeEnum.cs)  to contain all different types of columns possible
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
|ColumnTypeEnum value|Desired Behaviour|
-|-
|**String**|  The main column type which represents every normal 'JUST-SHOW-IT' field from the server| Just
|**Date**|  Just like Strings with support for Javascript datepicker plugins. The header can support selecting dates with javascript libraries|
|**Checkbox**| Dedicated to show the value in checkbox format. It will have the support of calling javascript function when user clicks on the checkbox|
|**Enum**| Showing enum Data properly with user friendly Description for every type
|**Custom**| Completely open-ended with support of showing any type of column with simple javascript function|

2. Now it is the time for defining the Columns
Here is the Column class that contains information about how the table columns will be generated.

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
|Property| Explanation  |
|--|--|
|**Field**  |the name of the field. for example, if I am getting a list of products from the server, 'ProductName' is the field. this is the field name not its value.  |
|**Sort**|Specifies wether the column has sort buttons or not.|
|**HasOwnSearch**| Specifies whether the column has its own search in the header or not.
|**ClickFunctionName**|specific to Checkbox type. It can carry a javascript function to be called after a click on the checkbox. for example it can be useful for instant enabling and disabling records.
|**HeaderName**| the string that will be shown in generated Datatable header for the column.
|**RenderFunction**|specific to the **Custom** column types. It can be a whole javascript function or just its name.
|**Disabled**|Specific for **checkbox** column types.
|**EnumDictionary**|Specific to **Enum** Column types to show a user-friendly enum text instead of just number or joint strings.


Ok, The column definition is done. But there are a lot of specific fields for specific column types and It can be confusing for the client who is using the code. So I added a [DatatableColumnBuilder](https://github.com/ghaagh/Datatable.Dotnet/blob/master/Datatable.Dotnet/DatatableColumnBuilder.cs) to help the client to create a list.
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
Say the client want to show a list of products to user. He/She wants to use a ViewModel called ProductViewModel. the ProductViewModel contains fields like these:
```
    public class ProductViewModel
    {
        public int Id { get; set; }
        public ProductTypeEnum ProductType { get; set; }
        public string Name { get; set; }
        public PersianDateTime Date { get; set; }
        public string Desciption { get; set; }
        public bool Visible { get; set; }
        public virtual IEnumerable<string> ProductTags { get; set; }
    }
```
Then for creating a list of column for this viewModel all the user has to do is to use the DatatableColumnBuilder  in his/her get() handler or MVC controller method like this and return the generated column list to View or Page.
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

**NOTE**

1. after all columns are added, you still need to call Build() to get the list of columns.
2. The column Builder is just returning a list of Columns, It is not connecting to any database yet.





## Setting

There are a lot of possible customizations. But for the first version. I thought these are enough. 
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
|Setting Field | Explanation|
|-|-|
|**Language**| The whole object is a mapped object from Datatable.Js language JSON. Extra information can be found on [Datatable.Net Languages](https://datatables.net/examples/basic_init/language.html).
|**DefaultPageSize**| When the Page size in our code is not specified, our tag helper will use this parameter as a default value.
|**Header.OrderAscHtml** and **Header.OrderDescHtml**| For customizing the **asc** and **desc** button on the header for the field that have **Sort** enabled|
|**All**| This text will be replaced in **Enum** and **Checkbox** Column types as a default filter for all results.
|**DateColumnPluginCall**| This line will be the place for calling your desired **DatePicker** plugin. If you don't want that. Empty the string|
|**Checked** and **Unchecked**| Specific to **Checkbox** column type and for creating search Filter with dropdown. Note that the Filter will also contain "Header.All" value for showing all results.|
|**OwnSearch**| A placeholder for the column filter for **String** and **Date** columns. If {0} exists in this setting, it will be replaced by Column **HeaderName**.
The class representation of my setting JSON is:

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

Obviously, I added this line to program.cs
```
builder.Services.Configure<DatatableSetting>(builder.Configuration.GetSection("DatatableSetting"));
```
## Tag Helper
The tag helper code can be found here: [DataTableTagHelper](https://github.com/ghaagh/Datatable.Dotnet/blob/master/Datatable.Dotnet/TagHelper/DataTableHelper.cs)
Surprisingly, The explanation for this part is minimal because all I am doing is creating a string with havascript content for the **Column** List it is getting as an input.(For variable)
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

        var stringBuilder = new StringBuilder();

        stringBuilder.Append(GetJavaScriptDictionariesString(For.Where(c => c.Type == ColumnTypeEnum.Enum)));

        stringBuilder.Append(GetTableVariablesString(TableId));

        stringBuilder.Append(GetHeadersString(For, _setting));

        stringBuilder.Append(GetDatatableBodyString(For, TableId, AjaxAddress));

        stringBuilder.Append(GetFooterString(_setting, PageSize));

        output.PreContent.SetHtmlContent(stringBuilder.ToString());
    }

    private static string GetJavaScriptDictionariesString(IEnumerable<DatatableColumn> enumColumns)
    {
        var stringBuilder = new StringBuilder();
        foreach (var item in enumColumns)
        {
            stringBuilder.Append(GetEnumDictionaryString(item));
        }
        return stringBuilder.ToString();
    }

    private static string GetFooterString(DatatableSetting setting, int pageSize)
    {
        var stringBuilder = new StringBuilder();
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

", pageSize == 0 ? setting.DefaultPageSize : pageSize, JsonConvert.SerializeObject(setting.Language), setting.Header.DateColumnPluginCall);
        return stringBuilder.ToString();

    }

    private static string GetEnumDictionaryString(DatatableColumn item)
    {
        var javaSciptMapItems = item.EnumDictionary.Select(c => $"[{ c.Key},'{c.Value}']");
        return string.Format("let {0}Map=new Map([{1}]);\n", GetNormalizedFieldName(item.Field), string.Join(',', javaSciptMapItems));
    }

    private static string GetTableVariablesString(string tableId)
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.Append("let tbl;\n");
        stringBuilder.AppendFormat("document.getElementById('{0}')\n", tableId);
        stringBuilder.Append("let tableHeader = document.createElement('thead');\n");
        return stringBuilder.ToString();
    }

    private static string GetHeadersString(IEnumerable<DatatableColumn> columns, DatatableSetting setting)
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.Append("tableHeader.innerHTML += `<tr>\n");
        var index = 0;
        foreach (var item in columns)
        {
            switch (item.Type)
            {
                case ColumnTypeEnum.String:
                case ColumnTypeEnum.Date:
                    stringBuilder.Append(GetNormalHeaderString(item, index, setting));
                    index++;
                    break;
                case ColumnTypeEnum.Enum:
                case ColumnTypeEnum.CheckBox:
                case ColumnTypeEnum.Custom:
                    stringBuilder.Append(GetCustomHeaderString(item, index, setting));
                    index++;
                    break;
            }

        }
        stringBuilder.Append("</tr>`;\n");
        stringBuilder.Append("table.appendChild(tableHeader);\n");
        return stringBuilder.ToString();
    }

    private static string GetNormalHeaderString(DatatableColumn column, int index, DatatableSetting setting)
    {
        var stringHeaderBuilder = new StringBuilder();
        var additionalClass = column.Type == ColumnTypeEnum.Date ? "date-picker" : "";

        stringHeaderBuilder.AppendFormat(@"<th>
                                        <label>{0}</label>
", column.HeaderName);
        if (column.HasOwnSearch)
        {
            stringHeaderBuilder.AppendFormat(@"<input class=""column-search {0}""  type=""text"" data-column=""{2}"" placeholder=""{1}"" />
", additionalClass, string.Format(setting.Header.OwnSearch, column.HeaderName), index);
        }
        if (column.Sort)
        {
            stringHeaderBuilder.AppendFormat(@"<span class=""sort-box""><a class=""asc"" data-column=""{0}"">{1}</a><a class=""desc"" data-column=""{0}"">{2}</a></span>",
                index,setting.Header.OrderAscHtml,setting.Header.OrderDescHtml);
        }
        return stringHeaderBuilder.ToString();
    }

    private static string GetDatatableBodyString(IEnumerable<DatatableColumn> columns, string tableId, string ajaxAddress)
    {
        var stringBuilder = new StringBuilder();
        var index = 0;
        stringBuilder.AppendFormat(@"
$(function(){{
		tbl = $('#{0}').DataTable({{
		proccessing: true,
		search: true,
		serverSide: true,
		ajax: '{1}',
		columnDefs : [
", tableId, ajaxAddress);
        foreach (var item in columns)
        {
            switch (item.Type)
            {
                case ColumnTypeEnum.String:
                case ColumnTypeEnum.Date:
                    stringBuilder.Append(GetNormalColumnDef(item.Field, index));
                    index++;
                    break;
                case ColumnTypeEnum.Enum:
                    stringBuilder.Append(GetHiddenColumnDef(item.Field, index));
                    index++;
                    stringBuilder.Append(GetEnumDesciptionColumnDef(item.Field, index));
                    index++;
                    break;
                case ColumnTypeEnum.Custom:
                    stringBuilder.Append(GetCustomHiddenColumnDef(index));
                    index++;
                    stringBuilder.Append(GetCustomColumnDef(item.RenderFunction, index));
                    index++;
                    break;
                case ColumnTypeEnum.CheckBox:
                    stringBuilder.Append(GetHiddenColumnDef(item.Field, index));
                    index++;
                    stringBuilder.Append(GetCheckboxDef(item.Field, index, item.ClickFunctionName));

                    index++;
                    break;
            }
        }
        return stringBuilder.ToString();
    }

    private static string GetCustomHeaderString(DatatableColumn column, int index, DatatableSetting setting)
    {
        var customHeaderBuilder = new StringBuilder();
        customHeaderBuilder.Append(@"
<th>
<label></label>
</th>");
        index++;
        customHeaderBuilder.AppendFormat(@"<th>
<label>{0}</label>
", column.HeaderName);
        if (column.HasOwnSearch)
        {
            if (column.Type == ColumnTypeEnum.Enum)
            {
                customHeaderBuilder.AppendFormat(@"
<select data-column=""{0}"">
<option>{1}</option>
{2}
</select>
", index - 1, setting.Header.All, String.Join(',', column.EnumDictionary.Select(c => "<option value=\"" + c.Key + "\">" + c.Value + "</option>")));
            }
            else if (column.Type == ColumnTypeEnum.CheckBox)
            {
                customHeaderBuilder.AppendFormat(@"
<select data-column=""{0}"">
<option value=""null"">{1}</option>
<option value=""false"">{2}</option>
<option value=""true"">{3}</option>
</select>
", index - 1, setting.Header.All, setting.Header.Unchecked, setting.Header.Checked);
            }
            else
            {
                customHeaderBuilder.AppendFormat(@"
<input class=""column-search""  type=""text"" data-column=""{0}"" placeholder=""{1}"" />
", index - 1, "Search In " + column.HeaderName);
            }

        }
        if (column.Sort)
        {
            customHeaderBuilder.AppendFormat(@"
<span class=""sort-box"">
<a class=""asc"" data-column=""{0}"">{1}</a>
<a class=""desc"" data-column=""{0}"">{2}</a>
</span>
", index, setting.Header.OrderAscHtml, setting.Header.OrderDescHtml);
        }
        customHeaderBuilder.Append("</th>");
        return customHeaderBuilder.ToString();
    }

    private static string GetNormalColumnDef(string field, int index)
    {
        return string.Format(@"
                            {{'data':'{0}','targets':{1}}},
", GetNormalizedFieldName(field), index);
    }

    private static string GetEnumDesciptionColumnDef(string field, int index)
    {

        return string.Format(@"
                            {{'data':'{0}','targets':{1},
                            render:function(data)
                                {{
                                    return {0}Map.get(data)
		                        }}
		                    }},
", GetNormalizedFieldName(field), index);
    }

    private static string GetCheckboxDef(string field, int index, string checkBoxClickFunction)
    {
        return string.Format(@"
                            {{'targets': {0},'data': '{1}', 
                            render: function(data, type, row, meta) 
                            {{let checked = data ? 'checked=' : '';return `<input ${{checked}} onclick=""{2}(${{row.id}},this)"" type=""checkbox""  />`}}
                            }},
", index, GetNormalizedFieldName(field), checkBoxClickFunction);
    }

    private static string GetHiddenColumnDef(string field, int index)
    {
        return string.Format(@"
                            {{'data':'{0}','targets':{1},'visible':false}},
", GetNormalizedFieldName(field), index);
    }

    private static string GetCustomHiddenColumnDef(int index)
    {
        return string.Format(@"
                            {{'data':null,'targets':{0},'visible':false}},
", index);
    }

    private static string GetCustomColumnDef(string function, int index)
    {
        return string.Format(@"
                            {{'targets': {0},
			                            data: null,
			                            render: {1}
			                            }},
", index, function);
    }

    private static string GetNormalizedFieldName(string field)
    {
        return char.ToLower(field[0]) + field[1..];
    }


}

```
As you can see there are 4 parameters for this tag helper
|Parameter| Required  | Explanation
|--|--|--|
|TableId  | true | The Html Table Id for assigning to datatable js and creating our customized table|
|PageSize|false| The page size it will be use to show data to users. If not provided the taghelper will use DefaultPageSize from the setting|
|AjaxAddress| true| The Address for the method that will get our **DatatableInput** and returns **DataTableResult** (this class will be covered in Ajax call part)|
|For|true|List of columns created by our **DatatableColumnBuilder**|


And finally, I referenced the tag helper inside my ViewImport.cshtml file like this:
```
@addTagHelper "*, Datatable.Dotnet"
```
Please note that this is the **assembly name(Project name)**, not the namespace.

## Server-Side Pagination, Sorting and Searching.
For the ajax part, I first created a simple generic class for returning paged data. It is a generic class and can be found Here: [DataTableResult](https://github.com/ghaagh/Datatable.Dotnet/blob/master/Datatable.Dotnet/DataTableResult.cs). 



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
**Note**: All the server side methods for pagination should return a JsonResult of type DataTableResult. For example in our ProductViewModel case, we should return a **JsonResult** of **DatatableResult** of **ProductViewModel**.

With all these classes implemented, Remaining is a simple Ajax Method. as I mentioned the input should always be DatatableInput and the response should be JsonResult of DatatableResult. So Here it is.

```
      public async Task<JsonResult> GetPagedProducts(DataTableInput datatableRequest)
    {
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
**Note:** : the my _queryHelper service is using [Dynamic LINQ for .Net Core](https://www.nuget.org/packages/System.Linq.Dynamic.Core/) . you can implement with any other library you want or write your own filter and sort. But for easier implementation it is **highly** recommended to use this library. You are going to love it. I promise!

**Note**: As you can see I only took < PageSize > Records from the Database, Not the whole list. All my service methods for global searching, column searching and ordering are returning just a filtered DbSet not a List. only on ApplyPaginationAsync() at the end, I am getting the records from database.

## Fun Part: Using the codes and seeing the result
OK, Here we go. Lets use the plugin
#### Empty HTML Table Tag
```

        <table id="products" class="display">
        </table>
```
#### Tag Helper Part
This will create a script. So it is recommended to put it in Script section and **below the Datatable.js script and stylesheets.**
```
<datatable-helper for="@Model.Input" ajax-address="/Products/GetPagedProducts" page-size="25" table-id="products"></datatable-helper>
```
#### (Optional) Custom Renderers
because I used some custom columns in my example, I added two functions. One for rendering the button group at the end. One for when user clicks on my **Visible**  checkbox.
```
<script>
    function renderButtons(data, type, row, meta){
       return  `<div class="btn-group btn-group-sm" role="group" aria-label="Basic example"">
             <button class="btn btn-danger btn-sm">Delete</button>
      <button class="btn btn-secondary btn-sm">Another</button>
            <button  class="btn btn-primary btn-sm" onclick="console.log(${row.id})">Edit</button>
    </div>`
    }
    //For checkbox clicking
    function onVisibleClick(row,sender){
        console.log(sender);
        console.log('visibled clicked:'+ row);
    }
</script>
```

And when I ran the project, I saw what I needed to see:

![Final view](https://github.com/ghaagh/Datatable.Dotnet/blob/master/Datatable.Dotnet.PNG)

As you can see, Absolute minimum javascript code even with a lot of custom buttons, different types, enums, etc. and with built-in support of Server side pagination, sort and searching. My journey is over. Any suggestions and pull request in this library is appreciated. as well as hitting the **STAR** button on this repo! you can also fork it, change it, copy it and customize it on your own. Happy coding ;)





