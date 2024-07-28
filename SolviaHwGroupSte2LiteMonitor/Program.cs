using System.Data.SQLite;
using System.Xml;

namespace XmlToSqlite
{
    class Program
    {
        private static readonly HttpClient client = new HttpClient();
        private const string url = "http://192.168.147.222/values.xml";
        private const string connectionString = "Data Source=SensorData.db";

        static async Task Main(string[] args)
        {
            InitializeDatabase();

            while (true)
            {
                try
                {
                    Console.WriteLine("Fetching XML...");
                    string xmlContent = await client.GetStringAsync(url);
                    Console.WriteLine("XML fetched successfully:\n" + xmlContent);
                    ParseAndStoreXml(xmlContent);
                    Console.WriteLine("Data stored in database.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }

                await Task.Delay(5000); // Wait for 1 second
            }
        }

        private static void InitializeDatabase()
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string tableQuery = @"
                    CREATE TABLE IF NOT EXISTS SensorData (
                        ID INTEGER PRIMARY KEY AUTOINCREMENT,
                        Version TEXT,
                        XmlVer TEXT,
                        DeviceName TEXT,
                        Model TEXT,
                        MAC TEXT,
                        IP TEXT,
                        MASK TEXT,
                        WifiIP TEXT,
                        WifiMASK TEXT,
                        SysName TEXT,
                        SysLocation TEXT,
                        SysContact TEXT,
                        UpTime INTEGER,
                        SensorID INTEGER,
                        SensorName TEXT,
                        Units TEXT,
                        Value REAL,
                        Min REAL,
                        Max REAL,
                        Hyst REAL,
                        AlarmMsgRecipID INTEGER,
                        State INTEGER,
                        StatusState INTEGER,
                        StatusAlarm INTEGER,
                        Exp INTEGER,
                        Timestamp DATETIME DEFAULT CURRENT_TIMESTAMP
                    )";
                using (var command = new SQLiteCommand(tableQuery, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

        private static void ParseAndStoreXml(string xmlContent)
        {
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xmlContent);

            var namespaceManager = GetNamespaceManager(xmlDoc);

            Console.WriteLine("Searching for Agent node...");
            var agentNode = xmlDoc.SelectSingleNode("//val:Root/Agent", namespaceManager);

            if (agentNode == null)
            {
                Console.WriteLine("Error: Agent node is missing.");
                return;
            }
            Console.WriteLine("Agent node found.");

            Console.WriteLine("Searching for Sensor entry node...");
            var sensorNode = xmlDoc.SelectSingleNode("//val:Root/SenSet/Entry", namespaceManager);
            if (sensorNode == null)
            {
                Console.WriteLine("Error: Sensor entry node is missing.");
                return;
            }
            Console.WriteLine("Sensor entry node found.");

            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string insertQuery = @"
                    INSERT INTO SensorData (
                        Version, XmlVer, DeviceName, Model, MAC, IP, MASK, WifiIP, WifiMASK, SysName, SysLocation, SysContact, UpTime,
                        SensorID, SensorName, Units, Value, Min, Max, Hyst, AlarmMsgRecipID, State, StatusState, StatusAlarm, Exp
                    ) VALUES (
                        @Version, @XmlVer, @DeviceName, @Model, @MAC, @IP, @MASK, @WifiIP, @WifiMASK, @SysName, @SysLocation, @SysContact, @UpTime,
                        @SensorID, @SensorName, @Units, @Value, @Min, @Max, @Hyst, @AlarmMsgRecipID, @State, @StatusState, @StatusAlarm, @Exp
                    )";

                using (var command = new SQLiteCommand(insertQuery, connection))
                {
                    command.Parameters.AddWithValue("@Version", agentNode["Version"]?.InnerText);
                    command.Parameters.AddWithValue("@XmlVer", agentNode["XmlVer"]?.InnerText);
                    command.Parameters.AddWithValue("@DeviceName", agentNode["DeviceName"]?.InnerText);
                    command.Parameters.AddWithValue("@Model", agentNode["Model"]?.InnerText);
                    command.Parameters.AddWithValue("@MAC", agentNode["MAC"]?.InnerText);
                    command.Parameters.AddWithValue("@IP", agentNode["IP"]?.InnerText);
                    command.Parameters.AddWithValue("@MASK", agentNode["MASK"]?.InnerText);
                    command.Parameters.AddWithValue("@WifiIP", agentNode["wifi_IP"]?.InnerText);
                    command.Parameters.AddWithValue("@WifiMASK", agentNode["wifi_MASK"]?.InnerText);
                    command.Parameters.AddWithValue("@SysName", agentNode["sys_name"]?.InnerText);
                    command.Parameters.AddWithValue("@SysLocation", agentNode["sys_location"]?.InnerText);
                    command.Parameters.AddWithValue("@SysContact", agentNode["sys_contact"]?.InnerText);
                    command.Parameters.AddWithValue("@UpTime", agentNode["UpTime"]?.InnerText);
                    command.Parameters.AddWithValue("@SensorID", sensorNode["ID"]?.InnerText);
                    command.Parameters.AddWithValue("@SensorName", sensorNode["Name"]?.InnerText);
                    command.Parameters.AddWithValue("@Units", sensorNode["Units"]?.InnerText);
                    command.Parameters.AddWithValue("@Value", sensorNode["Value"]?.InnerText);
                    command.Parameters.AddWithValue("@Min", sensorNode["Min"]?.InnerText);
                    command.Parameters.AddWithValue("@Max", sensorNode["Max"]?.InnerText);
                    command.Parameters.AddWithValue("@Hyst", sensorNode["Hyst"]?.InnerText);
                    command.Parameters.AddWithValue("@AlarmMsgRecipID", sensorNode["AlarmMsgRecipID"]?.InnerText);
                    command.Parameters.AddWithValue("@State", sensorNode["State"]?.InnerText);
                    command.Parameters.AddWithValue("@StatusState", sensorNode["status"]["state"]?.InnerText);
                    command.Parameters.AddWithValue("@StatusAlarm", sensorNode["status"]["alarm"]?.InnerText);
                    command.Parameters.AddWithValue("@Exp", sensorNode["Exp"]?.InnerText);

                    command.ExecuteNonQuery();
                }
            }
        }

        private static XmlNamespaceManager GetNamespaceManager(XmlDocument xmlDoc)
        {
            var nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);
            nsmgr.AddNamespace("val", "http://www.hw-group.com/XMLSchema/ste/values.xsd");
            return nsmgr;
        }
    }
}
