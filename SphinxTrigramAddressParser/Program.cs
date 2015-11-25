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

                // Загружаем адреса
                var addresses = LoadPremises(arguments["bks-file"]);

                var groupedAccounts = from row in addresses
                    group row by new {row.CRN, row.Account, row.Tenant, row.RawAddress}
                    into gs
                    select new
                    {
                        gs.Key.CRN,
                        gs.Key.Account,
                        gs.Key.Tenant,
                        gs.Key.RawAddress,
                        Steps = gs
                    };

                var premises = new List<Premise>();
                var premise = new Premise();

                foreach (var group in groupedAccounts)
                {
                    premise.Account = group.Account;
                    premise.RawAddress = group.RawAddress;
                    premise.CRN = group.CRN;
                    premise.Tenant = group.Tenant;

                    var account = group.Steps.Where(s => s.RawType == "Лицевой счет").ToList();
                    if (account.Any())
                    {
                        var accountFirst = account.First();
                        premise.TotalArea = accountFirst.TotalArea;
                        premise.LivingArea = accountFirst.LivingArea;
                        premise.Prescribed = accountFirst.Prescribed;
                    }
                    else
                    {
                        premise.TotalArea = "0";
                        premise.LivingArea = "0";
                        premise.Prescribed = "0";
                    }
                    var balanceInput = group.Steps.Where(s => s.RawType == "Вх. сальдо").ToList();
                    if (balanceInput.Any())
                    {
                        var balanceInputFirst = balanceInput.First();
                        premise.BalanceInput = balanceInputFirst.BalanceInput;
                        premise.BalanceTenancy = balanceInputFirst.BalanceTenancy;
                        premise.BalanceDGI = balanceInputFirst.DebetDGI;
                    }
                    else
                    {
                        premise.BalanceInput = "0";
                        premise.BalanceTenancy = "0";
                        premise.BalanceDGI = "0";
                    }
                    var charging = group.Steps.Where(s => s.RawType == "Начислено (расчет)").ToList();
                    if (charging.Any())
                    {
                        var chargingFirst = charging.First();
                        premise.ChargingTenancy = chargingFirst.BalanceTenancy;
                    }
                    else
                    {
                        premise.ChargingTenancy = "0";
                    }
                    var chargingTotal = group.Steps.Where(s => s.RawType == "Начислено (итого)").ToList();
                    if (chargingTotal.Any())
                    {
                        var chargingTotalFirst = chargingTotal.First();
                        premise.ChargingTotal = chargingTotalFirst.BalanceTenancy;
                        premise.ChargingDGI = chargingTotalFirst.DebetDGI;
                    }
                    else
                    {
                        premise.ChargingTotal = "0";
                        premise.ChargingDGI = "0";
                    }
                    var recalc = group.Steps.Where(s => s.RawType == "Разовые (перерасчеты").ToList();
                    if (recalc.Any())
                    {
                        var recalcFirst = recalc.First();
                        premise.RecalcTenancy = recalcFirst.BalanceTenancy;
                        premise.RecalcDGI = recalcFirst.DebetDGI;
                    }
                    else
                    {
                        premise.RecalcTenancy = "0";
                        premise.RecalcDGI = "0";
                    }
                    var payment = group.Steps.Where(s => s.RawType == "Оплачено").ToList();
                    if (payment.Any())
                    {
                        var paymentFirst = payment.First();
                        premise.PaymentTenancy = paymentFirst.BalanceTenancy;
                        premise.PaymentDGI = paymentFirst.DebetDGI;
                    }
                    else
                    {
                        premise.PaymentTenancy = "0";
                        premise.PaymentDGI = "0";
                    }
                    var transferBalance = group.Steps.Where(s => s.RawType == "Перенос сальдо").ToList();
                    if (transferBalance.Any())
                    {
                        var transferBalanceFirst = transferBalance.First();
                        premise.TransferBalance = transferBalanceFirst.BalanceTenancy;
                    }
                    else
                    {
                        premise.TransferBalance = "0";
                    }

                    var balanceOutput = group.Steps.Where(s => s.RawType == "Исх. сальдо").ToList();
                    if (balanceOutput.Any())
                    {
                        var balanceOutputFirst = balanceOutput.First();
                        premise.BalanceOutputTotal = balanceOutputFirst.BalanceOutput;
                        premise.BalanceOutputTenancy = balanceOutputFirst.BalanceTenancy;
                        premise.BalanceOutputDGI = balanceOutputFirst.DebetDGI;
                    }
                    else
                    {
                        premise.BalanceOutputTotal = "0";
                        premise.BalanceOutputTenancy = "0";
                        premise.BalanceOutputDGI = "0";
                    }
                    premises.Add(premise);
                    premise = new Premise();
                }

                using (var sphinx = new MySqlConnection(arguments["sphinx-connection-string"]))
                using (var connection = new DatabaseConnection(arguments["db-connection-string"]))
                {
                    sphinx.Open();
                    // Парсим адреса
                    var addressesParser = new AddressesParser(premises, Logger);
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

        private static List<PremiseRaw> LoadPremises(string path)
        {
            Logger.Write(string.Format("Load data from file {0}", path), MsgType.InformationMsg);
            var executor = new QueryExecutor("\t", "\r\n", true, true);
            var tableJoin = executor.Execute("SELECT * FROM \"" + path + "\"");
            var premises = new List<PremiseRaw>();
            foreach (var row in tableJoin.Rows)
            {
                premises.Add(new PremiseRaw
                {                  
                    CRN = row.Cells[0].Value.AsString().Value(),
                    Account = row.Cells[2].Value.AsString().Value(),
                    Tenant = row.Cells[3].Value.AsString().Value(),
                    RawAddress = row.Cells[4].Value.AsString().Value(),
                    RawType = row.Cells[8].Value.AsString().Value(),
                    TotalArea = row.Cells[9].Value.AsString().Value(),
                    LivingArea = row.Cells[10].Value.AsString().Value(),
                    Prescribed = row.Cells[11].Value.AsString().Value(),
                    BalanceInput = row.Cells[12].Value.AsString().Value(),
                    BalanceTenancy = row.Cells[15].Value.AsString().Value(),

                    DebetDGI = row.Cells[16].Value.AsString().Value(),      // sept 17
                    BalanceOutput = row.Cells[17].Value.AsString().Value(),  // sept 18
                });
            }
            return premises;
        }
    }
}
