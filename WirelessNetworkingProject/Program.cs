using System;
using System.Threading;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace WirelessNetworkingProject
{
    class WirelessNetworkingProject
    {
        static void Main(string[] args)
        {
            if(args.Length != 4 || args[0] == "-help" || args[0] == "-h")
            {
                Console.Out.WriteLine("Must Pass four arguments to the process");
                Console.Out.WriteLine("1) Device Id - This must be registered in the Database");
                Console.Out.WriteLine("2) Latitude");
                Console.Out.WriteLine("3) Longitude");
                Console.Out.WriteLine("4) Warning Temp");
            }
            int deviceId = int.Parse(args[0]);
            DatabaseConnection database = new DatabaseConnection();
            try
            {
                database.ActivateDevice(deviceId);
                Task.Factory.StartNew(() => Monitor(deviceId, args[1], args[2], args[3], database));
                Console.Out.WriteLine("Press any key to quit");
                Console.ReadKey();
                database.DeactivateDevices(deviceId);
            }
            catch
            {
                database.DeactivateDevices(deviceId);
                throw;
            }
            
        }
        static void Monitor(int deviceId, string longitude, string latitude, string warningTemp, DatabaseConnection database)
        {
            var warningThreshhold = double.Parse(warningTemp);
            Sensor tempProbe = new Sensor();
            while (true)
            {
                var tempReading = GetCurrentTemp(tempProbe);
                if (tempReading > warningThreshhold)
                    Console.Out.WriteLine(String.Format("WARNING: Temperature reading of {0} exceeded the warning threshhold of {1}."
                        , tempReading, warningTemp));
                database.WriteTemperature(tempReading, deviceId, longitude, latitude, tempReading > warningThreshhold);
                Thread.Sleep(10000);
            }
        }
        public static double GetCurrentTemp(Sensor currentProbe)
        {
            return currentProbe.GetTemperature();
        } 
    }
    class DatabaseConnection
    {
        MySqlConnection database;
        public void WriteTemperature(double currentTemperature, int deviceId, string longitude, string latitude, bool warning)
        {
            var insertStatement = database.CreateCommand();
            insertStatement.CommandText = "INSERT INTO readings (deviceId, currentTemp, longitude, latitude, readingTime)" +
                String.Format("VALUES ('{0}', '{1}', '{2}', '{3}', '{4}')", deviceId, currentTemperature, longitude, latitude, DateTime.Now.ToString("yyyy-MM-14 HH:mm"));
            var warningStatement = database.CreateCommand();
            warningStatement.CommandText = String.Format("Update devices SET warning={0} WHERE deviceId={1}", warning, deviceId);
            insertStatement.ExecuteNonQuery();
            warningStatement.ExecuteNonQuery();
        }
        public void ActivateDevice(int deviceId)
        {
            var query = database.CreateCommand();
            query.CommandText = String.Format("SELECT * FROM devices WHERE deviceId={0}", deviceId);
            var result = query.ExecuteReader();
            
            if (!result.HasRows)
            {
                result.Close();
                query.CommandText = String.Format("INSERT INTO devices (deviceId, warning, active) VALUES ({0}, False, True)", deviceId);
                query.ExecuteNonQuery();
            }
            else
            {
                result.Read();
                var isActive = result.GetBoolean(2);
                result.Close();
                if (isActive)
                    throw new InvalidOperationException(String.Format("Device {0} is already active we cannot activate it twice.", deviceId));
                query.CommandText = String.Format("UPDATE devices SET active=True WHERE deviceId={0}", deviceId);
                query.ExecuteNonQuery();
            }
        }

        public void DeactivateDevices(int deviceId)
        {
            var query = database.CreateCommand();
            query.CommandText = String.Format("UPDATE devices SET active=False WHERE deviceId={0}", deviceId);
            query.ExecuteNonQuery();
            database.Close();

        }
        //This sensor is a fake place holder for when we connect to an actual temperature probe.
        class Sensor
        {
            Random rand = new Random(DateTime.Now.Millisecond);
            public double GetTemperature()
            {
                return (rand.Next() % 480 + 500) / 10.0;
            }
        }
        public DatabaseConnection()
        {

            var myConnectionString = "server=us-cdbr-azure-central-a.cloudapp.net;uid=bddd8eb1e6edc6;" +
            "pwd=0019b5d3;database=as_e6179eb49bd1184;";

            try
            {
                database = new MySqlConnection();
                database.ConnectionString = myConnectionString;
                database.Open();
            }
            catch (MySqlException ex)
            {
                Console.Out.WriteLine("Database connection failed: ");
                Console.Out.WriteLine(ex);
            }
        }
    }


}
