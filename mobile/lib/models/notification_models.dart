class UserNotification {
  UserNotification({
    required this.id,
    required this.title,
    required this.body,
    required this.isRead,
    required this.createdUtc,
  });

  final int id;
  final String title;
  final String body;
  final bool isRead;
  final DateTime createdUtc;

  factory UserNotification.fromJson(Map<String, dynamic> json) {
    return UserNotification(
      id: json['id'] as int,
      title: json['title'] as String? ?? '',
      body: json['body'] as String? ?? '',
      isRead: json['isRead'] as bool? ?? false,
      createdUtc: DateTime.parse(json['createdUtc'] as String),
    );
  }
}

