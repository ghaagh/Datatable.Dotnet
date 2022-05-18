# DataTable.Dotnet

Hello! This Package is an **unofficial** easy to use implementation of [DataTable.js](https://datatables.net/) with built-in support for .net core server side pagination, ordering and searching.
## How to Configure
1.  Add neessary Javascript and Style libraries from the official website to your web page/ View  or _layout.cshtml.
2. Depending on what type of .Net project you are using, Add this lines  of code to the end of **.AddMvcControllersWithView()** Or **AddRazorPages()**
> builder.Services.AddRazorPages().AddMvcOptions(options => {
        options.ModelBinderProviders.Insert(1, new DataTableInputBinderProvider());
});


> builder.Services.AddControllersWithViews().AddMvcOptions(options => {
    options.ModelBinderProviders.Insert(1, new DataTableInputBinderProvider());
});

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
4. Configure the setting in dependency injection.

    builder.Services.Configure<DatatableSetting>(builder
    .Configuration.GetSection("DatatableSetting"));

5. And Finally Add the TagHelper to your _ViewImport.cshtml with this line

    @addTagHelper "*, Datatable.Dotnet"

## How to use
1. The input for TagHelper is an IEnumarable of **Column**. In order to create that, you can use **ColumnBuilder** like the code bellow. Remember that this part is not connecting to anydatabase and It is just creating the columns definitions for passing to tag helper.
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
You should change **ProductViewModel** and **ProductTypeEnum**  to whatever you are returning from to the datatable as a response. Obviously, you may not need to add all of the column types and this is a complete example containing different possible types.

 2. Add Your Pager Ajax Method to a controller or a page. As long as the input and the output remains the same it does not matter. Here is a simple example of the ajax Method.
Note that:
 - The input type is always **DataTableInput**. 
 - The output type is always a **JsonResult** of a generic class: **DataTableResult**.
 - For Adding dynamic filter and order to my query, **Dynamic Linq** is a good option.
 - Notice that after doing every little shrinking and ordering the returned data, then we call **ToListAsync()**.
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
3. Add a normal empty table with an ID to your page and then add this line to your script section. Please note that this tag helper is adding a script to your page.

```
<datatable-helper for="@Model.Input" ajax-address="/Products/GetPagedRecords" page-size="25" table-id="example"></datatable-helper>
```

4. That's it! But there are some cases that you used custom column or checkbox function colums. In these cases, remember to add their javascript function to your page. for the **Column Definition** I used above. Here is the remaining code for creating custom buttons and adding a function to checkbox.

```
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
```
**Now after runing your project, you should see the paged data.
If you need any more customization please visit the github page and use the source to add more features of your own.**