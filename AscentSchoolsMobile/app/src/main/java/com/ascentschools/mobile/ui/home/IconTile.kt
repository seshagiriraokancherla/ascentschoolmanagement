package com.ascentschools.mobile.ui.home

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.layout.Column
import androidx.compose.material3.Icon
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp

/**
 * A launcher-style tile: a rounded, gradient-filled square with a white glyph, and the
 * feature label in dark text below it. Used by the tile dashboard (parent + teacher).
 */
@Composable
fun IconTile(
    label: String,
    icon: ImageVector,
    gradient: Pair<Color, Color>,
    badge: String? = null,
    badgeAlert: Boolean = false,
    enabled: Boolean = true,
    modifier: Modifier = Modifier,
    onClick: () -> Unit
) {
    val alpha = if (enabled) 1f else 0.4f

    Column(
        modifier            = modifier
            .clickable(enabled = enabled) { onClick() }
            .padding(vertical = 4.dp),
        horizontalAlignment = Alignment.CenterHorizontally
    ) {
        Box(contentAlignment = Alignment.TopEnd) {
            Box(
                modifier = Modifier
                    .size(66.dp)
                    .clip(RoundedCornerShape(18.dp))
                    .background(
                        Brush.linearGradient(
                            listOf(gradient.first.copy(alpha = alpha), gradient.second.copy(alpha = alpha))
                        )
                    ),
                contentAlignment = Alignment.Center
            ) {
                Icon(icon, contentDescription = null, tint = Color.White, modifier = Modifier.size(32.dp))
            }

            if (!badge.isNullOrBlank()) {
                Surface(
                    color    = if (badgeAlert) Color(0xFFDC2626) else Color(0xFFF59E0B),
                    shape    = CircleShape,
                    modifier = Modifier.padding(top = 2.dp)
                ) {
                    Text(
                        badge,
                        color      = Color.White,
                        fontSize   = 10.sp,
                        fontWeight = FontWeight.Bold,
                        maxLines   = 1,
                        modifier   = Modifier.padding(horizontal = 6.dp, vertical = 2.dp)
                    )
                }
            }
        }

        Spacer(Modifier.height(6.dp))
        Text(
            label,
            fontSize   = 12.5.sp,
            fontWeight = FontWeight.Medium,
            color      = MaterialTheme.colorScheme.onSurface,
            textAlign  = TextAlign.Center,
            maxLines   = 2,
            overflow   = TextOverflow.Ellipsis
        )
    }
}

/** Parse a "#RRGGBB" / "RRGGBB" (or "#AARRGGBB") hex string to a Compose Color, else [fallback]. */
fun parseBrandColor(hex: String?, fallback: Color): Color {
    if (hex.isNullOrBlank()) return fallback
    return try {
        val s = if (hex.startsWith("#")) hex else "#$hex"
        Color(android.graphics.Color.parseColor(s))
    } catch (e: Exception) {
        fallback
    }
}
