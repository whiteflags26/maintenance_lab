using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spendit.DataAccess;
using Spendit.Models;
using Spendit.Services;
using System.Globalization;

namespace SpenditWeb.Controllers
{
    [Authorize]
    public class Dashboard : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly PredictionService _predictionService;

        public Dashboard(ILogger<HomeController> logger, ApplicationDbContext context, UserManager<IdentityUser> userManager, PredictionService predictionService)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
            _predictionService = predictionService;
        }

        public async Task<ActionResult> Index()
        {
            DateTime StartDate = DateTime.Today.AddDays(-29);
            DateTime EndDate = DateTime.Today.AddDays(1);

            List<Transaction> SelectedTransactions = await _context.Transactions
            .Include(x => x.Category)
                .Where(y => y.Date >= StartDate && y.Date <= EndDate && y.Category.UserId == _userManager.GetUserId(User))
                .ToListAsync();

            // Last 30 day income
            int TotalIncome = SelectedTransactions
                .Where(i => i.Category.Type == CategoryType.Income)
                .Sum(j => j.Amount);

            ViewBag.TotalIncome = TotalIncome.ToString("c0");

            // Last 30 day expense
            int TotalExpense = SelectedTransactions
                .Where(i => i.Category.Type == CategoryType.Expense)
                .Sum(j => j.Amount);

            ViewBag.Expense = TotalExpense.ToString("c0");


            // Last 30 day balance
            int Balance = TotalIncome - TotalExpense;
            int AbsoluteBalance = (Balance < 0) ? Balance * -1 : Balance;

            ViewBag.Balance = (Balance < 0 ? "-" : "") + AbsoluteBalance.ToString("c0");
            ViewBag.TrueBalance = Balance.ToString();


            //Doughnut Chart - Income By Category
            ViewBag.DoughnutChartData1 = SelectedTransactions
                .Where(i => i.Category.Type == CategoryType.Income)
                .GroupBy(j => j.Category.CategoryId)
                .Select(k => new
                {
                    CategoryTitleWithIcon = k.First().Category.Icon + " " + k.First().Category.Title,
                    amount = k.Sum(j => j.Amount),
                    formattedAmount = k.Sum(j => j.Amount).ToString("C0"),
                })
                .OrderByDescending(l => l.amount)
                .ToList();

            //Doughnut Chart - Expense By Category
            ViewBag.DoughnutChartData2 = SelectedTransactions
                .Where(i => i.Category.Type == CategoryType.Expense)
                .GroupBy(j => j.Category.CategoryId)
                .Select(k => new
                {
                    CategoryTitleWithIcon = k.First().Category.Icon + " " + k.First().Category.Title,
                    amount = k.Sum(j => j.Amount),
                    formattedAmount = k.Sum(j => j.Amount).ToString("C0"),
                })
                .OrderByDescending(l => l.amount)
                .ToList();

            //Spline Chart - Income vs Expense

            //Income
            List<SplineChartData> IncomeSummary = SelectedTransactions
                .Where(i => i.Category.Type == CategoryType.Income)
                .GroupBy(j => j.Date)
                .Select(k => new SplineChartData()
                {
                    day = k.First().Date.ToString("dd"),
                    income = k.Sum(l => l.Amount)
                })
                .ToList();

            //Expense
            List<SplineChartData> ExpenseSummary = SelectedTransactions
                .Where(i => i.Category.Type == CategoryType.Expense)
                .GroupBy(j => j.Date)
                .Select(k => new SplineChartData()
                {
                    day = k.First().Date.ToString("dd"),
                    expense = k.Sum(l => l.Amount)
                })
                .ToList();

            //Combine Income & Expense
            string[] Last30Days = Enumerable.Range(0, 30)
                .Select(i => StartDate.AddDays(i).ToString("dd"))
                .ToArray();

            ViewBag.SplineChartData = from day in Last30Days
                                      join income in IncomeSummary on day equals income.day into dayIncomeJoined
                                      from income in dayIncomeJoined.DefaultIfEmpty()
                                      join expense in ExpenseSummary on day equals expense.day into expenseJoined
                                      from expense in expenseJoined.DefaultIfEmpty()
                                      select new
                                      {
                                          day = day,
                                          income = income == null ? 0 : income.income,
                                          expense = expense == null ? 0 : expense.expense,
                                      };
            //Recent Transactions
            ViewBag.RecentTransactions = await _context.Transactions
                .Where(h => h.Category.UserId == _userManager.GetUserId(User))
                .Include(i => i.Category)
                .OrderByDescending(j => j.Date)
                .Take(10)
            .ToListAsync();



            
            //prediction graph
            bool predictionSuccess = true;

            try
            {
                var historicalData = _predictionService.GetHistoricalData(_userManager.GetUserId(User));
                var predictedData = _predictionService.GetPredictedData(historicalData);

                var chartData = historicalData
                    .Select(h => new ChartData
                    {
                        Period = h.Month.ToString("MMM, yy", CultureInfo.InvariantCulture),
                        Amount = h.TotalAmount
                    })
                    .ToList();

                var predictedChartData = predictedData
                    .Select(p => new ChartData
                    {
                        Period = p.Month.ToString("MMM, yy", CultureInfo.InvariantCulture),
                        Amount = p.TotalAmount
                    })
                    .ToList();

                ViewBag.HistoricalData = chartData;
                ViewBag.PredictedData = predictedChartData;
            }
            catch (InvalidOperationException ex)
            {
                predictionSuccess = false; 
            }

            ViewBag.PredictionSuccess = predictionSuccess;

            return View();
        }
    }
    public class SplineChartData
    {
        public string day;
        public int income;
        public int expense;

    }

    public class ChartData
    {
        public string Period { get; set; }
        public int Amount { get; set; }
    }
}
