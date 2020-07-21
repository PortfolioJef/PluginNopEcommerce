using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Plugin.Payment.Cielo.Models;
using Nop.Plugin.Payments.Cielo;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Messages;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Security;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;
namespace Nop.Plugin.Payment.Cielo.Controllers
{
    public class PaymentCieloController : BasePaymentController
    {
        private readonly ILocalizationService _localizationService;
        private readonly INotificationService _notificationService;
        private readonly IPermissionService _permissionService;
        private readonly ISettingService _settingService;
        private readonly IStoreContext _storeContext;

        private readonly CieloPaymentSettings _CieloPaymentSettings;

        public PaymentCieloController(ILocalizationService localizationService,
            IPermissionService permissionService,
            ISettingService settingService,
            IStoreContext storeContext,
            CieloPaymentSettings cieloPaymentSettings
            )
        {
            this._localizationService = localizationService;
            this._permissionService = permissionService;
            this._settingService = settingService;
            this._storeContext = storeContext;
            this._CieloPaymentSettings = cieloPaymentSettings;
        }

        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        public IActionResult Configure()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            //load settings for a chosen store scope
            var storeScope = _storeContext.ActiveStoreScopeConfiguration;
            var CieloPaymentSettings = _settingService.LoadSetting<CieloPaymentSettings>(storeScope);

            var model = new ConfigurationCieloModel
            {
                MerchantID = _CieloPaymentSettings.MerchantID,
                idEmpresa = _CieloPaymentSettings.idEmpresa,
                idFilial = _CieloPaymentSettings.idFilial,
                MerchantKey = _CieloPaymentSettings.MerchantKey,
                softDescriptor = _CieloPaymentSettings.softDescriptor,
                UrlNotification = _CieloPaymentSettings.UrlNotification
            };

            return View("~/Plugins/Payments.Cielo/Views/Configure.cshtml", model);
        }

        [HttpPost]
        [AuthorizeAdmin]
        [AdminAntiForgery]
        [Area(AreaNames.Admin)]
        public IActionResult Configure(ConfigurationCieloModel model)
        {
            if (!ModelState.IsValid)
                return Configure();

            //load settings for a chosen store scope
            var storeScope = _storeContext.ActiveStoreScopeConfiguration;
            var cieloPaymentSettings = _settingService.LoadSetting<CieloPaymentSettings>(storeScope);

            cieloPaymentSettings.idEmpresa = model.idEmpresa;
            cieloPaymentSettings.idFilial = model.idFilial;
            cieloPaymentSettings.MerchantID = model.MerchantID;
            cieloPaymentSettings.MerchantKey = model.MerchantKey;
            cieloPaymentSettings.softDescriptor = "Ecommerce";
            cieloPaymentSettings.UrlNotification = model.UrlNotification;

            _settingService.SaveSettingOverridablePerStore(cieloPaymentSettings, x => x.idEmpresa, true, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(cieloPaymentSettings, x => x.idFilial, true, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(cieloPaymentSettings, x => x.MerchantKey, true, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(cieloPaymentSettings, x => x.MerchantID, true, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(cieloPaymentSettings, x => x.softDescriptor, true, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(cieloPaymentSettings, x => x.UrlNotification, true, storeScope, false);

            _settingService.SaveSetting(cieloPaymentSettings);
            //now clear settings cache
            _settingService.ClearCache();

            _notificationService.SuccessNotification(_localizationService.GetResource("Admin.Plugins.Saved"));

            return Configure();
        }

    }

}

