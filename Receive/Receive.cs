using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using System.Drawing.Imaging;
using System.Drawing;

class Receive
{
    public static void Main()
    {
        var factory = new ConnectionFactory() { HostName = "localhost" };
        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        channel.QueueDeclare(queue: "Hot", durable: false, exclusive: false, autoDelete: false, arguments: null);
        channel.QueueDeclare(queue: "Cold", durable: false, exclusive: false, autoDelete: false, arguments: null);

        var count = 0;
        Console.WriteLine(" [*] Waiting for messages.");

        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            var weatherForecast = JsonSerializer.Deserialize<WeatherForecast>(message);
            if (weatherForecast != null)
            {
                Console.WriteLine($" [x] Received {weatherForecast.Date} : {weatherForecast.Summary} ");
                var path = $@"d:\temp\imageprocessor\";
                var fileName = $"{path}{count++}.jpg";
                var reverseFileName = $"{path}{count++}_reverse.jpg";
                var pathDesc = $@"d:\temp\imageprocessor\{count++}.txt";

                File.WriteAllBytes($"{fileName}", weatherForecast.PictureOfTheDay);
                var byteImg = ReverseImage(fileName);

                File.WriteAllText(pathDesc, weatherForecast.Date.ToString());
                File.WriteAllBytes($"{reverseFileName}", byteImg);
            }
        };
        channel.BasicConsume(queue: "Hot", autoAck: true, consumer: consumer);
        channel.BasicConsume(queue: "Cold", autoAck: true, consumer: consumer);

        Console.WriteLine(" Press [enter] to exit.");
        Console.ReadLine();
    }

    private static byte[] ReverseImage(string fileName)
    {
        var img = Image.FromFile(fileName);
        var reverseImg = ColorReplace(img);

        var converter = new ImageConverter();
        var tmp = new Bitmap(reverseImg);
        byte[] byteImg = (byte[])converter.ConvertTo(tmp.Clone(), typeof(byte[]));
        return byteImg;
    }

    public static Image ByteArrayToImage(byte[] byteArrayIn)
    {
        using var ms = new MemoryStream(byteArrayIn);
        return Image.FromStream(ms);
    }


    public static byte[] ImageToByteArray(Image imageIn)
    {
        using var ms = new MemoryStream();
        imageIn.Save(ms, ImageFormat.Jpeg);
        return ms.ToArray();
    }

    public static Image ColorReplace(Image inputImage)
    {
        var tolerance = new Random().Next(1, 100);
        var oldColor = Color.White;
        var NewColor = Color.FromArgb(new Random().Next(0, 255), new Random().Next(0, 255), new Random().Next(0, 255));

        var outputImage = new Bitmap(inputImage.Width, inputImage.Height);
        var G = Graphics.FromImage(outputImage);
        G.DrawImage(inputImage, 0, 0);
        for (int y = 0; y < outputImage.Height; y++)
            for (int x = 0; x < outputImage.Width; x++)
            {
                var PixelColor = outputImage.GetPixel(x, y);
                if (PixelColor.R > oldColor.R - tolerance && PixelColor.R < oldColor.R + tolerance && PixelColor.G > oldColor.G - tolerance && PixelColor.G < oldColor.G + tolerance && PixelColor.B > oldColor.B - tolerance && PixelColor.B < oldColor.B + tolerance)
                {
                    int RColorDiff = oldColor.R - PixelColor.R;
                    int GColorDiff = oldColor.G - PixelColor.G;
                    int BColorDiff = oldColor.B - PixelColor.B;

                    if (PixelColor.R > oldColor.R) RColorDiff = NewColor.R + RColorDiff;
                    else RColorDiff = NewColor.R - RColorDiff;
                    if (RColorDiff > 255) RColorDiff = 255;
                    if (RColorDiff < 0) RColorDiff = 0;
                    if (PixelColor.G > oldColor.G) GColorDiff = NewColor.G + GColorDiff;
                    else GColorDiff = NewColor.G - GColorDiff;
                    if (GColorDiff > 255) GColorDiff = 255;
                    if (GColorDiff < 0) GColorDiff = 0;
                    if (PixelColor.B > oldColor.B) BColorDiff = NewColor.B + BColorDiff;
                    else BColorDiff = NewColor.B - BColorDiff;
                    if (BColorDiff > 255) BColorDiff = 255;
                    if (BColorDiff < 0) BColorDiff = 0;

                    outputImage.SetPixel(x, y, Color.FromArgb(RColorDiff, GColorDiff, BColorDiff));
                }
            }
        return outputImage;
    }
}