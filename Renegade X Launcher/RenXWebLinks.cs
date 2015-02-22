using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


    public static class RenXWebLinks
    {
        public static readonly string[] RENX_SERVER_INFO_BREAK_SYMBOL = new string[1] {"<@>"};
        public static readonly char RENX_SERVER_INFO_SPACER_SYMBOL = '~';
        public static readonly char RENX_SERVER_SETTING_SPACE_SYMBOL = ';';
        public const string RENX_SERVER_PAGES_URL = "http://renegadexgs.appspot.com/GetServerPages.jsp";
        public const string RENX_ACTIVE_SERVERS_LIST_URL = "http://renegadexgs.appspot.com/browser_3.jsp?view=false";
        public const string RENX_ACTIVE_SERVER_COUNT_URL = "http://renegadexgs.appspot.com/browser_3.jsp?view=true";
        public const string RENX_ACTIVE_SERVER_JSON_URL = "http://renegadexgs.appspot.com/servers.jsp";
        public const string RENX_BANNERS_JSON_URL = "http://renegade-x.com/launcher_data/banners";
        public const string IP_PROVIDER_URL = "http://renegadexgs.appspot.com/getip.jsp";
    }

