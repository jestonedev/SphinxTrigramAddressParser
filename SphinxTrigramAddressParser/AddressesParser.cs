using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SphinxTrigramAddressParser
{
    internal class AddressesParser
    {
        public List<List<List<Premise>>> PreparedPremises { get; set; }
        public List<List<List<Premise>>> InvalidPremises { get; set; }

        public AddressesParser(IEnumerable<Premise> rawPremises, Logger logger)
        {
            PreparedPremises = new List<List<List<Premise>>>();
            InvalidPremises = new List<List<List<Premise>>>();
            foreach (var premisesVariants in rawPremises.Where(premise => premise.RawAddress != null).Select(ParseRawAddress))
            {
                if (!AddressHelper.IsValidAddress(premisesVariants))
                {
                    InvalidPremises.Add(premisesVariants);
                    logger.Write(string.Format("Invalid address `{0}`", premisesVariants[0][0].RawAddress), MsgType.ErrorMsg);
                    continue;
                }
                PreparedPremises.Add(premisesVariants);
                logger.Write(string.Format("Prepared address `{0}`", premisesVariants[0][0].RawAddress), MsgType.InformationMsg);
            }
        }

        private static List<List<Premise>> ParseRawAddress(Premise premiseAddress)
        {
            const string regExpression = @"^(?:ул\.?[ ]+|пер\.?[ ]+|пр-кт\.?[ ]+|бул\.?[ ]+|б-р\.?[ ]+|проезд\.?[ ]+)?(.*?)(?:[ ]+ул\.?|[ ]+пер\.?|[ ]+пр-кт\.?|[ ]+бул\.?|[ ]+б-р\.?|[ ]+проезд\.?)?(?:[ ]*,|,?[ ]*дом\.?|,?[ ]*д\.?)[ ]*([0-9]+[ ""\-]*[а-яА-Я]?[ ""\-]*(?:[ ]*[\/\\][ ]*[0-9]+[ ""\-]*[а-яА-Я]?[ ""\-]*)?).*?(?:,?[ ]*квартира\.?|,?[ ]*кв\.?[ ]*комн\.?|,?[ ]*кв\.?[ ]*ком\.?|,?[ ]*кв\.?[ ]*пом\.?|,?[ ]*кв\.?[ ]*к\.?|,?[ ]*кв\.?)[ ]*([0-9]+[ ]*[а-дА-Д]?(?:[ ]*(?:[ ]*-[ ]*|[ ]*,[ ]*|,?[ ]*квартира\.?|,?[ ]*кв\.?[ ]*комн\.?|,?[ ]*кв\.?[ ]*ком\.?|,?[ ]*кв\.?[ ]*пом\.?|,?[ ]*кв\.?[ ]*к\.?|,?[ ]*кв\.?)[ ]*(?:[0-9]+[ ]*[а-дА-Д]?))*).*?((?:\/|\\|,?[ ]*комната\.?|,?[ ]*ком\.?|,?[ ]*км\.?|,?[ ]*к\.?)[ ]*(?:[0-9а-яА-Я]+(?:[ ]*(?:,|,?[ ]*комната\.?|,?[ ]*ком\.?|,?[ ]*км\.?|,?[ ]*к\.?)[ ]*[0-9а-яА-Я]+)*))?[ ]*[з]?[ ]*\.?$";
            var matches = Regex.Match(premiseAddress.RawAddress.ToLower(), regExpression);
            if (matches.Length < 4)
            {
                var copyPremise = PartialCopyPremise(premiseAddress);
                copyPremise.Description = "Invalid parse address";
                var emptyAddress = new List<List<Premise>>
                {
                    new List<Premise> {copyPremise}
                };
                return new List<List<Premise>>(emptyAddress);
            }
            var sleshedRooms = false;
            var street = matches.Groups[1].Value;
            var house = AddressHelper.NormalizeHouse(matches.Groups[2].Value);
            var premises = AddressHelper.NormalizePremises(matches.Groups[3].Value);
            string subPremises = null;
            if (matches.Length > 4)
            {
                subPremises = AddressHelper.NormalizeSubPremises(matches.Groups[4].Value);
                subPremises = subPremises.Trim(',');
                if (subPremises.StartsWith("/"))
                {
                    sleshedRooms = true;
                    subPremises = subPremises.Trim('/');
                }
            }
            var addresses = new List<List<Premise>>();
            if (string.IsNullOrEmpty(subPremises))
            {
                var newAddress = PartialCopyPremise(premiseAddress);
                newAddress.SubPremises = new List<SubPremise>();
                newAddress.PremiseNumber = premises;
                newAddress.House = house;
                newAddress.Street = street;
                addresses.Add(new List<Premise> { newAddress });
                var premisesArray = premises.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (premisesArray.Length <= 1) return addresses;
                var newAddressesList = new List<Premise>();
                foreach (var premise in premisesArray)
                {
                    newAddress = PartialCopyPremise(premiseAddress);
                    newAddress.SubPremises = new List<SubPremise>();
                    newAddress.PremiseNumber = premise;
                    newAddress.House = house;
                    newAddress.Street = street;
                    newAddressesList.Add(newAddress);
                }
                addresses.Add(newAddressesList);
            }
            else
            {
                var subPremisesList = subPremises.Split(new[] { ',' }, 2);
                if (sleshedRooms)
                {
                    var newConcatedPremiseSubpremise = PartialCopyPremise(premiseAddress);
                    newConcatedPremiseSubpremise.Street = street;
                    newConcatedPremiseSubpremise.House = house;
                    newConcatedPremiseSubpremise.PremiseNumber = premises + "," + subPremisesList[0];
                    newConcatedPremiseSubpremise.SubPremises = subPremisesList.Count() == 2
                        ? subPremisesList[1].Split(',').
                            Select(subPremise => new SubPremise {SubPremiseNumber = subPremise}).ToList()
                        : new List<SubPremise>();
                    addresses.Add(new List<Premise> { newConcatedPremiseSubpremise });
                }
                var newAddress = PartialCopyPremise(premiseAddress);
                newAddress.Street = street;
                newAddress.House = house;
                newAddress.PremiseNumber = premises;
                newAddress.SubPremises = new List<SubPremise>();
                var subPremisesArray = subPremises.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var subPremise in subPremisesArray)
                    newAddress.SubPremises.Add(new SubPremise { SubPremiseNumber = subPremise });
                addresses.Add(new List<Premise> { newAddress });
                if (sleshedRooms && (subPremisesList.Length == 1))
                {
                    var premisesArray = (premises + "," + subPremisesList[0]).
                        Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    var newAddressesList = new List<Premise>();
                    foreach (var premise in premisesArray)
                    {
                        newAddress = PartialCopyPremise(premiseAddress);
                        newAddress.Street = street;
                        newAddress.House = house;
                        newAddress.PremiseNumber = premise;
                        newAddress.SubPremises = new List<SubPremise>();
                        newAddressesList.Add(newAddress);
                    }
                    addresses.Add(newAddressesList);
                }
            }
            return addresses;
        }

        private static Premise PartialCopyPremise(Premise premise)
        {
            return new Premise
            {
                RawAddress = premise.RawAddress,
                Account = premise.Account,
                CRN = premise.CRN,
                Tenant = premise.Tenant,
                BalanceTenancy = premise.BalanceTenancy,
                TotalArea = premise.TotalArea,
                LivingArea = premise.LivingArea,
                Prescribed = premise.Prescribed,
                ChargingTenancy = premise.ChargingTenancy,
                BalanceDGI = premise.BalanceDGI,
                BalanceInput = premise.BalanceInput,
                BalanceInputPenalties = premise.BalanceInputPenalties,
                TransferBalance = premise.TransferBalance,
                ChargingDGI = premise.ChargingDGI,
                ChargingTotal = premise.ChargingTotal,
                ChargingPenalties = premise.ChargingPenalties,
                BalanceOutputTenancy = premise.BalanceOutputTenancy,
                PaymentDGI = premise.PaymentDGI,
                PaymentTenancy = premise.PaymentTenancy,
                PaymentPenalties = premise.PaymentPenalties,
                RecalcTenancy = premise.RecalcTenancy,
                RecalcDGI = premise.RecalcDGI,
                RecalcPenalties = premise.RecalcPenalties,
                BalanceOutputTotal = premise.BalanceOutputTotal,
                BalanceOutputDGI = premise.BalanceOutputDGI,
                BalanceOutputPenalties = premise.BalanceOutputPenalties
            };
        }
    }
}
