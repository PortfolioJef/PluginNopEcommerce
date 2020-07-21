using Nop.Core.Configuration;

namespace Nop.Plugin.Payments.Boleto
{
    /// <summary>
    /// Represents settings of Boleto payment plugin
    /// </summary>
    public class BoletoPaymentSettings : ISettings
    {

        public bool UseSandbox { get; set; }     
        public string ClienteID { get; set; }       
        public string ClientSecret { get; set; }        
        public string UrlNotification { get; set; }
      
    }
}
