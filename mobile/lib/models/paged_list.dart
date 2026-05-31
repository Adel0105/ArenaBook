class PagedList<T> {
  PagedList({
    required this.items,
    required this.page,
    required this.pageSize,
    required this.totalCount,
  });

  final List<T> items;
  final int page;
  final int pageSize;
  final int totalCount;

  int get totalPages {
    if (pageSize <= 0) {
      return 0;
    }
    return (totalCount / pageSize).ceil();
  }

  static PagedList<T> fromJson<T>(
    Map<String, dynamic> json,
    T Function(Map<String, dynamic>) fromItem,
  ) {
    final raw = json['items'] as List<dynamic>? ?? [];
    return PagedList<T>(
      items: raw.map((e) => fromItem(e as Map<String, dynamic>)).toList(),
      page: json['page'] as int? ?? 1,
      pageSize: json['pageSize'] as int? ?? 20,
      totalCount: json['totalCount'] as int? ?? 0,
    );
  }
}

