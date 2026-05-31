import 'dart:async';
import 'dart:io';

import 'package:arena_book_mobile/core/api_config.dart';
import 'package:http/http.dart' as http;

class ApiError implements Exception {
  ApiError(this.statusCode, this.message);

  final int? statusCode;
  final String message;

  @override
  String toString() => message;

  static String networkTimeoutMessage() =>
      'Server ne odgovara na vrijeme. Provjerite da li API radi na ${ApiConfig.baseUrl}';

  static String networkUnreachableMessage() =>
      'Nema veze s API-jem na ${ApiConfig.baseUrl}. Provjerite Wi‑Fi i IP adresu PC-a.';

  static String friendlyMessage(Object error) {
    if (error is ApiError) {
      return error.message;
    }
    if (error is TimeoutException) {
      return networkTimeoutMessage();
    }
    if (error is SocketException) {
      return networkUnreachableMessage();
    }
    if (error is http.ClientException) {
      final msg = error.message;
      if (msg.contains('Connection refused') ||
          msg.contains('Failed host lookup')) {
        return networkUnreachableMessage();
      }
      return 'Greška mreže: $msg';
    }
    return error.toString();
  }
}

