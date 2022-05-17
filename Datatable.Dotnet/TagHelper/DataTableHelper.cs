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
