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
