package com.ascentschools.mobile.ui.profile

import android.content.Intent
import android.net.Uri
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.DeleteForever
import androidx.compose.material.icons.filled.Person
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.ascentschools.mobile.data.api.StudentProfileDto

@Composable
fun ProfileScreen(
    viewModel: ProfileViewModel,
    modifier : Modifier = Modifier
) {
    val uiState by viewModel.uiState.collectAsState()

    Box(modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
        when (val s = uiState) {
            is ProfileUiState.Loading -> CircularProgressIndicator()
            is ProfileUiState.Error   -> ErrorState(s.message) { viewModel.load() }
            is ProfileUiState.Success -> ProfileContent(s.profile)
        }
    }
}

@Composable
private fun ProfileContent(p: StudentProfileDto) {
    Column(
        modifier            = Modifier
            .fillMaxSize()
            .verticalScroll(rememberScrollState())
            .padding(16.dp),
        horizontalAlignment = Alignment.CenterHorizontally
    ) {
        Spacer(Modifier.height(16.dp))

        // Avatar placeholder
        Surface(
            shape  = CircleShape,
            color  = MaterialTheme.colorScheme.primaryContainer,
            modifier = Modifier.size(80.dp)
        ) {
            Box(contentAlignment = Alignment.Center) {
                Icon(
                    Icons.Default.Person,
                    contentDescription = null,
                    modifier = Modifier.size(48.dp),
                    tint     = MaterialTheme.colorScheme.onPrimaryContainer
                )
            }
        }

        Spacer(Modifier.height(12.dp))
        Text(p.fullName, fontWeight = FontWeight.Bold, fontSize = 20.sp)
        Text(
            "${p.className} · ${p.admissionNo}",
            fontSize = 13.sp,
            color    = MaterialTheme.colorScheme.onBackground.copy(alpha = 0.6f)
        )

        Spacer(Modifier.height(24.dp))

        ProfileCard("Personal Details") {
            InfoRow("Father's Name", p.fatherName)
            InfoRow("Mother's Name", p.motherName)
            InfoRow("Date of Birth", p.dateOfBirth ?: "-")
            InfoRow("Gender", p.gender ?: "-")
            InfoRow("Blood Group", p.bloodGroup ?: "-")
            InfoRow("Mobile", p.mobile ?: "-")
        }

        Spacer(Modifier.height(12.dp))

        ProfileCard("Academic Details") {
            InfoRow("Class", p.className)
            InfoRow("Section", p.sectionName ?: "-")
            InfoRow("Admission No", p.admissionNo)
            InfoRow("Academic Year", p.academicYear ?: "-")
        }

        if (!p.address.isNullOrBlank()) {
            Spacer(Modifier.height(12.dp))
            ProfileCard("Address") {
                Text(p.address, fontSize = 14.sp)
            }
        }

        Spacer(Modifier.height(24.dp))

        // ── Delete Account ────────────────────────────────────────────
        DeleteAccountButton()

        Spacer(Modifier.height(24.dp))
    }
}

@Composable
private fun DeleteAccountButton() {
    val context = LocalContext.current
    var showDialog by remember { mutableStateOf(false) }

    if (showDialog) {
        AlertDialog(
            onDismissRequest = { showDialog = false },
            icon  = { Icon(Icons.Default.DeleteForever, contentDescription = null, tint = MaterialTheme.colorScheme.error) },
            title = { Text("Delete Account") },
            text  = {
                Text("This will open a form to request deletion of your Ascent Schools mobile account and associated data.\n\nYour academic records held by your school are not affected.")
            },
            confirmButton = {
                TextButton(onClick = {
                    showDialog = false
                    val intent = Intent(Intent.ACTION_VIEW, Uri.parse("https://edu-care.in/account-deletion.html"))
                    context.startActivity(intent)
                }) {
                    Text("Continue", color = MaterialTheme.colorScheme.error)
                }
            },
            dismissButton = {
                TextButton(onClick = { showDialog = false }) { Text("Cancel") }
            }
        )
    }

    OutlinedButton(
        onClick = { showDialog = true },
        modifier = Modifier.fillMaxWidth(),
        colors   = ButtonDefaults.outlinedButtonColors(contentColor = MaterialTheme.colorScheme.error),
        border   = androidx.compose.foundation.BorderStroke(1.dp, MaterialTheme.colorScheme.error)
    ) {
        Icon(Icons.Default.DeleteForever, contentDescription = null, modifier = Modifier.size(18.dp))
        Spacer(Modifier.width(8.dp))
        Text("Delete My Account")
    }
}

@Composable
private fun ProfileCard(title: String, content: @Composable ColumnScope.() -> Unit) {
    Card(
        shape    = RoundedCornerShape(12.dp),
        modifier = Modifier.fillMaxWidth()
    ) {
        Column(Modifier.padding(16.dp)) {
            Text(title, fontWeight = FontWeight.SemiBold, fontSize = 14.sp,
                color = MaterialTheme.colorScheme.primary)
            Spacer(Modifier.height(8.dp))
            content()
        }
    }
}

@Composable
private fun InfoRow(label: String, value: String?) {
    Row(
        modifier = Modifier
            .fillMaxWidth()
            .padding(vertical = 4.dp),
        horizontalArrangement = Arrangement.SpaceBetween
    ) {
        Text(label,  fontSize = 13.sp, color = MaterialTheme.colorScheme.onSurface.copy(alpha = 0.6f))
        Text(value ?: "-", fontSize = 13.sp, fontWeight = FontWeight.Medium)
    }
}

@Composable
private fun ErrorState(message: String, onRetry: () -> Unit) {
    Column(horizontalAlignment = Alignment.CenterHorizontally) {
        Text(message, color = MaterialTheme.colorScheme.error)
        Spacer(Modifier.height(12.dp))
        Button(onClick = onRetry) { Text("Retry") }
    }
}
