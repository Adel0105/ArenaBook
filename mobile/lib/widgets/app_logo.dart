import 'package:arena_book_mobile/core/app_theme.dart';
import 'package:flutter/material.dart';

class AppLogo extends StatelessWidget {
  const AppLogo({
    super.key,
    this.size = 72,
    this.showTitle = true,
    this.subtitle,
    this.iconColor,
    this.lightOnDark = false,
  });

  final double size;
  final bool showTitle;
  final String? subtitle;
  final Color? iconColor;
  final bool lightOnDark;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final titleColor = lightOnDark ? Colors.white : AppColors.slateDark;
    final subColor =
        lightOnDark ? Colors.white70 : theme.colorScheme.onSurfaceVariant;
    final iconFg =
        iconColor ?? (lightOnDark ? Colors.white : AppColors.primaryGreen);

    return Column(
      mainAxisSize: MainAxisSize.min,
      children: [
        Container(
          width: size + 24,
          height: size + 24,
          decoration: BoxDecoration(
            color: lightOnDark
                ? Colors.white.withValues(alpha: 0.12)
                : AppColors.mintSurface,
            shape: BoxShape.circle,
            border: Border.all(
              color: lightOnDark
                  ? Colors.white24
                  : AppColors.primaryGreen.withValues(alpha: 0.25),
              width: 2,
            ),
          ),
          child: Icon(Icons.stadium_rounded, size: size, color: iconFg),
        ),
        if (showTitle) ...[
          const SizedBox(height: 16),
          Text(
            'ArenaBook',
            textAlign: TextAlign.center,
            style: theme.textTheme.headlineSmall?.copyWith(
              fontWeight: FontWeight.w700,
              color: titleColor,
            ),
          ),
        ],
        if (subtitle != null) ...[
          const SizedBox(height: 4),
          Text(
            subtitle!,
            textAlign: TextAlign.center,
            style: theme.textTheme.titleSmall?.copyWith(color: subColor),
          ),
        ],
      ],
    );
  }
}

