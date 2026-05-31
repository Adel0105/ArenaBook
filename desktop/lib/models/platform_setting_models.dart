class PlatformSettingItem {
  PlatformSettingItem({
    required this.id,
    required this.settingKey,
    required this.settingValue,
    required this.updatedUtc,
  });

  final int id;
  final String settingKey;
  final String settingValue;
  final DateTime updatedUtc;

  factory PlatformSettingItem.fromJson(Map<String, dynamic> json) {
    return PlatformSettingItem(
      id: json['id'] as int,
      settingKey: json['settingKey'] as String? ?? '',
      settingValue: json['settingValue'] as String? ?? '',
      updatedUtc: DateTime.parse(json['updatedUtc'] as String).toLocal(),
    );
  }
}

