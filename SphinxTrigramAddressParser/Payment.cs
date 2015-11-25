using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SphinxTrigramAddressParser
{
    class Payment
    {
        public string Account { get; set; }
        public string RawAddress { get; set; }
        public string TrigrammAddress { get; set; }
        public string House { get; set; }
        public string Premise { get; set; }
        public string SubPremise { get; set; }
        public bool HasZSymbol { get; set; }
        public int? IdPremises { get; set; }
    }
}
