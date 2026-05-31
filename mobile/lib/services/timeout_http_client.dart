import 'dart:async';
import 'dart:io';

import 'package:arena_book_mobile/services/api_error.dart';
import 'package:http/http.dart' as http;

class TimeoutHttpClient extends http.BaseClient {
  TimeoutHttpClient({
    http.Client? inner,
    this.timeout = const Duration(seconds: 25),
  }) : _inner = inner ?? http.Client();

  final http.Client _inner;
  final Duration timeout;

  @override
  Future<http.StreamedResponse> send(http.BaseRequest request) async {
    try {
      return await _inner.send(request).timeout(timeout);
    } on TimeoutException {
      throw ApiError(null, ApiError.networkTimeoutMessage());
    } on SocketException {
      throw ApiError(null, ApiError.networkUnreachableMessage());
    } on http.ClientException catch (e) {
      throw ApiError(null, ApiError.friendlyMessage(e));
    }
  }

  @override
  void close() => _inner.close();
}

