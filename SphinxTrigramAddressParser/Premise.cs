using System.Collections.Generic;

namespace SphinxTrigramAddressParser
{
    internal class Premise
    {
        public string RawAddress { get; set; }
        public List<int?> IdPremisesList { get; set; }  // Finded duplicates premises
        public int? IdPremisesValid { get; set; }
        public string Street { get; set; }
        public string House { get; set; }
        public string PremiseNumber { get; set; }
        public List<SubPremise> SubPremises { get; set; }
        public string Account { get; set; }
        public string Description { get; set; }
    }
}
