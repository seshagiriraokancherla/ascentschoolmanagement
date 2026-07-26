package com.ascentschools.mobile.ui.update

import android.app.Activity
import android.util.Log
import androidx.activity.ComponentActivity
import androidx.activity.result.ActivityResultLauncher
import androidx.activity.result.IntentSenderRequest
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.runtime.State
import androidx.compose.runtime.mutableStateOf
import com.google.android.play.core.appupdate.AppUpdateInfo
import com.google.android.play.core.appupdate.AppUpdateManager
import com.google.android.play.core.appupdate.AppUpdateManagerFactory
import com.google.android.play.core.appupdate.AppUpdateOptions
import com.google.android.play.core.install.InstallStateUpdatedListener
import com.google.android.play.core.install.model.AppUpdateType
import com.google.android.play.core.install.model.InstallStatus
import com.google.android.play.core.install.model.UpdateAvailability

/**
 * Play In-App Updates wrapper. Replaces the old `app_config` versionCode gate
 * (server-held `latest_version_code` vs `BuildConfig.VERSION_CODE`).
 *
 * Why: the DB number could drift ahead of what Play actually serves a device,
 * producing a false "update available" that Play couldn't satisfy (the "Open"
 * button loop). This asks **Google Play directly** whether a newer build is
 * available to THIS device, so it can never prompt when nothing newer exists.
 *
 * Policy:
 *  - priority >= [HIGH_PRIORITY] OR available for >= [IMMEDIATE_STALENESS_DAYS]
 *    days  -> IMMEDIATE (full-screen blocking update)
 *  - otherwise                                -> FLEXIBLE (background download;
 *    the user keeps using the app, then taps "Restart" to install)
 *
 * Priority is set per-release via the Play Developer API
 * (`Edits.tracks.releases.inAppUpdatePriority`, 0–5).
 *
 * Lifecycle: construct in `Activity.onCreate` (it registers an ActivityResult
 * launcher, which must happen before the activity is STARTED); call
 * [checkForUpdate] from onCreate, [onResume] from `Activity.onResume`, and
 * [destroy] from `Activity.onDestroy`.
 *
 * NOTE: in-app updates only work when the app was installed from Google Play (or
 * Play Internal App Sharing). A sideloaded/debug build always reports "no update".
 */
class InAppUpdater(private val activity: ComponentActivity) {

    private val manager: AppUpdateManager = AppUpdateManagerFactory.create(activity)

    // Observed by Compose: flips true once a FLEXIBLE update has finished
    // downloading and is ready to install. Show a "Restart to update" prompt
    // and call [completeUpdate].
    private val _flexibleDownloaded = mutableStateOf(false)
    val flexibleDownloaded: State<Boolean> get() = _flexibleDownloaded

    private val launcher: ActivityResultLauncher<IntentSenderRequest> =
        activity.registerForActivityResult(ActivityResultContracts.StartIntentSenderForResult()) { result ->
            // RESULT_OK: accepted. RESULT_CANCELED: user dismissed. Anything else: failed.
            // For FLEXIBLE the real signal is the install listener, so we only log here.
            if (result.resultCode != Activity.RESULT_OK)
                Log.w(TAG, "In-app update flow not completed (resultCode=${result.resultCode})")
        }

    private val installListener = InstallStateUpdatedListener { state ->
        if (state.installStatus() == InstallStatus.DOWNLOADED) _flexibleDownloaded.value = true
    }

    init { manager.registerListener(installListener) }

    /** Query Play and, if an update is available to this device, start the flow. */
    fun checkForUpdate() {
        manager.appUpdateInfo
            .addOnSuccessListener { info ->
                if (info.updateAvailability() != UpdateAvailability.UPDATE_AVAILABLE) return@addOnSuccessListener

                val staleEnough = (info.clientVersionStalenessDays() ?: 0) >= IMMEDIATE_STALENESS_DAYS
                val wantImmediate = info.updatePriority() >= HIGH_PRIORITY || staleEnough

                val type = when {
                    wantImmediate && info.isUpdateTypeAllowed(AppUpdateType.IMMEDIATE) -> AppUpdateType.IMMEDIATE
                    info.isUpdateTypeAllowed(AppUpdateType.FLEXIBLE)                    -> AppUpdateType.FLEXIBLE
                    info.isUpdateTypeAllowed(AppUpdateType.IMMEDIATE)                   -> AppUpdateType.IMMEDIATE
                    else                                                               -> return@addOnSuccessListener
                }
                startFlow(info, type)
            }
            .addOnFailureListener { Log.w(TAG, "appUpdateInfo lookup failed", it) }
    }

    /** Resume an interrupted IMMEDIATE update, and surface a FLEXIBLE update that
     *  finished downloading while the app was in the background. */
    fun onResume() {
        manager.appUpdateInfo.addOnSuccessListener { info ->
            if (info.updateAvailability() == UpdateAvailability.DEVELOPER_TRIGGERED_UPDATE_IN_PROGRESS)
                startFlow(info, AppUpdateType.IMMEDIATE)
            if (info.installStatus() == InstallStatus.DOWNLOADED)
                _flexibleDownloaded.value = true
        }
    }

    /** Install a downloaded FLEXIBLE update (restarts the app). */
    fun completeUpdate() = manager.completeUpdate()

    /** Manual fallback (e.g. the login-screen "Update App" button): open the listing. */
    fun openInPlayStore() = openPlayStore(activity, null)

    fun destroy() = manager.unregisterListener(installListener)

    private fun startFlow(info: AppUpdateInfo, type: Int) {
        runCatching {
            manager.startUpdateFlowForResult(info, launcher, AppUpdateOptions.newBuilder(type).build())
        }.onFailure { Log.e(TAG, "startUpdateFlowForResult failed", it) }
    }

    companion object {
        private const val TAG = "InAppUpdater"
        private const val HIGH_PRIORITY = 4            // Play priority 0–5; 4+ => force
        private const val IMMEDIATE_STALENESS_DAYS = 7 // available this long => force
    }
}
