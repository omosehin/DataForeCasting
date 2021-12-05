using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace DataForeCasting
{
    public class ConsumptionDTO
    {
        public double ConsumptionKWA { get; set; }
        public DateTime ReadingPeriod { get; set; }
    }

    public class ForeCastedDTO
    {
        public double ActualData { get; set; }
        public double foreCastData { get; set; }
        public double trainingData { get; set; }
    }
    public class GetEcwinData
    {
        public List<ConsumptionDTO> GetPowerQualityDataFix(string startDate, string endDate,string MeterNumber)
        {
            var consumptionProfiles = new List<ConsumptionDTO>();

            string powerQualitySP = "dbo.GetConsumptionProfileData";
            using var sqlAdapter = new SqlDataAdapter();
           // Console.WriteLine(EnvironmentConfig.ECWinConnection);
            var dbConnection = new SqlConnection("Data Source=10.128.1.58;initial catalog=EC7EKO;Persist Security Info=True;User Id=reader;password=re@der;MultipleActiveResultSets=True");

            sqlAdapter.SelectCommand = new SqlCommand(powerQualitySP, dbConnection);
            sqlAdapter.SelectCommand.CommandType = CommandType.StoredProcedure;
            sqlAdapter.SelectCommand.Parameters.Add("@StartDate", SqlDbType.NVarChar).Value = startDate;
            sqlAdapter.SelectCommand.Parameters.Add("@EndDate", SqlDbType.NVarChar).Value = endDate;
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
    }
}
