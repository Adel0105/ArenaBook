class ApiError implements Exception {
  ApiError(this.statusCode, this.message);

  final int? statusCode;
  final String message;

  @override
  String toString() => message;
}

