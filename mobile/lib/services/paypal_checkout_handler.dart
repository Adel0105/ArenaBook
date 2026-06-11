import 'dart:async';

import 'package:arena_book_mobile/services/api_error.dart';

/// Koordinira povratak iz PayPal preglednika preko deep linka `arenabook://paypal/...`.
class PayPalCheckoutHandler {
  PayPalCheckoutHandler._();

  static final PayPalCheckoutHandler instance = PayPalCheckoutHandler._();

  static const returnUrl = 'arenabook://paypal/return';
  static const cancelUrl = 'arenabook://paypal/cancel';

  Completer<String>? _approvalCompleter;
  String? _expectedOrderId;

  Future<String> waitForApproval(String payPalOrderId) {
    cancelPending();
    _expectedOrderId = payPalOrderId;
    _approvalCompleter = Completer<String>();
    return _approvalCompleter!.future.timeout(
      const Duration(minutes: 15),
      onTimeout: () {
        cancelPending();
        throw ApiError(null, 'PayPal odobrenje nije završeno na vrijeme.');
      },
    );
  }

  void handleUri(Uri uri) {
    if (uri.scheme != 'arenabook' || uri.host != 'paypal') {
      return;
    }

    final segment = uri.pathSegments.isNotEmpty
        ? uri.pathSegments.first
        : uri.path.replaceFirst('/', '');

    if (segment == 'cancel') {
      _fail(ApiError(null, 'PayPal uplata je otkazana.'));
      return;
    }

    if (segment != 'return') {
      return;
    }

    final token = uri.queryParameters['token'] ?? _expectedOrderId;
    if (token == null || token.isEmpty) {
      _fail(ApiError(null, 'PayPal nije vratio token narudžbe.'));
      return;
    }

    if (_expectedOrderId != null && token != _expectedOrderId) {
      _fail(ApiError(null, 'PayPal token ne odgovara narudžbi.'));
      return;
    }

    final completer = _approvalCompleter;
    if (completer == null || completer.isCompleted) {
      return;
    }

    completer.complete(token);
    _reset();
  }

  void cancelPending() {
    _fail(ApiError(null, 'PayPal uplata prekinuta.'));
  }

  void _fail(ApiError error) {
    final completer = _approvalCompleter;
    if (completer != null && !completer.isCompleted) {
      completer.completeError(error);
    }
    _reset();
  }

  void _reset() {
    _approvalCompleter = null;
    _expectedOrderId = null;
  }
}
