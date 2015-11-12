using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using DataTypes;
using MySql.Data.MySqlClient;
using QueryTextDriver;

namespace SphinxTrigramAddressParser
{
    class Program
    {
        static void Main(string[] args)
        {
            const string sphinxConnectionString = "Server=localhost;Port=9306;Character Set=utf8";
            try
            {
                // Загружаем данные из файла
                var rawPayments = LoadDataFromCsv(
                    @"D:\Projects\Visual Studio Projects\SphinxTrigramAddressParser\SphinxTrigramAddressParser\payments_sept.csv");
                // Парсим адреса
                var preparedPayments = new List<Payment>();
                var invalidePayments = new List<Payment>();
                for (var i = 0; i < rawPayments.Rows.Count; i++)
                {
                    var row = rawPayments.Rows[i];

                    var address = row.Cells[4].Value.AsString().Value();
                    var account = row.Cells[2].Value.AsString().Value();
                    var trigrammedAddress = TrigrammAddress(NormalizeAddress(address));
                    string house;
                    string premise;
                    string subPremises;
                    if (!ParseAddress(address, out house, out premise, out subPremises))
                    {
                        var invalidePayment = new Payment { RawAddress = address, Account = account };
                        invalidePayments.Add(invalidePayment);
                        Console.WriteLine("{0}: invalid", invalidePayment.RawAddress);
                        continue;
                    }

                    var hasZSymbol = false;
                    if (premise.EndsWith("З"))
                    {
                        premise = premise.Substring(0, premise.Length - 1);
                        hasZSymbol = true;
                    }
                    if (subPremises != null && subPremises.EndsWith("З"))
                    {
                        subPremises = subPremises.Substring(0, subPremises.Length - 1);
                        hasZSymbol = true;
                    }

                    var validPayment = new Payment
                    {
                        RawAddress = address,
                        TrigrammAddress = trigrammedAddress,
                        House =  house,
                        Premise = premise,
                        SubPremise = subPremises,
                        Account = account,
                        HasZSymbol =  hasZSymbol
                    };
                    preparedPayments.Add(validPayment);
                    Console.WriteLine("{0}: valid", validPayment.RawAddress);
                }
                using (var sphinx = new MySqlConnection(sphinxConnectionString))
                {
                    sphinx.Open();
                    foreach (var payment in preparedPayments)
                    {
                        var sphinxQuery = @"SELECT *, WEIGHT() AS weight
                        FROM test1
                        WHERE MATCH('@street_name """+payment.TrigrammAddress+@"""/3 @house ""___"+payment.House+@"___"" 
                           @premises_num ""___" + payment.Premise + @"___""') LIMIT 1";
                        var sphinxCommand = new MySqlCommand(sphinxQuery, sphinx);
                        using (var sphinxReader = sphinxCommand.ExecuteReader())
                        {
                            if (!sphinxReader.Read())
                            {
                                invalidePayments.Add(payment);
                                Console.WriteLine("{0}: invalid", payment.RawAddress);
                                continue;
                            }
                            var id = sphinxReader.GetInt32("id");
                            payment.IdPremises = id;
                            sphinxReader.Close();
                            Console.WriteLine("{0}: {1}", payment.RawAddress, id);
                        }
                    }
                }
                // Импортируем данные в базу для тестирования
                using (var connection = new DatabaseConnection("dsn=registry"))
                {
                    foreach (var payment in preparedPayments)
                    {
                        if (payment.IdPremises == null) continue;
                        var validCommand = connection.CreateCommand();
                        validCommand.CommandText = "INSERT INTO _valid VALUES(?,?,?,?,?,?,?)";
                        var idPremisesParam = connection.CreateParameter("id_premises", typeof(int));
                        idPremisesParam.Value = (object)payment.IdPremises ?? DBNull.Value;
                        validCommand.Parameters.Add(idPremisesParam);
                        var rawAddressParam = connection.CreateParameter("rawAddress", typeof (string));
                        rawAddressParam.Value = (object)payment.RawAddress ?? DBNull.Value;
                        validCommand.Parameters.Add(rawAddressParam);
                        var houseParam = connection.CreateParameter("house", typeof(string));
                        houseParam.Value = (object)payment.House ?? DBNull.Value;
                        validCommand.Parameters.Add(houseParam);
                        var premisesNumParam = connection.CreateParameter("premiseNum", typeof(string));
                        premisesNumParam.Value = (object)payment.Premise ?? DBNull.Value;
                        validCommand.Parameters.Add(premisesNumParam);
                        var subPremisesParam = connection.CreateParameter("subPremises", typeof(string));
                        subPremisesParam.Value = (object)payment.SubPremise ?? DBNull.Value;
                        validCommand.Parameters.Add(subPremisesParam);
                        var accountParam = connection.CreateParameter("account", typeof(string));
                        accountParam.Value = (object)payment.Account ?? DBNull.Value;
                        validCommand.Parameters.Add(accountParam);
                        var hasZSybmolParam = connection.CreateParameter("account", typeof(bool));
                        hasZSybmolParam.Value = (object)payment.HasZSymbol ?? DBNull.Value;
                        validCommand.Parameters.Add(hasZSybmolParam);
                        validCommand.ExecuteNonQuery();
                    }
                    foreach (var payment in invalidePayments)
                    {
                        var validCommand = connection.CreateCommand();
                        validCommand.CommandText = "INSERT INTO _invalid VALUES(?,?,?,?,?,?,?)";
                        var idPremisesParam = connection.CreateParameter("id_premises", typeof(int));
                        idPremisesParam.Value = DBNull.Value;
                        validCommand.Parameters.Add(idPremisesParam);
                        var rawAddressParam = connection.CreateParameter("rawAddress", typeof(string));
                        rawAddressParam.Value = (object)payment.RawAddress ?? DBNull.Value;
                        validCommand.Parameters.Add(rawAddressParam);
                        var houseParam = connection.CreateParameter("house", typeof(string));
                        houseParam.Value = (object)payment.House ?? DBNull.Value;
                        validCommand.Parameters.Add(houseParam);
                        var premisesNumParam = connection.CreateParameter("premiseNum", typeof(string));
                        premisesNumParam.Value = (object)payment.Premise ?? DBNull.Value;
                        validCommand.Parameters.Add(premisesNumParam);
                        var subPremisesParam = connection.CreateParameter("subPremises", typeof(string));
                        subPremisesParam.Value = (object)payment.SubPremise ?? DBNull.Value;
                        validCommand.Parameters.Add(subPremisesParam);
                        var accountParam = connection.CreateParameter("account", typeof(string));
                        accountParam.Value = (object)payment.Account ?? DBNull.Value;
                        validCommand.Parameters.Add(accountParam);
                        var hasZSybmolParam = connection.CreateParameter("account", typeof(bool));
                        hasZSybmolParam.Value = (object)payment.HasZSymbol ?? DBNull.Value;
                        validCommand.Parameters.Add(hasZSybmolParam);
                        validCommand.ExecuteNonQuery();
                    }
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
                Console.ReadLine();
            }
        }

        private static TableJoin LoadDataFromCsv(string filePath)
        {
            Console.WriteLine("Load data from file {0}", filePath);
            var executor = new QueryExecutor("\t", "\r\n", true, true);
            return executor.Execute("SELECT * FROM \""+filePath+"\"");
        }

        private static string NormalizeAddress(string address)
        {
            return Regex.Replace(address, @"[^а-яА-Я0-9\-]", "").ToLower();
        }

        private static string TrigrammAddress(string address)
        {
            var trigramma = "___";
            var buffer = "";
            foreach (var chr in address)
            {
                trigramma = trigramma.Substring(1) + chr;
                buffer = buffer + " " + trigramma;
            }
            trigramma = trigramma.Substring(1) + "_";
            buffer = buffer + " " + trigramma;
            trigramma = trigramma.Substring(1) + "_";
            buffer = buffer + " " + trigramma;
            return buffer.Trim();
        }

        private static bool ParseAddress(string address, out string house, out string premise, out string subPremises)
        {
            var regexp = new Regex(@"^.*?(?:,|д|дом).*?([0-9]+[ \-""]*[а-яА-Я]?[ \-""]*(?:[ ]*[\/\\][ ]*[0-9]+[ \-""]*[а-яА-Я]?[ \-""]*)?).*?(?:,|кв|квар|квартира)\.?.*?([0-9]+[ \-""]*[а-яА-Я]?[ \-""]*(?:[ ]*[,\-][ ]*(?:кв|квар|квартира)?\.?[ ]*[0-9]+[ \-""]*[а-яА-Я]?[ \-""]*)*)(?:(?:,|\/|\\|комната|ком|км|к)\.?(.*)?)?$");
            var match = regexp.Match(address);
            if (match.Groups.Count < 3)
            {
                house = null;
                premise = null;
                subPremises = null;
                return false;
            }
            house = Regex.Replace(match.Groups[1].Value, @"[^а-яА-Я0-9\/\\]", "").ToUpper()
                .Replace("/", "|").Replace("\\", "|").Replace(".", "").Replace(" ", "");
            premise = Regex.Replace( match.Groups[2].Value, @"[^а-яА-Я0-9,\-]","").ToUpper()
                .Replace("КВАРТИРА", "").Replace("КВАР", "").Replace("КВ", "").Replace(".", "").Replace(" ", "");
            if (match.Groups.Count > 3 && !string.IsNullOrEmpty(match.Groups[3].Value))
                subPremises = Regex.Replace(match.Groups[3].Value, @"[^а-яА-Я0-9,]", "").ToUpper()
                    .Replace("КОМНАТА", "")
                    .Replace("КОМ", "")
                    .Replace("КМ", "")
                    .Replace("К", "")
                    .Replace(".", "")
                    .Replace(" ", "");
            else
                subPremises = null;
            return true;
        }
    }
}
