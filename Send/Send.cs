using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

class Send
{
    public static void Main()
    {
        var factory = new ConnectionFactory() { HostName = "localhost" };
        using (var connection = factory.CreateConnection())
        using (var channel = connection.CreateModel())
        {
            channel.QueueDeclare(queue: "Hot", durable: false, exclusive: false, autoDelete: false, arguments: null);
            channel.QueueDeclare(queue: "Cold", durable: false, exclusive: false, autoDelete: false, arguments: null);

            for (int i = 0; i <= 1000000; i++)
            {
                var temp = new Random().Next(100);
                var val = (temp >= 30) ? "Hot" : "Cold";

                var weatherForecast = new WeatherForecast
                {
                    Date = DateTime.UtcNow,
                    TemperatureCelsius = temp,
                    Summary = val,
                    PictureOfTheDay = File.ReadAllBytes(@"pic.jpg")
                };
                string jsonString = JsonSerializer.Serialize(weatherForecast);

                //string message = "Hello World!";
                var body = Encoding.UTF8.GetBytes(jsonString);

                channel.BasicPublish(exchange: "", routingKey: val, basicProperties: null, body: body);
                Console.WriteLine(" [x] Sent {0}", jsonString);
            }
        }

        Console.WriteLine(" Press [enter] to exit.");
        Console.ReadLine();
    }
}