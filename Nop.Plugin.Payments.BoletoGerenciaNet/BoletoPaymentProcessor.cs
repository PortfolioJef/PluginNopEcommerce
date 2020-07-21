using System;
using System.Collections.Generic;
using System.Linq;
using Gerencianet.SDK;
using Microsoft.AspNetCore.Http;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Domain.Shipping;
using Nop.Plugin.Payments.Boleto.Models;
using Nop.Plugin.Payments.Boleto.Validators;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Plugins;
using Nop.Services.Tax;

namespace Nop.Plugin.Payments.Boleto
{
    /// <summary>
    /// Boleto payment processor
    /// </summary>
    public class BoletoPaymentProcessor : BasePlugin, IPaymentMethod
    {
        #region Fields

        private readonly CurrencySettings _currencySettings;
        private readonly ICheckoutAttributeParser _checkoutAttributeParser;
        private readonly ICurrencyService _currencyService;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILocalizationService _localizationService;
        private readonly IPaymentService _paymentService;
        private readonly ISettingService _settingService;
        private readonly ITaxService _taxService;
        private readonly IWebHelper _webHelper;
        private readonly BoletoPaymentSettings _BoletoPaymentSettings;
        private readonly IWorkContext _workContext;
        private readonly IStoreContext _storeContext;
        public static string OrderTotalSentToPayPal { get; set; }

        public string _clienteID;
        public string _ClientSecret;
        public string _UrlNotification;
        #endregion

        #region Ctor

        public BoletoPaymentProcessor(CurrencySettings currencySettings,
            ILocalizationService localizationService,
           ICheckoutAttributeParser checkoutAttributeParser,
            ICurrencyService currencyService,
            IGenericAttributeService genericAttributeService,
            IHttpContextAccessor httpContextAccessor,
            IPaymentService paymentService,
            ISettingService settingService,
            ITaxService taxService,
            IWebHelper webHelper,
            IWorkContext workContext,
             IStoreContext storeContext,
            BoletoPaymentSettings BoletoPaymentSettings)
        {
            this._currencySettings = currencySettings;
            this._checkoutAttributeParser = checkoutAttributeParser;
            this._currencyService = currencyService;
            this._genericAttributeService = genericAttributeService;
            this._httpContextAccessor = httpContextAccessor;
            this._localizationService = localizationService;
            this._paymentService = paymentService;
            this._settingService = settingService;
            this._taxService = taxService;
            this._webHelper = webHelper;
            this._BoletoPaymentSettings = BoletoPaymentSettings;
            this._workContext = workContext;
            this._storeContext = storeContext;

            this._clienteID = _BoletoPaymentSettings.ClienteID;
            this._ClientSecret = _BoletoPaymentSettings.ClientSecret;
            this._UrlNotification = _BoletoPaymentSettings.UrlNotification;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Process a payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>Process payment result</returns>        
        public ProcessPaymentResult ProcessPayment(ProcessPaymentRequest processPaymentRequest)
        {
            string clienteID = _clienteID;
            string ClientSecret = _ClientSecret;
            string UrlNotification = _UrlNotification;

            var result = new ProcessPaymentResult();


            List<ShoppingCartItem> cart = _workContext.CurrentCustomer.ShoppingCartItems
                      .Where(sci => sci.ShoppingCartType == ShoppingCartType.ShoppingCart)
                      .LimitPerStore(_storeContext.CurrentStore.Id)
                      .ToList();

            dynamic endpoints = new Endpoints(clienteID, ClientSecret, _BoletoPaymentSettings.UseSandbox);

            var shippingOption = _genericAttributeService.GetAttribute<ShippingOption>(_workContext.CurrentCustomer,
               NopCustomerDefaults.SelectedShippingOptionAttribute, _storeContext.CurrentStore.Id);

            var items = new List<items>();

            foreach (var i in cart)
            {
                items.Add(new Models.items
                {
                    name = i.Product.Name,
                    //A api da gerenciaNet recebe um inteiro
                    value = (decimal.Round(i.Product.Price * 100, 0, MidpointRounding.AwayFromZero)),// Convert.ToDecimal(i.Product.Price.ToString().Replace(".", ""),// Formatar valor 
                    amount = i.Quantity
                });
            }

            var Transaction = new
            {
                items = items,
                shippings = new[] {
                new {
                    name =  shippingOption.Name,
                    value =(decimal.Round(shippingOption.Rate * 100, 0, MidpointRounding.AwayFromZero)),
                }
                }
            };

            try
            {
                //Cria a transação      
                var response = endpoints.CreateCharge(null, Transaction);

                var param = new
                {
                    id = response.data.charge_id,

                };
                ///Atribui metodo de pagamento

                var PaymentInformation = new
                {
                    payment = new
                    {
                        banking_billet = new
                        {
                            expire_at = DateTime.Now.AddDays(1).ToString("yyyy-MM-dd"),
                            customer = new
                            {
                                name = cart[0].Customer.Addresses[0].FirstName + " " + cart[0].Customer.Addresses[0].LastName,
                                email = cart[0].Customer.Email.Trim(),
                                cpf = _genericAttributeService.GetAttribute<string>(cart[0].Customer, NopCustomerDefaults.CnpjCpf), //cart[0].Customer. CnpjCpf.Trim(),
                                phone_number = cart[0].Customer.Addresses[0].PhoneNumber.Trim(),
                            }
                        }
                    }
                };

                var responseResult = endpoints.PayCharge(param, PaymentInformation);
                result.CaptureTransactionId = response.data.charge_id;
                result.NewPaymentStatus = PaymentStatus.Pending;
                result.CaptureTransactionResult = responseResult.data.link;

            }
            catch (GnException e)
            {
                //Console.WriteLine(e.ErrorType);
                //Console.WriteLine(e.Message);
                result.AddError(e.ErrorType + "-" + e.Message);
            }
            return result;
        }

        /// <summary>
        /// Get payment information
        /// </summary>
        /// <param name="form">The parsed form values</param>
        /// <returns>Payment info holder</returns>
        public ProcessPaymentRequest GetPaymentInfo(IFormCollection form)
        {

            return new ProcessPaymentRequest();

            //return paymentRequest;
        }


        /// <summary>
        /// Post process payment (used by payment gateways that require redirecting to a third-party URL)
        /// </summary>
        /// <param name="postProcessPaymentRequest">Payment info required for an order processing</param>
        public void PostProcessPayment(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            //nothing
        }

        /// <summary>
        /// Returns a value indicating whether payment method should be hidden during checkout
        /// </summary>
        /// <param name="cart">Shopping cart</param>
        /// <returns>true - hide; false - display.</returns>
        public bool HidePaymentMethod(IList<ShoppingCartItem> cart)
        {
            //you can put any logic here
            //for example, hide this payment method if all products in the cart are downloadable
            //or hide this payment method if current customer is from certain country
            return false;
        }

        /// <summary>
        /// Gets additional handling fee
        /// </summary>
        /// <returns>Additional handling fee</returns>
        public decimal GetAdditionalHandlingFee(IList<ShoppingCartItem> cart)
        {
            return 0;// _paymentService.CalculateAdditionalFee(cart,
                     //_BoletoPaymentSettings.AdditionalFee, _BoletoPaymentSettings.AdditionalFeePercentage);
        }

        /// <summary>
        /// Captures payment
        /// </summary>
        /// <param name="capturePaymentRequest">Capture payment request</param>
        /// <returns>Capture payment result</returns>
        public CapturePaymentResult Capture(CapturePaymentRequest capturePaymentRequest)
        {
            return new CapturePaymentResult { Errors = new[] { "Capture method not supported" } };
        }

        /// <summary>
        /// Refunds a payment
        /// </summary>
        /// <param name="refundPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public RefundPaymentResult Refund(RefundPaymentRequest refundPaymentRequest)
        {
            return new RefundPaymentResult { Errors = new[] { "Refund method not supported" } };
        }

        /// <summary>
        /// Voids a payment
        /// </summary>
        /// <param name="voidPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public VoidPaymentResult Void(VoidPaymentRequest voidPaymentRequest)
        {
            return new VoidPaymentResult { Errors = new[] { "Void method not supported" } };
        }

        /// <summary>
        /// Process recurring payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>Process payment result</returns>
        public ProcessPaymentResult ProcessRecurringPayment(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new ProcessPaymentResult();

            return result;
        }

        /// <summary>
        /// Cancels a recurring payment
        /// </summary>
        /// <param name="cancelPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public CancelRecurringPaymentResult CancelRecurringPayment(CancelRecurringPaymentRequest cancelPaymentRequest)
        {
            //always success
            return new CancelRecurringPaymentResult();
        }

        /// <summary>
        /// Gets a value indicating whether customers can complete a payment after order is placed but not completed (for redirection payment methods)
        /// </summary>
        /// <param name="order">Order</param>
        /// <returns>Result</returns>
        public bool CanRePostProcessPayment(Order order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            //it's not a redirection payment method. So we always return false
            return false;
        }

        /// <summary>
        /// Validate payment form
        /// </summary>
        /// <param name="form">The parsed form values</param>
        /// <returns>List of validating errors</returns>
        public IList<string> ValidatePaymentForm(IFormCollection form)
        {
            var warnings = new List<string>();

            //validate
            var validator = new PaymentInfoValidator(_localizationService);
            var model = new PaymentInfoModel
            {
                //CardholderName = form["CardholderName"],
                //CardNumber = form["CardNumber"],
                //CardCode = form["CardCode"],
                //ExpireMonth = form["ExpireMonth"],
                //ExpireYear = form["ExpireYear"]
            };
            var validationResult = validator.Validate(model);
            if (!validationResult.IsValid)
                warnings.AddRange(validationResult.Errors.Select(error => error.ErrorMessage));

            return warnings;
        }



        /// <summary>
        /// Gets a configuration page URL
        /// </summary>
        public override string GetConfigurationPageUrl()
        {
            return $"{_webHelper.GetStoreLocation()}Admin/PaymentBoleto/Configure";
        }

        /// <summary>
        /// Gets a name of a view component for displaying plugin in public store ("payment info" checkout step)
        /// </summary>
        /// <returns>View component name</returns>
        public string GetPublicViewComponentName()
        {
            return "PaymentBoleto";
        }

        /// <summary>
        /// Install the plugin
        /// </summary>
        public override void Install()
        {
            //settings
            var settings = new BoletoPaymentSettings
            {
                UseSandbox = true

                //TransactMode = TransactMode.Pending
            };
            _settingService.SaveSetting(settings);

            //locales
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.Boleto.Instructions", "This payment method stores credit card information in database (it's not sent to any third-party processor). In order to store credit card information, you must be PCI compliant.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.Boleto.Fields.AdditionalFee", "Additional fee");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.Boleto.Fields.AdditionalFee.Hint", "Enter additional fee to charge your customers.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.Boleto.Fields.AdditionalFeePercentage", "Additional fee. Use percentage");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.Boleto.Fields.AdditionalFeePercentage.Hint", "Determines whether to apply a percentage additional fee to the order total. If not enabled, a fixed value is used.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.Boleto.Fields.TransactMode", "After checkout mark payment as");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.Boleto.Fields.TransactMode.Hint", "Specify transaction mode.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.Boleto.PaymentMethodDescription", "Pay by credit / debit card");

            base.Install();
        }

        /// <summary>
        /// Uninstall the plugin
        /// </summary>
        public override void Uninstall()
        {
            //settings
            _settingService.DeleteSetting<BoletoPaymentSettings>();

            //locales
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.Boleto.Instructions");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.Boleto.Fields.AdditionalFee");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.Boleto.Fields.AdditionalFee.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.Boleto.Fields.AdditionalFeePercentage");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.Boleto.Fields.AdditionalFeePercentage.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.Boleto.Fields.TransactMode");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.Boleto.Fields.TransactMode.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.Boleto.PaymentMethodDescription");

            base.Uninstall();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets a value indicating whether capture is supported
        /// </summary>
        public bool SupportCapture
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a value indicating whether partial refund is supported
        /// </summary>
        public bool SupportPartiallyRefund
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a value indicating whether refund is supported
        /// </summary>
        public bool SupportRefund
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a value indicating whether void is supported
        /// </summary>
        public bool SupportVoid
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a recurring payment type of payment method
        /// </summary>
        public RecurringPaymentType RecurringPaymentType
        {
            get { return RecurringPaymentType.Manual; }
        }

        /// <summary>
        /// Gets a payment method type
        /// </summary>
        public PaymentMethodType PaymentMethodType
        {
            get { return PaymentMethodType.Standard; }
        }

        /// <summary>
        /// Gets a value indicating whether we should display a payment information page for this plugin
        /// </summary>
        public bool SkipPaymentInfo
        {
            get { return true; }
        }

        /// <summary>
        /// Gets a payment method description that will be displayed on checkout pages in the public store
        /// </summary>
        public string PaymentMethodDescription
        {
            //return description of this payment method to be display on "payment method" checkout step. good practice is to make it localizable
            //for example, for a redirection payment method, description may be like this: "You will be redirected to PayPal site to complete the payment"
            get { return _localizationService.GetResource("Boleto"); }
        }

        public string clienteID
        {
            get { return _clienteID; }
        }
        public string ClientSecret
        {
            get { return _ClientSecret; }
        }
        public string UrlNotification
        {
            get { return _UrlNotification; }
        }

        #endregion

    }
}