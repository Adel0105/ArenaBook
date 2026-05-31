class AdminDashboardActivity {
  const AdminDashboardActivity({
    required this.usersByMonth,
    required this.sessionsByMonth,
    required this.paymentsByMonth,
  });

  final List<MonthlyPoint> usersByMonth;
  final List<MonthlyPoint> sessionsByMonth;
  final List<MonthlyPoint> paymentsByMonth;

  factory AdminDashboardActivity.fromJson(Map<String, dynamic> json) {
    return AdminDashboardActivity(
      usersByMonth: _parseList(json['usersByMonth']),
      sessionsByMonth: _parseList(json['sessionsByMonth']),
      paymentsByMonth: _parseList(json['paymentsByMonth']),
    );
  }

  static List<MonthlyPoint> _parseList(dynamic raw) {
    if (raw is! List) {
      return const [];
    }
    return raw.map((e) => MonthlyPoint.fromJson(e as Map<String, dynamic>)).toList();
  }
}

class MonthlyPoint {
  const MonthlyPoint({
    required this.year,
    required this.month,
    required this.label,
    required this.count,
  });

  final int year;
  final int month;
  final String label;
  final int count;

  factory MonthlyPoint.fromJson(Map<String, dynamic> json) {
    return MonthlyPoint(
      year: (json['year'] as num?)?.toInt() ?? 0,
      month: (json['month'] as num?)?.toInt() ?? 0,
      label: json['label'] as String? ?? '',
      count: (json['count'] as num?)?.toInt() ?? 0,
    );
  }
}

