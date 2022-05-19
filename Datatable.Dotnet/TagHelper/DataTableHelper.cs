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
        stringBuilder.AppendFormat("let table = document.getElementById('{0}')\n", tableId);
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
                case ColumnTypeEnum.Number:
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
                case ColumnTypeEnum.Number:
                    stringBuilder.Append(GetNumberColumnDef(item.Field, item.Format, index));
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
", index - 1, string.Format(setting.Header.OwnSearch, column.HeaderName));
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
    private static string GetNumberColumnDef(string field,NumberFormat format, int index)
    {
        return string.Format(@"
                            {{'data':'{0}','targets':{1}, render: $.fn.dataTable.render.number('{2}', '{3}', {4}, '') }},
", GetNormalizedFieldName(field), index,format.ThousandSeparator,format.DecimalPoint,format.MaxDecimal);
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
