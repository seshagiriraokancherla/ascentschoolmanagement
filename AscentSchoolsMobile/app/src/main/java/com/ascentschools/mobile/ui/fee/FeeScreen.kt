package com.ascentschools.mobile.ui.fee

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.CheckCircle
import androidx.compose.material.icons.filled.Refresh
import androidx.compose.material.icons.filled.Warning
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import com.ascentschools.mobile.data.api.MobileFeeLineItemDto

@Composable
fun FeeScreen(
    viewModel: FeeViewModel,
    onInitiatePayment: (items: List<MobileFeeLineItemDto>, academicYearId: Int, paymentModeId: Int) -> Unit,
    modifier: Modifier = Modifier
) {
    val uiState     by viewModel.uiState.collectAsState()
    val payState    by viewModel.paymentState.collectAsState()

    // Selected pending items
    val selectedItems = remember { mutableStateListOf<MobileFeeLineItemDto>() }

    // Show success/failure dialogs
    var showSuccessDialog by remember { mutableStateOf(false) }
    var showErrorDialog   by remember { mutableStateOf(false) }
    var dialogMessage     by remember { mutableStateOf("") }

    // React to payment state changes
    LaunchedEffect(payState) {
        when (payState) {
            is PaymentState.Success -> {
                val result = (payState as PaymentState.Success).result
                dialogMessage = "Payment successful!\nReceipt: ${result.receiptNo ?: result.receiptId}\nAmount: ₹${"%.2f".format(result.amount)}"
                showSuccessDialog = true
                selectedItems.clear()
            }
            is PaymentState.Failed -> {
                dialogMessage = (payState as PaymentState.Failed).message
                showErrorDialog = true
            }
            else -> {}
        }
    }

    if (showSuccessDialog) {
        AlertDialog(
            onDismissRequest = { showSuccessDialog = false; viewModel.resetPaymentState() },
            icon    = { Icon(Icons.Default.CheckCircle, contentDescription = null, tint = Color(0xFF4CAF50)) },
            title   = { Text("Payment Successful") },
            text    = { Text(dialogMessage) },
            confirmButton = {
                TextButton(onClick = { showSuccessDialog = false; viewModel.resetPaymentState() }) {
                    Text("OK")
                }
            }
        )
    }

    if (showErrorDialog) {
        AlertDialog(
            onDismissRequest = { showErrorDialog = false; viewModel.resetPaymentState() },
            icon    = { Icon(Icons.Default.Warning, contentDescription = null, tint = MaterialTheme.colorScheme.error) },
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
                val summary = s.summary
                val pendingItems = summary.lineItems.filter { !it.isPaid }

                LazyColumn(
                    modifier            = Modifier.weight(1f),
                    contentPadding      = PaddingValues(16.dp),
                    verticalArrangement = Arrangement.spacedBy(8.dp)
                ) {
                    // ── Summary card ──────────────────────────────────────────
                    item {
                        FeeSummaryCard(
                            year        = summary.academicYear ?: "Current Year",
                            total       = summary.totalAmount,
                            paid        = summary.paidAmount,
                            outstanding = summary.outstandingAmount
                        )
                        Spacer(Modifier.height(8.dp))
                    }

                    // ── Section header ────────────────────────────────────────
                    if (pendingItems.isNotEmpty()) {
                        item {
                            Row(
                                verticalAlignment = Alignment.CenterVertically,
                                modifier = Modifier.padding(vertical = 4.dp)
                            ) {
                                Text(
                                    "Pending Payments",
                                    style = MaterialTheme.typography.titleSmall,
                                    fontWeight = FontWeight.Bold
                                )
                                Spacer(Modifier.weight(1f))
                                TextButton(
                                    onClick = {
                                        if (selectedItems.size == pendingItems.size) selectedItems.clear()
                                        else { selectedItems.clear(); selectedItems.addAll(pendingItems) }
                                    }
                                ) {
                                    Text(if (selectedItems.size == pendingItems.size) "Deselect All" else "Select All")
                                }
                            }
                        }
                    }

                    // ── Pending fee items ─────────────────────────────────────
                    items(pendingItems) { item ->
                        val isSelected = selectedItems.contains(item)
                        FeeItemCard(
                            item       = item,
                            isSelected = isSelected,
                            isPaid     = false,
                            onToggle   = {
                                if (isSelected) selectedItems.remove(item)
                                else selectedItems.add(item)
                            }
                        )
                    }

                    // ── Paid items ────────────────────────────────────────────
                    val paidItems = summary.lineItems.filter { it.isPaid }
                    if (paidItems.isNotEmpty()) {
                        item {
                            Text(
                                "Paid",
                                style      = MaterialTheme.typography.titleSmall,
                                fontWeight = FontWeight.Bold,
                                modifier   = Modifier.padding(vertical = 8.dp)
                            )
                        }
                        items(paidItems) { item ->
                            FeeItemCard(item = item, isSelected = false, isPaid = true, onToggle = {})
                        }
                    }

                    // ── Refresh row ───────────────────────────────────────────
                    item {
                        Row(
                            modifier = Modifier.fillMaxWidth().padding(top = 8.dp),
                            horizontalArrangement = Arrangement.Center
                        ) {
                            TextButton(onClick = { viewModel.loadFees() }) {
                                Icon(Icons.Default.Refresh, contentDescription = null, modifier = Modifier.size(16.dp))
                                Spacer(Modifier.width(4.dp))
                                Text("Refresh")
                            }
                        }
                    }
                }

                // ── Pay button ────────────────────────────────────────────────
                if (selectedItems.isNotEmpty()) {
                    val total = selectedItems.sumOf { it.outstanding.coerceAtLeast(0.0) }
                    Surface(shadowElevation = 8.dp) {
                        Row(
                            modifier = Modifier
                                .fillMaxWidth()
                                .padding(horizontal = 16.dp, vertical = 12.dp),
                            verticalAlignment = Alignment.CenterVertically
                        ) {
                            Column(Modifier.weight(1f)) {
                                Text("Selected: ${selectedItems.size} item(s)", style = MaterialTheme.typography.bodySmall)
                                Text("Total: ₹${"%.2f".format(total)}", fontWeight = FontWeight.Bold)
                            }
                            val isCreating = payState is PaymentState.CreatingOrder
                            Button(
                                onClick = {
                                    onInitiatePayment(
                                        selectedItems.toList(),
                                        summary.academicYearId ?: 0,
                                        1 // Online payment mode ID — typically 3 for "Online"
                                    )
                                },
                                enabled = !isCreating
                            ) {
                                if (isCreating) {
                                    CircularProgressIndicator(
                                        modifier = Modifier.size(18.dp),
                                        strokeWidth = 2.dp,
                                        color = MaterialTheme.colorScheme.onPrimary
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

// ── Summary Card ──────────────────────────────────────────────────────────────

@Composable
private fun FeeSummaryCard(year: String, total: Double, paid: Double, outstanding: Double) {
    Card(
        modifier = Modifier.fillMaxWidth(),
        colors   = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.primaryContainer)
    ) {
        Column(modifier = Modifier.padding(16.dp)) {
            Text(year, style = MaterialTheme.typography.titleMedium, fontWeight = FontWeight.Bold)
            Spacer(Modifier.height(12.dp))
            Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceEvenly) {
                SummaryAmount("Total",       total,       MaterialTheme.colorScheme.onPrimaryContainer)
                SummaryAmount("Paid",        paid,        Color(0xFF2E7D32))
                SummaryAmount("Outstanding", outstanding, if (outstanding > 0) Color(0xFFC62828) else Color(0xFF2E7D32))
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

// ── Fee Item Card ─────────────────────────────────────────────────────────────

@Composable
private fun FeeItemCard(
    item      : MobileFeeLineItemDto,
    isSelected: Boolean,
    isPaid    : Boolean,
    onToggle  : () -> Unit
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
            modifier = Modifier.padding(12.dp),
            verticalAlignment = Alignment.CenterVertically
        ) {
            if (!isPaid) {
                Checkbox(checked = isSelected, onCheckedChange = { onToggle() })
                Spacer(Modifier.width(4.dp))
            } else {
                Icon(
                    Icons.Default.CheckCircle,
                    contentDescription = "Paid",
                    tint = Color(0xFF4CAF50),
                    modifier = Modifier.size(24.dp).padding(end = 4.dp)
                )
                Spacer(Modifier.width(8.dp))
            }

            Column(Modifier.weight(1f)) {
                Text(item.feeTypeName ?: "Fee", fontWeight = FontWeight.Medium)
                if (!item.termName.isNullOrBlank()) {
                    Text(item.termName, style = MaterialTheme.typography.bodySmall, color = Color.Gray)
                }
            }

            Column(horizontalAlignment = Alignment.End) {
                Text("₹${"%.2f".format(item.amount)}", fontWeight = FontWeight.Bold)
                if (isPaid) {
                    Text("Paid", style = MaterialTheme.typography.labelSmall, color = Color(0xFF2E7D32))
                } else if (item.paidAmount > 0) {
                    Text(
                        "Paid: ₹${"%.2f".format(item.paidAmount)}",
                        style = MaterialTheme.typography.labelSmall,
                        color = Color(0xFF2E7D32)
                    )
                    Text(
                        "Due: ₹${"%.2f".format(item.outstanding)}",
                        style = MaterialTheme.typography.labelSmall,
                        color = Color(0xFFC62828)
                    )
                }
            }
        }
    }
}
