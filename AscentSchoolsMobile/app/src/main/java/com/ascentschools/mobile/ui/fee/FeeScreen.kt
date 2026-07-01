package com.ascentschools.mobile.ui.fee

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.RoundedCornerShape
import android.widget.Toast
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.CheckCircle
import androidx.compose.material.icons.filled.Print
import androidx.compose.material.icons.filled.Refresh
import androidx.compose.material.icons.filled.Warning
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import com.ascentschools.mobile.AscentApp
import com.ascentschools.mobile.R
import com.ascentschools.mobile.data.api.MobileFeeLineItemDto
import com.ascentschools.mobile.data.api.MobileFeeSummaryDto
import com.ascentschools.mobile.util.ReceiptPrinter

@Composable
fun FeeScreen(
    viewModel         : FeeViewModel,
    onInitiatePayment : (items: List<MobileFeeLineItemDto>, academicYearId: Int, feeTypeCategory: String) -> Unit,
    modifier          : Modifier = Modifier
) {
    val uiState          by viewModel.uiState.collectAsState()
    val payState         by viewModel.paymentState.collectAsState()
    val selectedCategory by viewModel.selectedCategory.collectAsState()

    // Selection is scoped to a single academic year — mixing years in one payment is not allowed
    val selectedItems = remember { mutableStateListOf<MobileFeeLineItemDto>() }
    var selectedYearId by remember { mutableIntStateOf(0) }

    // Receipt print (Option 2 — WebView + system print / save-as-PDF)
    val context    = LocalContext.current
    val schoolName = (context.applicationContext as? AscentApp)?.tokenStore?.brandingName
        ?: context.getString(R.string.app_name)
    val printReceipt: (Int) -> Unit = { receiptId ->
        viewModel.fetchReceipt(
            receiptId,
            onReady = { ReceiptPrinter.print(context, it, schoolName) },
            onError = { Toast.makeText(context, it, Toast.LENGTH_LONG).show() }
        )
    }

    // Clear selection whenever category changes
    LaunchedEffect(selectedCategory) {
        selectedItems.clear()
        selectedYearId = 0
    }

    // Success / failure dialogs
    var showSuccessDialog by remember { mutableStateOf(false) }
    var showErrorDialog   by remember { mutableStateOf(false) }
    var dialogMessage     by remember { mutableStateOf("") }
    var successReceiptId  by remember { mutableStateOf<Int?>(null) }

    LaunchedEffect(payState) {
        when (val s = payState) {
            is PaymentState.Success -> {
                dialogMessage     = "Payment successful!\nReceipt: ${s.result.receiptNo ?: s.result.receiptId}\nAmount: ₹${"%.2f".format(s.result.totalAmount)}"
                successReceiptId  = s.result.receiptId
                showSuccessDialog = true
                selectedItems.clear()
                selectedYearId = 0
            }
            is PaymentState.Failed -> {
                dialogMessage   = s.message
                showErrorDialog = true
            }
            else -> {}
        }
    }

    if (showSuccessDialog) {
        AlertDialog(
            onDismissRequest = { showSuccessDialog = false; viewModel.resetPaymentState() },
            icon    = { Icon(Icons.Default.CheckCircle, null, tint = Color(0xFF4CAF50)) },
            title   = { Text("Payment Successful") },
            text    = { Text(dialogMessage) },
            confirmButton = {
                TextButton(onClick = { showSuccessDialog = false; viewModel.resetPaymentState() }) {
                    Text("OK")
                }
            },
            dismissButton = {
                successReceiptId?.let { rid ->
                    TextButton(onClick = { printReceipt(rid) }) {
                        Icon(Icons.Default.Print, null, Modifier.size(18.dp))
                        Spacer(Modifier.width(4.dp))
                        Text("View / Print Receipt")
                    }
                }
            }
        )
    }

    if (showErrorDialog) {
        AlertDialog(
            onDismissRequest = { showErrorDialog = false; viewModel.resetPaymentState() },
            icon    = { Icon(Icons.Default.Warning, null, tint = MaterialTheme.colorScheme.error) },
            title   = { Text("Payment Failed") },
            text    = { Text(dialogMessage) },
            confirmButton = {
                TextButton(onClick = { showErrorDialog = false; viewModel.resetPaymentState() }) {
                    Text("OK")
                }
            }
        )
    }

    Column(modifier = modifier.fillMaxSize()) {

        // ── Category tabs: School / Transport / Hostel ────────────────────────
        TabRow(
            selectedTabIndex = viewModel.categories.indexOf(selectedCategory),
            containerColor   = MaterialTheme.colorScheme.surface
        ) {
            viewModel.categories.forEach { cat ->
                Tab(
                    selected = selectedCategory == cat,
                    onClick  = { viewModel.selectCategory(cat) },
                    text     = { Text(cat, style = MaterialTheme.typography.labelLarge) }
                )
            }
        }

        when (val s = uiState) {
            is FeeUiState.Loading -> {
                Box(Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
                    CircularProgressIndicator()
                }
            }

            is FeeUiState.Error -> {
                Column(
                    Modifier.fillMaxSize().padding(24.dp),
                    horizontalAlignment = Alignment.CenterHorizontally,
                    verticalArrangement = Arrangement.Center
                ) {
                    Text(s.message, color = MaterialTheme.colorScheme.error)
                    Spacer(Modifier.height(12.dp))
                    Button(onClick = { viewModel.loadFees() }) { Text("Retry") }
                }
            }

            is FeeUiState.Success -> {
                val years = s.years

                LazyColumn(
                    modifier            = Modifier.weight(1f),
                    contentPadding      = PaddingValues(16.dp),
                    verticalArrangement = Arrangement.spacedBy(8.dp)
                ) {
                    if (years.isEmpty()) {
                        item {
                            Box(Modifier.fillParentMaxWidth().padding(top = 48.dp),
                                contentAlignment = Alignment.Center) {
                                Text("No outstanding $selectedCategory fees.",
                                     color = MaterialTheme.colorScheme.onSurfaceVariant)
                            }
                        }
                    }

                    years.forEach { yearSummary ->
                        val yearId       = yearSummary.academicYearId ?: 0
                        val pendingItems = yearSummary.lineItems.filter { !it.isPaid }
                        val paidItems    = yearSummary.lineItems.filter {  it.isPaid }
                        val allPendingSel = pendingItems.isNotEmpty() &&
                                           pendingItems.all { selectedItems.contains(it) }

                        // Year summary card
                        item(key = "summary_$yearId") {
                            YearSummaryCard(yearSummary)
                            Spacer(Modifier.height(4.dp))
                        }

                        // Pending items header
                        if (pendingItems.isNotEmpty()) {
                            item(key = "pending_hdr_$yearId") {
                                Row(
                                    verticalAlignment = Alignment.CenterVertically,
                                    modifier          = Modifier.padding(vertical = 2.dp)
                                ) {
                                    Text(
                                        "Pending",
                                        style      = MaterialTheme.typography.titleSmall,
                                        fontWeight = FontWeight.Bold,
                                        modifier   = Modifier.weight(1f)
                                    )
                                    TextButton(onClick = {
                                        if (allPendingSel) {
                                            selectedItems.removeAll(pendingItems.toSet())
                                        } else {
                                            // Clear items from other years before selecting this year
                                            if (selectedYearId != yearId) {
                                                selectedItems.clear()
                                                selectedYearId = yearId
                                            }
                                            pendingItems.forEach {
                                                if (!selectedItems.contains(it)) selectedItems.add(it)
                                            }
                                        }
                                    }) {
                                        Text(if (allPendingSel) "Deselect All" else "Select All")
                                    }
                                }
                            }

                            items(pendingItems, key = { "${yearId}_${it.feeTypeId}_${it.termId}_${it.feePeriodId}" }) { item ->
                                val isSelected = selectedItems.contains(item)
                                FeeItemCard(
                                    item       = item,
                                    isSelected = isSelected,
                                    isPaid     = false,
                                    onToggle   = {
                                        if (isSelected) {
                                            selectedItems.remove(item)
                                            if (selectedItems.isEmpty()) selectedYearId = 0
                                        } else {
                                            // Switching year clears previous selection
                                            if (selectedYearId != yearId && selectedYearId != 0) {
                                                selectedItems.clear()
                                            }
                                            selectedYearId = yearId
                                            selectedItems.add(item)
                                        }
                                    }
                                )
                            }
                        }

                        // Paid items
                        if (paidItems.isNotEmpty()) {
                            item(key = "paid_hdr_$yearId") {
                                Text(
                                    "Paid",
                                    style      = MaterialTheme.typography.titleSmall,
                                    fontWeight = FontWeight.Bold,
                                    modifier   = Modifier.padding(top = 8.dp, bottom = 2.dp)
                                )
                            }
                            items(paidItems, key = { "${yearId}_paid_${it.feeTypeId}_${it.termId}_${it.feePeriodId}" }) { item ->
                                FeeItemCard(
                                    item = item, isSelected = false, isPaid = true, onToggle = {},
                                    onPrint = if (item.canPrint) {{ printReceipt(item.receiptId!!) }} else null
                                )
                            }
                        }

                        item(key = "divider_$yearId") {
                            Divider(modifier = Modifier.padding(vertical = 8.dp))
                        }
                    }

                    // Refresh row
                    item(key = "refresh") {
                        Row(
                            modifier              = Modifier.fillMaxWidth().padding(top = 4.dp),
                            horizontalArrangement = Arrangement.Center
                        ) {
                            TextButton(onClick = { viewModel.loadFees() }) {
                                Icon(Icons.Default.Refresh, null, Modifier.size(16.dp))
                                Spacer(Modifier.width(4.dp))
                                Text("Refresh")
                            }
                        }
                    }
                }

                // ── Sticky pay bar (appears when items selected) ──────────────
                if (selectedItems.isNotEmpty()) {
                    val total = selectedItems.sumOf { it.outstanding.coerceAtLeast(0.0) }
                    Surface(shadowElevation = 8.dp) {
                        Row(
                            modifier          = Modifier.fillMaxWidth()
                                                        .padding(horizontal = 16.dp, vertical = 12.dp),
                            verticalAlignment = Alignment.CenterVertically
                        ) {
                            Column(Modifier.weight(1f)) {
                                Text(
                                    "${selectedItems.size} item(s) selected",
                                    style = MaterialTheme.typography.bodySmall
                                )
                                Text(
                                    "₹${"%.2f".format(total)}",
                                    fontWeight = FontWeight.Bold,
                                    style      = MaterialTheme.typography.titleMedium
                                )
                            }
                            val isCreating = payState is PaymentState.CreatingOrder ||
                                             payState is PaymentState.Verifying
                            Button(
                                onClick = {
                                    onInitiatePayment(
                                        selectedItems.toList(),
                                        selectedYearId,
                                        selectedCategory
                                    )
                                },
                                enabled = !isCreating
                            ) {
                                if (isCreating) {
                                    CircularProgressIndicator(
                                        modifier    = Modifier.size(18.dp),
                                        strokeWidth = 2.dp,
                                        color       = MaterialTheme.colorScheme.onPrimary
                                    )
                                } else {
                                    Text("Pay ₹${"%.2f".format(total)}")
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}

// ── Year summary card ─────────────────────────────────────────────────────────

@Composable
private fun YearSummaryCard(summary: MobileFeeSummaryDto) {
    Card(
        modifier = Modifier.fillMaxWidth(),
        colors   = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.primaryContainer),
        shape    = RoundedCornerShape(10.dp)
    ) {
        Column(modifier = Modifier.padding(16.dp)) {
            Text(
                summary.academicYear ?: "Academic Year",
                style      = MaterialTheme.typography.titleMedium,
                fontWeight = FontWeight.Bold
            )
            Spacer(Modifier.height(10.dp))
            Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceEvenly) {
                SummaryAmount("Total",       summary.totalAmount,       MaterialTheme.colorScheme.onPrimaryContainer)
                SummaryAmount("Paid",        summary.paidAmount,        Color(0xFF2E7D32))
                SummaryAmount("Outstanding", summary.outstandingAmount,
                    if (summary.outstandingAmount > 0) Color(0xFFC62828) else Color(0xFF2E7D32))
            }
        }
    }
}

@Composable
private fun SummaryAmount(label: String, amount: Double, color: Color) {
    Column(horizontalAlignment = Alignment.CenterHorizontally) {
        Text(label, style = MaterialTheme.typography.labelSmall)
        Text("₹${"%.0f".format(amount)}", fontWeight = FontWeight.Bold, color = color)
    }
}

// ── Fee item card ─────────────────────────────────────────────────────────────

@Composable
private fun FeeItemCard(
    item      : MobileFeeLineItemDto,
    isSelected: Boolean,
    isPaid    : Boolean,
    onToggle  : () -> Unit,
    onPrint   : (() -> Unit)? = null
) {
    val containerColor = when {
        isPaid     -> Color(0xFFF1F8E9)
        isSelected -> MaterialTheme.colorScheme.secondaryContainer
        else       -> MaterialTheme.colorScheme.surface
    }

    Card(
        modifier = Modifier.fillMaxWidth(),
        onClick  = { if (!isPaid) onToggle() },
        colors   = CardDefaults.cardColors(containerColor = containerColor),
        shape    = RoundedCornerShape(8.dp)
    ) {
        Row(
            modifier          = Modifier.padding(horizontal = 12.dp, vertical = 10.dp),
            verticalAlignment = Alignment.CenterVertically
        ) {
            if (!isPaid) {
                Checkbox(checked = isSelected, onCheckedChange = { onToggle() })
                Spacer(Modifier.width(4.dp))
            } else {
                Icon(
                    Icons.Default.CheckCircle,
                    contentDescription = "Paid",
                    tint     = Color(0xFF4CAF50),
                    modifier = Modifier.size(24.dp)
                )
                Spacer(Modifier.width(8.dp))
            }

            // Label column
            Column(Modifier.weight(1f)) {
                Text(item.feeTypeName ?: "Fee", fontWeight = FontWeight.Medium)
                if (!item.termName.isNullOrBlank()) {
                    Text(item.termName, style = MaterialTheme.typography.bodySmall, color = Color.Gray)
                }
            }

            // Amount column
            Column(horizontalAlignment = Alignment.End) {
                Text("₹${"%.2f".format(item.amount)}", fontWeight = FontWeight.Bold)
                if (item.concessionAmount > 0) {
                    Text(
                        "Concession: ₹${"%.2f".format(item.concessionAmount)}",
                        style = MaterialTheme.typography.labelSmall,
                        color = Color(0xFF1565C0)
                    )
                }
                when {
                    isPaid -> Text("Paid", style = MaterialTheme.typography.labelSmall, color = Color(0xFF2E7D32))
                    item.paidAmount > 0 -> {
                        Text("Paid: ₹${"%.2f".format(item.paidAmount)}",
                             style = MaterialTheme.typography.labelSmall, color = Color(0xFF2E7D32))
                        Text("Due: ₹${"%.2f".format(item.outstanding)}",
                             style = MaterialTheme.typography.labelSmall, color = Color(0xFFC62828))
                    }
                    item.outstanding > 0 -> Text(
                        "Due: ₹${"%.2f".format(item.outstanding)}",
                        style = MaterialTheme.typography.labelSmall, color = Color(0xFFC62828)
                    )
                }
            }

            // Print receipt — only for paid rows created via the mobile app
            if (onPrint != null) {
                Spacer(Modifier.width(4.dp))
                IconButton(onClick = onPrint) {
                    Icon(Icons.Default.Print, contentDescription = "Print receipt",
                         tint = MaterialTheme.colorScheme.primary)
                }
            }
        }
    }
}
