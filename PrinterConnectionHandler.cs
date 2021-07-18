using System;
using Zebra.Sdk.Comm;
using Zebra.Sdk.Printer;

namespace ZebraPrint
{
    public class PrinterCOnnectionHandler : PrinterReconnectionHandler
    {
        
        public static Connection PrinterConnect()
        {
            //todo: COnectar no startup da aplicação
            Connection conn = new UsbConnection("\\\\?\\usb#vid_0a5f&pid_00d5#52j183700459#{28d78fad-5a12-11d1-ae5b-0000f803a8c2}");
            conn.Open();
             
            return conn;
        }   

        public void PrinterOnline(ZebraPrinterLinkOs printer, string firmwareVersion)
        {
            //var newprinter = ZebraPrinterFactory.GetInstance(printer.Connection);
            Console.WriteLine("Connection Handler");
            Program.conn = printer.Connection;
        }
    }
}