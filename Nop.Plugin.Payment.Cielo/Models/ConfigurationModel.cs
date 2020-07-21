using Nop.Web.Framework.Mvc.ModelBinding;
using Nop.Web.Framework.Models;

namespace Nop.Plugin.Payment.Cielo.Models
{
    public class ConfigurationCieloModel : BaseNopEntityModel
    {
        public int idEmpresa { get; set; }
        public int idFilial { get; set; }
        public string softDescriptor { get; set; }
        public string MerchantID { get; set; }
        public string MerchantKey { get; set; }
        public string UrlNotification { get; set; }
    }
}