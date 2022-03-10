using CefSharp;
using CefSharp.Handler;

namespace SiteCrawler
{
    class MyRequestHandler : RequestHandler
    {
        public int NrOfCalls { get; set; }


        public bool OnBeforeBrowse(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, bool userGesture, bool isRedirect)
        {
            NrOfCalls++;
            return false;
        }


    }
}
