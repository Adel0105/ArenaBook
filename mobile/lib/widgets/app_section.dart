import 'package:arena_book_mobile/core/app_currency.dart';
import 'package:arena_book_mobile/core/app_theme.dart';
import 'package:flutter/material.dart';

enum AppSectionTone { mint, slate, neutral }

class AppSection extends StatelessWidget {
  const AppSection({
    super.key,
    required this.title,
    required this.icon,
    required this.children,
    this.subtitle,
    this.action,
    this.tone = AppSectionTone.mint,
    this.padding = const EdgeInsets.fromLTRB(16, 0, 16, 16),
  });

  final String title;
  final String? subtitle;
  final IconData icon;
  final Widget? action;
  final List<Widget> children;
  final AppSectionTone tone;
  final EdgeInsets padding;

  Color get _background {
    switch (tone) {
      case AppSectionTone.mint:
        return AppColors.mintSurface;
      case AppSectionTone.slate:
        return AppColors.slateSurface;
      case AppSectionTone.neutral:
        return Colors.white;
    }
  }

  Color get _iconBg {
    switch (tone) {
      case AppSectionTone.mint:
        return AppColors.primaryGreen.withValues(alpha: 0.12);
      case AppSectionTone.slate:
        return AppColors.slateDark.withValues(alpha: 0.08);
      case AppSectionTone.neutral:
        return AppColors.accentTeal.withValues(alpha: 0.1);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: padding,
      child: DecoratedBox(
        decoration: BoxDecoration(
          color: _background,
          borderRadius: BorderRadius.circular(16),
          border: Border.all(color: AppColors.cardBorder),
        ),
        child: Padding(
          padding: const EdgeInsets.fromLTRB(14, 14, 14, 8),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              AppSectionHeader(
                title: title,
                subtitle: subtitle,
                icon: icon,
                iconBackground: _iconBg,
                action: action,
              ),
              const SizedBox(height: 8),
              ...children,
            ],
          ),
        ),
      ),
    );
  }
}

class AppSectionHeader extends StatelessWidget {
  const AppSectionHeader({
    super.key,
    required this.title,
    required this.icon,
    this.subtitle,
    this.action,
    this.iconBackground,
  });

  final String title;
  final String? subtitle;
  final IconData icon;
  final Widget? action;
  final Color? iconBackground;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Container(
          padding: const EdgeInsets.all(8),
          decoration: BoxDecoration(
            color: iconBackground ?? AppColors.mintSurface,
            borderRadius: BorderRadius.circular(10),
          ),
          child: Icon(icon, size: 22, color: AppColors.forest),
        ),
        const SizedBox(width: 12),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                title,
                style: theme.textTheme.titleMedium?.copyWith(
                  fontWeight: FontWeight.w700,
                  color: AppColors.slateDark,
                ),
              ),
              if (subtitle != null)
                Padding(
                  padding: const EdgeInsets.only(top: 2),
                  child: Text(
                    subtitle!,
                    style: theme.textTheme.bodySmall?.copyWith(
                      color: theme.colorScheme.onSurfaceVariant,
                    ),
                  ),
                ),
            ],
          ),
        ),
        if (action != null) action!,
      ],
    );
  }
}

class AppListTile extends StatelessWidget {
  const AppListTile({
    super.key,
    required this.icon,
    required this.title,
    this.subtitle,
    this.trailing,
    this.onTap,
    this.imageUrl,
  });

  final IconData icon;
  final String title;
  final String? subtitle;
  final Widget? trailing;
  final VoidCallback? onTap;
  final String? imageUrl;

  @override
  Widget build(BuildContext context) {
    return Card(
      margin: const EdgeInsets.only(bottom: 8),
      child: ListTile(
        leading: imageUrl != null && imageUrl!.isNotEmpty
            ? ClipRRect(
                borderRadius: BorderRadius.circular(8),
                child: Image.network(
                  imageUrl!,
                  width: 48,
                  height: 48,
                  fit: BoxFit.cover,
                  errorBuilder: (_, __, ___) => CircleAvatar(
                    backgroundColor: AppColors.mintSurface,
                    child: Icon(icon, size: 20, color: AppColors.forest),
                  ),
                ),
              )
            : CircleAvatar(
                backgroundColor: AppColors.mintSurface,
                child: Icon(icon, size: 20, color: AppColors.forest),
              ),
        title: Text(title, style: const TextStyle(fontWeight: FontWeight.w600)),
        subtitle: subtitle != null ? Text(subtitle!) : null,
        trailing: trailing,
        isThreeLine: subtitle != null && subtitle!.contains('\n'),
        onTap: onTap,
      ),
    );
  }
}

class NovcicChip extends StatelessWidget {
  const NovcicChip({super.key, required this.amount, this.perHour = false});

  final double amount;
  final bool perHour;

  @override
  Widget build(BuildContext context) {
    return Chip(
      avatar:
          const Icon(Icons.toll_outlined, size: 16, color: AppColors.forest),
      label: Text(
        AppCurrency.format(amount, perHour: perHour),
        style: const TextStyle(fontSize: 12),
      ),
      backgroundColor: Colors.white,
      side: const BorderSide(color: AppColors.cardBorder),
      padding: EdgeInsets.zero,
      visualDensity: VisualDensity.compact,
    );
  }
}

