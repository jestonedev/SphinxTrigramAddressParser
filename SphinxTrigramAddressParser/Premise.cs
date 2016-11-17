using System.Collections.Generic;

namespace SphinxTrigramAddressParser
{
    public class Premise
    {
        public string RawAddress { get; set; }
        public List<int?> IdPremisesList { get; set; }  // Finded duplicates premises
        public int? IdPremisesValid { get; set; }
        public string Street { get; set; }
        public string House { get; set; }
        public string PremiseNumber { get; set; }
        public List<SubPremise> SubPremises { get; set; }
        public string Description { get; set; }
        public string Account { get; set; }
        public string CRN { get; set; }
        public string Tenant { get; set; }
        public string Prescribed { get; set; }
        public string TotalArea { get; set; }
        public string LivingArea { get; set; }

        public string BalanceInput { get; set; }

        public string BalanceTenancy { get; set; }

        public string BalanceDGI { get; set; }
        public string BalanceInputPenalties { get; set; }

        public string ChargingTenancy { get; set; }

        public string ChargingTotal { get; set; }

        public string ChargingDGI { get; set; }
        public string ChargingPenalties { get; set; }

        public string RecalcTenancy { get; set; }

        public string RecalcDGI { get; set; }
        public string RecalcPenalties { get; set; }

        public string PaymentTenancy { get; set; }

        public string PaymentDGI { get; set; }
        public string PaymentPenalties { get; set; }

        public string TransferBalance { get; set; }

        public string BalanceOutputTotal { get; set; }

        public string BalanceOutputTenancy { get; set; }

        public string BalanceOutputDGI { get; set; }

        public string BalanceOutputPenalties { get; set; }
    }
}
