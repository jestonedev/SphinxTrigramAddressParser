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
        public string Crn { get; set; }
        public string Tenant { get; set; }
        public string Prescribed { get; set; }
        public string TotalArea { get; set; }
        public string LivingArea { get; set; }

        public string BalanceInput { get; set; }

        public string BalanceTenancy { get; set; }

        public string BalanceDgi { get; set; }
        public string BalancePadun { get; set; }
        public string BalancePkk { get; set; }
        public string BalanceInputPenalties { get; set; }

        public string ChargingTenancy { get; set; }

        public string ChargingTotal { get; set; }

        public string ChargingDgi { get; set; }
        public string ChargingPadun { get; set; }
        public string ChargingPkk { get; set; }
        public string ChargingPenalties { get; set; }

        public string RecalcTenancy { get; set; }

        public string RecalcDgi { get; set; }
        public string RecalcPadun { get; set; }
        public string RecalcPkk { get; set; }
        public string RecalcPenalties { get; set; }

        public string PaymentTenancy { get; set; }

        public string PaymentDgi { get; set; }
        public string PaymentPadun { get; set; }
        public string PaymentPkk { get; set; }
        public string PaymentPenalties { get; set; }

        public string TransferBalance { get; set; }

        public string BalanceOutputTotal { get; set; }

        public string BalanceOutputTenancy { get; set; }

        public string BalanceOutputDgi { get; set; }
        public string BalanceOutputPadun { get; set; }
        public string BalanceOutputPkk { get; set; }

        public string BalanceOutputPenalties { get; set; }
    }
}
