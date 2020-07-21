using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Plugin.Payments.Boleto.Models;
using Nop.Web.Framework.Components;

namespace Nop.Plugin.Payments.Boleto.Components
{
    [ViewComponent(Name = "PaymentBoleto")]
    public class PaymentBoletoViewComponent : NopViewComponent
    {
        public IViewComponentResult Invoke()
        {
            var model = new PaymentInfoModel()
            {
                //CreditCardTypes = new List<SelectListItem>
                //{
                //    new SelectListItem { Text = "Visa", Value = "visa" },
                //    new SelectListItem { Text = "Master card", Value = "MasterCard" },
                //    new SelectListItem { Text = "Discover", Value = "Discover" },
                //    new SelectListItem { Text = "Amex", Value = "Amex" },
                //}
            };             

            //set postback values (we cannot access "Form" with "GET" requests)
            if (this.Request.Method != WebRequestMethods.Http.Get)
            {
                //var form = this.Request.Form;
                //model.CardholderName = form["CardholderName"];
                //model.CardNumber = form["CardNumber"];
                //model.CardCode = form["CardCode"];
                //var selectedCcType = model.CreditCardTypes.FirstOrDefault(x => x.Value.Equals(form["CreditCardType"], StringComparison.InvariantCultureIgnoreCase));
                //if (selectedCcType != null)
                //    selectedCcType.Selected = true;
                //var selectedMonth = model.ExpireMonths.FirstOrDefault(x => x.Value.Equals(form["ExpireMonth"], StringComparison.InvariantCultureIgnoreCase));
                //if (selectedMonth != null)
                //    selectedMonth.Selected = true;
                //var selectedYear = model.ExpireYears.FirstOrDefault(x => x.Value.Equals(form["ExpireYear"], StringComparison.InvariantCultureIgnoreCase));
                //if (selectedYear != null)
                //    selectedYear.Selected = true;
            }

            return View("~/Plugins/Payments.Boleto/Views/PaymentInfo.cshtml", model);
        }
    }
}
