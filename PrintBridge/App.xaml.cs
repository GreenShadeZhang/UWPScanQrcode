using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Windows.ApplicationModel;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;

namespace PrintBridge
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        private AppServiceConnection connection = null;
        public App()
        {
            InitializeAppServiceConnection();
        }
        private async void InitializeAppServiceConnection()
        {
            try
            {
                connection = new AppServiceConnection
                {
                    AppServiceName = "CommunicationService",
                    PackageFamilyName = Package.Current.Id.FamilyName
                };
                connection.RequestReceived += Connection_RequestReceived;
                connection.ServiceClosed += Connection_ServiceClosed;


                AppServiceConnectionStatus status = await connection.OpenAsync();
                if (status != AppServiceConnectionStatus.Success)
                {
                    await SendMessage("response", $"AppServiceConnectionStatus : {status.ToString()}");
                    Current.Shutdown();
                }


                await SendMessage("response", "connet ok");

            }
            catch (Exception ex)
            {
                await SendMessage("response", ex.ToString());
                Current.Shutdown();
            }
        }

        private void Connection_ServiceClosed(AppServiceConnection sender, AppServiceClosedEventArgs args)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
            {
                Current.Shutdown();
            }));
        }
        Bitmap _printBmp = null;
        private async void Connection_RequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            string key = args.Request.Message.First().Key;
            string value = args.Request.Message.First().Value.ToString();
            string printList = "";
            // Console.ForegroundColor = ConsoleColor.Cyan;
            // Console.WriteLine(string.Format("Received message '{0}' with value '{1}'", key, value));
            if (key == "request")
            {
                List<string> prints = new List<string>();
                try
                {
                    foreach (string item in PrinterSettings.InstalledPrinters)
                    {
                        prints.Add(item);
                    }
                    printList = Newtonsoft.Json.JsonConvert.SerializeObject(prints);
                }
                catch (Exception ex)
                {
                    printList = "error" + ex.Message;
                }


                ValueSet valueSet = new ValueSet();
                valueSet.Add("response", printList);
                // Console.ForegroundColor = ConsoleColor.White;
                // Console.WriteLine(string.Format("Sending response: '{0}'", value.ToUpper()));
                // Console.WriteLine();
                args.Request.SendResponseAsync(valueSet).Completed += delegate { };
            }
            else if (key == "print")
            {
               
                _printBmp = new Bitmap(500, 500);
                using (Graphics graphic = Graphics.FromImage(_printBmp))
                {
                    graphic.DrawString(value, new Font("宋体", 34), new SolidBrush(Color.Black), 0, 0);
                    graphic.Flush();
                }
                using (PrintDocument printDocument = new PrintDocument())
                {
                    
                    String _printerName = value;
                 
                    printDocument.PrinterSettings.PrinterName = _printerName;
                    printDocument.DocumentName = "票券_" + value;

                    PrintController printController = new StandardPrintController();//禁止"打印中"弹窗
                    printDocument.PrintController = printController;
                 
                    printDocument.PrintPage += PrintDocument_PrintPage;
                    printDocument.Print();
                }
                await SendMessage("response", value);

            }
        }

        private async void PrintDocument_PrintPage(object sender, PrintPageEventArgs e)
        {
            try
            {

                e.Graphics.DrawImage(_printBmp, 0, 0);
                
                e.HasMorePages = false;
                await SendMessage("response","ok");
            }
            catch (System.Exception ex)
            {
                await SendMessage("response", ex.ToString());
                //Log...
            }
        }

        private async Task SendMessage(string key, string message)
        {
            await connection.SendMessageAsync(new ValueSet
                {
                    { key, message }
                });
        }
    }
}
