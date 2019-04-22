# Azure IoT Edge On-Premise Solution
This is a sample architecture Solution/ Code for Azure IoT edge **On-Premise** Data processing and storage.
Caused by in many industry or scenarios our customer will request us as their data is sensitive and classified so they can not send data to the cloud platform directly. If that happens to you, I hope this sample will help for you and your customer.

So In this solution, you will see how to use Azure IoT Edge for The data collection, data stream analysis, data processing, and finally save the data to the local SQL database in the on-premise environment. However, all the configuration and deployment are based on IoT Edge, This will reduce customers a lot of effort in deploying and managing equipment.

![Azure IoT Edge On-Premise Gateway Architecture](https://github.com/Nick287/AzureIoTEdgeOnPremiseSolution/blob/master/Img/Motherson%20Azure%20IoTEdge%20On-Premise%20Solution.jpg?raw=true)

For the sample code you will see have 4 module here：
1. ***TemperatureSensorModule*** this module will send the Json message in to the IoT Edge Runtime. (temperature value will between 10 and 70 when the value over 50 alerttype will form 'normal' change to 'overheating' )

    ```js
    {"alerttype":"normal","temperature":29.4,"timestamp":"04/22/2019 07:33:41"}
    ```

2. ***Azure Stream Analytics on IoT Edge*** this module will receive message form ***TemperatureSensorModule*** and then output the message when the temperature property value above 50. Please setup your ASA module on you own Azure subscription follow the tutorials [Azure Stream Analytics on IoT Edge](https://docs.microsoft.com/en-us/azure/stream-analytics/stream-analytics-edge)

    please use this Query script configure your ASA
    ``` sql
    SELECT
    *
    INTO
        AlertMessage
    FROM
        TemperatureModuleOutput
    WHERE
        temperature > 50
    ```

> **Note:**  
For each deployment, a new subfolder is created in the "EdgeJobs" folder. In order to deploy your job to edge devices, ASA creates a shared access signature (SAS) for the job definition file. The SAS key is securely transmitted to the IoT Edge devices using device twin. The expiration of this key is three years from the day of its creation.

3. ***SQLModule*** it's Data processing module, this module will receive the message only the temperature value above 50 and then SQLModule generate the SQL sctipt and connect to the local SQL Server for execute it. In this solution sample code have a SQLConnection project it is use to connect SQL and execute SQL script. and there have other project **MessageStorageModule** this project is uesd to processing data. 

please configure your string Sql connection string in your 'deployment.template.json' file

```js
"MessageStorageModule": {
    "version": "1.0",
    "type": "docker",
    "status": "running",
    "restartPolicy": "always",
    "settings": {
    "image": "${MODULES.MessageStorageModule}",
    "createOptions": {}
    },
    "env": {
    "SqlConnectionString": {
        "value": "Data Source=52.172.30.81;Initial Catalog=iDACSDB;User Id=SA;Password=Mind@987;TrustServerCertificate=False;Connection Timeout=30;"
    }
    }
}
```
Please reference generate the SQL sctipt code here

``` C#
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
                    _sb.Append("INSERT INTO[dbo].[TableName]([DeviceID],[ModuleName],[MessageType],[Subject],[NotificationText],[ReceivedOn])");
                    _sb.Append("VALUES('BoDevice001',");
                    _sb.Append("'IoTModule',");
                    _sb.Append("'ALERT',");
                    _sb.Append("'" + _subject + "',");
                    _sb.Append("'" + _notificationText + "',");
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
```

