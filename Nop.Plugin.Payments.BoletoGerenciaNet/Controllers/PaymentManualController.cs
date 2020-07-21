using System;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Plugin.Payments.Boleto.Models;
using Nop.Services;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Payments.Boleto.Controllers
{
    [AuthorizeAdmin]
    [Area(AreaNames.Admin)]
    public class PaymentBoletoController : BasePaymentController
    {
        #region Fields

        private readonly ILocalizationService _localizationService;
        private readonly INotificationService _notificationService;
        private readonly IPermissionService _permissionService;
        private readonly ISettingService _settingService;
        private readonly IStoreContext _storeContext;

        #endregion

        #region Ctor

        public PaymentBoletoController(ILocalizationService localizationService,
            IPermissionService permissionService,
            ISettingService settingService,
            IStoreContext storeContext)
        {
            this._localizationService = localizationService;
            this._permissionService = permissionService;
            this._settingService = settingService;
            this._storeContext = storeContext;
        }

        #endregion

        #region Methods

        public IActionResult Configure()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            //load settings for a chosen store scope
            var storeScope = _storeContext.ActiveStoreScopeConfiguration;
            var BoletoPaymentSettings = _settingService.LoadSetting<BoletoPaymentSettings>(storeScope);

            var model = new ConfigurationModel
            {

                ClienteID = BoletoPaymentSettings.ClienteID,
                ClientSecret = BoletoPaymentSettings.ClientSecret,
                UrlNotification = BoletoPaymentSettings.UrlNotification,
                UseSandbox = BoletoPaymentSettings.UseSandbox,
                //TransactModeValues = BoletoPaymentSettings.TransactMode.ToSelectList(),
                ActiveStoreScopeConfiguration = storeScope
            };
            //if (storeScope > 0)
            // {
            //    model.TransactModeId_OverrideForStore = _settingService.SettingExists(BoletoPaymentSettings, x => x.TransactMode, storeScope);
            //    model.AdditionalFee_OverrideForStore = _settingService.SettingExists(BoletoPaymentSettings, x => x.AdditionalFee, storeScope);
            //    model.AdditionalFeePercentage_OverrideForStore = _settingService.SettingExists(BoletoPaymentSettings, x => x.AdditionalFeePercentage, storeScope);
            //}

            return View("~/Plugins/Payments.Boleto/Views/Configure.cshtml", model);
        }

        [HttpPost]
        [AdminAntiForgery]
        public IActionResult Configure(ConfigurationModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            if (!ModelState.IsValid)
                return Configure();

            //load settings for a chosen store scope
            var storeScope = _storeContext.ActiveStoreScopeConfiguration;
            var BoletoPaymentSettings = _settingService.LoadSetting<BoletoPaymentSettings>(storeScope);

            //save settings
            BoletoPaymentSettings.ClienteID = model.ClienteID;
            BoletoPaymentSettings.ClientSecret = model.ClientSecret;
            BoletoPaymentSettings.UrlNotification = model.UrlNotification;
            BoletoPaymentSettings.UseSandbox = model.UseSandbox;

            /* We do not clear cache after each setting update.
             * This behavior can increase performance because cached settings will not be cleared 
             * and loaded from database after each update */

            _settingService.SaveSettingOverridablePerStore(BoletoPaymentSettings, x => x.ClienteID, model.ClienteID_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(BoletoPaymentSettings, x => x.ClientSecret, model.ClientSecret_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(BoletoPaymentSettings, x => x.UrlNotification, model.UrlNotification_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(BoletoPaymentSettings, x => x.UseSandbox, model.UseSandbox_OverrideForStore, storeScope, false);

            //now clear settings cache
            _settingService.ClearCache();

            _notificationService.SuccessNotification(_localizationService.GetResource("Admin.Plugins.Saved"));

            return Configure();
        }

        #endregion
    }
}