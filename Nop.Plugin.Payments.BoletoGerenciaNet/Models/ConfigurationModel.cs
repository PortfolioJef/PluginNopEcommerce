using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Mvc.ModelBinding;
using Nop.Web.Framework.Models;

namespace Nop.Plugin.Payments.Boleto.Models
{
    public class ConfigurationModel : BaseNopModel
    {
     
        public int ActiveStoreScopeConfiguration { get; set; }       

        public bool UseSandbox { get; set; }
        public bool UseSandbox_OverrideForStore { get; set; }
        public string ClienteID { get; set; }
        public bool ClienteID_OverrideForStore { get; set; }
        public string ClientSecret { get; set; }
        public bool ClientSecret_OverrideForStore { get; set; }
        public string UrlNotification { get; set; }
        public bool UrlNotification_OverrideForStore { get; set; }

    }
}