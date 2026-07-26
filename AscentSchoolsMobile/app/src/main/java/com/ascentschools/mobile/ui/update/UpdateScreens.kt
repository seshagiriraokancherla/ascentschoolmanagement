package com.ascentschools.mobile.ui.update

import android.content.ActivityNotFoundException
import android.content.Context
import android.content.Intent
import android.net.Uri

/** Opens the app's Play Store listing. Prefers the supplied [storeUrl], else builds
 *  a market:// link, falling back to the https Play Store URL.
 *
 *  Manual fallback for the login-screen "Update App" button and [InAppUpdater].
 *  The automatic update flow itself is now driven by Play In-App Updates
 *  ([InAppUpdater]), not by opening this listing. */
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
