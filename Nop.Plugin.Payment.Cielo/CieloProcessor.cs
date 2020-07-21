using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using ApiCielo;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using Nop.Core;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Domain.Shipping;
using Nop.Plugin.Payments.Cielo;
using Nop.Plugin.Payments.Cielo.Models;
using Nop.Plugin.Payments.Cielo.Validators;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Plugins;
using Nop.Services.Tax;



namespace Nop.Plugin.Payment.Cielo
{
    /// <summary>
    /// Cielo Payment Processor
    /// </summary>
    public class CieloProcessor : BasePlugin, IPaymentMethod
    {
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
        private readonly CieloPaymentSettings _CieloPaymentSettings;

        public string _clienteID;
        public string _ClientSecret;
        public string _UrlNotification;

        ///Dados Cielo
        private ICieloApi api;
        private DateTime validExpirationDate;
        private DateTime invalidExpirationDate;

        public CieloProcessor(CurrencySettings currencySettings,
            ICheckoutAttributeParser checkoutAttributeParser,
            ICurrencyService currencyService,
            IGenericAttributeService genericAttributeService,
            IHttpContextAccessor httpContextAccessor,
            ILocalizationService localizationService,
            IPaymentService paymentService,
            ISettingService settingService,
            ITaxService taxService,
            IWebHelper webHelper,
            CieloPaymentSettings _CieloPaymentSettings)
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
            this._CieloPaymentSettings = _CieloPaymentSettings;
        }

        CancelRecurringPaymentResult IPaymentMethod.CancelRecurringPayment(CancelRecurringPaymentRequest cancelPaymentRequest)
        {
            return new CancelRecurringPaymentResult { Errors = new[] { "Recurring payment not supported" } };
        }

        bool IPaymentMethod.CanRePostProcessPayment(Order order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            //let's ensure that at least 5 seconds passed after order is placed
            //P.S. there's no any particular reason for that. we just do it
            if ((DateTime.UtcNow - order.CreatedOnUtc).TotalSeconds < 5)
                return false;

            return true;
        }

        CapturePaymentResult IPaymentMethod.Capture(CapturePaymentRequest capturePaymentRequest)
        {
            return new CapturePaymentResult { Errors = new[] { "Capture method not supported" } };
        }

        ProcessPaymentRequest IPaymentMethod.GetPaymentInfo(IFormCollection form)
        {
            return new ProcessPaymentRequest
            {
                CreditCardType = form["CreditCardType"],
                CreditCardName = form["CardholderName"],
                CreditCardNumber = form["CardNumber"],
                CreditCardExpireMonth = int.Parse(form["ExpireMonth"]),
                CreditCardExpireYear = int.Parse(form["ExpireYear"]),
                CreditCardCvv2 = form["CardCode"],
                ParcelsQtd = int.Parse(form["ParcelsQtd"]),
            };
            //return new ProcessPaymentRequest();
        }

        /// <summary>
        /// Gets a configuration page URL
        /// </summary>
        public override string GetConfigurationPageUrl()
        {
            return $"{_webHelper.GetStoreLocation()}Admin/PaymentCielo/Configure";
        }
        string IPaymentMethod.GetPublicViewComponentName()
        {
            return "PaymentCielo";
        }

        bool IPaymentMethod.HidePaymentMethod(IList<ShoppingCartItem> cart)
        {
            //you can put any logic here
            //for example, hide this payment method if all products in the cart are downloadable
            //or hide this payment method if current customer is from certain country
            return false;
        }

        void IPaymentMethod.PostProcessPayment(PostProcessPaymentRequest postProcessPaymentRequest)
        {

        }

        ProcessPaymentResult IPaymentMethod.ProcessPayment(ProcessPaymentRequest processPaymentRequest)
        {

            //var model = new PaymentInfoModel
            //{
            //    CardholderName = processPaymentRequest.CreditCardName,
            //    CardNumber = processPaymentRequest.CreditCardNumber,
            //    CardCode = processPaymentRequest.CreditCardCvv2,
            //    ExpireMonth = processPaymentRequest.CreditCardExpireMonth.ToString(),
            //    ExpireYear = processPaymentRequest.CreditCardExpireYear.ToString(),
            //    ParcelsQtd = processPaymentRequest.ParcelsQtd.ToString()
            //};

            Merchant prd = new Merchant(
               Guid.Parse(_CieloPaymentSettings.MerchantID),
                _CieloPaymentSettings.MerchantKey);

            api = new CieloApi(CieloEnvironment.Production, prd);

            var customer = new Customer(name: processPaymentRequest.CreditCardName);

            DateTime dtExpire = new DateTime(processPaymentRequest.CreditCardExpireYear, processPaymentRequest.CreditCardExpireMonth, 01);

            var creditCard = new CreditCard(
                cardNumber: processPaymentRequest.CreditCardNumber,
                holder: processPaymentRequest.CreditCardName,
                expirationDate: dtExpire,
                securityCode: processPaymentRequest.CreditCardCvv2,
                brand: CardBrand.Visa);

            var payment = new ApiCielo.Payment(
                amount: processPaymentRequest.OrderTotal,
                currency: ApiCielo.Currency.BRL,
                installments: processPaymentRequest.ParcelsQtd,
                capture: true,
                softDescriptor: "ECommerce",
                creditCard: creditCard);

            /* store order number */
            var merchantOrderId = new Random().Next();

            var transaction = new Transaction(
                merchantOrderId: merchantOrderId.ToString(),
                customer: customer,
                payment: payment
                );

            Transaction returnTransaction = api.CreateTransaction(Guid.NewGuid(), transaction);
            ProcessPaymentResult result = new ProcessPaymentResult();

            switch (returnTransaction.Payment.Status)
            {
                case ApiCielo.Status.Aborted:

                    break;
                case ApiCielo.Status.Authorized:
                    result.NewPaymentStatus = PaymentStatus.Authorized;
                    break;
                case ApiCielo.Status.Denied:
                    result.NewPaymentStatus = PaymentStatus.Pending;
                    break;
                case ApiCielo.Status.NotFinished:
                    result.NewPaymentStatus = PaymentStatus.Pending;
                    break;
                case ApiCielo.Status.PaymentConfirmed:
                    result.NewPaymentStatus = PaymentStatus.Paid;
                    break;
                case ApiCielo.Status.Pending:
                    result.NewPaymentStatus = PaymentStatus.Pending;
                    break;
                case ApiCielo.Status.Refunded:
                    result.NewPaymentStatus = PaymentStatus.Refunded;
                    break;
                case ApiCielo.Status.Scheduled:
                    result.NewPaymentStatus = PaymentStatus.Pending;
                    break;
                case ApiCielo.Status.Voided:
                    result.NewPaymentStatus = PaymentStatus.Voided;
                    break;
                default:
                    result.AddError("Não Suportado");
                    break;
            }

            result.AuthorizationTransactionId = returnTransaction.Payment.PaymentId.ToString();
            result.AuthorizationTransactionCode = returnTransaction.Payment.AuthorizationCode;
            result.AuthorizationTransactionResult = returnTransaction.Payment.ReturnMessage;

            //  result.CaptureTransactionId = 

            return result; // new ProcessPaymentResult { Errors = new[] { "Impmentar " } };
        }

        ProcessPaymentResult IPaymentMethod.ProcessRecurringPayment(ProcessPaymentRequest processPaymentRequest)
        {
            return new ProcessPaymentResult { Errors = new[] { "Recurring payment not supported" } };
        }

        RefundPaymentResult IPaymentMethod.Refund(RefundPaymentRequest refundPaymentRequest)
        {
            return new RefundPaymentResult { Errors = new[] { "Refund method not supported" } };
        }

        IList<string> IPaymentMethod.ValidatePaymentForm(IFormCollection form)
        {
            var warnings = new List<string>();

            //validate
            var validator = new Nop.Plugin.Payments.Cielo.Validators.PaymentInfoValidator(_localizationService);
            var model = new Nop.Plugin.Payments.Cielo.Models.PaymentInfoModel
            {
                CardholderName = form["CardholderName"],
                CardNumber = form["CardNumber"],
                CardCode = form["CardCode"],
                ExpireMonth = form["ExpireMonth"],
                ExpireYear = form["ExpireYear"],
                ParcelsQtd = form["ParcelsQtd"]
            };
            var validationResult = validator.Validate(model);
            if (!validationResult.IsValid)
                warnings.AddRange(validationResult.Errors.Select(error => error.ErrorMessage));

            return warnings;
        }

        VoidPaymentResult IPaymentMethod.Void(VoidPaymentRequest voidPaymentRequest)
        {
            return new VoidPaymentResult { Errors = new[] { "Void method not supported" } };
        }

        decimal IPaymentMethod.GetAdditionalHandlingFee(IList<ShoppingCartItem> cart)
        {
            //return _paymentService.CalculateAdditionalFee(cart,
            //  _paypalStandardPaymentSettings.AdditionalFee, _paypalStandardPaymentSettings.AdditionalFeePercentage);
            return 0;
        }

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
            get { return RecurringPaymentType.NotSupported; }
        }

        /// <summary>
        /// Gets a payment method type
        /// </summary>
        public PaymentMethodType PaymentMethodType
        {
            get { return PaymentMethodType.Redirection; }
        }

        /// <summary>
        /// Gets a value indicating whether we should display a payment information page for this plugin
        /// </summary>
        public bool SkipPaymentInfo
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a payment method description that will be displayed on checkout pages in the public store
        /// </summary>
        public string PaymentMethodDescription
        {
            //return description of this payment method to be display on "payment method" checkout step. good practice is to make it localizable
            //for example, for a redirection payment method, description may be like this: "You will be redirected to PayPal site to complete the payment"
            get { return _localizationService.GetResource("Cartão de Crédito"); }
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
