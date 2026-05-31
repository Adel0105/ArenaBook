class SessionListItem {
  SessionListItem({
    required this.id,
    required this.hallId,
    required this.hallName,
    required this.organizerUserId,
    required this.organizerEmail,
    required this.sessionKindId,
    required this.sessionKindCode,
    required this.sessionLifecycleStatusId,
    required this.sessionLifecycleCode,
    required this.startUtc,
    required this.endUtc,
    required this.maxParticipants,
    required this.participantCount,
    required this.maxAgeYears,
    required this.inviteCode,
    required this.priceTotalCoins,
  });

  final int id;
  final int hallId;
  final String hallName;
  final String organizerUserId;
  final String? organizerEmail;
  final int sessionKindId;
  final String sessionKindCode;
  final int sessionLifecycleStatusId;
  final String sessionLifecycleCode;
  final DateTime startUtc;
  final DateTime endUtc;
  final int maxParticipants;
  final int participantCount;
  final int? maxAgeYears;
  final String? inviteCode;
  final double priceTotalCoins;

  factory SessionListItem.fromJson(Map<String, dynamic> json) {
    return SessionListItem(
      id: (json['id'] as num).toInt(),
      hallId: (json['hallId'] as num).toInt(),
      hallName: json['hallName'] as String? ?? '',
      organizerUserId: json['organizerUserId'] as String? ?? '',
      organizerEmail: json['organizerEmail'] as String?,
      sessionKindId: (json['sessionKindId'] as num).toInt(),
      sessionKindCode: json['sessionKindCode'] as String? ?? '',
      sessionLifecycleStatusId: (json['sessionLifecycleStatusId'] as num).toInt(),
      sessionLifecycleCode: json['sessionLifecycleCode'] as String? ?? '',
      startUtc: DateTime.parse(json['startUtc'] as String),
      endUtc: DateTime.parse(json['endUtc'] as String),
      maxParticipants: (json['maxParticipants'] as num).toInt(),
      participantCount: (json['participantCount'] as num).toInt(),
      maxAgeYears: (json['maxAgeYears'] as num?)?.toInt(),
      inviteCode: json['inviteCode'] as String?,
      priceTotalCoins: (json['priceTotalCoins'] as num).toDouble(),
    );
  }
}

class SessionDetails {
  SessionDetails({
    required this.id,
    required this.hallId,
    required this.hallName,
    required this.organizerUserId,
    required this.organizerEmail,
    required this.sessionKindId,
    required this.sessionKindCode,
    required this.sessionLifecycleStatusId,
    required this.sessionLifecycleCode,
    required this.startUtc,
    required this.endUtc,
    required this.maxParticipants,
    required this.maxAgeYears,
    required this.inviteCode,
    required this.createdUtc,
    required this.priceTotalCoins,
    required this.participants,
  });

  final int id;
  final int hallId;
  final String hallName;
  final String organizerUserId;
  final String? organizerEmail;
  final int sessionKindId;
  final String sessionKindCode;
  final int sessionLifecycleStatusId;
  final String sessionLifecycleCode;
  final DateTime startUtc;
  final DateTime endUtc;
  final int maxParticipants;
  final int? maxAgeYears;
  final String? inviteCode;
  final DateTime createdUtc;
  final double priceTotalCoins;
  final List<SessionParticipant> participants;

  factory SessionDetails.fromJson(Map<String, dynamic> json) {
    final raw = json['participants'];
    final parts = raw is List
        ? raw.map((e) => SessionParticipant.fromJson(e as Map<String, dynamic>)).toList()
        : <SessionParticipant>[];
    return SessionDetails(
      id: (json['id'] as num).toInt(),
      hallId: (json['hallId'] as num).toInt(),
      hallName: json['hallName'] as String? ?? '',
      organizerUserId: json['organizerUserId'] as String? ?? '',
      organizerEmail: json['organizerEmail'] as String?,
      sessionKindId: (json['sessionKindId'] as num).toInt(),
      sessionKindCode: json['sessionKindCode'] as String? ?? '',
      sessionLifecycleStatusId: (json['sessionLifecycleStatusId'] as num).toInt(),
      sessionLifecycleCode: json['sessionLifecycleCode'] as String? ?? '',
      startUtc: DateTime.parse(json['startUtc'] as String),
      endUtc: DateTime.parse(json['endUtc'] as String),
      maxParticipants: (json['maxParticipants'] as num).toInt(),
      maxAgeYears: (json['maxAgeYears'] as num?)?.toInt(),
      inviteCode: json['inviteCode'] as String?,
      createdUtc: DateTime.parse(json['createdUtc'] as String),
      priceTotalCoins: (json['priceTotalCoins'] as num).toDouble(),
      participants: parts,
    );
  }
}

class SessionParticipant {
  SessionParticipant({
    required this.userId,
    required this.userEmail,
    required this.joinedUtc,
    required this.coinsPaid,
    required this.isOrganizer,
  });

  final String userId;
  final String? userEmail;
  final DateTime joinedUtc;
  final double coinsPaid;
  final bool isOrganizer;

  factory SessionParticipant.fromJson(Map<String, dynamic> json) {
    return SessionParticipant(
      userId: json['userId'] as String? ?? '',
      userEmail: json['userEmail'] as String?,
      joinedUtc: DateTime.parse(json['joinedUtc'] as String),
      coinsPaid: (json['coinsPaid'] as num).toDouble(),
      isOrganizer: json['isOrganizer'] as bool? ?? false,
    );
  }
}

class ReferenceItem {
  ReferenceItem({required this.id, required this.code, required this.displayName});

  final int id;
  final String code;
  final String displayName;

  factory ReferenceItem.fromJson(Map<String, dynamic> json) {
    return ReferenceItem(
      id: (json['id'] as num).toInt(),
      code: json['code'] as String? ?? '',
      displayName: json['displayName'] as String? ?? '',
    );
  }
}

class CityItem {
  CityItem({required this.id, required this.name, required this.countryId});

  final int id;
  final String name;
  final int countryId;

  factory CityItem.fromJson(Map<String, dynamic> json) {
    return CityItem(
      id: (json['id'] as num).toInt(),
      name: json['name'] as String? ?? '',
      countryId: (json['countryId'] as num).toInt(),
    );
  }
}

