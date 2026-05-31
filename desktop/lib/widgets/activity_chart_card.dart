import 'package:arena_book_desktop/models/admin_dashboard_activity.dart';
import 'package:fl_chart/fl_chart.dart';
import 'package:flutter/material.dart';

class ActivityChartCard extends StatelessWidget {
  const ActivityChartCard({
    super.key,
    required this.title,
    required this.subtitle,
    required this.points,
    required this.color,
    this.yAxisLabel = 'Broj',
  });

  final String title;
  final String subtitle;
  final List<MonthlyPoint> points;
  final Color color;
  final String yAxisLabel;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final maxY = points.isEmpty
        ? 1.0
        : points.map((e) => e.count.toDouble()).reduce((a, b) => a > b ? a : b);
    final chartMax = maxY < 1 ? 1.0 : maxY * 1.2;

    return Card(
      elevation: 0,
      clipBehavior: Clip.antiAlias,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(12),
        side: BorderSide(color: theme.colorScheme.outlineVariant),
      ),
      child: Padding(
        padding: const EdgeInsets.fromLTRB(16, 16, 16, 12),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text(
              title,
              maxLines: 2,
              overflow: TextOverflow.ellipsis,
              style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w700),
            ),
            if (subtitle.isNotEmpty) ...[
              const SizedBox(height: 6),
              Text(
                subtitle,
                maxLines: 2,
                overflow: TextOverflow.ellipsis,
                style: theme.textTheme.bodySmall?.copyWith(
                  color: theme.colorScheme.onSurfaceVariant,
                  height: 1.35,
                ),
              ),
            ],
            const SizedBox(height: 12),
            Text(
              yAxisLabel,
              style: theme.textTheme.labelSmall?.copyWith(
                color: theme.colorScheme.onSurfaceVariant,
                fontWeight: FontWeight.w500,
              ),
            ),
            const SizedBox(height: 4),
            LayoutBuilder(
              builder: (context, constraints) {
                final barWidth = points.isEmpty
                    ? 16.0
                    : ((constraints.maxWidth - 40) / points.length * 0.55).clamp(6.0, 16.0);
                return SizedBox(
                  height: 200,
                  width: double.infinity,
                  child: points.isEmpty
                      ? Center(
                          child: Text(
                            'Nema podataka za prikaz',
                            textAlign: TextAlign.center,
                            style: theme.textTheme.bodyMedium?.copyWith(color: theme.colorScheme.onSurfaceVariant),
                          ),
                        )
                      : BarChart(
                          BarChartData(
                            maxY: chartMax,
                            gridData: const FlGridData(show: true, drawVerticalLine: false),
                            borderData: FlBorderData(show: false),
                            titlesData: FlTitlesData(
                              leftTitles: AxisTitles(
                                sideTitles: SideTitles(
                                  showTitles: true,
                                  reservedSize: 32,
                                  getTitlesWidget: (value, meta) {
                                    if (value != value.roundToDouble()) {
                                      return const SizedBox.shrink();
                                    }
                                    return Text(
                                      value.toInt().toString(),
                                      style: theme.textTheme.labelSmall,
                                    );
                                  },
                                ),
                              ),
                              rightTitles: const AxisTitles(sideTitles: SideTitles(showTitles: false)),
                              topTitles: const AxisTitles(sideTitles: SideTitles(showTitles: false)),
                              bottomTitles: AxisTitles(
                                sideTitles: SideTitles(
                                  showTitles: true,
                                  reservedSize: 26,
                                  getTitlesWidget: (value, meta) {
                                    final i = value.toInt();
                                    if (i < 0 || i >= points.length) {
                                      return const SizedBox.shrink();
                                    }
                                    final label = points[i].label;
                                    final short = label.length > 4 ? label.substring(0, 3) : label;
                                    return Padding(
                                      padding: const EdgeInsets.only(top: 4),
                                      child: Text(
                                        short,
                                        style: theme.textTheme.labelSmall,
                                        maxLines: 1,
                                        overflow: TextOverflow.ellipsis,
                                        textAlign: TextAlign.center,
                                      ),
                                    );
                                  },
                                ),
                              ),
                            ),
                            barGroups: [
                              for (var i = 0; i < points.length; i++)
                                BarChartGroupData(
                                  x: i,
                                  barRods: [
                                    BarChartRodData(
                                      toY: points[i].count.toDouble(),
                                      color: color,
                                      width: barWidth,
                                      borderRadius: const BorderRadius.vertical(top: Radius.circular(4)),
                                    ),
                                  ],
                                ),
                            ],
                          ),
                        ),
                );
              },
            ),
            const SizedBox(height: 8),
            Align(
              alignment: Alignment.center,
              child: Text(
                'Mjesec',
                style: theme.textTheme.labelSmall?.copyWith(color: theme.colorScheme.onSurfaceVariant),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

