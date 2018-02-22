using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Newtonsoft.Json;

namespace LiveCam.Droid
{
    public class RecievedJson
    {
        //public Guid Id { get; set; }
        //public int Id { get; set; }

        [JsonProperty(PropertyName = "time")]
        public string time { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string name { get; set; }
        
        [JsonProperty(PropertyName = "roi")]
        public List<int> roi { get; set; }
    }
}