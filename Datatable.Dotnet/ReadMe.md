

# DataTable Js .Net Core Implementation With Server-Side Processing

This Package is an **unofficial** easy to use, .Net Core implementation of [DataTable.js](https://datatables.net/) with built-in support for: Server-Side Pagination, Server-Side Ordering, Server-Side Global and Column Search, Build In DatePicker for Date Columns and Sort.

**Note:** If you need any help on bootstrapping the package, contract me. I'll be happy to help.
**Note:** If you need any more customization please visit the [Github Page For DataTable.Dotnet](https://github.com/ghaagh/Datatable.Dotnet). Any feature and bug-fix pull request is appreciated.

## Configuration
1.  Add neessary Javascript and Style libraries from the [official website](https://datatables.net/) to your web page/ View  or _Layout.cshtml.

2. Depending on what type of .Net project you are using, Add these lines  of code to the end of **.AddMvcControllersWithView()** Or **AddRazorPages()**

For MVC projects:
```
builder.Services.AddRazorPages().AddMvcOptions(options => {
        options.ModelBinderProviders.Insert(0, new DataTableInputBinderProvider());
});
```

For Razor Page Projects:
``` 
builder.Services.AddControllersWithViews().AddMvcOptions(options => {
    options.ModelBinderProviders.Insert(0, new DataTableInputBinderProvider());
});
```

3. Add some setting to your appsettings.json file. 

 
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

4. In your Program.cs, add DatatableSetting to dependency injection like this:
```
    builder.Services.Configure<DatatableSetting>(builder
    .Configuration.GetSection("DatatableSetting"));
```
5. And Finally Add the TagHelper to your _ViewImport.cshtml with the line below. 
```
    @addTagHelper "*, Datatable.Dotnet"
```
**Note:**  "Datatable.Dotnet" is the assembly name, not the namespace.

## Building The Datatable Columns In Controller
The input for TagHelper is an IEnumarable of **Column**. In order to create that, you can use **DatatableColumnBuilder** class.  Suppose we have a complex ViewModel like this:
```
    public class ProductViewModel
    {
        public int Id { get; set; }
        public ProductTypeEnum ProductType { get; set; }
        public string Name { get; set; }
        public DateTime Date { get; set; }
        public string Desciption { get; set; }
        public bool Visible { get; set; }
        public virtual IEnumerable<string> ProductTags { get; set; }
    }
    public enum ProductTypeEnum
{
    Book = 1,
    WritingTools,
    Other
}
```
In your **Get() View Method** or **OnGet() page handler**, you can use the DatatableColumnBuilder like this to create a model:
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
                {(int)ProductTypeEnum.WritingTools,"Writing Tools" },
                {(int) ProductTypeEnum.Other,"Other Tools" }
            })
            .AddDateTimeColumn("Date", nameof(ProductViewModel.Date))
            .AddCheckBoxColumn("Visible in Website", nameof(ProductViewModel.Visible), false, "onVisibleClick")
            .AddCustomColumn("Operations", null, "renderButtons", false, false, false)
            .Build();
```
### Column Properties

|Property| Explanation  |
|--|--|
|**Field**  |the name of the field. for example, if I am getting a list of products from the server, 'ProductName' is the field. this is the field name not its value.  |
|**Sort**|Specifies wether the column has sort buttons or not.|
|**HasOwnSearch**| Specifies whether the column has its own search in the header or not.|
|**ClickFunctionName**|specific to Checkbox type. It can carry a javascript function to be called after a click on the checkbox. for example it can be useful for instant enabling and disabling records.|
|**HeaderName**| the string that will be shown in generated Datatable header for the column.|
|**RenderFunction**|specific to the **Custom** column types. It can be a whole javascript function or just its name.|
|**Disabled**|Specific for **checkbox** column types.|
|**EnumDictionary**|Specific to **Enum** Column types to show a user-friendly enum text instead of numbers or joint strings.|


**Note**: Remember that this part is not connecting to any database, and It is just creating the columns definitions for passing to tag helper.

**Note**: you may not need any custom column or checkbox column. It is just here to show your options on creating the list of columns.

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
### An Ajax Method Example:
```
public async Task<JsonResult> GetPagedRecords(DataTableInput datatableRequest)
    {
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

## Viewing The Results In Page

 1. Add a normal empty table with an ID to your page like this:
```
<table id="products" class="display">
        </table>
```

 2. use this line to initialize. Please note that this tag helper will be converted to a script. So load it after Datatable script and stylesheet (Generally in **Script** section of your page)

```
section Script{
<!--Datatable.js official scripts here-->
<datatable-helper for="@Model.Input" ajax-address="/Products/GetPagedRecords" page-size="25" table-id="products"></datatable-helper>
<!--Your custom Column render functions here-->
}
```

3. If you Added custom columns, Add those definitions at the bottom of the tag-helper.

```
section Script{
<!--Datatable.js official scripts here-->

<datatable-helper for="@Model.Input" ajax-address="/Products/GetPagedRecords" page-size="25" table-id="example"></datatable-helper>

<!--Your custom Column render functions here-->
<script>
    function renderButtons(data, type, row, meta){
       return  `<div class="btn-group btn-group-sm" role="group" aria-label="Basic example"">
             <button class="btn btn-danger btn-sm">Delete</button>
            <button  class="btn btn-primary btn-sm" onclick="console.log(${row.id})">Edit</button>
    </div>`
    }
    
    function onVisibleClick(row,sender){
        console.log(sender);
        console.log('visibled clicked:'+ row);
    }
</script>
}
```
Run the project and See the result. Start customizing the table and buttons.
