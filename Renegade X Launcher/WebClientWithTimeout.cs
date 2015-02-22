using System;
using System.Net;

namespace LauncherTwo
{
    public class WebClientWithTimeout : WebClient
    {
        /// <summary>
        /// Time in milliseconds
        /// </summary>
        public int Timeout { get; set; }

        public WebClientWithTimeout() : this(60000) { }

        public WebClientWithTimeout(int timeout) : base()
        {
            this.Timeout = timeout;
        }

        protected override WebRequest GetWebRequest(Uri address)
        {
            var request = base.GetWebRequest(address);
            if (request != null)
            {
                request.Timeout = this.Timeout;
            }
            return request;
        }
    }
}
