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
                    premise.Crn = group.CRN;
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
                        premise.BalanceDgi = balanceInputFirst.DzDgi;
                        premise.BalancePadun = balanceInputFirst.DzPadun;
                        premise.BalancePkk = balanceInputFirst.DzPkk;
                        premise.BalanceInputPenalties = balanceInputFirst.Penalties;
                    }
                    else
                    {
                        premise.BalanceInput = "0";
                        premise.BalanceTenancy = "0";
                        premise.BalanceDgi = "0";
                        premise.BalancePadun = "0";
                        premise.BalancePkk = "0";
                        premise.BalanceInputPenalties = "0";
                    }
                    var charging = group.Steps.Where(s => s.RawType == "Начислено (расчет)").ToList();
                    if (charging.Any())
                    {
                        var chargingFirst = charging.First();
                        premise.ChargingPenalties = chargingFirst.Penalties;
                        premise.ChargingTenancy = chargingFirst.BalanceTenancy;
                    }
                    else
                    {
                        premise.ChargingTenancy = "0";
                        premise.ChargingPenalties = "0";
                    }
                    var chargingTotal = group.Steps.Where(s => s.RawType == "Начислено (итого)").ToList();
                    if (chargingTotal.Any())
                    {
                        var chargingTotalFirst = chargingTotal.First();
                        premise.ChargingTotal = chargingTotalFirst.BalanceTenancy;
                        premise.ChargingDgi = chargingTotalFirst.DzDgi;
                        premise.ChargingPadun = chargingTotalFirst.DzPadun;
                        premise.ChargingPkk = chargingTotalFirst.DzPkk;
                    }
                    else
                    {
                        premise.ChargingTotal = "0";
                        premise.ChargingDgi = "0";
                        premise.ChargingPadun = "0";
                        premise.ChargingPkk = "0";
                    }
                    var recalc = group.Steps.Where(s => s.RawType == "Разовые (перерасчеты").ToList();
                    if (recalc.Any())
                    {
                        var recalcFirst = recalc.First();
                        premise.RecalcTenancy = recalcFirst.BalanceTenancy;
                        premise.RecalcDgi = recalcFirst.DzDgi;
                        premise.RecalcPadun = recalcFirst.DzPadun;
                        premise.RecalcPkk = recalcFirst.DzPkk;
                        premise.RecalcPenalties = recalcFirst.Penalties;
                    }
                    else
                    {
                        premise.RecalcTenancy = "0";
                        premise.RecalcDgi = "0";
                        premise.RecalcPadun = "0";
                        premise.RecalcPkk = "0";
                        premise.RecalcPenalties = "0";
                    }
                    var payment = group.Steps.Where(s => s.RawType == "Оплачено").ToList();
                    if (payment.Any())
                    {
                        var paymentFirst = payment.First();
                        premise.PaymentTenancy = paymentFirst.BalanceTenancy;
                        premise.PaymentDgi = paymentFirst.DzDgi;
                        premise.PaymentPadun = paymentFirst.DzPadun;
                        premise.PaymentPkk = paymentFirst.DzPkk;
                        premise.PaymentPenalties = paymentFirst.Penalties;
                    }
                    else
                    {
                        premise.PaymentTenancy = "0";
                        premise.PaymentDgi = "0";
                        premise.PaymentPadun = "0";
                        premise.PaymentPkk = "0";
                        premise.PaymentPenalties = "0";
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
                        premise.BalanceOutputDgi = balanceOutputFirst.DzDgi;
                        premise.BalanceOutputPadun = balanceOutputFirst.DzPadun;
                        premise.BalanceOutputPkk = balanceOutputFirst.DzPkk;
                        premise.BalanceOutputPenalties = balanceOutputFirst.Penalties;
                    }
                    else
                    {
                        premise.BalanceOutputTotal = "0";
                        premise.BalanceOutputTenancy = "0";
                        premise.BalanceOutputDgi = "0";
                        premise.BalanceOutputPadun = "0";
                        premise.BalanceOutputPkk = "0";
                        premise.BalanceOutputPenalties = "0";
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
                    CRN = row.Cells[0].Value.AsString().Value(),            // feb, mar, apr, may, jun, jul, aug, sept, oct 0, nov 0
                    Account = row.Cells[1].Value.AsString().Value(),        // feb, mar, apr, may, jun 2, jul 1, aug, sept, oct 2, nov 0
                    Tenant = row.Cells[2].Value.AsString().Value(),         // feb, mar, apr, may, jun 3, jul 2, aug, sept, oct 3, nov 0
                    RawAddress = row.Cells[3].Value.AsString().Value(),     // feb, mar, apr, may, jun 4, jul 3, aug, sept, oct 4, nov 0
                    RawType = row.Cells[4].Value.AsString().Value(),        // feb 7, mar, apr 8, may, jun 6, jul 5, aug, sept, oct 8, nov 6
                    TotalArea = row.Cells[5].Value.AsString().Value(),      // feb 8, mar, apr 9, may, jun 7, jul 6, aug, sept, oct 9, nov 7
                    LivingArea = row.Cells[6].Value.AsString().Value(),     // feb 9, mar, apr 10, may, jun 8, jul 7, aug, sept, oct 10, nov 8
                    Prescribed = row.Cells[7].Value.AsString().Value(),    // feb 10, mar, apr 11, may, jun 9, jul 8, aug, sept, oct 11, nov 9
                    BalanceInput = row.Cells[8].Value.AsString().Value(),  // feb 11,  mar, apr 12, may, jun 10, jul 9, aug, sept, oct 12, nov 10
                    BalanceTenancy = row.Cells[9].Value.AsString().Value(),// feb 12, mar, apr 13, may 11, jun 13, jul 10, aug, sept, oct 15, nov 13
                    Penalties = row.Cells[10].Value.AsString().Value(),
                    BalanceOutput = row.Cells[11].Value.AsString().Value(), // feb 14, mar 15, apr 16, may 14, jun 16, jul 13, aug, sept 18, oct 19, nov 16
                    DzPadun = row.Cells[12].Value.AsString().Value(),
                    DzPkk = row.Cells[13].Value.AsString().Value(),
                    DzDgi = row.Cells[14].Value.AsString().Value(),         // feb, mar empty, apr 15, may 13, jun 15, jul 12, aug, sept 17, oct 18, nov 15
                });
            }
            return premises;
        }
    }
}
