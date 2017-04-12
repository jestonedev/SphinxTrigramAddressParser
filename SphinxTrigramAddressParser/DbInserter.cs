using System.Collections.Generic;
using System.Linq;

namespace SphinxTrigramAddressParser
{
    internal class DbInserter
    {
        private DatabaseConnection DbConnection { get; set; }
        private Logger Logger { get; set; }

        private int IdPremiseGroup { get; set; }
        private int IdKey { get; set; }

        public DbInserter(DatabaseConnection dbConnection, Logger logger)
        {
            DbConnection = dbConnection;
            Logger = logger;
            IdPremiseGroup = 1;
            IdKey = 1;
        }

        public void InsertIntoDb(List<List<List<Premise>>> preparedPremises, List<List<List<Premise>>> invalidPremises)
        {
            var invalidPremisesIds = new List<List<List<Premise>>>();
            // Загружаем данные в БД
            PreInitDb();
            foreach (var address in preparedPremises)
            {
                var validPremisesLists = AddressHelper.GetValidPremisesLists(address);
                if (validPremisesLists != null && validPremisesLists.Count > 0)
                {
                    InsertIntoDbTable("_prevalid", validPremisesLists.First());
                    Logger.Write(string.Format("Insert valid address `{0}`", validPremisesLists.First().First().RawAddress),
                        MsgType.InformationMsg);
                }
                else
                    invalidPremisesIds.Add(address);
            }
            foreach (var invalidPremise in invalidPremises)
            {
                var bestVariants =
                    invalidPremise.Where(premiseList => premiseList.All(premise => premise.IdPremisesValid != null)).ToList();
                if (bestVariants.Any())
                    InsertIntoDbTable("_invalid", bestVariants[0]);
                else
                {
                    if (invalidPremises[0].Count <= 0) continue;
                    InsertIntoDbTable("_invalid", invalidPremise[0]);
                }
                Logger.Write(string.Format("Insert invalid address `{0}`. Description: {1}", invalidPremise[0][0].RawAddress, invalidPremise[0][0].Description), MsgType.ErrorMsg);
            }
            foreach (var invalidPremise in invalidPremisesIds)
            {
                if (invalidPremisesIds[0].Count <= 0) continue;
                InsertIntoDbTable("_invalid", invalidPremise[0]);
                Logger.Write(string.Format("Insert invalid address `{0}`. Description: {1}", invalidPremise[0][0].RawAddress, invalidPremise[0][0].Description), MsgType.ErrorMsg);
            }
            PostInsertIntoDb();

            var command = DbConnection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) as cnt FROM _invalid";
            var invalidCount = command.ExecuteScalar();

            command = DbConnection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) as cnt FROM _valid";
            var validCount = command.ExecuteScalar();

            Logger.Write(string.Format("Inserted valid addresses count: {0}", validCount), MsgType.InformationMsg);
            Logger.Write(string.Format("Inserted invalid addresses count: {0}", invalidCount), MsgType.InformationMsg);
        }

        private void PostInsertIntoDb()
        {
            var command = DbConnection.CreateCommand();
            Logger.Write("Copy incorrect resolved addresses from table _prevalid into table _invalid", MsgType.InformationMsg);
            command.CommandText =
                @"INSERT INTO _invalid
                            SELECT pr.*
                            FROM _prevalid pr
                            WHERE pr.account IN (
                            SELECT v.account
                            FROM _prevalid v
                              INNER JOIN premises p ON v.id_premises_valid = p.id_premises
                              INNER JOIN buildings b ON p.id_building = b.id_building
                              INNER JOIN 
                            (
                            SELECT vks.id_street,  
                              TRIM(SUBSTRING(
                              TRIM(SUBSTRING_INDEX(REPLACE(vks.street_name,'жилрайон. ',''),',',-1)),
                              LOCATE(' ', TRIM(SUBSTRING_INDEX(REPLACE(vks.street_name,'жилрайон. ',''),',',-1))))) AS street_name
                            FROM v_kladr_streets vks) x ON b.id_street = x.id_street
                            WHERE v.raw_address NOT LIKE REPLACE(CONCAT('%',x.street_name,'%'),'XX','ХХ'))";
            command.ExecuteNonQuery();
            Logger.Write("Copy correct resolved addresses from table _prevalid into table _valid", MsgType.InformationMsg);
            command.CommandText =
                @"INSERT INTO _valid
                   SELECT pr.*
                    FROM _prevalid pr
                    WHERE pr.account NOT IN (
                    SELECT v.account
                    FROM _prevalid v
                      INNER JOIN premises p ON v.id_premises_valid = p.id_premises
                      INNER JOIN buildings b ON p.id_building = b.id_building
                      INNER JOIN 
                    (
                    SELECT vks.id_street,  
                      TRIM(SUBSTRING(
                      TRIM(SUBSTRING_INDEX(REPLACE(vks.street_name,'жилрайон. ',''),',',-1)),
                      LOCATE(' ', TRIM(SUBSTRING_INDEX(REPLACE(vks.street_name,'жилрайон. ',''),',',-1))))) AS street_name
                    FROM v_kladr_streets vks) x ON b.id_street = x.id_street
                    WHERE v.raw_address NOT LIKE REPLACE(CONCAT('%',x.street_name,'%'),'XX','ХХ'))";
            command.ExecuteNonQuery();
        }

        private void PreInitDb()
        {
            Logger.Write("Preinit database", MsgType.InformationMsg);
            Logger.Write("Create doesn't exists tables", MsgType.InformationMsg);
            var command = DbConnection.CreateCommand();
            command.CommandText = CreateTableQuery("_invalid");
            command.ExecuteNonQuery();
            command.CommandText = CreateTableQuery("_prevalid");
            command.ExecuteNonQuery();
            command.CommandText = CreateTableQuery("_valid");
            command.ExecuteNonQuery();
            Logger.Write("Truncate tables", MsgType.InformationMsg);
            command.CommandText = TruncateTableQuery("_invalid");
            command.ExecuteNonQuery();
            command.CommandText = TruncateTableQuery("_prevalid");
            command.ExecuteNonQuery();
            command.CommandText = TruncateTableQuery("_valid");
            command.ExecuteNonQuery();
            // TODO: Logger.Write("Delete data from main bks table for selected period", MsgType.InformationMsg);
        }

        private void InsertIntoDbTable(string tableName, List<Premise> premiseVariant)
        {
            foreach (var premise in premiseVariant)
            {
                var query = string.Format("INSERT INTO {0} VALUES (" +
                                          "?,?,?,?,?," +
                                          "?,?,?,?,?," +
                                          "?,?,?,?,?," +
                                          "?,?,?,?,?," +
                                          "?,?,?,?,?," +
                                          "?,?,?,?,?," +
                                          "?,?,?,?,?," +
                                          "?,?,?,?,?," +
                                          "?,?,?,?,?)", tableName);
                var command = DbConnection.CreateCommand();
                command.CommandText = query;
                if (string.IsNullOrEmpty(premise.RawAddress)) continue;
                var idPremisesList = "";
                var idSubPremises = "";
                var subPremises = "";
                if (premise.IdPremisesList != null && premise.IdPremisesList.Count > 0)
                    idPremisesList += ","+premise.IdPremisesList.Select(id => id.ToString()).Aggregate((acc, v) => acc + "," + v);
                if (premise.SubPremises != null && premise.SubPremises.Count > 0)
                    idSubPremises = premise.SubPremises.Where(subPremise => subPremise.IdSubPremise != null).Aggregate(idSubPremises, (current, subPremise) => current + ("," + subPremise.IdSubPremise));
                if (premise.SubPremises != null && premise.SubPremises.Count > 0)
                    subPremises = premise.SubPremises.Where(subPremise => subPremise.SubPremiseNumber != null).Aggregate(subPremises, (current, subPremise) => current + ("," + subPremise.SubPremiseNumber));
                idPremisesList = idPremisesList.Trim(',');
                idSubPremises = idSubPremises.Trim(',');
                subPremises = subPremises.Trim(',');
                command.Parameters.Add(DbConnection.CreateParameter("id_key", IdKey));
                command.Parameters.Add(DbConnection.CreateParameter("id_premises_group", IdPremiseGroup));
                command.Parameters.Add(DbConnection.CreateParameter("id_premises_list", idPremisesList));
                command.Parameters.Add(DbConnection.CreateParameter("id_premises_valid", premise.IdPremisesValid));
                command.Parameters.Add(DbConnection.CreateParameter("id_sub_premises", idSubPremises));
                command.Parameters.Add(DbConnection.CreateParameter("raw_address", premise.RawAddress));
                command.Parameters.Add(DbConnection.CreateParameter("house", premise.House));
                command.Parameters.Add(DbConnection.CreateParameter("premise_number", premise.PremiseNumber));
                command.Parameters.Add(DbConnection.CreateParameter("subPremises", subPremises));
                command.Parameters.Add(DbConnection.CreateParameter("account", premise.Account));
                command.Parameters.Add(DbConnection.CreateParameter("description", premise.Description));
                command.Parameters.Add(DbConnection.CreateParameter("crn", premise.Crn));
                command.Parameters.Add(DbConnection.CreateParameter("tenant", premise.Tenant));
                command.Parameters.Add(DbConnection.CreateParameter("prescribed", premise.Prescribed));
                command.Parameters.Add(DbConnection.CreateParameter("total_area", premise.TotalArea));
                command.Parameters.Add(DbConnection.CreateParameter("living_area", premise.LivingArea));
                command.Parameters.Add(DbConnection.CreateParameter("balance_input", premise.BalanceInput));
                command.Parameters.Add(DbConnection.CreateParameter("balance_tenancy", premise.BalanceTenancy));
                command.Parameters.Add(DbConnection.CreateParameter("balance_dgi", premise.BalanceDgi));
                command.Parameters.Add(DbConnection.CreateParameter("balance_padun", premise.BalancePadun));
                command.Parameters.Add(DbConnection.CreateParameter("balance_pkk", premise.BalancePkk));
                command.Parameters.Add(DbConnection.CreateParameter("balance_input_penalties", premise.BalanceInputPenalties));
                command.Parameters.Add(DbConnection.CreateParameter("charging_tenancy", premise.ChargingTenancy));
                command.Parameters.Add(DbConnection.CreateParameter("charging_total", premise.ChargingTotal));
                command.Parameters.Add(DbConnection.CreateParameter("charging_dgi", premise.ChargingDgi));
                command.Parameters.Add(DbConnection.CreateParameter("charging_padun", premise.ChargingPadun));
                command.Parameters.Add(DbConnection.CreateParameter("charging_pkk", premise.ChargingPkk));
                command.Parameters.Add(DbConnection.CreateParameter("charging_penalties", premise.ChargingPenalties));
                command.Parameters.Add(DbConnection.CreateParameter("recalc_tenancy", premise.RecalcTenancy));
                command.Parameters.Add(DbConnection.CreateParameter("recalc_dgi", premise.RecalcDgi));
                command.Parameters.Add(DbConnection.CreateParameter("recalc_padun", premise.RecalcPadun));
                command.Parameters.Add(DbConnection.CreateParameter("recalc_pkk", premise.RecalcPkk));
                command.Parameters.Add(DbConnection.CreateParameter("recalc_penalties", premise.RecalcPenalties));
                command.Parameters.Add(DbConnection.CreateParameter("payment_tenancy", premise.PaymentTenancy));
                command.Parameters.Add(DbConnection.CreateParameter("payment_dgi", premise.PaymentDgi));
                command.Parameters.Add(DbConnection.CreateParameter("payment_padun", premise.PaymentPadun));
                command.Parameters.Add(DbConnection.CreateParameter("payment_pkk", premise.PaymentPkk));
                command.Parameters.Add(DbConnection.CreateParameter("payment_penalties", premise.PaymentPenalties));
                command.Parameters.Add(DbConnection.CreateParameter("transfer_balance", premise.TransferBalance));
                command.Parameters.Add(DbConnection.CreateParameter("balance_output_total", premise.BalanceOutputTotal));
                command.Parameters.Add(DbConnection.CreateParameter("balance_output_tenancy", premise.BalanceOutputTenancy));
                command.Parameters.Add(DbConnection.CreateParameter("balance_output_dgi", premise.BalanceOutputDgi));
                command.Parameters.Add(DbConnection.CreateParameter("balance_output_padun", premise.BalanceOutputPadun));
                command.Parameters.Add(DbConnection.CreateParameter("balance_output_pkk", premise.BalanceOutputPkk));
                command.Parameters.Add(DbConnection.CreateParameter("balance_output_penalties", premise.BalanceOutputPenalties));
                command.ExecuteNonQuery();
                IdKey++;
            }
            IdPremiseGroup++;
        }

        private static string CreateTableQuery(string tableName)
        {
            return string.Format("CREATE TABLE IF NOT EXISTS {0} ( " +
                      "id_key int NOT NULL, " +
                      "id_premises_group INT DEFAULT NULL, " +
                      "id_premises_list varchar(255) DEFAULT NULL, "+
                      "id_premises_valid varchar(255) DEFAULT NULL, " +
                      "id_sub_premises varchar(255) DEFAULT NULL, "+
                      "raw_address varchar(255) DEFAULT NULL, "+
                      "house varchar(255) DEFAULT NULL, "+
                      "premise_number varchar(255) DEFAULT NULL, "+
                      "sub_premises varchar(255) DEFAULT NULL, "+
                      "account varchar(255) DEFAULT NULL, "+
                      "description varchar(255) DEFAULT NULL, "+
                      "crn varchar(255) DEFAULT NULL, " +
                      "tenant varchar(255) DEFAULT NULL, " +
                      "prescribed varchar(255) DEFAULT NULL, " +
                      "total_area varchar(255) DEFAULT NULL, " +
                      "living_area varchar(255) DEFAULT NULL, " +
                      "balance_input varchar(255) DEFAULT NULL, " +
                      "balance_tenancy varchar(255) DEFAULT NULL, " +
                      "balance_dgi varchar(255) DEFAULT NULL, " +
                      "balance_padun varchar(255) DEFAULT NULL, " +
                      "balance_pkk varchar(255) DEFAULT NULL, " +
                      "balance_input_penalties varchar(255) DEFAULT NULL, " +
                      "charging_tenancy varchar(255) DEFAULT NULL, " +
                      "charging_total varchar(255) DEFAULT NULL, " +
                      "charging_dgi varchar(255) DEFAULT NULL, " +
                      "charging_padun varchar(255) DEFAULT NULL, " +
                      "charging_pkk varchar(255) DEFAULT NULL, " +
                      "charging_penalties varchar(255) DEFAULT NULL, " +
                      "recalc_tenancy varchar(255) DEFAULT NULL, " +
                      "recalc_dgi varchar(255) DEFAULT NULL, " +
                      "recalc_padun varchar(255) DEFAULT NULL, " +
                      "recalc_pkk varchar(255) DEFAULT NULL, " +
                      "recalc_penalties varchar(255) DEFAULT NULL, " +
                      "payment_tenancy varchar(255) DEFAULT NULL, " +
                      "payment_dgi varchar(255) DEFAULT NULL, " +
                      "payment_padun varchar(255) DEFAULT NULL, " +
                      "payment_pkk varchar(255) DEFAULT NULL, " +
                      "payment_penalties varchar(255) DEFAULT NULL, " +
                      "transfer_balance varchar(255) DEFAULT NULL, " +
                      "balance_output_total varchar(255) DEFAULT NULL, " +
                      "balance_output_tenancy varchar(255) DEFAULT NULL, " +
                      "balance_output_dgi varchar(255) DEFAULT NULL, " +
                      "balance_output_padun varchar(255) DEFAULT NULL, " +
                      "balance_output_pkk varchar(255) DEFAULT NULL, " +
                      "balance_output_penalties varchar(255) DEFAULT NULL" +
                    ") "+
                    "ENGINE = INNODB "+
                    "CHARACTER SET utf8 "+
                    "COLLATE utf8_general_ci", tableName);
        }

        private string TruncateTableQuery(string tableName)
        {
            return string.Format("TRUNCATE TABLE {0} ", tableName);
        }
    }
}
