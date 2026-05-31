class CurrentUser {
  CurrentUser({
    required this.userId,
    required this.email,
    required this.firstName,
    required this.lastName,
    this.dateOfBirth,
    this.cityId,
    this.profileImageUrl,
    required this.roles,
  });

  final String userId;
  final String email;
  final String firstName;
  final String lastName;
  final DateTime? dateOfBirth;
  final int? cityId;
  final String? profileImageUrl;
  final List<String> roles;

  String get displayName => '$firstName $lastName'.trim();

  bool get isPlayer => roles.contains('Member') || roles.contains('Organizer');

  factory CurrentUser.fromJson(Map<String, dynamic> json) {
    DateTime? dob;
    final dobRaw = json['dateOfBirth'];
    if (dobRaw is String && dobRaw.isNotEmpty) {
      dob = DateTime.parse(dobRaw);
    }
    final rolesRaw = json['roles'] as List<dynamic>? ?? [];
    return CurrentUser(
      userId: json['userId'] as String? ?? '',
      email: json['email'] as String? ?? '',
      firstName: json['firstName'] as String? ?? '',
      lastName: json['lastName'] as String? ?? '',
      dateOfBirth: dob,
      cityId: json['cityId'] as int?,
      profileImageUrl: json['profileImageUrl'] as String?,
      roles: rolesRaw.map((e) => e.toString()).toList(),
    );
  }
}

