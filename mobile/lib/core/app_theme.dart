import 'package:flutter/material.dart';

abstract final class AppColors {
  static const primaryGreen = Color(0xFF16A34A);
  static const slateDark = Color(0xFF0F172A);
  static const forest = Color(0xFF14532D);
  static const mintSurface = Color(0xFFF0FDF4);
  static const slateSurface = Color(0xFFF1F5F9);
  static const accentTeal = Color(0xFF0D9488);
  static const cardBorder = Color(0xFFE2E8F0);
}

ThemeData buildAppTheme() {
  final base = ColorScheme.fromSeed(
    seedColor: AppColors.primaryGreen,
    brightness: Brightness.light,
  );
  return ThemeData(
    useMaterial3: true,
    colorScheme: base.copyWith(
      primary: AppColors.primaryGreen,
      secondary: AppColors.accentTeal,
      surface: Colors.white,
    ),
    scaffoldBackgroundColor: const Color(0xFFFAFAFA),
    appBarTheme: AppBarTheme(
      elevation: 0,
      scrolledUnderElevation: 1,
      backgroundColor: Colors.white,
      foregroundColor: AppColors.slateDark,
      titleTextStyle: const TextStyle(
        color: AppColors.slateDark,
        fontSize: 18,
        fontWeight: FontWeight.w600,
      ),
    ),
    navigationBarTheme: NavigationBarThemeData(
      indicatorColor: AppColors.mintSurface,
      labelTextStyle: WidgetStateProperty.resolveWith((states) {
        if (states.contains(WidgetState.selected)) {
          return const TextStyle(
              fontSize: 12,
              fontWeight: FontWeight.w600,
              color: AppColors.forest);
        }
        return const TextStyle(fontSize: 12, color: Color(0xFF64748B));
      }),
    ),
    inputDecorationTheme: InputDecorationTheme(
      filled: true,
      fillColor: Colors.white,
      border: OutlineInputBorder(borderRadius: BorderRadius.circular(12)),
      enabledBorder: OutlineInputBorder(
        borderRadius: BorderRadius.circular(12),
        borderSide: const BorderSide(color: AppColors.cardBorder),
      ),
    ),
    cardTheme: CardThemeData(
      elevation: 0,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(12),
        side: const BorderSide(color: AppColors.cardBorder),
      ),
      color: Colors.white,
    ),
    filledButtonTheme: FilledButtonThemeData(
      style: FilledButton.styleFrom(
        backgroundColor: AppColors.primaryGreen,
        foregroundColor: Colors.white,
        padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 14),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
      ),
    ),
  );
}

