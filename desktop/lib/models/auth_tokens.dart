class AuthTokens {
  const AuthTokens({
    required this.accessToken,
    required this.tokenType,
    required this.expiresInSeconds,
    required this.expiresAtUtc,
  });

  final String accessToken;
  final String tokenType;
  final int expiresInSeconds;
  final DateTime expiresAtUtc;

  factory AuthTokens.fromJson(Map<String, dynamic> json) {
    final expiresRaw = json['expiresAtUtc'];
    DateTime expiresAtUtc;
    if (expiresRaw is String) {
      expiresAtUtc = DateTime.tryParse(expiresRaw)?.toUtc() ?? DateTime.now().toUtc();
    } else {
      expiresAtUtc = DateTime.now().toUtc();
    }
    return AuthTokens(
      accessToken: json['accessToken'] as String? ?? '',
      tokenType: json['tokenType'] as String? ?? 'Bearer',
      expiresInSeconds: (json['expiresInSeconds'] as num?)?.toInt() ?? 0,
      expiresAtUtc: expiresAtUtc,
    );
  }
}

