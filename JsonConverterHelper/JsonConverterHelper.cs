using System;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using Newtonsoft.Json;
using System.IO;

namespace IoTEdgeJsonConverterHelper
{
    public class JsonConverterHelper
    {
        public string Object2Json(object obj, Type type)
        {
            string Json = string.Empty;
            MemoryStream stream1 = new MemoryStream();
            DataContractJsonSerializer ser = new DataContractJsonSerializer(type);
            ser.WriteObject(stream1, obj);
            stream1.Position = 0;
            StreamReader sr = new StreamReader(stream1);
            //Console.Write("JSON form of Person object: ");
            return sr.ReadToEnd();
        }

        // public List<Data> GetData(string Json)
        // {
        //     var data = JsonConvert.DeserializeObject<List<Data>>(Json);
        //     return data;
        // }

    }

    [DataContract]
    public class TemperatureSensor
    {
        [DataMember]
        public string timestamp;

        [DataMember]
        public string alerttype;

        [DataMember]
        public double temperature;
    }
}
