using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using Confluent.Kafka;
using Zebra.Sdk.Comm;
using Zebra.Sdk.Printer;
using ZebraApi.Models;

namespace ZebraPrint
{
    class Program
    {

        public static bool isDebug = false;
        public static Connection conn = PrinterCOnnectionHandler.PrinterConnect();

        static void Main(string[] args)
        {
            isDebug = args.Length > 0 && args[0].ToLower().Equals("debug");

            Debug("Starting agent");

            var conf = GetConsumerConfig();
            using var c = new ConsumerBuilder<Ignore, string>(conf).Build();
            {
                //todo: Parametrizar o tópico
                c.Subscribe("topic");
                Debug("Subscribing topic");

                Consume(c);
            }
        }

        static void Consume( IConsumer<Ignore, string> consumer)
        {
            Debug("Starting Consume");
             try
                {
                    var cts = GetToken();

                    while (true)
                    {
                        try
                        {
                            Debug("Listening to queue");
                            var consumerResult = consumer.Consume(cts.Token);
                            Debug("Message Consumed " + consumerResult.Offset.Value + "" + consumerResult.Message.Value);

                            var processResult = ProcessMessage(consumerResult.Message.Value);

                            if(processResult){
                                consumer.Commit();
                                
                            }else{
                                consumer.Seek(consumerResult.TopicPartitionOffset);
                            }
                        }
                        catch (ConsumeException e)
                        {
                            //todo: reconect to queue if anything wrong
                            Debug($"Error occured: {e.Error.Reason}");
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                   consumer.Close();
                }
        }

        static bool ProcessMessage(string json)
        {
            Debug("Processing Message");
            PrintModel model = JsonSerializer.Deserialize<PrintModel>(json);

            try
            {
                ZebraPrinter printer = ZebraPrinterFactory.GetInstance(conn); 
                if(IsPrinterReady(printer))
                {
                    Print(printer, model);
                    return true;
                }

                return false;

            }catch(Exception)
            {
                Debug("Something went wrong with printer connection");
                PrinterReconnect();
                return false;
            }
            
        }
        
        static void Print(ZebraPrinter printer, PrintModel model)
        {
            Debug("Printing");
            try
            {

                for(var i=1; i<=model.Paletes; i++)
                {
                    var text = FillTextModel(model, i);
                    conn.Write(Encoding.UTF8.GetBytes(text));
                }

               
            }
            catch(Exception e)
            {
                throw new Exception(e.Message);
            }
            
        }

        static string FillTextModel(PrintModel model, int page)
        {
            string text = File.ReadAllText("Templates/cte.prn");

            var paletes = page.ToString()+"/"+model.Paletes.ToString();

            text = text.Replace("{$1}", model.Carga);
            text = text.Replace("{$2}", model.DataRecebimento);
            text = text.Replace("{$3}", model.DataLimiteEntrega);
            text = text.Replace("{$4}", model.Remetente);
            text = text.Replace("{$5}", model.Destinatario);
            text = text.Replace("{$6}", model.Volumes.ToString());
            text = text.Replace("{$7}", model.NotaFiscal);
            text = text.Replace("{$8}", paletes);
            text = text.Replace("{$9}", model.ConferidoPor);
            text = text.Replace("{$10}", model.Cidade);

            return text;
        }

        static PrintModel DeserializeModel(string json)
        {
            try
            {
                return JsonSerializer.Deserialize<PrintModel>(json);
            }
            catch(Exception e)
            {
                throw new JsonException(e.Message);
            }
           
        }

        static bool IsPrinterReady(ZebraPrinter printer)
        {   
            Debug("Checking if the printer is ready");
            try
            {           
                PrinterStatus printerStatus = printer.GetCurrentStatus();

                //todo: check the numberOfFormatsInReceiveBuffer attribute
                
                if (printerStatus.isReadyToPrint) 
                {
                    Debug("Printer ready");
                    return true;
                } else {
                    Debug("Printer not ready");
                    return false;
                }

            }
            catch(Exception e)
            {
                throw new Exception(e.Message);
            }
                   
        }

        static bool PrinterReconnect()
        {
            Debug("Reconnectingt");
            while(true)
            {
                try
                {
                    Debug("Trying to reconnect");
                    conn.GetConnectionReestablisher(1).ReestablishConnection(new PrinterCOnnectionHandler());
                    return true;
                }
                catch(Exception)
                {
                    Debug("Could not reconnect");
                }
            }
        }

        static ConsumerConfig GetConsumerConfig()
        {
            //todo: Parametrizar configurações
            return new ConsumerConfig
            {
                EnableAutoCommit = false,
                GroupId = "test-consumer-group",
                BootstrapServers = "52.91.116.209:9092",
                AutoOffsetReset = AutoOffsetReset.Earliest
            };
        }

        static CancellationTokenSource GetToken()
        {
            var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) => {
                e.Cancel = true;
                cts.Cancel();
            };
            return cts;
        }
    
        static void Debug(string message)
        {
            if(isDebug)
            {
                Console.WriteLine(message);
            }
        }
    }
}
