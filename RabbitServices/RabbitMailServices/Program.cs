using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;
using RabbitMailServices;
using System.Net;
using System.Net.Mail;

namespace MailingBackEndSimulation
{
    class Program
    {
        static void Main(string[] args)
        {
            var factory = new ConnectionFactory() { HostName = "68.183.130.209", UserName = "test", Password = "Password123" };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: "EmailingRequests", durable: false,
                  exclusive: false, autoDelete: false, arguments: null);
                channel.BasicQos(0, 1, false);
                var consumer = new EventingBasicConsumer(channel);
                channel.BasicConsume(queue: "EmailingRequests",
                  autoAck: false, consumer: consumer);
                Console.WriteLine(" [x] Awaiting RPC requests");

                consumer.Received += (model, ea) =>
                {
                    string response = null;

                    var body = ea.Body;
                    var props = ea.BasicProperties;
                    var replyProps = channel.CreateBasicProperties();
                    replyProps.CorrelationId = props.CorrelationId;

                    try
                    {
                        var message = Encoding.UTF8.GetString(body);
                        response = handleMessage(message);
                    }
                    catch (Exception e)
                    {
                        //Console.WriteLine(" [.] " + e.Message);
                        response = "false";
                    }
                    finally
                    {
                        var responseBytes = Encoding.UTF8.GetBytes(response);
                        channel.BasicPublish(exchange: "", routingKey: props.ReplyTo,
                          basicProperties: replyProps, body: responseBytes);
                        channel.BasicAck(deliveryTag: ea.DeliveryTag,
                          multiple: false);
                    }
                };

                Console.WriteLine(" Press [enter] to exit.");
                Console.ReadLine();
            }

        }

        public static void SendEmail(string To, string Subject, string Body)
        {
            System.Net.Mail.MailMessage mail = new System.Net.Mail.MailMessage();

            mail.From = new MailAddress("dibu.richard.test@gmail.com");

            mail.To.Add(To);
            mail.Subject = Subject;
            mail.Body = Body;

            SmtpClient smtp = new SmtpClient();

            smtp.Host = "smtp.gmail.com";
            smtp.Port = 587; //465; //25 //587 //25
            smtp.Credentials = new NetworkCredential("dibu.richard.test@gmail.com", "Passw0rdTest123");
            smtp.EnableSsl = true;
            try
            {
                smtp.Send(mail);
            }
            catch (Exception ex)
            {
                throw new Exception("No se ha podido enviar el email", ex.InnerException);
            }
            finally
            {
                smtp.Dispose();
            }

        }

        private static string handleMessage(string message)
        {
            List<MailRequest> mailList = JsonConvert.DeserializeObject<List<MailRequest>>(message);
            foreach (var mail in mailList)
            {
                if (!Directory.Exists(@".\logsmail"))
                {
                    Directory.CreateDirectory(@".\logsmail");
                }
                if (!File.Exists(@".\logsmail\logCorreo.txt"))
                {
                    File.CreateText(@".\logsmail\logCorreo.txt");
                }
                File.AppendAllText(@".\logsmail\logCorreo.txt", "------------------" + Environment.NewLine);
                File.AppendAllText(@".\logsmail\logCorreo.txt", "Correo enviado el " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + Environment.NewLine);
                File.AppendAllText(@".\logsmail\logCorreo.txt", "De: Administrador@RabbitmqTest.com" + Environment.NewLine);
                File.AppendAllText(@".\logsmail\logCorreo.txt", "Para: " + mail.to + Environment.NewLine);
                File.AppendAllText(@".\logsmail\logCorreo.txt", "Asunto: " + mail.subject + Environment.NewLine);
                File.AppendAllText(@".\logsmail\logCorreo.txt", "Mensaje: " + mail.message + Environment.NewLine);
                SendEmail(mail.to, mail.subject, mail.message);
            }
            return "true";
        }
    }
}
