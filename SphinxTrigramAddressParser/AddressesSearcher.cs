using System;
using System.Collections.Generic;
using System.Linq;
using MySql.Data.MySqlClient;

namespace SphinxTrigramAddressParser
{
    internal class AddressesSearcher
    {
        private MySqlConnection SphinxConnection { get; set; }
        private DatabaseConnection DbConnection { get; set; }
        private Logger Logger { get; set; }

        public AddressesSearcher(MySqlConnection sphinxConnection, DatabaseConnection dbConnection, Logger logger)
        {
            SphinxConnection = sphinxConnection;
            DbConnection = dbConnection;
            Logger = logger;
        }

        public void SearchIds(List<List<Premise>> premisesVariants)
        {
            foreach (var premisesList in premisesVariants)
            {
                foreach (var premise in premisesList)
                {
                    var idPremises = FindPremiseInSphinx(premise.Street, premise.House, premise.PremiseNumber);
                    if (idPremises == null || idPremises.Count == 0)
                    {
                        premise.Description = "Premise identificator not found";
                        continue;
                    }
                    premise.IdPremisesList = idPremises;
                    if (premise.SubPremises.Count == 0)
                    {
                        if (premise.IdPremisesList.Count == 1)
                        {
                            premise.IdPremisesValid = premise.IdPremisesList[0];
                            Logger.Write(
                                string.Format("Raw address: `{0}`, premise identity: {1}", premise.RawAddress,
                                    premise.IdPremisesValid), MsgType.InformationMsg);
                        }
                        else
                        {
                            Logger.Write(
                                string.Format("Raw address: `{0}`, premises identities: {1}", premise.RawAddress,
                                    premise.IdPremisesList.Select(id => id.ToString())
                                        .Aggregate((acc, v) => acc + "," + v)),
                                MsgType.WarningMsg);
                            premise.Description = "Founded more then on premise identificator";
                        }
                    }
                    else
                    {
                        foreach (var id in premise.IdPremisesList)
                        {
                            if (id == null) continue;
                            var finded = true;
                            foreach (var subPremise in premise.SubPremises)
                            {
                                var idSubPremise = FindSubPremiseBy(id.Value, subPremise.SubPremiseNumber);
                                if (idSubPremise == null)
                                {
                                    finded = false;
                                    break;
                                }
                                subPremise.IdSubPremise = idSubPremise.Value;
                            }
                            if (!finded) continue;
                            premise.IdPremisesValid = id;
                            Logger.Write(string.Format("Raw address: `{0}`, premise identity: {1}, rooms identities: {2}", premise.RawAddress,
                                    premise.IdPremisesValid, premise.SubPremises.Select(room => room.IdSubPremise.ToString()).Aggregate((acc, v) => acc + "," + v)), MsgType.InformationMsg);
                            break;
                        }
                        if (premise.IdPremisesValid != null) continue;
                        if (premise.IdPremisesList.Count > 1)
                            premise.Description = "Founded more then on premise identificator";
                        premise.Description = (premise.Description == null ? "" : premise.Description + ". ") + "Not founded rooms identificators for exist premise";
                    }
                }
            }
        }

        private int? FindSubPremiseBy(int idPremise, string subPremiseNumber)
        {
            var query = "SELECT id_sub_premises FROM sub_premises WHERE id_premises = ? AND sub_premises_num = ?";
            var command = DbConnection.CreateCommand();
            command.CommandText = query;
            command.Parameters.Add(DbConnection.CreateParameter("id_premises", idPremise));
            command.Parameters.Add(DbConnection.CreateParameter("sub_premises_num", subPremiseNumber));
            using (var reader = command.ExecuteReader())
            {
                if (reader.Read())
                {
                    return reader.GetInt32(0);
                }
            }
            return null;
        }

        private List<int?> FindPremiseInSphinx(string street, string house, string premiseNumber)
        {
            var trigramms = AddressHelper.TrigrammingStreet(AddressHelper.NormalizeStreet(street));
            var matchCount = (int)Math.Max(Math.Floor((double)(trigramms.Split(' ').Count() - 4) / 2), 2);
            var query = string.Format(@"SELECT *, WEIGHT() AS weight
                        FROM test1
                        WHERE MATCH('@street_name """ + AddressHelper.TrigrammingStreet(AddressHelper.NormalizeStreet(street)) + 
                            @"""/"+matchCount+@" @house ""___" + house + @"___"" @premises_num ""___" + premiseNumber.Replace(',','_') + @"___""')");
            var command = new MySqlCommand(query, SphinxConnection);
            var ids = new List<int?>();
            using (var reader = command.ExecuteReader())
            {
                string lastStreetName = null;
                while (reader.Read())
                {
                    var id = reader.GetInt32("id");
                    var streetName = reader.GetString("street_name");
                    if (lastStreetName == null)
                    {
                        lastStreetName = streetName;
                        ids.Add(id);
                    }
                    else
                    if (lastStreetName == streetName)
                        ids.Add(id);
                }
            }
            return ids;
        }
    }
}
