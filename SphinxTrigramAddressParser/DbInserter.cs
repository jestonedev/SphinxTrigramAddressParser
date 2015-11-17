using System.CodeDom;
using System.Collections.Generic;
using System.Linq;

namespace SphinxTrigramAddressParser
{
    internal class DbInserter
    {
        private DatabaseConnection DbConnection { get; set; }
        private Logger Logger { get; set; }

        private int IdPremiseGroup { get; set; }

        public DbInserter(DatabaseConnection dbConnection, Logger logger)
        {
            DbConnection = dbConnection;
            Logger = logger;
            IdPremiseGroup = 1;
        }

        public void InsertIntoDb(List<List<List<Premise>>> preparedPremises, List<List<List<Premise>>> invalidPremises)
        {
            var invalidPremisesIds = new List<List<List<Premise>>>();
            // Загружаем данные в БД
            PreInitDb();
            foreach (var address in preparedPremises)
            {
                var validPremisesLists = AddressHelper.GetValidPremisesLists(address);
                if (validPremisesLists != null && validPremisesLists.Count == 1)
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
            var errorCount = PostInsertIntoDb();
            Logger.Write(string.Format("Inserted valid addresses count: {0}", preparedPremises.Count - invalidPremisesIds.Count - errorCount), MsgType.InformationMsg);
            Logger.Write(string.Format("Inserted invalid addresses count: {0}", invalidPremises.Count + invalidPremisesIds.Count + errorCount), MsgType.InformationMsg);
        }

        private int PostInsertIntoDb()
        {
            var command = DbConnection.CreateCommand();
            Logger.Write("Copy incorrect resolved addresses from table _prevalid into table _invalid", MsgType.InformationMsg);
            command.CommandText =
                @"INSERT INTO _invalid SELECT v.*
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
                            WHERE v.rawAddress NOT LIKE REPLACE(CONCAT('%',x.street_name,'%'),'XX','ХХ');";
            var errorCount = command.ExecuteNonQuery();
            Logger.Write("Copy correct resolved addresses from table _prevalid into table _valid", MsgType.InformationMsg);
            command.CommandText =
                @"INSERT INTO _valid
                            SELECT v.*
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
                            WHERE v.rawAddress LIKE REPLACE(CONCAT('%',x.street_name,'%'),'XX','ХХ');";
            command.ExecuteNonQuery();
            return errorCount;
            /* Console.ForegroundColor = ConsoleColor.Yellow;
            TODO: Console.Write("Are you sure, that you want execute stored procedure for inserting data into main table for selected? [yes/no]: ");
            if (!(new List<string> { "Y", "YES", "1", "OK", "ДА" }).Contains((Console.ReadLine() ?? "").ToUpper())) return;
            Console.ForegroundColor = ConsoleColor.Green;
            TODO: Console.WriteLine("Execute stored procedure for inserting data into main table for selected period");
            Console.ResetColor();*/
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
            var query = string.Format("INSERT INTO {0} VALUES (?,?,?,?,?,?,?,?,?,?)", tableName);
            var command = DbConnection.CreateCommand();
            command.CommandText = query;
            foreach (var premise in premiseVariant)
            {
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
                command.Parameters.Add(DbConnection.CreateParameter("id_premises_group", IdPremiseGroup));
                command.Parameters.Add(DbConnection.CreateParameter("id_premises_list", idPremisesList));
                command.Parameters.Add(DbConnection.CreateParameter("id_premises_valid", premise.IdPremisesValid));
                command.Parameters.Add(DbConnection.CreateParameter("id_sub_premises", idSubPremises));
                command.Parameters.Add(DbConnection.CreateParameter("rawAddress", premise.RawAddress));
                command.Parameters.Add(DbConnection.CreateParameter("house", premise.House));
                command.Parameters.Add(DbConnection.CreateParameter("premiseNum", premise.PremiseNumber));
                command.Parameters.Add(DbConnection.CreateParameter("subPremises", subPremises));
                command.Parameters.Add(DbConnection.CreateParameter("account", premise.Account));
                command.Parameters.Add(DbConnection.CreateParameter("description", premise.Description));
                command.ExecuteNonQuery();
            }
            IdPremiseGroup++;
        }

        private static string CreateTableQuery(string tableName)
        {
            return string.Format("CREATE TABLE IF NOT EXISTS {0} ( " +
                      "id_premises_group INT DEFAULT NULL, " +
                      "id_premises_list varchar(255) DEFAULT NULL, "+
                      "id_premises_valid varchar(255) DEFAULT NULL, " +
                      "id_sub_premises varchar(255) DEFAULT NULL, "+
                      "rawAddress varchar(255) DEFAULT NULL, "+
                      "house varchar(255) DEFAULT NULL, "+
                      "premiseNum varchar(255) DEFAULT NULL, "+
                      "subPremises varchar(255) DEFAULT NULL, "+
                      "account varchar(255) DEFAULT NULL, "+
                      "description varchar(255) DEFAULT NULL "+
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
