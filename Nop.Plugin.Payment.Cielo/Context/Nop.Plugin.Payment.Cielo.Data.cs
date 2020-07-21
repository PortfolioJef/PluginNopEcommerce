using System;
using System.Collections.Generic;
using System.Text;
using Nop.Core;
using Nop.Data;
using Nop.Web.Framework;

namespace Nop.Plugin.Payments.Cielo.data
{
    public class PaymentCieloRecordMap : NopEntityTypeConfiguration<ConfigurationCieloModel>
    {
        public ProductViewTrackerRecordMap()
        {
            ToTable("CieloConfig");
            
            //Map the primary key
            HasKey(m => m.Id);
            //Map the additional properties
            Property(m => m.idEmpresa);
            //Avoiding truncation/failure

            //so we set the same max length used in the product tame
            Property(m => m.idFilial);
            Property(m => m.softDescriptor);
            Property(m => m.MerchantID);
            Property(m => m.MerchantKey);
            Property(m => m.UrlNotification);
        }
    }
}
