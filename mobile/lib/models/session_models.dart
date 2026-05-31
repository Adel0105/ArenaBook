class SessionListItem {
  SessionListItem({
    required this.id,
    required this.hallId,
    required this.hallName,
    required this.sessionKindCode,
    required this.sessionLifecycleCode,
    required this.startUtc,
    required this.endUtc,
    required this.maxParticipants,
    required this.participantCount,
    this.maxAgeYears,
    required this.priceTotalCoins,
    this.organizerEmail,
    this.inviteCode,
  });

  final int id;
  final int hallId;
  final String hallName;
  final String sessionKindCode;
  final String sessionLifecycleCode;
  final DateTime startUtc;
  final DateTime endUtc;
  final int maxParticipants;
  final int participantCount;
  final int? maxAgeYears;
  final double priceTotalCoins;
  final String? organizerEmail;
  final String? inviteCode;

  factory SessionListItem.fromJson(Map<String, dynamic> json) {
    return SessionListItem(
      id: json['id'] as int,
      hallId: json['hallId'] as int,
      hallName: json['hallName'] as String? ?? '',
      sessionKindCode: json['sessionKindCode'] as String? ?? '',
      sessionLifecycleCode: json['sessionLifecycleCode'] as String? ?? '',
      startUtc: DateTime.parse(json['startUtc'] as String),
      endUtc: DateTime.parse(json['endUtc'] as String),
      maxParticipants: json['maxParticipants'] as int? ?? 0,
      participantCount: json['participantCount'] as int? ?? 0,
      maxAgeYears: json['maxAgeYears'] as int?,
      priceTotalCoins: (json['priceTotalCoins'] as num?)?.toDouble() ?? 0,
      organizerEmail: json['organizerEmail'] as String?,
      inviteCode: json['inviteCode'] as String?,
    );
  }
}

class SessionKindItem {
  SessionKindItem(
      {required this.id, required this.code, required this.displayName});

  final int id;
  final String code;
  final String displayName;

  factory SessionKindItem.fromJson(Map<String, dynamic> json) {
    return SessionKindItem(
      id: json['id'] as int,
      code: json['code'] as String? ?? '',
      displayName:
          json['displayName'] as String? ?? json['name'] as String? ?? '',
    );
  }
}

class SessionDetails {
  SessionDetails({
    required this.id,
    required this.hallId,
    required this.hallName,
    required this.organizerUserId,
    this.organizerEmail,
    required this.sessionKindCode,
    required this.sessionLifecycleCode,
    required this.startUtc,
    required this.endUtc,
    required this.maxParticipants,
    this.maxAgeYears,
    this.inviteCode,
    required this.priceTotalCoins,
  });

  final int id;
  final int hallId;
  final String hallName;
  final String organizerUserId;
  final String? organizerEmail;
  final String sessionKindCode;
  final String sessionLifecycleCode;
  final DateTime startUtc;
  final DateTime endUtc;
  final int maxParticipants;
  final int? maxAgeYears;
  final String? inviteCode;
  final double priceTotalCoins;

  factory SessionDetails.fromJson(Map<String, dynamic> json) {
    return SessionDetails(
      id: json['id'] as int,
      hallId: json['hallId'] as int,
      hallName: json['hallName'] as String? ?? '',
      organizerUserId: json['organizerUserId'] as String? ?? '',
      organizerEmail: json['organizerEmail'] as String?,
      sessionKindCode: json['sessionKindCode'] as String? ?? '',
      sessionLifecycleCode: json['sessionLifecycleCode'] as String? ?? '',
      startUtc: DateTime.parse(json['startUtc'] as String),
      endUtc: DateTime.parse(json['endUtc'] as String),
      maxParticipants: json['maxParticipants'] as int? ?? 0,
      maxAgeYears: json['maxAgeYears'] as int?,
      inviteCode: json['inviteCode'] as String?,
      priceTotalCoins: (json['priceTotalCoins'] as num?)?.toDouble() ?? 0,
    );
  }
}

class RecommendedSession {
  RecommendedSession({
    required this.sessionId,
    required this.hallId,
    required this.hallName,
    required this.cityName,
    required this.sessionKindCode,
    required this.startUtc,
    required this.endUtc,
    required this.participantCount,
    required this.maxParticipants,
    required this.priceTotalCoins,
    this.organizerEmail,
    required this.explanation,
  });

  final int sessionId;
  final int hallId;
  final String hallName;
  final String cityName;
  final String sessionKindCode;
  final DateTime startUtc;
  final DateTime endUtc;
  final int participantCount;
  final int maxParticipants;
  final double priceTotalCoins;
  final String? organizerEmail;
  final String explanation;

  factory RecommendedSession.fromJson(Map<String, dynamic> json) {
    return RecommendedSession(
      sessionId: json['sessionId'] as int,
      hallId: json['hallId'] as int,
      hallName: json['hallName'] as String? ?? '',
      cityName: json['cityName'] as String? ?? '',
      sessionKindCode: json['sessionKindCode'] as String? ?? '',
      startUtc: DateTime.parse(json['startUtc'] as String),
      endUtc: DateTime.parse(json['endUtc'] as String),
      participantCount: json['participantCount'] as int? ?? 0,
      maxParticipants: json['maxParticipants'] as int? ?? 0,
      priceTotalCoins: (json['priceTotalCoins'] as num?)?.toDouble() ?? 0,
      organizerEmail: json['organizerEmail'] as String?,
      explanation: json['explanation'] as String? ?? '',
    );
  }
}

class PlayerProfileStats {
  PlayerProfileStats({
    required this.totalParticipations,
    required this.completedParticipations,
    required this.organizedSessions,
    required this.upcomingParticipations,
    required this.totalCoinsSpentOnSessions,
    required this.totalCoinsPurchased,
    required this.playFrequencyPerMonth,
  });

  final int totalParticipations;
  final int completedParticipations;
  final int organizedSessions;
  final int upcomingParticipations;
  final double totalCoinsSpentOnSessions;
  final double totalCoinsPurchased;
  final double playFrequencyPerMonth;

  factory PlayerProfileStats.fromJson(Map<String, dynamic> json) {
    return PlayerProfileStats(
      totalParticipations: json['totalParticipations'] as int? ?? 0,
      completedParticipations: json['completedParticipations'] as int? ?? 0,
      organizedSessions: json['organizedSessions'] as int? ?? 0,
      upcomingParticipations: json['upcomingParticipations'] as int? ?? 0,
      totalCoinsSpentOnSessions:
          (json['totalCoinsSpentOnSessions'] as num?)?.toDouble() ?? 0,
      totalCoinsPurchased:
          (json['totalCoinsPurchased'] as num?)?.toDouble() ?? 0,
      playFrequencyPerMonth:
          (json['playFrequencyPerMonth'] as num?)?.toDouble() ?? 0,
    );
  }
}

class SessionJoinQuote {
  SessionJoinQuote(
      {required this.scheduledSessionId, required this.coinsRequired});

  final int scheduledSessionId;
  final double coinsRequired;

  factory SessionJoinQuote.fromJson(Map<String, dynamic> json) {
    return SessionJoinQuote(
      scheduledSessionId: json['scheduledSessionId'] as int,
      coinsRequired: (json['coinsRequired'] as num?)?.toDouble() ?? 0,
    );
  }
}

class PendingReview {
  PendingReview({
    required this.hallId,
    required this.hallName,
    required this.scheduledSessionId,
  });

  final int hallId;
  final String hallName;
  final int scheduledSessionId;

  factory PendingReview.fromJson(Map<String, dynamic> json) {
    return PendingReview(
      hallId: json['hallId'] as int,
      hallName: json['hallName'] as String? ?? '',
      scheduledSessionId: json['scheduledSessionId'] as int,
    );
  }
}

