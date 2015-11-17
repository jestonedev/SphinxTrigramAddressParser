using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MySql.Data.MySqlClient;
using QueryTextDriver;

namespace SphinxTrigramAddressParser
{
    class Program
    {
        static readonly Logger Logger = new ConsoleLogger();
        static void Main(string[] args)
        {
            try
            {
                var arguments = ParseCommandLineArgs(args);
                if (!arguments.ContainsKey("bks-file") || !File.Exists(arguments["bks-file"])) throw new ApplicationException("CSV file from BKS not found");
                if (!arguments.ContainsKey("sphinx-connection-string")) throw new ApplicationException("Sphinx connction string is require");
                if (!arguments.ContainsKey("db-connection-string")) throw new ApplicationException("Database 'registry' connction string is require");
                using (var sphinx = new MySqlConnection(arguments["sphinx-connection-string"]))
                using (var connection = new DatabaseConnection(arguments["db-connection-string"]))
                {
                    sphinx.Open();
                    // Загружаем адреса
                    var addresses = LoadPremises(arguments["bks-file"]);
                    // Парсим адреса
                    var addressesParser = new AddressesParser(addresses, Logger);
                    // Привязываем идентификаторы помещений и комнат для корректно распарсеных адресов
                    var addressesSearcher = new AddressesSearcher(sphinx, connection, Logger);
                    foreach (var address in addressesParser.PreparedPremises)
                        addressesSearcher.SearchIds(address); // Корректирует идентификаторы у переданного объекта!!!
                    // Сохраняем в базу данных полученую информацию
                    var dbInserter = new DbInserter(connection, Logger);
                    dbInserter.InsertIntoDb(addressesParser.PreparedPremises, addressesParser.InvalidPremises);
                }
                Console.ReadLine();
            }
            catch (Exception e)
            {
                Logger.Write(e.Message, MsgType.ErrorMsg);
                Console.ReadLine();
            }
        }

        private static Dictionary<string, string> ParseCommandLineArgs(IEnumerable<string> args)
        {
            var result = new Dictionary<string, string>();
            foreach (var arg in args.Select(arg => arg.Split(new[] { '=' }, 2)))
                result.Add(arg[0], arg.Length == 2 ? arg[1] : "");
            return result;
        }

        private static List<Premise> LoadPremises(string path)
        {
            Logger.Write(string.Format("Load data from file {0}", path), MsgType.InformationMsg);
            var executor = new QueryExecutor("\t", "\r\n", true, true);
            var tableJoin = executor.Execute("SELECT * FROM \"" + path + "\"");
            var premises = new List<Premise>();
            foreach (var row in tableJoin.Rows)
            {
                premises.Add(new Premise
                {
                    RawAddress = row.Cells[4].Value.AsString().Value(),
                    Account = row.Cells[2].Value.AsString().Value()
                });
            }
            return premises;
        }
    }
}
