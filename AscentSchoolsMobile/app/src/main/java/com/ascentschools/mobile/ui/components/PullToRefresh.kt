package com.ascentschools.mobile.ui.components

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.BoxScope
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.pulltorefresh.PullToRefreshContainer
import androidx.compose.material3.pulltorefresh.rememberPullToRefreshState
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.input.nestedscroll.nestedScroll
import kotlinx.coroutines.delay

/**
 * Swipe-down pull-to-refresh wrapper (Material3 1.2.x experimental API).
 * Calls [onRefresh] when the user pulls; the wrapped screen shows its own loading
 * state for the actual reload, so the spinner is just a brief acknowledgement.
 * The [content] must contain a vertical scroller (LazyColumn / verticalScroll) for
 * the pull gesture to be detected.
 */
@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun PullToRefresh(
    onRefresh: () -> Unit,
    modifier: Modifier = Modifier,
    content: @Composable BoxScope.() -> Unit,
) {
    val state = rememberPullToRefreshState()

    if (state.isRefreshing) {
        LaunchedEffect(true) {
            onRefresh()
            delay(600)
            state.endRefresh()
        }
    }

    Box(modifier.fillMaxSize().nestedScroll(state.nestedScrollConnection)) {
        content()
        PullToRefreshContainer(state = state, modifier = Modifier.align(Alignment.TopCenter))
    }
}
