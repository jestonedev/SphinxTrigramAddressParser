using System.Collections.Generic;

namespace SphinxTrigramAddressParser
{
    internal class PremiseRaw
    {
        public string RawAddress { get; set; }
        public string Account { get; set; }
        public string CRN { get; set; }
        public string Tenant { get; set; }
        public string RawType { get; set; }

        public string Prescribed { get; set; }

        public string BalanceInput { get; set; }

        public string TotalBalance { get; set; }

        public string DebetDGI { get; set; }

        public string BalanceOutput { get; set; }

        public string DzMpDgiRso { get; set; }

        public string TotalArea { get; set; }

        public string LivingArea { get; set; }

        public string BalanceTenancy { get; set; }

        public string Penalties { get; set; }
    }
}
