using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Mvc.ModelBinding;
using Nop.Web.Framework.Models;
using System.ComponentModel.DataAnnotations;

namespace Nop.Plugin.Payments.Cielo.Models
{
    public class PaymentInfoModel : BaseNopModel
    {
        public PaymentInfoModel()
        {
            CreditCardTypes = new List<SelectListItem>();
            ExpireMonths = new List<SelectListItem>();
            ExpireYears = new List<SelectListItem>();
            ParcelsQtds = new List<SelectListItem>();

        }

        [NopResourceDisplayName("Payment.SelectCreditCard")]
        public string CreditCardType { get; set; }

        [NopResourceDisplayName("Payment.SelectCreditCard")]
        public IList<SelectListItem> CreditCardTypes { get; set; }

        [NopResourceDisplayName("Payment.CardholderName")]
        public string CardholderName { get; set; }

        [NopResourceDisplayName("Payment.CardNumber")]
        public string CardNumber { get; set; }

        [NopResourceDisplayName("Payment.ExpirationDate")]
        public string ExpireMonth { get; set; }

        [NopResourceDisplayName("Payment.ExpirationDate")]
        public string ExpireYear { get; set; }

        [Display(Name = "Parcelar em:")]
        public string ParcelsQtd { get; set; }

        public IList<SelectListItem> ExpireMonths { get; set; }

        public IList<SelectListItem> ExpireYears { get; set; }
                
        public IList<SelectListItem> ParcelsQtds { get; set; }

        [NopResourceDisplayName("Payment.CardCode")]
        public string CardCode { get; set; }
    }
}