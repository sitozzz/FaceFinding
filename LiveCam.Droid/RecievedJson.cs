﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace LiveCam.Droid
{
    public class RecievedJson
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Roi { get; set; }
    }
}