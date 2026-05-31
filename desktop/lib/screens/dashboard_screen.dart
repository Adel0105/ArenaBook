import 'package:arena_book_desktop/models/admin_dashboard_activity.dart';
import 'package:arena_book_desktop/models/admin_dashboard_summary.dart';
import 'package:arena_book_desktop/services/api_error.dart';
import 'package:arena_book_desktop/services/arena_book_api.dart';
import 'package:arena_book_desktop/widgets/activity_chart_card.dart';
import 'package:flutter/material.dart';

class DashboardScreen extends StatefulWidget {
  const DashboardScreen({super.key, required this.api});

  final ArenaBookApi api;

  @override
  State<DashboardScreen> createState() => _DashboardScreenState();
}

class _DashboardScreenState extends State<DashboardScreen> {
  AdminDashboardSummary? _summary;
  AdminDashboardActivity? _activity;
  bool _loading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final summary = await widget.api.adminDashboardSummary();
      final activity = await widget.api.adminDashboardActivity(months: 6);
      if (!mounted) {
        return;
      }
      setState(() {
        _summary = summary;
        _activity = activity;
        _loading = false;
      });
    } on ApiError catch (e) {
      if (!mounted) {
        return;
      }
      setState(() {
        _error = e.message;
        _loading = false;
      });
    } catch (_) {
      if (!mounted) {
        return;
      }
      setState(() {
        _error = 'Nije moguće učitati podatke.';
        _loading = false;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return LayoutBuilder(
      builder: (context, constraints) {
        return SingleChildScrollView(
          padding: const EdgeInsets.all(24),
          child: ConstrainedBox(
            constraints: BoxConstraints(minWidth: constraints.maxWidth),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Expanded(
                      child: Text(
                        'Pregled ključnih pokazatelja platforme',
                        style: theme.textTheme.bodyLarge?.copyWith(color: theme.colorScheme.onSurfaceVariant),
                      ),
                    ),
                    IconButton.filledTonal(
                      onPressed: _loading ? null : _load,
                      tooltip: 'Osvježi',
                      icon: _loading
                          ? const SizedBox(width: 20, height: 20, child: CircularProgressIndicator(strokeWidth: 2))
                          : const Icon(Icons.refresh),
                    ),
                  ],
                ),
                const SizedBox(height: 24),
                if (_error != null) _ErrorBanner(message: _error!, onRetry: _load),
                if (_loading && _summary == null)
                  const Center(child: Padding(padding: EdgeInsets.all(48), child: CircularProgressIndicator()))
                else if (_summary != null) ...[
                  _KpiGrid(summary: _summary!, maxWidth: constraints.maxWidth),
                  const SizedBox(height: 32),
                  Text(
                    'Aktivnost po mjesecima',
                    style: theme.textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w600),
                  ),
                  const SizedBox(height: 8),
                  Text(
                    'Kretanje broja korisnika, rezervacija i transakcija u zadnjih 6 mjeseci',
                    style: theme.textTheme.bodyMedium?.copyWith(color: theme.colorScheme.onSurfaceVariant),
                  ),
                  const SizedBox(height: 20),
                  if (_activity != null)
                    LayoutBuilder(
                      builder: (context, c) => _ChartsLayout(
                        width: c.maxWidth,
                        charts: [
                          ActivityChartCard(
                            title: 'Korisnici',
                            subtitle: '',
                            points: _activity!.usersByMonth,
                            color: const Color(0xFF2563EB),
                          ),
                          ActivityChartCard(
                            title: 'Rezervacije',
                            subtitle: '',
                            points: _activity!.sessionsByMonth,
                            color: const Color(0xFF16A34A),
                          ),
                          ActivityChartCard(
                            title: 'Transakcije',
                            subtitle: '',
                            points: _activity!.paymentsByMonth,
                            color: const Color(0xFF9333EA),
                          ),
                        ],
                      ),
                    ),
                ],
              ],
            ),
          ),
        );
      },
    );
  }
}

class _ChartsLayout extends StatelessWidget {
  const _ChartsLayout({required this.width, required this.charts});

  final double width;
  final List<Widget> charts;

  static const _gap = 16.0;
  static const _minCardWidth = 300.0;

  @override
  Widget build(BuildContext context) {
    if (width >= _minCardWidth * 3 + _gap * 2) {
      return Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          for (var i = 0; i < charts.length; i++) ...[
            if (i > 0) const SizedBox(width: _gap),
            Expanded(child: charts[i]),
          ],
        ],
      );
    }

    if (width >= _minCardWidth * 2 + _gap) {
      return Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Expanded(child: charts[0]),
              const SizedBox(width: _gap),
              Expanded(child: charts[1]),
            ],
          ),
          const SizedBox(height: _gap),
          charts[2],
        ],
      );
    }

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        for (var i = 0; i < charts.length; i++) ...[
          if (i > 0) const SizedBox(height: _gap),
          charts[i],
        ],
      ],
    );
  }
}

class _ErrorBanner extends StatelessWidget {
  const _ErrorBanner({required this.message, required this.onRetry});

  final String message;
  final VoidCallback onRetry;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Padding(
      padding: const EdgeInsets.only(bottom: 16),
      child: Material(
        color: theme.colorScheme.errorContainer,
        borderRadius: BorderRadius.circular(8),
        child: Padding(
          padding: const EdgeInsets.all(16),
        child: LayoutBuilder(
          builder: (context, c) {
            if (c.maxWidth < 420) {
              return Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Icon(Icons.warning_amber_rounded, color: theme.colorScheme.onErrorContainer),
                      const SizedBox(width: 12),
                      Expanded(
                        child: Text(
                          message,
                          style: TextStyle(color: theme.colorScheme.onErrorContainer),
                        ),
                      ),
                    ],
                  ),
                  Align(
                    alignment: Alignment.centerRight,
                    child: TextButton(onPressed: onRetry, child: const Text('Pokušaj ponovo')),
                  ),
                ],
              );
            }
            return Row(
              children: [
                Icon(Icons.warning_amber_rounded, color: theme.colorScheme.onErrorContainer),
                const SizedBox(width: 12),
                Expanded(
                  child: Text(
                    message,
                    style: TextStyle(color: theme.colorScheme.onErrorContainer),
                  ),
                ),
                TextButton(onPressed: onRetry, child: const Text('Pokušaj ponovo')),
              ],
            );
          },
        ),
        ),
      ),
    );
  }
}

class _KpiGrid extends StatelessWidget {
  const _KpiGrid({required this.summary, required this.maxWidth});

  final AdminDashboardSummary summary;
  final double maxWidth;

  @override
  Widget build(BuildContext context) {
    final cross = maxWidth >= 920 ? 4 : (maxWidth >= 480 ? 2 : 1);
    final items = [
      _KpiData('Korisnici', summary.totalUsers.toString(), Icons.people_outline, const Color(0xFF2563EB)),
      _KpiData('Aktivne rezervacije', summary.activeSessionsCount.toString(), Icons.event_available_outlined, const Color(0xFF16A34A)),
      _KpiData('Dvorane', summary.totalHalls.toString(), Icons.apartment_outlined, const Color(0xFFCA8A04)),
      _KpiData('Transakcije', summary.externalPaymentsCount.toString(), Icons.payments_outlined, const Color(0xFF9333EA)),
    ];
    return GridView.count(
      crossAxisCount: cross,
      shrinkWrap: true,
      physics: const NeverScrollableScrollPhysics(),
      mainAxisSpacing: 16,
      crossAxisSpacing: 16,
      childAspectRatio: cross == 1 ? 3.2 : (cross == 4 ? 2.1 : 2.4),
      children: items.map((e) => _KpiCard(data: e)).toList(),
    );
  }
}

class _KpiData {
  const _KpiData(this.label, this.value, this.icon, this.accent);

  final String label;
  final String value;
  final IconData icon;
  final Color accent;
}

class _KpiCard extends StatelessWidget {
  const _KpiCard({required this.data});

  final _KpiData data;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Card(
      elevation: 0,
      clipBehavior: Clip.antiAlias,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(12),
        side: BorderSide(color: theme.colorScheme.outlineVariant),
      ),
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 14),
        child: Row(
          children: [
            Container(
              padding: const EdgeInsets.all(10),
              decoration: BoxDecoration(
                color: data.accent.withValues(alpha: 0.12),
                borderRadius: BorderRadius.circular(12),
              ),
              child: Icon(data.icon, color: data.accent, size: 26),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                crossAxisAlignment: CrossAxisAlignment.start,
                mainAxisSize: MainAxisSize.min,
                children: [
                  Text(
                    data.value,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: theme.textTheme.headlineSmall?.copyWith(fontWeight: FontWeight.w700),
                  ),
                  const SizedBox(height: 2),
                  Text(
                    data.label,
                    maxLines: 2,
                    overflow: TextOverflow.ellipsis,
                    style: theme.textTheme.bodySmall?.copyWith(
                      color: theme.colorScheme.onSurfaceVariant,
                      fontWeight: FontWeight.w500,
                      height: 1.25,
                    ),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}

