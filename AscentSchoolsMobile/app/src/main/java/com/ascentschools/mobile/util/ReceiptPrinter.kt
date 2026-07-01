package com.ascentschools.mobile.util

import android.content.Context
import android.print.PrintAttributes
import android.print.PrintManager
import android.view.View
import android.webkit.WebView
import android.webkit.WebViewClient
import com.ascentschools.mobile.data.api.ReceiptDetailDto

/**
 * Renders a fee receipt to an offscreen WebView and hands it to Android's
 * PrintManager — the user gets the system print sheet (real printer OR
 * "Save as PDF"). No third-party PDF library required.
 */
object ReceiptPrinter {

    // Hold the WebView so it isn't garbage-collected before the print adapter runs.
    private var webViewRef: WebView? = null

    fun print(context: Context, receipt: ReceiptDetailDto, schoolName: String?) {
        val wv = WebView(context)
        // Offscreen print-only WebView: force software rendering so it never
        // touches the GPU/Vulkan path. Avoids native crashes inside the device
        // GPU driver (Adreno vkCreateInstance) during WebView GPU init, and is
        // the documented recommendation for WebViews used purely for printing.
        wv.setLayerType(View.LAYER_TYPE_SOFTWARE, null)
        wv.webViewClient = object : WebViewClient() {
            override fun onPageFinished(view: WebView, url: String?) {
                val jobName = "Receipt_" + (receipt.receiptNo ?: receipt.receiptId.toString())
                val pm = context.getSystemService(Context.PRINT_SERVICE) as PrintManager
                val adapter = view.createPrintDocumentAdapter(jobName)
                pm.print(jobName, adapter, PrintAttributes.Builder().build())
                webViewRef = null
            }
        }
        wv.loadDataWithBaseURL(null, buildHtml(receipt, schoolName), "text/html", "UTF-8", null)
        webViewRef = wv
    }

    private fun money(v: Double) = "₹" + String.format("%.2f", v)

    private fun esc(s: String?): String = (s ?: "")
        .replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;")

    private fun buildHtml(r: ReceiptDetailDto, schoolName: String?): String {
        val rows = StringBuilder()
        for (it in r.items) {
            val name = it.feeTypeName ?: it.routeName ?: it.hostelName ?: "-"
            rows.append(
                "<tr>" +
                    "<td>${esc(name)}</td>" +
                    "<td>${esc(it.termName ?: "-")}</td>" +
                    "<td class='r'>${money(it.amount)}</td>" +
                    "<td class='r'>${if (it.concessionAmount > 0) money(it.concessionAmount) else "-"}</td>" +
                    "<td class='r'>${money(it.netAmount)}</td>" +
                "</tr>"
            )
        }

        val cancelled = (r.status ?: "").equals("Cancelled", ignoreCase = true)
        val cancelBanner = if (cancelled)
            "<div class='cancel'>CANCELLED</div>" else ""

        return """
            <html><head><meta name="viewport" content="width=device-width, initial-scale=1"/>
            <style>
              body { font-family: -apple-system, Roboto, Arial, sans-serif; color:#222; padding:16px; }
              .hdr { text-align:center; border-bottom:2px solid #1677ff; padding-bottom:8px; margin-bottom:12px; }
              .hdr h2 { margin:0; color:#1677ff; }
              .hdr .sub { color:#555; font-size:13px; }
              .meta { display:flex; justify-content:space-between; font-size:13px; margin-bottom:10px; }
              table { width:100%; border-collapse:collapse; font-size:13px; }
              th,td { border:1px solid #ddd; padding:6px 8px; text-align:left; }
              th { background:#f5f7fa; }
              .r { text-align:right; }
              .total { text-align:right; font-weight:bold; font-size:15px; margin-top:10px; }
              .foot { margin-top:24px; font-size:12px; color:#666; display:flex; justify-content:space-between; }
              .cancel { color:#cf1322; border:2px solid #cf1322; display:inline-block; padding:2px 10px;
                        font-weight:bold; transform:rotate(-6deg); margin-top:6px; }
            </style></head><body>
              <div class="hdr">
                <h2>${esc(schoolName ?: "Fee Receipt")}</h2>
                <div class="sub">Fee Receipt</div>
                $cancelBanner
              </div>
              <div class="meta">
                <div>
                  <div><b>Receipt No:</b> ${esc(r.receiptNo ?: r.receiptId.toString())}</div>
                  <div><b>Date:</b> ${esc(r.paymentDate ?: "")}</div>
                  <div><b>Mode:</b> ${esc(r.paymentModeName ?: "")}</div>
                </div>
                <div>
                  <div><b>Student:</b> ${esc(r.studentName ?: "")}</div>
                  <div><b>Adm No:</b> ${esc(r.admissionNo ?: "")}</div>
                  <div><b>Class:</b> ${esc(r.className ?: "")} &nbsp; ${esc(r.academicYear ?: "")}</div>
                </div>
              </div>
              <table>
                <thead><tr><th>Fee</th><th>Term</th><th class="r">Amount</th>
                  <th class="r">Concession</th><th class="r">Net</th></tr></thead>
                <tbody>$rows</tbody>
              </table>
              <div class="total">Total Paid: ${money(r.totalAmount)}</div>
              <div class="foot">
                <span>Collected by: ${esc(r.createdBy ?: "")}</span>
                <span>This is a computer-generated receipt.</span>
              </div>
            </body></html>
        """.trimIndent()
    }
}
