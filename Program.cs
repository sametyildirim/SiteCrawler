using CefSharp;
using CefSharp.OffScreen;
using HtmlAgilityPack;
using Newtonsoft.Json;

namespace SiteCrawler
{
    class Program
    {
        private static MyRequestHandler _requestHandler;
        private static int previousRequestNrWhereLoadingFinished = -1;

        private static State nextState = State.Login;
        private static ChromiumWebBrowser browser;
        private const string testUrl = "https://www.cars.com/signin/";

        static List<CarMainInformation> myCarsS = new List<CarMainInformation>();
        static Dictionary<string, string> myCarS = new Dictionary<string, string>();
        static string[] myCarSHomeDelivery;
        static List<CarMainInformation> myCarsX = new List<CarMainInformation>();
        static Dictionary<string, string> myCarX = new Dictionary<string, string>();
        static string[] myCarXHomeDelivery;
        static async Task Main()
        {


            CefSharpSettings.SubprocessExitIfParentProcessClosed = true;

            var settings = new CefSettings()
            {
                CachePath = Path.Combine(Environment.GetFolderPath(
                                         Environment.SpecialFolder.LocalApplicationData), "CefSharp\\Cache")
            };
            Cef.Initialize(settings, performDependencyCheck: true, browserProcessHandler: null);
            browser = new ChromiumWebBrowser(testUrl);
            _requestHandler = new MyRequestHandler();
            browser.RequestHandler = _requestHandler;
            browser.LoadingStateChanged += BrowserLoadingStateChanged;

            while (nextState != State.Finished)
            {
                Thread.Sleep(1000);
            }
            Cef.Shutdown();
            string mycarsS = JsonConvert.SerializeObject(myCarsS);
            var filePath = "c:\\temp\\MyCarsS.json";
            File.WriteAllText(filePath, mycarsS);
            string mycarsX = JsonConvert.SerializeObject(myCarsX);
            filePath = "c:\\temp\\MyCarsX.json";
            File.WriteAllText(filePath, mycarsX);
            filePath = "c:\\temp\\MyCarDetailS.json";
            File.WriteAllText(filePath, JsonConvert.SerializeObject(myCarS));
            filePath = "c:\\temp\\MyCarDetailX.json";
            File.WriteAllText(filePath, JsonConvert.SerializeObject(myCarX));
            filePath = "c:\\temp\\MyCarHomeDeliveryS.json";
            File.WriteAllText(filePath, JsonConvert.SerializeObject(myCarSHomeDelivery));
            filePath = "c:\\temp\\MyCarHomeDeliveryX.json";
            File.WriteAllText(filePath, JsonConvert.SerializeObject(myCarXHomeDelivery));

        }
        private static void BrowserLoadingStateChanged(object sender, LoadingStateChangedEventArgs e)
        {
            if (!e.IsLoading)
            {
                if (previousRequestNrWhereLoadingFinished < _requestHandler.NrOfCalls)
                {
                    previousRequestNrWhereLoadingFinished = _requestHandler.NrOfCalls;

                    switch (nextState)
                    {
                        case State.Login:
                            {
                                var loginScript = @"document.querySelector('#email').value = 'johngerson808@gmail.com';
                               document.querySelector('#password').value = 'test8008';
                               document.querySelector('.session-form').submit();";

                                browser.EvaluateScriptAsync(loginScript).ContinueWith(u =>
                                {
                                    Console.WriteLine("User Logged in.\n");
                                    previousRequestNrWhereLoadingFinished = -1;
                                    nextState = State.Search;
                                });
                                break;
                            }
                        case State.Search:
                            {
                                var pageQueryScript = @"document.querySelector('#make-model-search-stocktype').value = 'used';
                                            document.querySelector('#makes').value = 'tesla';
                                            document.querySelector('#models').value = 'tesla-model_s';
                                            document.querySelector('#make-model-max-price').value = '100000';
                                            document.querySelector('#make-model-maximum-distance').value = 'all';
                                            document.querySelector('#make-model-zip').value = '94596'; 
                                            document.querySelector('.search-form').submit();";

                                var scriptTask = browser.EvaluateScriptAsync(pageQueryScript);
                                scriptTask.ContinueWith(u =>
                                {
                                    Console.WriteLine("User searched\n");
                                    previousRequestNrWhereLoadingFinished = -1;
                                    nextState = State.Gather;

                                });

                                break;
                            }
                        case State.Gather:
                            {
                                var pageQueryScript = @"
                                     (function(){
                                         var lis = document.querySelectorAll('div.vehicle-details');
                                         var result = [];
                                         for(var i=0; i < lis.length; i++) { result.push(lis[i].innerHTML) } 
                                         return result; 
                                     })()";

                                var scriptTask = browser.EvaluateScriptAsync(pageQueryScript);
                                scriptTask.ContinueWith(u =>
                                {

                                    var response = (List<dynamic>)u.Result.Result;
                                    GatherMainInformation(response, myCarsS);
                                    Console.WriteLine("First page gathered\n");

                                    pageQueryScript = @"window.location.href = document.getElementById('pagination-direct-link-2').href;";
                                    browser.EvaluateScriptAsync(pageQueryScript).ContinueWith(u =>
                                    {
                                        Console.WriteLine("Second page redirected.\n");
                                        previousRequestNrWhereLoadingFinished = -1;
                                        nextState = State.GatherSecondPage;
                                    });


                                });

                                break;
                            }
                        case State.GatherSecondPage:
                            {
                                var pageQueryScript = @"
                                     (function(){
                                         var lis = document.querySelectorAll('div.vehicle-details');
                                         var result = [];
                                         for(var i=0; i < lis.length; i++) { result.push(lis[i].innerHTML) } 
                                         return result; 
                                     })()";

                                var scriptTask = browser.EvaluateScriptAsync(pageQueryScript);
                                scriptTask.ContinueWith(u =>
                                {

                                    var response = (List<dynamic>)u.Result.Result;
                                    GatherMainInformation(response, myCarsS);
                                    Console.WriteLine("Second page gathered\n");
                                    previousRequestNrWhereLoadingFinished = -1;
                                    nextState = State.GatherDetails;


                                });
                                pageQueryScript = @"window.location.href  = document.getElementsByClassName('sds-badge--home-delivery')[0].parentNode.parentNode.getElementsByClassName('vehicle-card-link')[0].href";
                                browser.EvaluateScriptAsync(pageQueryScript).ContinueWith(u =>
                                {
                                    Console.WriteLine("detail page redirected.\n");
                                });

                                break;
                            }
                        case State.GatherDetails:
                            {
                                var pageQueryScript = @"
                                     (function(){
                                         var lis = document.querySelectorAll('div.basics-content-wrapper');
                                         var result = [];
                                         for(var i=0; i < lis.length; i++) { result.push(lis[i].innerHTML) } 
                                         return result; 
                                     })()";
                                var scriptTask = browser.EvaluateScriptAsync(pageQueryScript);
                                scriptTask.ContinueWith(u =>
                                {

                                    var response = (List<dynamic>)u.Result.Result;
                                    if (response.Count > 0)
                                    {
                                        myCarS = GatherDetailInformation(response);
                                        Console.WriteLine("Details gathered\n");
                                        nextState = State.ClickX;
                                        previousRequestNrWhereLoadingFinished = -1;
                                        pageQueryScript = @" document.getElementsByClassName('sds-badge--home-delivery')[0].click();
                                                (function(){
                                                     var result = [];
                                                     result.push(document.querySelector('div.price-badge').innerHTML);
                                                     result.push(document.querySelector('div.home_delivery-badge').innerHTML);
                                                     result.push(document.querySelector('div.virtual_appointments-badge').innerHTML);
                                                     return result; 
                                                 })()";


                                        browser.EvaluateScriptAsync(pageQueryScript).ContinueWith(u =>
                                        {
                                            var response = (List<dynamic>)u.Result.Result;
                                            myCarSHomeDelivery = GatherHomeDeliveryInformation(response);

                                        });

                                        browser.EvaluateScriptAsync(" window.location.href = document.referrer;").ContinueWith(u =>
                                        {
                                            Console.WriteLine("results page redirected.\n");

                                        });
                                    }
                                    else
                                    {
                                        browser.EvaluateScriptAsync(" location.reload();").ContinueWith(u =>
                                        {
                                            previousRequestNrWhereLoadingFinished = -1;
                                            Console.WriteLine("detail page reloaded.\n");

                                        });
                                    }



                                });

                                break;
                            }
                        case State.ClickX:
                            {

                                var scriptTask = browser.EvaluateScriptAsync("document.getElementById('model_tesla-model_x').click();");

                                scriptTask.ContinueWith(u =>
                                {

                                    Console.WriteLine("User searched tesla x\n");
                                    previousRequestNrWhereLoadingFinished = -1;
                                    nextState = State.GatherX;
                                });

                                break;
                            }
                        case State.GatherX:
                            {
                                var pageQueryScript = @"
                             (function(){
                                 var lis = document.querySelectorAll('div.vehicle-details');
                                 var result = [];
                                 var title = document.getElementsByClassName('sds-heading--1')[0].innerText;
                                 if(title == 'Used vehicles for sale')
                                  {
                                 for(var i=0; i < lis.length; i++) { result.push(lis[i].innerHTML) } 
                                   }
                                 return result; 
                             })()";
                                var scriptTask = browser.EvaluateScriptAsync(pageQueryScript);
                                scriptTask.ContinueWith(u =>
                                {

                                    if (u.Result.Result != null)
                                    {

                                        var response = (List<dynamic>)u.Result.Result;
                                        if (response.Count > 0)
                                        {
                                            nextState = State.GatherXSecondPage;
                                            GatherMainInformation(response, myCarsX);
                                            Console.WriteLine("first page gathered tesla x \n");
                                            pageQueryScript = @"window.location.href = document.getElementById('pagination-direct-link-2').href;";
                                            browser.EvaluateScriptAsync(pageQueryScript).ContinueWith(u =>
                                            {
                                                Console.WriteLine("Second page redirected.\n");
                                                previousRequestNrWhereLoadingFinished = -1;
                                            });



                                        }
                                        else
                                        {
                                            var scriptTask = browser.EvaluateScriptAsync("document.getElementById('model_tesla-model_x').click();");

                                            scriptTask.ContinueWith(u =>
                                            {

                                                Console.WriteLine("User cliecked tesla x\n");
                                                previousRequestNrWhereLoadingFinished = -1;
                                            });
                                        }
                                    }




                                });


                                break;
                            }
                        case State.GatherXSecondPage:
                            {
                                var pageQueryScript = @"
                             (function(){
                                 var lis = document.querySelectorAll('div.vehicle-details');
                                 var result = [];
                                 var title = document.getElementsByClassName('sds-heading--1')[0].innerText;
                                 if(title == 'Used vehicles for sale')
                                  {
                                 for(var i=0; i < lis.length; i++) { result.push(lis[i].innerHTML) } 
                                   }
                                 return result; 
                             })()";
                                var scriptTask = browser.EvaluateScriptAsync(pageQueryScript);
                                scriptTask.ContinueWith(u =>
                                {

                                    if (u.Result.Result != null)
                                    {

                                        var response = (List<dynamic>)u.Result.Result;
                                        if (response.Count > 0)
                                        {
                                            nextState = State.GatherDetailsX;
                                            GatherMainInformation(response, myCarsX);
                                            previousRequestNrWhereLoadingFinished = -1;
                                            pageQueryScript = @"window.location.href  = document.getElementsByClassName('sds-badge--home-delivery')[0].parentNode.parentNode.getElementsByClassName('vehicle-card-link')[0].href";
                                            browser.EvaluateScriptAsync(pageQueryScript).ContinueWith(u =>
                                            {
                                                Console.WriteLine("Detail page redirected.\n");
                                            });

                                        }
                                        else
                                        {
                                            var scriptTask = browser.EvaluateScriptAsync("document.getElementById('model_tesla-model_x').click();");

                                            scriptTask.ContinueWith(u =>
                                            {

                                                Console.WriteLine("User clicked tesla x\n");
                                                previousRequestNrWhereLoadingFinished = -1;
                                            });
                                        }
                                    }




                                });
                                break;

                            }
                        case State.GatherDetailsX:
                            {
                                var pageQueryScript = @"
                                     (function(){
                                         var lis = document.querySelectorAll('div.basics-content-wrapper');
                                         var result = [];
                                         for(var i=0; i < lis.length; i++) { result.push(lis[i].innerHTML) } 
                                         return result; 
                                     })()";
                                var scriptTask = browser.EvaluateScriptAsync(pageQueryScript);
                                scriptTask.ContinueWith(u =>
                                {

                                    var response = (List<dynamic>)u.Result.Result;
                                    if (response.Count > 0)
                                    {
                                        myCarX = GatherDetailInformation(response);
                                        Console.WriteLine("Details gathered x\n");
                                        nextState = State.Finished;
                                        previousRequestNrWhereLoadingFinished = -1;
                                        pageQueryScript = @" document.getElementsByClassName('sds-badge--home-delivery')[0].click();
                                                (function(){
                                                     var result = [];
                                                     result.push(document.querySelector('div.price-badge').innerHTML);
                                                     result.push(document.querySelector('div.home_delivery-badge').innerHTML);
                                                     result.push(document.querySelector('div.virtual_appointments-badge').innerHTML);
                                                     return result; 
                                                 })()";


                                        browser.EvaluateScriptAsync(pageQueryScript).ContinueWith(u =>
                                        {
                                            var response = (List<dynamic>)u.Result.Result;
                                            myCarXHomeDelivery = GatherHomeDeliveryInformation(response);

                                        });

                                    }
                                    else
                                    {
                                        browser.EvaluateScriptAsync(" location.reload();").ContinueWith(u =>
                                        {
                                            previousRequestNrWhereLoadingFinished = -1;
                                            Console.WriteLine("detail page reloaded.\n");

                                        });
                                    }



                                });

                                break;

                            }
                        default: break;
                    }

                }
            }
        }
        private static void GatherMainInformation(List<dynamic> response, List<CarMainInformation> myCars)
        {
            var htmlDoc = new HtmlDocument();
            foreach (string v in response)
            {
                htmlDoc.LoadHtml(v);
                string title = htmlDoc.DocumentNode.SelectSingleNode("//h2[@class='title']").InnerText;
                string mileage = htmlDoc.DocumentNode.SelectSingleNode("//div[@class='mileage']").InnerText;
                string primaryPrice = htmlDoc.DocumentNode.SelectSingleNode("//span[@class='primary-price']").InnerText;
                string dealerName = htmlDoc.DocumentNode.SelectSingleNode("//div[@class='dealer-name']").InnerText;
                myCars.Add(new CarMainInformation { Title = title, MileAge = mileage, PrimaryPrice = primaryPrice, DealerName = dealerName });
            }
        }
        private static Dictionary<string, string> GatherDetailInformation(List<dynamic> response)
        {
            var htmlDoc = new HtmlDocument();

            htmlDoc.LoadHtml(response[0]);
            HtmlNode node = htmlDoc.DocumentNode;

            HtmlNodeCollection ddt = node.SelectNodes("//dt"); //Select all dt tags
            HtmlNodeCollection dds = node.SelectNodes("//dd"); //Select all dd tags

            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            for (int i = 0; i < ddt.Count; i++)
            {
                string header = ddt[i].InnerText;
                string value = dds[i].InnerText;

                dictionary.Add(header, value);


            }


            return dictionary;

        }
        private static string[] GatherHomeDeliveryInformation(List<dynamic> response)
        {
            int count = response.Count();
            string[] ret = new string[response.Count()];
            var htmlDoc = new HtmlDocument();
            for (int i = 0; i < response.Count; i++)
            {
                htmlDoc.LoadHtml((string)response[i]);
                ret[i] = htmlDoc.DocumentNode.SelectSingleNode("//p[@class='badge-description']").InnerText;

            }
            return ret;

        }
    }
}
