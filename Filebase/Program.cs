using Firebase.Database;
using Firebase.Database.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Reactive.Linq;
using MQTTnet.Core;
using MQTTnet.Core.Protocol;
using MQTTnet.Core.Client;
using MQTTnet;
using MQTTnet.Core.Packets;
using Newtonsoft.Json;

using Accord;
using Accord.IO;
using Accord.MachineLearning.DecisionTrees;
using Accord.MachineLearning.DecisionTrees.Learning;
using Accord.Math;
using Accord.Statistics.Analysis;
using System.Data;
using System.Net.Http;
using System.Web.UI.WebControls;

namespace IotHub
{
    class Program
    {
        //Excel file for machine learning
        const string excelPath = @"<excelPath>";

        //FirebaseUrl url https://<Projectname>.firebaseio.com/
        const string FirebaseUrl = "https://<Projectname>.firebaseio.com/";


        const string MqttServer = "<host>";
        const int MqttPort = 19518;
        const string MqttUser = "<user>";
        const string MqttPassword = "<password>";
        // learning Tree
        static DecisionTree tree;


        //Line notification
        const string UserIdForNoti = "<UserId>";
        const string LineToken = "<token>";
        static void Main(string[] args)
        {
            TrainingData(excelPath);
             mqttTeparak().Wait();
        }
        
        private static void TrainingData(string filename)
        {
            // source column names
            string[] columnNames;
            ExcelReader db = new ExcelReader(filename, true, false);
            DataTable tableSource = db.GetWorksheet("data");

            // Creates a matrix from the entire source data table
            double[,] table = tableSource.ToMatrix(out columnNames);
            // Get only the input vector values (first two columns)
            double[][] inputs = table.GetColumns(0, 1).ToJagged();

            // Get only the output labels (last column)
            int[] outputs = table.GetColumn(2).ToInt32();
            // Specify the input variables
            DecisionVariable[] variables =
            {
                new DecisionVariable("x", DecisionVariableKind.Continuous),
                new DecisionVariable("y", DecisionVariableKind.Continuous),
            };

            // Create the C4.5 learning algorithm
            var c45 = new C45Learning(variables);

            // Learn the decision tree using C4.5
            tree = c45.Learn(inputs, outputs);

        }
       
        private static async Task PostToFirebase(TeparakDTO data)
        {
            var client = new FirebaseClient(FirebaseUrl);
            
            double[][] inputs = new double[1][];
            double[] dataInput = new double[2];
            dataInput[0] = data.T;
            dataInput[1] = data.H;

            inputs[0] = dataInput;
            int[] actual = tree.Decide(inputs);
            data.Status = actual[0];
            data.ReportDate = long.Parse(DateTime.Now.ToString("yyyyMMddHHmmss"));
            string topic = "teparak_"+ data.ID;
            if (data.Status > 0)

            {
                topic = "teparak-noti_"+ data.ID;
                NotiLine(data);
            }

            var child = client.Child(topic);
            await child.PostAsync(data);
            Console.WriteLine("### Sendto Fierbase ###"+ data.ID);

        }
        private static async Task mqttTeparak()
        {
            Outer:
            var options = new MqttClientOptions
            {
                Server = MqttServer,
                Port = MqttPort,
                UserName = MqttUser,
                Password = MqttPassword
            };

            var client = new MqttClientFactory().CreateMqttClient(options);
            client.ApplicationMessageReceived += (s, e) =>
            {

                string jsonStr = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                if (jsonStr.Contains("ID") && jsonStr.Contains("Location"))
                {
                    TeparakDTO result = JsonConvert.DeserializeObject<TeparakDTO>(jsonStr);
                    PostToFirebase(result);
                }
                Task.Delay(TimeSpan.FromSeconds(10));

            };

            client.Connected += async (s, e) =>
            {
                Console.WriteLine("### CONNECTED WITH SERVER ###");

                await client.SubscribeAsync(new List<TopicFilter>
    {
        new TopicFilter("teparak", MqttQualityOfServiceLevel.AtMostOnce)
    });

                Console.WriteLine("### SUBSCRIBED ###");
            };



            try
            {
                await client.ConnectAsync();
            }
            catch
            {
                Console.WriteLine("### CONNECTING FAILED 2 ###");
            }

            Console.WriteLine("### WAITING FOR APPLICATION MESSAGES ###");
            goto Outer;
            Console.ReadLine();


        }
        private static  async Task NotiLine(TeparakDTO data)
        {
            
            double latitude = double.Parse(data.Location.Split(',')[0]);
            double longitude = double.Parse(data.Location.Split(',')[1]);
            PushMessageModel reply = new PushMessageModel()
            {
               to= UserIdForNoti
            };
            reply.messages.Add(new Message() { text =string.Format( "พบข้อมูล อุณหภูมิผิดปกติ ที่ {0}c {1}H",data.T,data.H), type = "text" });
            reply.messages.Add(new Message() { address="อุทยานแห่งชาติ", type = "location", title= data.ID,latitude = latitude ,longitude= longitude });

            string token = LineToken;
            using (var client = new HttpClient(new HttpClientHandler { UseProxy = false }))
            {
                string url = "https://api.line.me/v2/bot/message/push";
                StringContent content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(reply), System.Text.Encoding.UTF8, "application/json");


                try
                {

                    //  client.BaseAddress = new Uri(url);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                    // HTTP POST
                    HttpResponseMessage response = await client.PostAsync(url, content);
                    string result = await response.Content.ReadAsStringAsync();
                   // log.Debug(string.Format("============ Line Response Message {0} ==================", url));
                    //log.Debug(result);
                }
                catch (Exception ex)
                {

                   
                }



            }
        }
         
    }
}
