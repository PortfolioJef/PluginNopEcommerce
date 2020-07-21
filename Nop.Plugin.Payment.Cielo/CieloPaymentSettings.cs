using Nop.Core.Configuration;

namespace Nop.Plugin.Payments.Cielo
{
    /// <summary>
    /// Represents settings of the Cielo payment plugin
    /// </summary>
    public class CieloPaymentSettings : ISettings
    {
        public string softDescriptor { get; set; }
        public string MerchantID { get; set; }
        public string MerchantKey { get; set; }
        public string UrlNotification { get; set; }
        public int idEmpresa { get; set; }
        public int idFilial { get; set; }        

    }
}
