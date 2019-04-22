namespace MessageStorageModule
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Runtime.Loader;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Client.Transport.Mqtt;
    using System.Collections.Generic;
    using IoTEdgeJsonConverterHelper;
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Json;
    using Newtonsoft.Json;
    using SQLConnection;

    class Program
    {
        static int counter;

        public static long ToEpoch(DateTime dateTime) => (long)(dateTime - new DateTime(1970, 1, 1)).TotalSeconds;

        public static long ToEpochutc() => (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;

        static void Main(string[] args)
        {
            Init().Wait();

            // Wait until the app unloads or is cancelled
            var cts = new CancellationTokenSource();
            AssemblyLoadContext.Default.Unloading += (ctx) => cts.Cancel();
            Console.CancelKeyPress += (sender, cpe) => cts.Cancel();
            WhenCancelled(cts.Token).Wait();
        }

        /// <summary>
        /// Handles cleanup operations when app is cancelled or unloads
        /// </summary>
        public static Task WhenCancelled(CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();
            cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).SetResult(true), tcs);
            return tcs.Task;
        }

        /// <summary>
        /// Initializes the ModuleClient and sets up the callback to receive
        /// messages containing temperature information
        /// </summary>
        static async Task Init()
        {
            MqttTransportSettings mqttSetting = new MqttTransportSettings(TransportType.Mqtt_Tcp_Only);
            ITransportSettings[] settings = { mqttSetting };

            // Open a connection to the Edge runtime
            ModuleClient ioTHubModuleClient = await ModuleClient.CreateFromEnvironmentAsync(settings);
            await ioTHubModuleClient.OpenAsync();
            Console.WriteLine("IoT Hub module client initialized.");

            // Register callback to be called when a message is received by the module
            await ioTHubModuleClient.SetInputMessageHandlerAsync("input1", PipeMessage, ioTHubModuleClient);
        }

        /// <summary>
        /// This method is called whenever the module is sent a message from the EdgeHub. 
        /// It just pipe the messages without any change.
        /// It prints all the incoming messages.
        /// </summary>
        static async Task<MessageResponse> PipeMessage(Message message, object userContext)
        {
            int counterValue = Interlocked.Increment(ref counter);

            var moduleClient = userContext as ModuleClient;
            if (moduleClient == null)
            {
                throw new InvalidOperationException("UserContext doesn't contain " + "expected values");
            }

            byte[] messageBytes = message.GetBytes();
            string messageString = Encoding.UTF8.GetString(messageBytes);

            Console.WriteLine($"SQL Received message: {counterValue}, Body: [{messageString}]");

            /**************************************************************************************************/

            try
            {
                string str = "Server=tcp:<your sql IP>,1433;Initial Catalog=<databasename>;User ID=<username>;Password=<Password>;TrustServerCertificate=False;Connection Timeout=30;";
                if (Environment.GetEnvironmentVariable("SqlConnectionString") != string.Empty)
                {
                    str = Environment.GetEnvironmentVariable("SqlConnectionString");
                }
                string _sql = string.Empty;
                List<TemperatureSensor> _listts = null;
                string _json = messageString;
                if (_json.StartsWith("[") && _json.EndsWith("]"))
                {
                    List<TemperatureSensor> lts = JsonConvert.DeserializeObject<List<TemperatureSensor>>(_json);
                    _listts = lts;
                }
                else
                {
                    TemperatureSensor ts = JsonConvert.DeserializeObject<TemperatureSensor>(_json);
                    _listts = new List<TemperatureSensor>();
                    _listts.Add(ts);
                }
                foreach (TemperatureSensor ts in _listts)
                {
                    DateTime dt = DateTime.Parse(ts.timestamp);
                    string _subject = "TEMPERATURE > 50";
                    string _notificationText = "TEMPERATURE > 50 and TEMPERATURE IS " + ts.temperature + "C°";
                    string _epochtimes = Program.ToEpoch(dt).ToString();
                    System.Text.StringBuilder _sb = new System.Text.StringBuilder();
                    _sb.Append("INSERT INTO[dbo].[OrgNotifications]([TenantCode],[BaseOrgCode],[CreatorType],[CreatorId],[ReceiverId],[NotificationCategoryCode],[Subject],[NotificationText],[SentOn],[ReceivedOn])");
                    _sb.Append("VALUES('MIND','MSSLU16',");
                    _sb.Append("'IoTModule',");
                    _sb.Append("52,52,'ALERT',");
                    _sb.Append("'" + _subject + "',");
                    _sb.Append("'" + _notificationText + "',");
                    _sb.Append("'" + _epochtimes + "',");
                    _sb.Append("'" + _epochtimes + "'");
                    _sb.Append(")");
                    _sql += _sb.ToString();
                }

                Console.WriteLine("SQL script: " + _sql);

                string result = new EdgeSqlModuleHelper(str).ExecuteSQL(_sql);

                Console.WriteLine(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            /*************************************************************************************************/

            if (!string.IsNullOrEmpty(messageString))
            {
                var pipeMessage = new Message(messageBytes);
                foreach (var prop in message.Properties)
                {
                    pipeMessage.Properties.Add(prop.Key, prop.Value);
                }
                await moduleClient.SendEventAsync("output1", pipeMessage);
                Console.WriteLine("Received message sent");
            }
            return MessageResponse.Completed;
        }
    }
}
