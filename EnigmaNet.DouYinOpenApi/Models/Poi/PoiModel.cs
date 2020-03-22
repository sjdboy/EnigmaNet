using System;
using System.Collections.Generic;
using System.Text;

namespace EnigmaNet.DouYinOpenApi.Models.Poi
{
    public class PoiModel
    {
        public string PoiId { get; set; }
        public string PoiName { get; set; }
        public string Location { get; set; }
        public string Country { get; set; }
        public string CountryCode { get; set; }
        public string Province { get; set; }
        public string City { get; set; }
        public string CityCode { get; set; }
        public string District { get; set; }
        public string Address { get; set; }
    }
}
