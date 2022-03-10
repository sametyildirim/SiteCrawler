using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SiteCrawler
{
    class CarMainInformation
    {
        public string Title { get; set; }
        public string MileAge { get; set; }
        public string PrimaryPrice { get; set; }
        public string DealerName { get; set; }
    }

    enum State
    {
        Login,
        Search,
        Gather,
        GatherSecondPage,
        GatherDetails,
        GatherHomeDelivery,
        ClickX,
        GatherX,
        GatherXSecondPage,
        GatherDetailsX,
        GatherHomeDeliveryX,
        Finished
    }
}
