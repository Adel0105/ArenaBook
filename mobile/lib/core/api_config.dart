class ApiConfig {
  ApiConfig._();

  static String get baseUrl {
    const fromEnv = String.fromEnvironment('API_BASE_URL');
    if (fromEnv.isNotEmpty) {
      return fromEnv.replaceAll(RegExp(r'/+$'), '');
    }
    return 'http://10.0.2.2:5000';
  }
}

