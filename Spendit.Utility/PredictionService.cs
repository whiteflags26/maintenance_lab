using Spendit.DataAccess;
using Spendit.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Spendit.Services
{
    public class PredictionService
    {
        private readonly ApplicationDbContext _context;

        public PredictionService(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<MonthlyData> GetHistoricalData(string userId)
        {
            var transactions = _context.Transactions
                .Where(t => t.Category.UserId == userId)
                .ToList();

            var monthlyData = transactions
                .GroupBy(t => new { t.Date.Year, t.Date.Month })
                .Select(g => new MonthlyData
                {
                    Month = new DateTime(g.Key.Year, g.Key.Month, 1),
                    TotalAmount = g.Sum(t => t.Category.Type == CategoryType.Income ? t.Amount : -t.Amount)
                })
                .OrderBy(m => m.Month)
                .ToList();
            
            return monthlyData;
        }

        public List<MonthlyData> GetPredictedData(List<MonthlyData> historicalData, int futureMonths = 3)
        {
            int n = historicalData.Count;
            if (n < 12) throw new InvalidOperationException("Not enough data to perform linear regression");

            var xValues = Enumerable.Range(0, n).Select(i => (double)i).ToArray();
            var yValues = historicalData.Select(h => (double)h.TotalAmount).ToArray();

            var (slope, intercept) = CalculateLinearRegression(xValues, yValues);

            var lastAmount = historicalData.Last().TotalAmount;
            var dataPoints = new List<MonthlyData>();

            for (int i = 0; i < futureMonths; i++)
            {
                var futureMonth = historicalData.Last().Month.AddMonths(i);
                double y = slope * (n + i) + intercept;

                dataPoints.Add(new MonthlyData
                {
                    Month = futureMonth,
                    TotalAmount = (int)y
                });
            }
            
            var predictedData = new List<MonthlyData>();

            foreach (var point in historicalData)
            {
                DateTime targetMonth = point.Month;
                int targetTotalAmount = CalculateYValue(dataPoints, targetMonth);
               
                predictedData.Add(new MonthlyData
                {
                    Month = targetMonth,
                    TotalAmount = (int)targetTotalAmount
                });
            }

            foreach (var point in dataPoints)
            {
                predictedData.Add(point);
            }

            return predictedData;
        }

        private (double slope, double intercept) CalculateLinearRegression(double[] x, double[] y)
        {
            double xAvg = x.Average();
            double yAvg = y.Average();
            double sumXy = 0;
            double sumX2 = 0;

            for (int i = 0; i < x.Length; i++)
            {
                sumXy += (x[i] - xAvg) * (y[i] - yAvg);
                sumX2 += (x[i] - xAvg) * (x[i] - xAvg);
            }

            double slope = sumXy / sumX2;
            double intercept = yAvg - slope * xAvg;

            return (slope, intercept);
        }

        public double CalculateSlope(List<MonthlyData> points)
        {
            // Ensure we have exactly 3 points
            if (points.Count != 3)
            {
                throw new ArgumentException("There must be exactly 3 points.");
            }

            // Calculate the slope (assuming points are on a straight line)
            double deltaX = (points[1].Month - points[0].Month).TotalDays;
            double deltaY = points[1].TotalAmount - points[0].TotalAmount;
            double slope = deltaY / deltaX;

            return slope;
        }

        public int CalculateYValue(List<MonthlyData> points, DateTime targetMonth)
        {
            double slope = CalculateSlope(points);
            DateTime referenceMonth = points[0].Month;
            int referenceAmount = points[0].TotalAmount;

            // Calculate the number of days between the reference month and the target month
            double daysDifference = (targetMonth - referenceMonth).TotalDays;

            // Calculate the target TotalAmount
            int targetAmount = (int)(referenceAmount + slope * daysDifference);

            return targetAmount;
        }
    }

    public class MonthlyData
    {
        public DateTime Month { get; set; }
        public int TotalAmount { get; set; }
    }
}
