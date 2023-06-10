using Dashboard.Db;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dashboard.Models
{
    public struct RevenueByDate
    {
        public string Date { get; set; }
        public decimal TotalAmount { get; set; }
    }
    public class DashBoard : DbConnection
    {
        private DateTime startDate;
        private DateTime endDate;
        private int numberDays;

        public int NumCustomers { get; private set; }
        public int NumSuppliers { get; private set; }
        public int NumProducts { get; private set; }
        public List<KeyValuePair<string, int>> TopProductsList { get; private set; }
        public List<KeyValuePair<string, int>> UnderstockList { get; private set; }
        public List<RevenueByDate> GrossRevenueList { get; private set; }
        public int NumOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalProfit { get; set; }

        // Constructor
        public DashBoard()
        {

        }

        // Private Method
        private void GetNumberItems()
        {
            using (var connection = GetConnection())
            {
                connection.Open();
                using (var command = new SqlCommand())
                {
                    command.Connection =connection;
                    //get total number of customers
                    command.CommandText = "select count(Id) from Customer";
                    NumCustomers = (int)command.ExecuteScalar();

                    // Total number of Suppliers
                    command.CommandText = "select count(Id) from Supplier";
                    NumSuppliers = (int)command.ExecuteScalar();

                    // Total number of Products
                    command.CommandText = "select count(Id) from Product";
                    NumProducts = (int)command.ExecuteScalar();

                    // count number of total order with date between startDate to enDate (is enter in dashboard)
                    command.CommandText = @"select count(Id) from [Order]"+
                                            "where OrderDate between @fromDate to @toDate";
                    command.Parameters.Add("@fromDate", System.Data.SqlDbType.DateTime).Value = startDate;
                    command.Parameters.Add("@toDate", System.Data.SqlDbType.DateTime).Value = endDate;
                    NumOrders = (int)command.ExecuteScalar();
                }
            }
        }
        private void GetOrderAnalisys()
        {
            GrossRevenueList = new List<RevenueByDate>();
            TotalProfit = 0;
            TotalRevenue = 0;

            using (var connection = GetConnection())
            {
                connection.Open();
                using (var command = new SqlCommand())
                {
                    command.Connection =connection;
                    command.CommandText = @"select OrderDate, sum(TotalAmount) from [Order]
                                            where OrderDate between @fromDate and @toDate
                                            group by OrderDate";
                    command.Parameters.Add("@fromDate", System.Data.SqlDbType.DateTime).Value = startDate;
                    command.Parameters.Add("@toDate", System.Data.SqlDbType.DateTime).Value = endDate;
                    var reader = command.ExecuteReader();
                    var resultTable = new List<KeyValuePair<DateTime, decimal>>();
                    while (reader.Read())
                    {
                        resultTable.Add(
                            new KeyValuePair<DateTime, decimal>((DateTime)reader[0], (decimal)reader[1]));
                        TotalRevenue += (decimal) reader[1];
                    }
                    TotalProfit = TotalRevenue * 0.2m; // suppose 20% TotalRevenue
                    reader.Close();

                    // GrossRevenue follow time (days, weeks, months, years)
                    // Group by Days
                    if(numberDays <= 30)
                    {
                        foreach(var item in resultTable)
                        {
                            GrossRevenueList.Add(new RevenueByDate
                            {
                                Date = item.Key.ToString("dd MMM"),
                                TotalAmount = item.Value
                            });
                        }
                    }

                    // Group by Weeks (at least 3 months)
                    else if (numberDays <= 92)
                    {
                        GrossRevenueList = (from orderList in resultTable
                                            group orderList by CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(
                                                orderList.Key, CalendarWeekRule.FirstDay, DayOfWeek.Monday)
                                            into order
                                            select new RevenueByDate
                                            {
                                                Date = "Week " + order.Key.ToString(),
                                                TotalAmount = order.Sum(amount => amount.Value)
                                            }).ToList();
                    }

                    // Group by Month (at least 2 years)
                    else if (numberDays <= (365*2))
                    {
                        bool isYear = numberDays <= 365 ? true : false;
                        GrossRevenueList = (from orderList in resultTable
                                            group orderList by orderList.Key.ToString("MMM yyyy")
                                            into order
                                            select new RevenueByDate
                                            {
                                                Date = isYear ? order.Key.Substring(0, order.Key.IndexOf(" ")) : order.Key.ToString(),
                                                TotalAmount = order.Sum(amount => amount.Value)
                                            }).ToList();
                    }

                    // Group by Year
                    else
                    {
                        GrossRevenueList = (from orderList in resultTable
                                            group orderList by orderList.Key.ToString("yyyy")
                                            into order
                                            select new RevenueByDate
                                            {
                                                Date = order.Key.ToString(),
                                                TotalAmount = order.Sum(amount => amount.Value)
                                            }).ToList();
                    }
                }
            }
        }
        private void GetProductAnalisys()
        {
            TopProductsList = new List<KeyValuePair<string, int>>();
            UnderstockList = new List<KeyValuePair<string, int>>();
            using (var connection = GetConnection())
            {
                connection.Open();
                using (var command = new SqlCommand()) 
                { 
                    command.Connection = connection;

                    // Get 5 Top Product
                    command.CommandText = @"select top 5 P.ProductName, sum(OrderItem.Quantity) as Q
                                            from OrderItem
                                            inner join [Order] O on O.Id = OrderItem.OrderId
                                            inner join Product P on P.Id = OrderItem.ProductId
                                            where OrderDate between @fromDate and @toDate
                                            group by P.ProductName
                                            order by Q desc";
                    command.Parameters.Add("@fromDate", System.Data.SqlDbType.DateTime).Value = startDate;
                    command.Parameters.Add("@toDate", System.Data.SqlDbType.DateTime).Value = endDate;
                    var reader = command.ExecuteReader();
                    while(reader.Read()) 
                    {
                        TopProductsList.Add(
                            new KeyValuePair<string, int>(reader[0].ToString(), (int)reader[1]));
                    }
                    reader.Close();

                    // Get UnderStock
                    command.CommandText = @"select ProductName, Stock 
                                            from Product
                                            where Stock <= 6 and IsDiscontinued = 0";
                    reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        UnderstockList.Add(
                            new KeyValuePair<string, int>(reader[0].ToString(), (int)reader[1]));
                    }
                    reader.Close();
                }
            }
        }

        // Public Method
    }
}
