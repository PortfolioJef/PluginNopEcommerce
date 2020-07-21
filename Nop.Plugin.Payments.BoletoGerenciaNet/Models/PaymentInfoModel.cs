using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Mvc.ModelBinding;
using Nop.Web.Framework.Models;

namespace Nop.Plugin.Payments.Boleto.Models
{
    public class PaymentInfoModel : BaseNopModel
    {


    }

    public class items
    {
        public string name { get; set; }
        public decimal value { get; set; }
        public int amount { get; set; }
    }

}