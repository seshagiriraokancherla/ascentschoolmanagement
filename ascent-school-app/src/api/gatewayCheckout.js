import api from './axiosInstance'

// Opens the payment gateway checkout for an order created by POST .../payment-orders and
// verifies the result with our API. The gateway is whatever the school has active —
// the order response says which (gatewayName), so callers never branch themselves.
//
// Resolves with the receipt returned by the verify endpoint.
// Rejects with:
//   Error('dismissed')                — the payer closed / cancelled checkout
//   Error('verify_failed') + .cause   — checkout finished but the server did not confirm it
//                                       (.cause is the API error, for apiError())

const RAZORPAY_JS = 'https://checkout.razorpay.com/v1/checkout.js'
const EASEBUZZ_JS = 'https://ebz-static.s3.ap-south-1.amazonaws.com/easecheckout/v2.0.0/easebuzz-checkout-v2.min.js'

// Easebuzz statuses that mean the payer abandoned checkout (no money moved).
const EASEBUZZ_ABANDONED = ['usercancelled', 'dropped', 'bounced']

const scriptCache = {}
function loadScript(src) {
  if (!scriptCache[src]) {
    scriptCache[src] = new Promise((resolve) => {
      const s   = document.createElement('script')
      s.src     = src
      s.onload  = () => resolve(true)
      s.onerror = () => { delete scriptCache[src]; resolve(false) }
      document.body.appendChild(s)
    })
  }
  return scriptCache[src]
}

function verifyFailed(cause) {
  const err = new Error('verify_failed')
  err.cause = cause
  return err
}

// Flat string map for the API's Dictionary<string,string> (drops nested objects).
function flatten(obj) {
  const out = {}
  Object.entries(obj || {}).forEach(([k, v]) => {
    if (v === null || v === undefined) out[k] = ''
    else if (typeof v !== 'object')   out[k] = String(v)
  })
  return out
}

/**
 * @param {object} p
 * @param {object} p.order        data from POST .../payment-orders
 * @param {string} p.verifyUrl    e.g. `/school/fees/payment-orders/${order.gatewayOrderId}/verify`
 * @param {string} [p.title]      Razorpay modal title
 * @param {string} [p.description]
 * @param {string} [p.payerName]  Razorpay prefill
 * @param {string} [p.themeColor]
 */
export async function openGatewayCheckout({ order, verifyUrl, title, description, payerName, themeColor }) {
  const gateway = (order?.gatewayName || 'Razorpay').toLowerCase()
  if (gateway === 'easebuzz') return easebuzzCheckout({ order, verifyUrl, themeColor })
  return razorpayCheckout({ order, verifyUrl, title, description, payerName, themeColor })
}

async function razorpayCheckout({ order, verifyUrl, title, description, payerName, themeColor }) {
  if (!(await loadScript(RAZORPAY_JS)))
    throw new Error('Could not load the Razorpay checkout. Check your internet connection.')

  return new Promise((resolve, reject) => {
    const rzp = new window.Razorpay({
      key:         order.keyId,
      amount:      order.amountInPaise,
      currency:    order.currency,
      order_id:    order.externalOrderId,
      name:        title || 'School Fee Payment',
      description: description || '',
      theme:       { color: themeColor || '#1677ff' },
      prefill:     { name: payerName || '' },
      modal:       { ondismiss: () => reject(new Error('dismissed')) },
      handler: async (response) => {
        try {
          const res = await api.post(verifyUrl, {
            PaymentId: response.razorpay_payment_id,
            OrderId:   response.razorpay_order_id,
            Signature: response.razorpay_signature,
          })
          resolve(res.data?.data)
        } catch (e) {
          reject(verifyFailed(e))
        }
      },
    })
    rzp.open()
  })
}

async function easebuzzCheckout({ order, verifyUrl, themeColor }) {
  if (!order.accessKey) throw new Error('The payment gateway did not return an access key.')
  if (!(await loadScript(EASEBUZZ_JS)))
    throw new Error('Could not load the Easebuzz checkout. Check your internet connection.')

  const env = (order.environment || 'prod').toLowerCase() === 'test' ? 'test' : 'prod'

  return new Promise((resolve, reject) => {
    const checkout = new window.EasebuzzCheckout(order.keyId, env)
    checkout.initiatePayment({
      access_key: order.accessKey,
      theme:      themeColor || '#1677ff',
      onResponse: async (response) => {
        const status = String(response?.status || '').toLowerCase()
        try {
          // Always tell the server — it re-checks the transaction with Easebuzz itself,
          // creates the receipt on success and marks abandoned orders as failed.
          const res = await api.post(verifyUrl, { GatewayResponse: flatten(response) })
          resolve(res.data?.data)
        } catch (e) {
          reject(EASEBUZZ_ABANDONED.includes(status) ? new Error('dismissed') : verifyFailed(e))
        }
      },
    })
  })
}
