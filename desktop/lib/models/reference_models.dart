class NamedReferenceItem {
  NamedReferenceItem({required this.id, required this.name});

  final int id;
  final String name;

  factory NamedReferenceItem.fromJson(Map<String, dynamic> json) {
    return NamedReferenceItem(
      id: (json['id'] as num).toInt(),
      name: json['name'] as String? ?? '',
    );
  }
}
