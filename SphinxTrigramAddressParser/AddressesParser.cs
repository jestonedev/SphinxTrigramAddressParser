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
                var emptyAddress = new List<List<Premise>>
                {
                    new List<Premise> {new Premise {
                        RawAddress = premiseAddress.RawAddress, 
                        Account = premiseAddress.Account,
                        Description = "Invalid parse address"}}
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
                var newAddress = new Premise
                {
                    RawAddress = premiseAddress.RawAddress,
                    Account = premiseAddress.Account,
                    Street = street,
                    House = house,
                    PremiseNumber = premises,
                    SubPremises = new List<SubPremise>()
                };
                addresses.Add(new List<Premise> { newAddress });
                var premisesArray = premises.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (premisesArray.Length <= 1) return addresses;
                var newAddressesList = new List<Premise>();
                foreach (var premise in premisesArray)
                {
                    newAddress = new Premise
                    {
                        RawAddress = premiseAddress.RawAddress,
                        Account = premiseAddress.Account,
                        Street = street,
                        House = house,
                        PremiseNumber = premise,
                        SubPremises = new List<SubPremise>()
                    };
                    newAddressesList.Add(newAddress);
                }
                addresses.Add(newAddressesList);
            }
            else
            {
                var subPremisesList = subPremises.Split(new[] { ',' }, 2);
                if (sleshedRooms)
                {
                    var newConcatedPremiseSubpremise = new Premise
                    {
                        RawAddress = premiseAddress.RawAddress,
                        Account = premiseAddress.Account,
                        Street = street,
                        House = house,
                        PremiseNumber = premises + "," + subPremisesList[0],
                        SubPremises = subPremisesList.Count() == 2 ? subPremisesList[1].Split(',').
                            Select(subPremise => new SubPremise { SubPremiseNumber = subPremise }).ToList() : new List<SubPremise>()
                    };
                    addresses.Add(new List<Premise> { newConcatedPremiseSubpremise });
                }
                var newAddress = new Premise
                {
                    RawAddress = premiseAddress.RawAddress,
                    Account = premiseAddress.Account,
                    Street = street,
                    House = house,
                    PremiseNumber = premises,
                    SubPremises = new List<SubPremise>()
                };
                var subPremisesArray = subPremises.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var subPremise in subPremisesArray)
                    newAddress.SubPremises.Add(new SubPremise { SubPremiseNumber = subPremise });
                addresses.Add(new List<Premise> { newAddress });
                if (sleshedRooms && (subPremisesList.Count() == 1))
                {
                    var premisesArray = (premises + "," + subPremisesList[0]).
                        Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    var newAddressesList = new List<Premise>();
                    foreach (var premise in premisesArray)
                    {
                        newAddress = new Premise
                        {
                            RawAddress = premiseAddress.RawAddress,
                            Account = premiseAddress.Account,
                            Street = street,
                            House = house,
                            PremiseNumber = premise,
                            SubPremises = new List<SubPremise>()
                        };
                        newAddressesList.Add(newAddress);
                    }
                    addresses.Add(newAddressesList);
                }
            }
            return addresses;
        }
    }
}
