using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlackAPI
{

    [RequestPath("rtm.connect")]
    public class RtmStartResponse : Response
    {
        public string url;
    }
}
