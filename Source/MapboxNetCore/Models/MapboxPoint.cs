using System;
using System.Collections.Generic;
using System.Text;

namespace MapboxNetCore
{
    // Will be converted into GeoJSON
    public class MapboxPoint
    {
        public string GUID { get; set; }

        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public object Properties { get; set; }
    }
}
