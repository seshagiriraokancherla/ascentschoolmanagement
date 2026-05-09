package com.ascentschools.mobile.ui.theme

import androidx.compose.animation.core.*
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp

@Composable
fun shimmerBrush(): Brush {
    val transition = rememberInfiniteTransition(label = "shimmer")
    val x by transition.animateFloat(
        initialValue  = 0f,
        targetValue   = 1200f,
        animationSpec = infiniteRepeatable(
            animation  = tween(900, easing = LinearEasing),
            repeatMode = RepeatMode.Restart
        ),
        label = "shimmerX"
    )
    return Brush.linearGradient(
        colors = listOf(Color(0xFFE8ECF0), Color(0xFFF8FAFC), Color(0xFFE8ECF0)),
        start  = Offset(x - 400f, 0f),
        end    = Offset(x, 0f)
    )
}

@Composable
fun ShimmerBox(modifier: Modifier, radius: Dp = 8.dp) {
    Box(modifier.clip(RoundedCornerShape(radius)).background(shimmerBrush()))
}

// ── Attendance shimmer skeleton ───────────────────────────────────────────────

@Composable
fun AttendanceShimmer() {
    Column(Modifier.fillMaxSize().padding(16.dp)) {
        ShimmerBox(Modifier.fillMaxWidth().height(130.dp), radius = 16.dp)
        Spacer(Modifier.height(16.dp))
        ShimmerBox(Modifier.fillMaxWidth().height(24.dp).width(120.dp), radius = 6.dp)
        Spacer(Modifier.height(12.dp))
        // Calendar grid shimmer rows
        repeat(5) {
            Spacer(Modifier.height(8.dp))
            Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.spacedBy(6.dp)) {
                repeat(7) {
                    ShimmerBox(Modifier.weight(1f).height(34.dp), radius = 17.dp)
                }
            }
        }
    }
}

// ── Marks shimmer skeleton ────────────────────────────────────────────────────

@Composable
fun MarksShimmer() {
    Column(Modifier.fillMaxSize().padding(16.dp), verticalArrangement = Arrangement.spacedBy(12.dp)) {
        repeat(2) {
            ShimmerBox(Modifier.fillMaxWidth().height(200.dp), radius = 16.dp)
        }
    }
}

// ── Generic list shimmer ──────────────────────────────────────────────────────

@Composable
fun ListShimmer(itemCount: Int = 4, itemHeight: Dp = 72.dp) {
    Column(Modifier.fillMaxSize().padding(16.dp), verticalArrangement = Arrangement.spacedBy(10.dp)) {
        repeat(itemCount) {
            ShimmerBox(Modifier.fillMaxWidth().height(itemHeight), radius = 12.dp)
        }
    }
}
