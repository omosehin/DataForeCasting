using ClosedXML.Excel;
//using Extreme.Statistics.TimeSeriesAnalysis;
using Extreme.Mathematics;
using Extreme.Statistics.TimeSeriesAnalysis;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Office.Interop.Excel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using NsExcel = Microsoft.Office.Interop.Excel;



namespace DataForeCasting.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DataForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<DataForecastController> _logger;

        public DataForecastController(ILogger<DataForecastController> logger)
        {
            _logger = logger;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <param name="meterNumber"></param>
        /// <param name="predictionLength"></param>
        /// <returns></returns>

        [HttpGet]
        public List<ForeCastedDTO> Get(string startDate, string endDate, string meterNumber, int predictionLength = 30)
        {
            var finalResult = new List<ForeCastedDTO>();
            //training date
            DateTime oDate = DateTime.ParseExact(startDate, "yyyy-MM-dd", CultureInfo.InvariantCulture); //1 
            DateTime iDate = DateTime.ParseExact(endDate, "yyyy-MM-dd", CultureInfo.InvariantCulture); //10days

            //date difference for autoregressive

            int dayDifference =  iDate.Day - oDate.Day;
            //prediction date
            DateTime tDate = iDate.AddMinutes(30);//1  startDate
            DateTime eDate = tDate.AddDays(predictionLength); //days

            //ecwin energy  prediction date
            var getPredictionData = GetPowerQualityDataFix(tDate, eDate, meterNumber); // 11-14days  // the startdate is a day after the get training day and end date is adding the predictionlength to the startdate

            //ecwin energy  training  date
            var getTrainingData = GetPowerQualityDataFix(oDate, iDate, meterNumber); // 1-10 days  //
            //var retrievePredictionData = getPredictionData.Select(x => x.ConsumptionKWA).ToArray();
            //Excluding 0 from data set
            var retrievePredictionData = getPredictionData.Select(x => x.ConsumptionKWA).Where(c => c > 0).ToArray();
            var retrieveTrainingData = getTrainingData.Select(x => x.ConsumptionKWA).Where(c => c > 0).ToArray();
            //var retrieveOutPutTrainingData = getTrainingData.Select(x => x.ConsumptionKWA).ToList();

            // var result = new List<double>();
            //modelling for training data
            var trainingSunspots = Extreme.Mathematics.Vector.Create(retrieveTrainingData);

           // ArimaModel model = new ArimaModel(trainingSunspots, 12, 0,2);
            ArimaModel model = new ArimaModel(trainingSunspots, predictionLength/2, 0,3);
            model.Fit();
            var getPredictionlength = retrievePredictionData.Length;
            //if (getPredictionData.Count > retrieveOutPutTrainingData.Count)
            //{
            //    int zeroCount = getPredictionData.Count - retrieveOutPutTrainingData.Count;
            //    for (int i = 0; i < zeroCount; i++)
            //    {
            //        retrieveOutPutTrainingData.Add(0);
            //    }
            //};

            // or to predict a specified number of values:
            var forecastValues = model.Forecast(getPredictionlength).Select(x => x).ToList(); /// prediction data length
            for (int i = 0; i < getPredictionlength; i++)
            {
                finalResult.Add(new ForeCastedDTO { ActualData = retrievePredictionData[i], foreCastData = forecastValues[i]});
            }
           // finalResult = finalResult.Where(x => x.ActualData > 0).ToList();
            //foreach (var item in retrieveTrainingData)
            //{
            //    finalResult.Add(new ForeCastedDTO { trainingData = item });
            //};
           // finalResult = finalResult.Where(c=>c.ActualData !=0).ToList();
            SaveToCsv<ForeCastedDTO>(finalResult);
            return finalResult;

        }
        public List<ConsumptionDTO> GetPowerQualityDataFix(DateTime startDate, DateTime endDate, string MeterNumber)
        {
            var consumptionProfiles = new List<ConsumptionDTO>();

            string powerQualitySP = "dbo.GetConsumptionProfileData";
            using var sqlAdapter = new SqlDataAdapter();
            // Console.WriteLine(EnvironmentConfig.ECWinConnection);
            var dbConnection = new SqlConnection("Server=xxxxxx;initial catalog=xxxxxx;Persist Security Info=True;User Id=reader;password=xxxxxxx;MultipleActiveResultSets=True");

            sqlAdapter.SelectCommand = new SqlCommand(powerQualitySP, dbConnection);
            sqlAdapter.SelectCommand.CommandType = CommandType.StoredProcedure;
            sqlAdapter.SelectCommand.Parameters.Add("@StartDate", SqlDbType.DateTime).Value = startDate;
            sqlAdapter.SelectCommand.Parameters.Add("@EndDate", SqlDbType.DateTime).Value = endDate;
            sqlAdapter.SelectCommand.Parameters.Add("@MeterNumber", SqlDbType.VarChar).Value = MeterNumber;

            var dataSet = new DataSet();
            sqlAdapter.Fill(dataSet);
            consumptionProfiles = dataSet.Tables[0].AsEnumerable()
                .Select(dataRow => new ConsumptionDTO
                {
                    ConsumptionKWA = dataRow.Field<double?>("CONSUMPTION KWH") ?? 0.0,
                    ReadingPeriod = ConvertECWinDateStringToDate(dataRow.Field<string>("DATE"),
                        dataRow.Field<string>("TIME"))
                }).ToList();
            return consumptionProfiles;
        }
        public DateTime ConvertECWinDateStringToDate(string date, string time)
        {
            var dateArray = date.Split('-');
            var timeArray = time.Split(':');
            var dateTime = new DateTime(Convert.ToInt32(dateArray[0]),
                Convert.ToInt32(dateArray[1]), Convert.ToInt32(dateArray[2]),
                Convert.ToInt32(timeArray[0]), Convert.ToInt32(timeArray[1]), 0);

            return dateTime;
        }
        private void SaveToCsv<T>(List<ForeCastedDTO> reportData)
        {
            using (StreamWriter file = new StreamWriter(@"‪Data73014568.csv"))
            {
                foreach (var item in reportData)
                {
                    file.WriteLine(string.Format("{0},{1}", item.ActualData, item.foreCastData));
                }
            }
        }

    }

}
