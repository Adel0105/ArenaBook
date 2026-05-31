class CoinWallet {
  CoinWallet({required this.balanceCoins, this.updatedUtc});

  final double balanceCoins;
  final DateTime? updatedUtc;

  factory CoinWallet.fromJson(Map<String, dynamic> json) {
    final updatedRaw = json['updatedUtc'];
    return CoinWallet(
      balanceCoins: (json['balanceCoins'] as num?)?.toDouble() ?? 0,
      updatedUtc: updatedRaw == null
          ? null
          : DateTime.parse(updatedRaw as String),
    );
  }
}

class CoinPurchaseResult {
  CoinPurchaseResult({
    required this.balanceCoins,
    required this.coinsPurchased,
  });

  final double balanceCoins;
  final double coinsPurchased;

  factory CoinPurchaseResult.fromJson(Map<String, dynamic> json) {
    return CoinPurchaseResult(
      balanceCoins: (json['balanceCoins'] as num?)?.toDouble() ?? 0,
      coinsPurchased: (json['coinsPurchased'] as num?)?.toDouble() ?? 0,
    );
  }
}

class CoinLedgerEntry {
  CoinLedgerEntry({
    required this.amountCoins,
    required this.balanceAfter,
    required this.reasonCode,
    required this.createdUtc,
  });

  final double amountCoins;
  final double balanceAfter;
  final String reasonCode;
  final DateTime createdUtc;

  factory CoinLedgerEntry.fromJson(Map<String, dynamic> json) {
    final createdRaw = json['createdUtc'];
    return CoinLedgerEntry(
      amountCoins: (json['amountCoins'] as num?)?.toDouble() ?? 0,
      balanceAfter: (json['balanceAfter'] as num?)?.toDouble() ?? 0,
      reasonCode: json['reasonCode'] as String? ?? '',
      createdUtc: createdRaw == null
          ? DateTime.now().toUtc()
          : DateTime.parse(createdRaw as String),
    );
  }
}

class StripeIntentResult {
  StripeIntentResult({
    required this.clientSecret,
    required this.paymentIntentId,
    required this.amountMoney,
    required this.coinsToPurchase,
  });

  final String clientSecret;
  final String paymentIntentId;
  final double amountMoney;
  final double coinsToPurchase;

  factory StripeIntentResult.fromJson(Map<String, dynamic> json) {
    return StripeIntentResult(
      clientSecret: json['clientSecret'] as String? ?? '',
      paymentIntentId: json['paymentIntentId'] as String? ?? '',
      amountMoney: (json['amountMoney'] as num?)?.toDouble() ?? 0,
      coinsToPurchase: (json['coinsToPurchase'] as num?)?.toDouble() ?? 0,
    );
  }
}

class PayPalOrderResult {
  PayPalOrderResult({
    required this.payPalOrderId,
    required this.approvalUrl,
    required this.coinsToPurchase,
    required this.amountMoney,
  });

  final String payPalOrderId;
  final String approvalUrl;
  final double coinsToPurchase;
  final double amountMoney;

  factory PayPalOrderResult.fromJson(Map<String, dynamic> json) {
    return PayPalOrderResult(
      payPalOrderId: json['payPalOrderId'] as String? ?? '',
      approvalUrl: json['approvalUrl'] as String? ?? '',
      coinsToPurchase: (json['coinsToPurchase'] as num?)?.toDouble() ?? 0,
      amountMoney: (json['amountMoney'] as num?)?.toDouble() ?? 0,
    );
  }
}

