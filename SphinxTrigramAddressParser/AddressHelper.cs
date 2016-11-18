using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SphinxTrigramAddressParser
{
    internal class AddressHelper
    {
        public static string NormalizeStreet(string street)
        {
            return Regex.Replace(street, @"[^а-яА-Я0-9]", "");
        }

        public static string TrigrammingStreet(string street)
        {
            var buffer = "";
            var trigramm = "___";
            foreach (var chr in street)
            {
                trigramm = trigramm.Substring(1) + chr;
                buffer += " " + trigramm;
            }
            for (var i = 0; i < 2; i++)
            {
                trigramm = trigramm.Substring(1) + "_";
                buffer += " " + trigramm;
            }
            return buffer.Trim();
        }

        public static string NormalizeHouse(string house)
        {
            return house.Replace(" ", "").Replace("-", "").Replace("\"", "").Replace("\\", "_").Replace("/", "_").ToUpper();
        }

        public static string NormalizePremises(string premises)
        {
            return Regex.Replace(premises.Replace("квартира.", ",").Replace("квартира", ",").Replace("кв.ком.", ",").Replace("кв.ком", ",")
                .Replace("квком.", ",").Replace("квком", ",")
                .Replace("кв.", ",").Replace("кв", ",").Replace("-", ",").Replace(" ", "").Replace(",,", ",").Trim(','),"[,]?[зЗ]$","").ToUpper();
        }

        public static string NormalizeSubPremises(string subPremises)
        {
            return Regex.Replace(Regex.Replace(subPremises, @"к[/\\]?м[0-9]+[ ]*з?\.?$",",") /*убираем койко-место*/
                .Replace("комната.", ",").Replace("комната", ",").Replace("ком.", ",").Replace("ком", ",")
                .Replace("к.", ",").Replace("к", ",").Replace("-", ",").Replace(" ", "").Replace("\\", "/")
                .Replace(",,", ",").Trim(','), @"[зЗ]\.?$", "").Trim().Trim(',').ToUpper();
        }

        public static bool IsValidAddress(IReadOnlyList<List<Premise>> address)
        {
            return address.Count > 0 && address[0].Count > 0 &&
                   address[0][0].Street != null && address[0][0].House != null &&
                   address[0][0].PremiseNumber != null;
        }

        public static List<List<Premise>> GetValidPremisesLists(IEnumerable<List<Premise>> premisesVariants)
        {
            var premisesLists = new List<List<Premise>>();
            foreach (var premisesList in premisesVariants)
            {
                var correct = premisesList.All(premise => premise.IdPremisesValid != null);
                if (!correct) continue;
                foreach (var premise in premisesList)
                {
                    if (premise.SubPremises.Count == 0)
                        continue;
                    correct = premise.SubPremises.All(subPremise => subPremise.IdSubPremise != null);
                    if (!correct)
                        break;
                }
                if (correct) premisesLists.Add(premisesList);
            }
            if (premisesLists.Count <= 1) return premisesLists;
            foreach (var premisesList in premisesLists)
                foreach (var premise in premisesList)
                    premise.Description = (premise.Description == null ? "" : premise.Description + ". ") + "Founded more then one correct premise variant";
            return premisesLists;
        }
    }

}
