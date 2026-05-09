using System;
using System.Collections.Generic;

namespace AscentSchools.API.Helpers
{
    /// <summary>
    /// Factory that resolves the correct IGatewayService by gateway name.
    /// To add a new gateway:
    ///   1. Implement IGatewayService (e.g. CashfreeGatewayService)
    ///   2. Add an entry to _services below
    ///   3. Add the gateway_name string to the UI dropdown in GatewaySettingsPage
    /// No other changes needed.
    /// </summary>
    public static class GatewayServiceFactory
    {
        private static readonly Dictionary<string, IGatewayService> _services =
            new Dictionary<string, IGatewayService>(StringComparer.OrdinalIgnoreCase)
            {
                { "Razorpay", new RazorpayGatewayService() },
                // { "Cashfree", new CashfreeGatewayService() },
                // { "PayU",     new PayUGatewayService() },
            };

        /// <summary>Returns the gateway service for the given name. Throws if not supported.</summary>
        public static IGatewayService Get(string gatewayName)
        {
            if (_services.TryGetValue(gatewayName ?? "", out var svc))
                return svc;
            throw new NotSupportedException(
                $"Payment gateway '{gatewayName}' is not supported. Supported: {string.Join(", ", _services.Keys)}");
        }

        public static IEnumerable<string> SupportedGateways => _services.Keys;
    }
}
