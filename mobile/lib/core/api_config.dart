class ApiConfig {
  ApiConfig._();

  static String get baseUrl {
    const fromEnv = String.fromEnvironment('API_BASE_URL');
    if (fromEnv.isNotEmpty) {
      return fromEnv.replaceAll(RegExp(r'/+$'), '');
    }
    return 'http://10.0.2.2:5000';
  }

  /// Opcionalni fallback ako API ne vrati publishable key u create-intent odgovoru.
  static String get stripePublishableKey =>
      const String.fromEnvironment('STRIPE_PUBLISHABLE_KEY');
}

