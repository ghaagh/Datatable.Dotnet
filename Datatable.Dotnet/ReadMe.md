






# DataTable.Dotnet

This Package is an **unofficial** easy to use, .Net Core implementation of [DataTable.js](https://datatables.net/)
## Features

 - Strongly-Typed script creation based on your viewmodel.
 - Support of **[Display(Name="")]** annotation for headers.
 - Support of AJAX and server-side pagination.
 - Support of column searching and sorting plus global search.
 - Customizable for other languages.
 - Support of DatePicker plugin for filtering the date fields.
 - Support of Formating Numbers.
 - Support of custom columns.

**Note:** Please report any possible bug or future request via Github or my mail.

## Configuration
1.  Add neessary Javascript and Style libraries from the [official website](https://datatables.net/) to your web page/ View  or _Layout.cshtml.

2. Depending on what type of .Net project you are using, Add these lines  of code to the end of **.AddMvcControllersWithView()** Or **AddRazorPages()**

For Razor Page Projects:
```c#
builder.Services.AddRazorPages().AddMvcOptions(options => {
        options.ModelBinderProviders.Insert(0, new DataTableInputBinderProvider());
});
```

For MVC projects:
```c#
builder.Services.AddControllersWithViews().AddMvcOptions(options => {
    options.ModelBinderProviders.Insert(0, new DataTableInputBinderProvider());
});
```

3. Add this line to your Program.cs to add Datatable.Dotnet services to your dependency injection provider:

```c#
builder.Services.AddDatatable(builder.Configuration.GetSection("DatatableSetting"));
```
4. Add some setting to your appsettings.json file. 

 
```js

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
  }
```

|Setting Field | Explanation|
|-|-|
|**Language**| The whole object is a mapped object from Datatable.Js language JSON. Extra information can be found on [Datatable.Net Languages](https://datatables.net/examples/basic_init/language.html) v.|
|**DefaultPageSize**| When the Page size in our code is not specified, our tag helper will use this parameter as a default value.|
|**Header.OrderAscHtml** and **Header.OrderDescHtml**| For customizing the **asc** and **desc** button on the header for the field that have **Sort** enabled|
|**All**| This text will be replaced in **Enum** and **Checkbox** Column types as a default filter for all results.|
|**DateColumnPluginCall**| This line will be the place for calling your desired **DatePicker** plugin. If you don't want that. Empty the string|
|**Checked** and **Unchecked**| Specific to **Checkbox** column type and for creating search Filter with dropdown. Note that the Filter will also contain "Header.All" |



## Building the Script
You can use the injected IDatatableBuilder< Your-View-Model> in your controller. For example for a viewModel like this:
```c#
       public class ProductViewModel{
        [Display(Name="Id")]
        public int Id { get; set; }
        [Display(Name="Product Type")]
        public ProductTypeEnum ProductType { get; set; }
        [Display(Name="Product Name")]
        public string Name { get; set; }
        [Display(Name="Date")]
        public DateTime Date { get; set; }
        public string Desciption { get; set; }
        public bool Visible { get; set; }
        [Display(Name="Product Tags")]
        public virtual IEnumerable<string> ProductTags { get; set; }
        }
    public enum ProductTypeEnum
{
    Book = 1,
    WritingTools,
    Other
}
```
You can inject the IDatatableBuilder to your controller/razor page like this:
```c#
    private readonly IDatatableBuilder<ProductViewModel> _tableBuilder;
    public IndexModel(IDatatableBuilder<ProductViewModel> tableBuilder)
    {
        _tableBuilder = tableBuilder;
    }
```
 Now, use the injected service to create a datatable script.

### Example
```c#
        var productTypeEnumDictionary = new Dictionary<int, string>
            {
                {(int)ProductTypeEnum.Book,"Books" },
                {(int)ProductTypeEnum.Tools,"Writing tools" },
                {(int) ProductTypeEnum.Other,"Other tools" }
            };
        ViewData["exampleScript"] = _tableBuilder
            .AddColumn(column => column.ForMember(c => c.Id).WithDefaultHeader().AsInt().WithDefaultFormat())
            .AddColumn(column => column.ForMember(c => c.Name).WithDefaultHeader().AsString())
            .AddColumn(column => column.ForMember(c => c.Desciption).WithDefaultHeader().AsString())
            .AddColumn(column => column.ForMember(c => c.ProductTags).WithDefaultHeader().AsString())
            .AddColumn(column => column.ForMember(c => c.ProductType).WithDefaultHeader().AsEnum().WithDictionary(productTypeEnumDictionary))
            .AddColumn(column => column.ForMember(c => c.Date).WithDefaultHeader().AsDate())
            .AddColumn(column => column.ForMember(c => c.Visible).WithDefaultHeader().AsCheckbox().WithClickFunction("onVisibleClick"))
            .AddColumn(column => column.ForNone().WithHeader("Operation").AsCustom().WithRender("renderButtons"))
            .BuildAjaxTable(tableId: "example", ajaxAddress: "./Index?handler=PagedRecords", pageSize: 25);
```
## Ajax Method
  Datatable.Dotnet will call the ajax method that is provided to it. Here is the things you should keep in mind.
  
|Requirement|Description|
|--|--|
|**Http Method**| The request is always a **GET** |
|**Input**|The type of the input is always **DataTableInput**. The name of the input does not matter|
|**Return Type** | Return type is a **JsonResult** of the type  **DataTableResult**. For example if you want to create datatable to show a list of ProductViewModel, you must return new **JsonResult** of **DataTableResult\<ProductViewModel\>** in your ajax method. |

  

**Notes:**
 - For Adding dynamic filter and order to my query, [Dynamic Linq](https://www.nuget.org/packages/System.Linq.Dynamic.Core/) is a good option.
 - After applying sort and search we are calling ApplyPaginationAsync() which is connecting to database. Use this pattern for minimizing the amount of data returned from the database.
### Example:
```c#
public async Task<JsonResult> GetPagedRecords(DataTableInput datatableRequest)
    {
        var records = _db.Products
            .Include(c => c.ProductTags)
            .ThenInclude(c => c.Tag)
            .AsQueryable();

        //Get the total count before filtering. it is needed by datatable
        var totalRecords = await records.CountAsync(); ;
        string[] globalSearchColumns = new string[5] { "Id", "Name", "Description", "ProductTags.Tag", "Visible" };
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
## Loading the table on Page/View
### Simple call
```html
<table id="example" class="display">
</table>
<script>
	@Html.Raw(ViewData["exampleScript"])
</script>
```
If you have custom rendering, or checkbox with click function, it would be like:
```html
<table id="example" class="display">
</table>
<script>
	@Html.Raw(ViewData["exampleScript"])
</script>
<script>
    function renderButtons(data, type, row, meta){
       return  `<div class="btn-group btn-group-sm" role="group" aria-label="Basic example"">
             <button class="btn btn-danger btn-sm">Delete</button>
      <button class="btn btn-secondary btn-sm">Something else</button>
            <button  class="btn btn-primary btn-sm" onclick="console.log(${row.id})">Edit</button>
    </div>`
    }
    function onVisibleClick(row,sender){
        console.log(sender);
        console.log('visibled clicked:'+ row);
    }
</script>
```
 Now, run the project and see the result.
