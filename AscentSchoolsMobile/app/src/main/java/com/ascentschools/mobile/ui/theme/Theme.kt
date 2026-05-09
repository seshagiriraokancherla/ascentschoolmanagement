package com.ascentschools.mobile.ui.theme

import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Typography
import androidx.compose.material3.lightColorScheme
import androidx.compose.runtime.Composable
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.TextStyle
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.sp

// ── Color palette — deep navy + warm gold ─────────────────────────────────────

val NavyBlue        = Color(0xFF1E3A8A)
val NavyBlueLight   = Color(0xFF2563EB)
val NavyContainer   = Color(0xFFDBEAFE)
val OnNavyContainer = Color(0xFF1E3A8A)

val Gold            = Color(0xFFB45309)
val GoldContainer   = Color(0xFFFEF3C7)
val OnGoldContainer = Color(0xFF78350F)

val Teal            = Color(0xFF0891B2)

val AppBackground   = Color(0xFFF1F5F9)
val AppSurface      = Color(0xFFFFFFFF)
val SurfaceVariant  = Color(0xFFE2E8F0)

val TextPrimary     = Color(0xFF0F172A)
val TextSecondary   = Color(0xFF64748B)

private val LightColors = lightColorScheme(
    primary              = NavyBlue,
    onPrimary            = Color.White,
    primaryContainer     = NavyContainer,
    onPrimaryContainer   = OnNavyContainer,
    secondary            = Gold,
    onSecondary          = Color.White,
    secondaryContainer   = GoldContainer,
    onSecondaryContainer = OnGoldContainer,
    tertiary             = Teal,
    onTertiary           = Color.White,
    background           = AppBackground,
    onBackground         = TextPrimary,
    surface              = AppSurface,
    onSurface            = TextPrimary,
    surfaceVariant       = SurfaceVariant,
    onSurfaceVariant     = TextSecondary,
    outline              = Color(0xFFCBD5E1),
)

// ── Typography ────────────────────────────────────────────────────────────────
// Using default system font for now.
// To add Poppins: download Regular/Medium/SemiBold/Bold TTF files from
// fonts.google.com, place them in res/font/, then replace FontFamily.Default below.

private val AppTypography = Typography(
    displayLarge  = TextStyle(fontFamily = FontFamily.Default, fontWeight = FontWeight.Bold,     fontSize = 57.sp, lineHeight = 64.sp),
    displayMedium = TextStyle(fontFamily = FontFamily.Default, fontWeight = FontWeight.Bold,     fontSize = 45.sp, lineHeight = 52.sp),
    displaySmall  = TextStyle(fontFamily = FontFamily.Default, fontWeight = FontWeight.Bold,     fontSize = 36.sp, lineHeight = 44.sp),
    headlineLarge = TextStyle(fontFamily = FontFamily.Default, fontWeight = FontWeight.SemiBold, fontSize = 32.sp, lineHeight = 40.sp),
    headlineMedium= TextStyle(fontFamily = FontFamily.Default, fontWeight = FontWeight.SemiBold, fontSize = 28.sp, lineHeight = 36.sp),
    headlineSmall = TextStyle(fontFamily = FontFamily.Default, fontWeight = FontWeight.SemiBold, fontSize = 24.sp, lineHeight = 32.sp),
    titleLarge    = TextStyle(fontFamily = FontFamily.Default, fontWeight = FontWeight.SemiBold, fontSize = 22.sp, lineHeight = 28.sp),
    titleMedium   = TextStyle(fontFamily = FontFamily.Default, fontWeight = FontWeight.Medium,   fontSize = 16.sp, lineHeight = 24.sp),
    titleSmall    = TextStyle(fontFamily = FontFamily.Default, fontWeight = FontWeight.Medium,   fontSize = 14.sp, lineHeight = 20.sp),
    bodyLarge     = TextStyle(fontFamily = FontFamily.Default, fontWeight = FontWeight.Normal,   fontSize = 16.sp, lineHeight = 24.sp),
    bodyMedium    = TextStyle(fontFamily = FontFamily.Default, fontWeight = FontWeight.Normal,   fontSize = 14.sp, lineHeight = 20.sp),
    bodySmall     = TextStyle(fontFamily = FontFamily.Default, fontWeight = FontWeight.Normal,   fontSize = 12.sp, lineHeight = 16.sp),
    labelLarge    = TextStyle(fontFamily = FontFamily.Default, fontWeight = FontWeight.Medium,   fontSize = 14.sp, lineHeight = 20.sp),
    labelMedium   = TextStyle(fontFamily = FontFamily.Default, fontWeight = FontWeight.Medium,   fontSize = 12.sp, lineHeight = 16.sp),
    labelSmall    = TextStyle(fontFamily = FontFamily.Default, fontWeight = FontWeight.Medium,   fontSize = 11.sp, lineHeight = 16.sp),
)

// ── Theme entry point ─────────────────────────────────────────────────────────

@Composable
fun AscentTheme(content: @Composable () -> Unit) {
    MaterialTheme(
        colorScheme = LightColors,
        typography  = AppTypography,
        content     = content
    )
}
