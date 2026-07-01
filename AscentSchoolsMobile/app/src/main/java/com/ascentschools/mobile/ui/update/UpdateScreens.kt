package com.ascentschools.mobile.ui.update

import android.content.ActivityNotFoundException
import android.content.Context
import android.content.Intent
import android.net.Uri
import androidx.activity.compose.BackHandler
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.SystemUpdate
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp

/** Opens the app's Play Store listing. Prefers the server-supplied [storeUrl],
 *  else builds a market:// link, falling back to the https Play Store URL. */
fun openPlayStore(context: Context, storeUrl: String?) {
    val pkg     = context.packageName
    val primary = storeUrl?.takeIf { it.isNotBlank() } ?: "market://details?id=$pkg"
    val webUrl  = "https://play.google.com/store/apps/details?id=$pkg"
    try {
        context.startActivity(
            Intent(Intent.ACTION_VIEW, Uri.parse(primary)).addFlags(Intent.FLAG_ACTIVITY_NEW_TASK)
        )
    } catch (e: ActivityNotFoundException) {
        context.startActivity(
            Intent(Intent.ACTION_VIEW, Uri.parse(webUrl)).addFlags(Intent.FLAG_ACTIVITY_NEW_TASK)
        )
    }
}

/** Full-screen, non-dismissible block. The user must update to proceed. */
@Composable
fun ForceUpdateScreen(message: String?, storeUrl: String?) {
    val context = LocalContext.current

    // Swallow the back button so the user can't bypass the gate.
    BackHandler(enabled = true) { }

    Box(
        modifier = Modifier
            .fillMaxSize()
            .background(MaterialTheme.colorScheme.background)
            .padding(32.dp),
        contentAlignment = Alignment.Center
    ) {
        Column(
            horizontalAlignment = Alignment.CenterHorizontally,
            verticalArrangement  = Arrangement.spacedBy(16.dp)
        ) {
            Icon(
                imageVector = Icons.Filled.SystemUpdate,
                contentDescription = null,
                modifier = Modifier.size(72.dp),
                tint = MaterialTheme.colorScheme.primary
            )
            Text(
                text = "Update Required",
                style = MaterialTheme.typography.headlineSmall
            )
            Text(
                text = message?.takeIf { it.isNotBlank() }
                    ?: "A newer version of the app is required to continue. Please update to proceed.",
                style = MaterialTheme.typography.bodyMedium,
                textAlign = TextAlign.Center
            )
            Spacer(Modifier.height(8.dp))
            Button(onClick = { openPlayStore(context, storeUrl) }) {
                Text("Update Now")
            }
        }
    }
}

/** Dismissible "update available" nudge shown over the normal app content. */
@Composable
fun UpdateAvailableDialog(message: String?, storeUrl: String?, onDismiss: () -> Unit) {
    val context = LocalContext.current
    AlertDialog(
        onDismissRequest = onDismiss,
        title   = { Text("Update Available") },
        text    = {
            Text(message?.takeIf { it.isNotBlank() }
                ?: "A new version of the app is available with improvements and fixes.")
        },
        confirmButton = {
            TextButton(onClick = { openPlayStore(context, storeUrl); onDismiss() }) {
                Text("Update")
            }
        },
        dismissButton = {
            TextButton(onClick = onDismiss) { Text("Later") }
        }
    )
}
