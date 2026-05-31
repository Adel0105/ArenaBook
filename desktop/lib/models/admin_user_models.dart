DateTime? _parseOptionalDate(dynamic value) {
  if (value == null) {
    return null;
  }
  if (value is String && value.length >= 10) {
    return DateTime.tryParse(value.substring(0, 10));
  }
  return null;
}

class AdminUserListItem {
  AdminUserListItem({
    required this.userId,
    required this.email,
    required this.firstName,
    required this.lastName,
    required this.dateOfBirth,
    required this.cityId,
    required this.cityName,
    required this.roles,
    required this.isLockedOut,
    required this.registeredUtc,
  });

  final String userId;
  final String email;
  final String firstName;
  final String lastName;
  final DateTime? dateOfBirth;
  final int? cityId;
  final String? cityName;
  final List<String> roles;
  final bool isLockedOut;
  final DateTime? registeredUtc;

  String get fullName => '$firstName $lastName'.trim();

  factory AdminUserListItem.fromJson(Map<String, dynamic> json) {
    final rolesRaw = json['roles'];
    return AdminUserListItem(
      userId: json['userId'] as String? ?? '',
      email: json['email'] as String? ?? '',
      firstName: json['firstName'] as String? ?? '',
      lastName: json['lastName'] as String? ?? '',
      dateOfBirth: _parseOptionalDate(json['dateOfBirth']),
      cityId: (json['cityId'] as num?)?.toInt(),
      cityName: json['cityName'] as String?,
      roles: rolesRaw is List ? rolesRaw.map((e) => e.toString()).toList() : const [],
      isLockedOut: json['isLockedOut'] as bool? ?? false,
      registeredUtc: json['registeredUtc'] != null ? DateTime.parse(json['registeredUtc'] as String) : null,
    );
  }
}

class AdminUserDetails {
  AdminUserDetails({
    required this.userId,
    required this.email,
    required this.firstName,
    required this.lastName,
    required this.dateOfBirth,
    required this.cityId,
    required this.cityName,
    required this.profileImageUrl,
    required this.roles,
    required this.isLockedOut,
    required this.registeredUtc,
    required this.sessionsOrganizedCount,
    required this.sessionsParticipatedCount,
  });

  final String userId;
  final String email;
  final String firstName;
  final String lastName;
  final DateTime? dateOfBirth;
  final int? cityId;
  final String? cityName;
  final String? profileImageUrl;
  final List<String> roles;
  final bool isLockedOut;
  final DateTime? registeredUtc;
  final int sessionsOrganizedCount;
  final int sessionsParticipatedCount;

  String get fullName => '$firstName $lastName'.trim();

  factory AdminUserDetails.fromJson(Map<String, dynamic> json) {
    final rolesRaw = json['roles'];
    return AdminUserDetails(
      userId: json['userId'] as String? ?? '',
      email: json['email'] as String? ?? '',
      firstName: json['firstName'] as String? ?? '',
      lastName: json['lastName'] as String? ?? '',
      dateOfBirth: _parseOptionalDate(json['dateOfBirth']),
      cityId: (json['cityId'] as num?)?.toInt(),
      cityName: json['cityName'] as String?,
      profileImageUrl: json['profileImageUrl'] as String?,
      roles: rolesRaw is List ? rolesRaw.map((e) => e.toString()).toList() : const [],
      isLockedOut: json['isLockedOut'] as bool? ?? false,
      registeredUtc: json['registeredUtc'] != null ? DateTime.parse(json['registeredUtc'] as String) : null,
      sessionsOrganizedCount: (json['sessionsOrganizedCount'] as num?)?.toInt() ?? 0,
      sessionsParticipatedCount: (json['sessionsParticipatedCount'] as num?)?.toInt() ?? 0,
    );
  }
}

