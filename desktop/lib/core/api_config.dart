class ApiConfig {
  ApiConfig._();

  static String get baseUrl {
    const fromEnv = String.fromEnvironment('API_BASE_URL');
    if (fromEnv.isNotEmpty) {
      return fromEnv.replaceAll(RegExp(r'/+$'), '');
    }
    return 'http://localhost:5000';
  }
}

