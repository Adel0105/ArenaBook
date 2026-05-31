import 'package:arena_book_desktop/core/app_roles.dart';

class CurrentUser {
  const CurrentUser({
    required this.userId,
    required this.email,
    required this.firstName,
    required this.lastName,
    required this.roles,
    this.dateOfBirth,
    this.cityId,
    this.profileImageUrl,
  });

  final String userId;
  final String email;
  final String firstName;
  final String lastName;
  final List<String> roles;
  final String? dateOfBirth;
  final int? cityId;
  final String? profileImageUrl;

  String get displayName {
    final a = firstName.trim();
    final b = lastName.trim();
    if (a.isEmpty && b.isEmpty) {
      return email;
    }
    return '$a $b'.trim();
  }

  bool get isAdministrator => roles.contains(AppRoles.administrator);

  factory CurrentUser.fromJson(Map<String, dynamic> json) {
    final rolesRaw = json['roles'];
    final roles = <String>[];
    if (rolesRaw is List) {
      for (final e in rolesRaw) {
        if (e is String) {
          roles.add(e);
        }
      }
    }
    final dob = json['dateOfBirth'];
    return CurrentUser(
      userId: json['userId'] as String? ?? '',
      email: json['email'] as String? ?? '',
      firstName: json['firstName'] as String? ?? '',
      lastName: json['lastName'] as String? ?? '',
      roles: roles,
      dateOfBirth: dob is String ? dob : null,
      cityId: (json['cityId'] as num?)?.toInt(),
      profileImageUrl: json['profileImageUrl'] as String?,
    );
  }
}

