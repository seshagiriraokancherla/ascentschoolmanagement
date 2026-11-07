import api from './axiosInstance'

// Opens the school's payment gateway checkout for an order created by
// POST /mobile/fees/payment-orders and verifies it with our API. The order response says
// which gateway the school uses (gatewayName) — Razorpay or Easebuzz.
// (Same logic as the school app's src/api/gatewayCheckout.js — keep the two in sync.)
//
// Resolves with the receipt. Rejects with Error('cancelled') when the parent closes /
// cancels checkout, otherwise with an Error carrying the server's message.
//
// Every gateway this file can open must be listed in X-Client-Gateways (axiosInstance.js).

const RAZORPAY_JS = 'https://checkout.razorpay.com/v1/checkout.js'
const EASEBUZZ_JS = 'https://ebz-static.s3.ap-south-1.amazonaws.com/easecheckout/v2.0.0/easebuzz-checkout-v2.min.js'

// Easebuzz statuses that mean the parent abandoned checkout (no money moved).
const EASEBUZZ_ABANDONED = ['usercancelled', 'dropped', 'bounced']

const scriptCache = {}
function loadScript(src) {
  if (!scriptCache[src]) {
    scriptCache[src] = new Promise((resolve, reject) => {
      const s   = document.createElement('script')
      s.src     = src
      s.onload  = () => resolve()
      s.onerror = () => {
        delete scriptCache[src]
        reject(new Error('Failed to load payment gateway. Check your internet connection.'))
      }
      document.body.appendChild(s)
    })
  }
  return scriptCache[src]
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
 * @param {object} p.order       data from POST /mobile/fees/payment-orders
 * @param {string} p.title       Razorpay modal title
 * @param {string} p.description
 * @param {string} [p.themeColor]
 */
export async function openGatewayCheckout({ order, title, description, themeColor }) {
  const verifyUrl = `/mobile/fees/payment-orders/${order.gatewayOrderId}/verify`
  const gateway   = (order?.gatewayName || 'Razorpay').toLowerCase()
  if (gateway === 'easebuzz') return easebuzzCheckout({ order, verifyUrl, themeColor })
  return razorpayCheckout({ order, verifyUrl, title, description, themeColor })
}

async function razorpayCheckout({ order, verifyUrl, title, description, themeColor }) {
  await loadScript(RAZORPAY_JS)
  return new Promise((resolve, reject) => {
    const rzp = new window.Razorpay({
      key:         order.keyId,
      amount:      order.amountInPaise,
      currency:    order.currency || 'INR',
      order_id:    order.externalOrderId,
      name:        title,
      description: description,
      theme:       { color: themeColor || '#1677ff' },
      modal:       { ondismiss: () => reject(new Error('cancelled')) },
      handler: async (response) => {
        try {
          const res = await api.post(verifyUrl, {
            PaymentId: response.razorpay_payment_id,
            OrderId:   response.razorpay_order_id,
            Signature: response.razorpay_signature,
          })
          resolve(res.data?.data)
        } catch (e) { reject(e) }
      },
    })
    rzp.open()
  })
}

async function easebuzzCheckout({ order, verifyUrl, themeColor }) {
  if (!order.accessKey) throw new Error('The payment gateway did not return an access key.')
  await loadScript(EASEBUZZ_JS)

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
          reject(EASEBUZZ_ABANDONED.includes(status) ? new Error('cancelled') : e)
        }
      },
    })
  })
}
