class AdminDashboardSummary {
  const AdminDashboardSummary({
    required this.totalUsers,
    required this.activeSessionsCount,
    required this.totalHalls,
    required this.externalPaymentsCount,
  });

  final int totalUsers;
  final int activeSessionsCount;
  final int totalHalls;
  final int externalPaymentsCount;

  factory AdminDashboardSummary.fromJson(Map<String, dynamic> json) {
    return AdminDashboardSummary(
      totalUsers: (json['totalUsers'] as num?)?.toInt() ?? 0,
      activeSessionsCount: (json['activeSessionsCount'] as num?)?.toInt() ?? 0,
      totalHalls: (json['totalHalls'] as num?)?.toInt() ?? 0,
      externalPaymentsCount: (json['externalPaymentsCount'] as num?)?.toInt() ?? 0,
    );
  }
}

