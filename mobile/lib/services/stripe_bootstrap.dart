import 'package:arena_book_mobile/core/api_config.dart';
import 'package:flutter_stripe/flutter_stripe.dart';

/// flutter_stripe na Androidu zahtijeva publishable key i applySettings prije PaymentSheet-a.
class StripeBootstrap {
  StripeBootstrap._();

  static String? _activeKey;

  static Future<void> initialize() async {
    final key = ApiConfig.stripePublishableKey;
    if (key.isEmpty) {
      return;
    }
    await _applyKey(key);
  }

  static Future<void> ensureReady(String publishableKey) async {
    if (publishableKey.isEmpty) {
      throw StateError('Stripe publishable key nije dostupan.');
    }
    await _applyKey(publishableKey);
  }

  static Future<void> _applyKey(String key) async {
    if (_activeKey == key) {
      return;
    }
    Stripe.publishableKey = key;
    await Stripe.instance.applySettings();
    _activeKey = key;
  }
}
