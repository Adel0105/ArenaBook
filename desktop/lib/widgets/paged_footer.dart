import 'package:flutter/material.dart';

class PagedFooter extends StatelessWidget {
  const PagedFooter({
    super.key,
    required this.page,
    required this.totalPages,
    required this.totalCount,
    required this.onPageChanged,
    this.isLoading = false,
  });

  final int page;
  final int totalPages;
  final int totalCount;
  final ValueChanged<int> onPageChanged;
  final bool isLoading;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 12, horizontal: 8),
      child: Row(
        children: [
          Text(
            'Ukupno: $totalCount',
            style: theme.textTheme.bodySmall?.copyWith(color: theme.colorScheme.onSurfaceVariant),
          ),
          const Spacer(),
          IconButton(
            onPressed: page > 1 && !isLoading ? () => onPageChanged(page - 1) : null,
            icon: const Icon(Icons.chevron_left),
          ),
          Text('Stranica $page / ${totalPages < 1 ? 1 : totalPages}'),
          IconButton(
            onPressed: page < totalPages && !isLoading ? () => onPageChanged(page + 1) : null,
            icon: const Icon(Icons.chevron_right),
          ),
        ],
      ),
    );
  }
}

