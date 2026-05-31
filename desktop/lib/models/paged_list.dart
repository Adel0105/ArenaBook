class PagedList<T> {
  const PagedList({
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

  factory PagedList.fromJson(
    Map<String, dynamic> json,
    T Function(Map<String, dynamic>) itemFromJson,
  ) {
    final raw = json['items'];
    final list = raw is List
        ? raw.map((e) => itemFromJson(e as Map<String, dynamic>)).toList()
        : <T>[];
    return PagedList(
      items: list,
      page: (json['page'] as num?)?.toInt() ?? 1,
      pageSize: (json['pageSize'] as num?)?.toInt() ?? 20,
      totalCount: (json['totalCount'] as num?)?.toInt() ?? 0,
    );
  }
}

