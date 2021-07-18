
using System;

namespace ZebraApi.Models
{
    public class PrintModel
    {
        public string Carga {get;set;}
        public string DataRecebimento {get;set;}
        public string DataLimiteEntrega {get;set;}
        public string Remetente {get;set;}
        public string Destinatario {get;set;}
        public string Cidade {get;set;}
        public int Volumes {get;set;}
        public string NotaFiscal {get;set;}
        public int Paletes {get;set;}
        public string ConferidoPor {get;set;}
    }
}