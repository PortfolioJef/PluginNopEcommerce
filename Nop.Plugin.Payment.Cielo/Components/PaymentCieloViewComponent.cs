using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Plugin.Payments.Cielo.Models;
using Nop.Web.Framework.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Nop.Plugin.Payments.Cielo.Components
{
    [ViewComponent(Name = "PaymentCielo")]
    public class PaymentCieloViewComponent : NopViewComponent
    {
        public IViewComponentResult Invoke()
        {
            // var model = new PaymentInfoModel()
            var model = new PaymentInfoModel()
            {
                CreditCardTypes = new List<SelectListItem>
                {
                    new SelectListItem { Text = "Visa", Value = "visa" },
                    new SelectListItem { Text = "Master card", Value = "MasterCard" },
                    new SelectListItem { Text = "Discover", Value = "Discover" },
                    new SelectListItem { Text = "Amex", Value = "Amex" },
                }
            };

            //years
            for (var i = 0; i < 15; i++)
            {
                var year = (DateTime.Now.Year + i).ToString();
                model.ExpireYears.Add(new SelectListItem { Text = year, Value = year, });
            }

            //months
            for (var i = 1; i <= 12; i++)
            {
                model.ExpireMonths.Add(new SelectListItem { Text = i.ToString("D2"), Value = i.ToString(), });
            }

            //Parcels
            for (var i = 1; i <= 6; i++)
            {
                model.ParcelsQtds.Add(new SelectListItem { Text = i.ToString(), Value = i.ToString(), });
            }

            //set postback values (we cannot access "Form" with "GET" requests)
            if (this.Request.Method != WebRequestMethods.Http.Get)
            {
                var form = this.Request.Form;
                model.CardholderName = form["CardholderName"];
                model.CardNumber = form["CardNumber"];
                model.CardCode = form["CardCode"];

                var selectedCcType = model.CreditCardTypes.FirstOrDefault(x => x.Value.Equals(form["CreditCardType"], StringComparison.InvariantCultureIgnoreCase));
                if (selectedCcType != null)
                    selectedCcType.Selected = true;
                var selectedMonth = model.ExpireMonths.FirstOrDefault(x => x.Value.Equals(form["ExpireMonth"], StringComparison.InvariantCultureIgnoreCase));
                if (selectedMonth != null)
                    selectedMonth.Selected = true;
                var selectedYear = model.ExpireYears.FirstOrDefault(x => x.Value.Equals(form["ExpireYear"], StringComparison.InvariantCultureIgnoreCase));
                if (selectedYear != null)
                    selectedYear.Selected = true;

                var selectedParcelsQtd = model.ParcelsQtds.FirstOrDefault(x => x.Value.Equals(form["ParcelsQtd"], StringComparison.InvariantCultureIgnoreCase));
                if (selectedParcelsQtd != null)
                    selectedParcelsQtd.Selected = true;
            }

            return View("~/Plugins/Payments.Cielo/Views/PaymentInfo.cshtml",model);
        }
    }
}
