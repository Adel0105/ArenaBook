class ExternalPaymentListItem {
  ExternalPaymentListItem({
    required this.id,
    required this.userId,
    this.userEmail,
    required this.purposeCode,
    required this.provider,
    required this.amountMoney,
    required this.currency,
    required this.paymentProcessingStatusId,
    this.paymentStatusCode,
    this.externalReference,
    required this.coinsPurchased,
    required this.createdUtc,
  });

  final int id;
  final String userId;
  final String? userEmail;
  final String purposeCode;
  final String provider;
  final double amountMoney;
  final String currency;
  final int paymentProcessingStatusId;
  final String? paymentStatusCode;
  final String? externalReference;
  final double coinsPurchased;
  final DateTime createdUtc;

  bool get isDemoSeed =>
      externalReference != null && externalReference!.startsWith('SEED-');

  factory ExternalPaymentListItem.fromJson(Map<String, dynamic> json) {
    return ExternalPaymentListItem(
      id: json['id'] as int,
      userId: json['userId'] as String? ?? '',
      userEmail: json['userEmail'] as String?,
      purposeCode: json['purposeCode'] as String? ?? '',
      provider: json['provider'] as String? ?? '',
      amountMoney: (json['amountMoney'] as num?)?.toDouble() ?? 0,
      currency: json['currency'] as String? ?? '',
      paymentProcessingStatusId: json['paymentProcessingStatusId'] as int? ?? 0,
      paymentStatusCode: json['paymentStatusCode'] as String?,
      externalReference: json['externalReference'] as String?,
      coinsPurchased: (json['coinsPurchased'] as num?)?.toDouble() ?? 0,
      createdUtc: DateTime.parse(json['createdUtc'] as String).toLocal(),
    );
  }
}

class CoinLedgerListItem {
  CoinLedgerListItem({
    required this.id,
    required this.userId,
    this.userEmail,
    required this.amountCoins,
    required this.balanceAfter,
    required this.reasonCode,
    this.relatedScheduledSessionId,
    this.relatedHallName,
    this.relatedSessionStartUtc,
    required this.createdUtc,
  });

  final int id;
  final String userId;
  final String? userEmail;
  final double amountCoins;
  final double balanceAfter;
  final String reasonCode;
  final int? relatedScheduledSessionId;
  final String? relatedHallName;
  final DateTime? relatedSessionStartUtc;
  final DateTime createdUtc;

  bool get isDemoSeed => reasonCode == 'SEED_INITIAL';

  factory CoinLedgerListItem.fromJson(Map<String, dynamic> json) {
    final sessionStart = json['relatedSessionStartUtc'];
    return CoinLedgerListItem(
      id: json['id'] as int,
      userId: json['userId'] as String? ?? '',
      userEmail: json['userEmail'] as String?,
      amountCoins: (json['amountCoins'] as num?)?.toDouble() ?? 0,
      balanceAfter: (json['balanceAfter'] as num?)?.toDouble() ?? 0,
      reasonCode: json['reasonCode'] as String? ?? '',
      relatedScheduledSessionId: json['relatedScheduledSessionId'] as int?,
      relatedHallName: json['relatedHallName'] as String?,
      relatedSessionStartUtc: sessionStart != null
          ? DateTime.parse(sessionStart as String).toLocal()
          : null,
      createdUtc: DateTime.parse(json['createdUtc'] as String).toLocal(),
    );
  }
}

class CoinWalletListItem {
  CoinWalletListItem({
    required this.userId,
    this.userEmail,
    required this.balanceCoins,
    required this.updatedUtc,
  });

  final String userId;
  final String? userEmail;
  final double balanceCoins;
  final DateTime updatedUtc;

  factory CoinWalletListItem.fromJson(Map<String, dynamic> json) {
    return CoinWalletListItem(
      userId: json['userId'] as String? ?? '',
      userEmail: json['userEmail'] as String?,
      balanceCoins: (json['balanceCoins'] as num?)?.toDouble() ?? 0,
      updatedUtc: DateTime.parse(json['updatedUtc'] as String).toLocal(),
    );
  }
}

class HallEarningsListItem {
  HallEarningsListItem({
    required this.hallId,
    required this.hallName,
    required this.cityName,
    required this.sessionCount,
    required this.totalCoinsEarned,
  });

  final int hallId;
  final String hallName;
  final String cityName;
  final int sessionCount;
  final double totalCoinsEarned;

  factory HallEarningsListItem.fromJson(Map<String, dynamic> json) {
    return HallEarningsListItem(
      hallId: json['hallId'] as int,
      hallName: json['hallName'] as String? ?? '',
      cityName: json['cityName'] as String? ?? '',
      sessionCount: json['sessionCount'] as int? ?? 0,
      totalCoinsEarned: (json['totalCoinsEarned'] as num?)?.toDouble() ?? 0,
    );
  }
}

