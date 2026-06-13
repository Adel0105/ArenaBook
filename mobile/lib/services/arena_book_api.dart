import 'dart:convert';

import 'package:arena_book_mobile/core/api_config.dart';
import 'package:arena_book_mobile/models/auth_tokens.dart';
import 'package:arena_book_mobile/models/coin_models.dart';
import 'package:arena_book_mobile/models/current_user.dart';
import 'package:arena_book_mobile/models/hall_models.dart';
import 'package:arena_book_mobile/models/notification_models.dart';
import 'package:arena_book_mobile/models/paged_list.dart';
import 'package:arena_book_mobile/models/session_models.dart';
import 'package:arena_book_mobile/services/api_error.dart';
import 'package:arena_book_mobile/services/timeout_http_client.dart';
import 'package:http/http.dart' as http;

class ArenaBookApi {
  ArenaBookApi({http.Client? httpClient})
      : _client = httpClient ?? TimeoutHttpClient();

  final http.Client _client;
  String? _accessToken;
  void Function()? onUnauthorized;

  void setAccessToken(String? token) {
    _accessToken = token;
  }

  Uri _uri(String path, [Map<String, String>? query]) {
    final base = ApiConfig.baseUrl;
    final p = path.startsWith('/') ? path : '/$path';
    return Uri.parse('$base$p').replace(queryParameters: query);
  }

  Map<String, String> _headers({bool jsonBody = false}) {
    final h = <String, String>{'Accept': 'application/json'};
    if (jsonBody) {
      h['Content-Type'] = 'application/json';
    }
    final t = _accessToken;
    if (t != null && t.isNotEmpty) {
      h['Authorization'] = 'Bearer $t';
    }
    return h;
  }

  Future<AuthTokens> register(Map<String, dynamic> body) async {
    final res = await _client.post(
      _uri('/api/auth/register'),
      headers: _headers(jsonBody: true),
      body: jsonEncode(body),
    );
    if (res.statusCode != 200) {
      throw _badResponse(res);
    }
    return AuthTokens.fromJson(jsonDecode(res.body) as Map<String, dynamic>);
  }

  Future<AuthTokens> login(
      {required String email, required String password}) async {
    final res = await _client.post(
      _uri('/api/auth/login'),
      headers: _headers(jsonBody: true),
      body: jsonEncode({'email': email, 'password': password}),
    );
    if (res.statusCode == 401) {
      throw ApiError(401, 'Neispravan e-mail ili lozinka.');
    }
    if (res.statusCode != 200) {
      throw _badResponse(res);
    }
    return AuthTokens.fromJson(jsonDecode(res.body) as Map<String, dynamic>);
  }

  Future<CurrentUser> me() async {
    final res = await _client.get(_uri('/api/auth/me'), headers: _headers());
    if (res.statusCode == 401) {
      throw ApiError(401, 'Sesija je istekla.');
    }
    if (res.statusCode != 200) {
      throw _badResponse(res);
    }
    return CurrentUser.fromJson(jsonDecode(res.body) as Map<String, dynamic>);
  }

  Future<void> logout() async {
    final res = await _client.post(
      _uri('/api/auth/logout'),
      headers: _headers(),
    );
    if (res.statusCode != 204 && res.statusCode != 401) {
      throw _badResponse(res);
    }
  }

  Future<CurrentUser> updateProfile(Map<String, dynamic> body) async {
    final res = await _client.put(
      _uri('/api/auth/me'),
      headers: _headers(jsonBody: true),
      body: jsonEncode(body),
    );
    if (res.statusCode != 200) {
      throw _badResponse(res);
    }
    return CurrentUser.fromJson(jsonDecode(res.body) as Map<String, dynamic>);
  }

  Future<void> changePassword({
    required String currentPassword,
    required String newPassword,
  }) async {
    final res = await _client.post(
      _uri('/api/auth/change-password'),
      headers: _headers(jsonBody: true),
      body: jsonEncode({
        'currentPassword': currentPassword,
        'newPassword': newPassword,
      }),
    );
    if (res.statusCode != 204) {
      throw _badResponse(res);
    }
  }

  Future<({String message, String? resetToken})> forgotPassword(String email) async {
    final res = await _client.post(
      _uri('/api/auth/forgot-password'),
      headers: _headers(jsonBody: true),
      body: jsonEncode({'email': email}),
    );
    if (res.statusCode != 200) {
      throw _badResponse(res);
    }
    final map = jsonDecode(res.body) as Map<String, dynamic>;
    return (
      message: map['message'] as String? ?? 'Zahtjev je zaprimljen.',
      resetToken: map['resetToken'] as String?,
    );
  }

  Future<void> resetPassword({
    required String email,
    required String token,
    required String newPassword,
  }) async {
    final res = await _client.post(
      _uri('/api/auth/reset-password'),
      headers: _headers(jsonBody: true),
      body: jsonEncode(
          {'email': email, 'token': token, 'newPassword': newPassword}),
    );
    if (res.statusCode != 204) {
      throw _badResponse(res);
    }
  }

  Future<PagedList<HallListItem>> halls({
    int page = 1,
    int pageSize = 20,
    String? q,
    int? cityId,
    int? countryId,
    bool? isActive,
  }) async {
    final query = <String, String>{
      'page': '$page',
      'pageSize': '$pageSize',
      if (q != null && q.isNotEmpty) 'q': q,
      if (cityId != null) 'cityId': '$cityId',
      if (countryId != null) 'countryId': '$countryId',
      if (isActive != null) 'isActive': '$isActive',
    };
    final res =
        await _client.get(_uri('/api/halls', query), headers: _headers());
    _ensureOk(res, 'Dvorane');
    return PagedList.fromJson(
      jsonDecode(res.body) as Map<String, dynamic>,
      HallListItem.fromJson,
    );
  }

  Future<HallDetails> hallById(int id) async {
    final res = await _client.get(_uri('/api/halls/$id'), headers: _headers());
    _ensureOk(res, 'Dvorana');
    return HallDetails.fromJson(jsonDecode(res.body) as Map<String, dynamic>);
  }

  Future<PagedList<HallPhoto>> hallPhotos(int hallId) async {
    final res = await _client.get(
      _uri('/api/halls/$hallId/photos', {'page': '1', 'pageSize': '50'}),
      headers: _headers(),
    );
    _ensureOk(res, 'Fotografije');
    return PagedList.fromJson(
      jsonDecode(res.body) as Map<String, dynamic>,
      HallPhoto.fromJson,
    );
  }

  Future<PagedList<HallReview>> hallReviews(int hallId) async {
    final res = await _client.get(
      _uri('/api/halls/$hallId/reviews', {'page': '1', 'pageSize': '50'}),
      headers: _headers(),
    );
    _ensureOk(res, 'Recenzije');
    return PagedList.fromJson(
      jsonDecode(res.body) as Map<String, dynamic>,
      HallReview.fromJson,
    );
  }

  Future<HallReview> createHallReview(
      int hallId, Map<String, dynamic> body) async {
    final res = await _client.post(
      _uri('/api/halls/$hallId/reviews'),
      headers: _headers(jsonBody: true),
      body: jsonEncode(body),
    );
    if (res.statusCode != 200) {
      throw _badResponse(res);
    }
    return HallReview.fromJson(jsonDecode(res.body) as Map<String, dynamic>);
  }

  Future<HallReactionSummary> hallReactions(int hallId) async {
    final res = await _client.get(
      _uri('/api/halls/$hallId/reactions'),
      headers: _headers(),
    );
    _ensureOk(res, 'Reakcije');
    return HallReactionSummary.fromJson(
      jsonDecode(res.body) as Map<String, dynamic>,
    );
  }

  Future<HallReactionSummary> setHallReaction(
      int hallId, String reaction) async {
    final res = await _client.put(
      _uri('/api/halls/$hallId/reactions'),
      headers: _headers(jsonBody: true),
      body: jsonEncode({'reaction': reaction}),
    );
    if (res.statusCode != 200) {
      throw _badResponse(res);
    }
    return HallReactionSummary.fromJson(
      jsonDecode(res.body) as Map<String, dynamic>,
    );
  }

  Future<List<PendingReview>> pendingReviews() async {
    final res =
        await _client.get(_uri('/api/me/reviews/pending'), headers: _headers());
    _ensureOk(res, 'Recenzije');
    final list = jsonDecode(res.body) as List<dynamic>;
    return list
        .map((e) => PendingReview.fromJson(e as Map<String, dynamic>))
        .toList();
  }

  Future<List<RecommendedHall>> recommendations(
      {int? cityId, int limit = 10}) async {
    final res = await _client.get(
      _uri('/api/me/recommendations/halls', {
        if (cityId != null) 'cityId': '$cityId',
        'limit': '$limit',
      }),
      headers: _headers(),
    );
    _ensureOk(res, 'Preporuke');
    final list = jsonDecode(res.body) as List<dynamic>;
    return list
        .map((e) => RecommendedHall.fromJson(e as Map<String, dynamic>))
        .toList();
  }

  Future<PagedList<HallEquipmentItem>> hallEquipment(int hallId) async {
    final res = await _client.get(
      _uri('/api/halls/$hallId/equipment', {'page': '1', 'pageSize': '50'}),
      headers: _headers(),
    );
    _ensureOk(res, 'Oprema');
    return PagedList.fromJson(
      jsonDecode(res.body) as Map<String, dynamic>,
      HallEquipmentItem.fromJson,
    );
  }

  Future<List<RecommendedSession>> recommendedSessions(
      {int? cityId, int limit = 10}) async {
    final res = await _client.get(
      _uri('/api/me/recommendations/sessions', {
        if (cityId != null) 'cityId': '$cityId',
        'limit': '$limit',
      }),
      headers: _headers(),
    );
    _ensureOk(res, 'Preporuke termina');
    final list = jsonDecode(res.body) as List<dynamic>;
    return list
        .map((e) => RecommendedSession.fromJson(e as Map<String, dynamic>))
        .toList();
  }

  Future<PlayerProfileStats> profileStats() async {
    final res =
        await _client.get(_uri('/api/me/profile-stats'), headers: _headers());
    _ensureOk(res, 'Statistika');
    return PlayerProfileStats.fromJson(
        jsonDecode(res.body) as Map<String, dynamic>);
  }

  Future<PagedList<SessionKindItem>> sessionKinds() async {
    final res = await _client.get(
      _uri('/api/reference/session-kinds', {'page': '1', 'pageSize': '20'}),
      headers: _headers(),
    );
    _ensureOk(res, 'Vrste termina');
    return PagedList.fromJson(
      jsonDecode(res.body) as Map<String, dynamic>,
      SessionKindItem.fromJson,
    );
  }

  Future<SessionDetails> sessionById(int id) async {
    final res =
        await _client.get(_uri('/api/sessions/$id'), headers: _headers());
    _ensureOk(res, 'Termin');
    return SessionDetails.fromJson(
        jsonDecode(res.body) as Map<String, dynamic>);
  }

  Future<SessionDetails> createSession(Map<String, dynamic> body) async {
    final res = await _client.post(
      _uri('/api/sessions'),
      headers: _headers(jsonBody: true),
      body: jsonEncode(body),
    );
    if (res.statusCode != 200) {
      throw _badResponse(res);
    }
    return SessionDetails.fromJson(
        jsonDecode(res.body) as Map<String, dynamic>);
  }

  Future<SessionDetails> updateSession(
      int id, Map<String, dynamic> body) async {
    final res = await _client.put(
      _uri('/api/sessions/$id'),
      headers: _headers(jsonBody: true),
      body: jsonEncode(body),
    );
    if (res.statusCode != 200) {
      throw _badResponse(res);
    }
    return SessionDetails.fromJson(
        jsonDecode(res.body) as Map<String, dynamic>);
  }

  Future<SessionDetails> confirmSession(int id) async {
    final res = await _client.post(_uri('/api/sessions/$id/confirm'),
        headers: _headers());
    if (res.statusCode != 200) {
      throw _badResponse(res);
    }
    return SessionDetails.fromJson(
        jsonDecode(res.body) as Map<String, dynamic>);
  }

  Future<SessionDetails> cancelSession(int id, {required String reason}) async {
    final res = await _client.post(
      _uri('/api/sessions/$id/cancel'),
      headers: _headers(jsonBody: true),
      body: jsonEncode({'reason': reason}),
    );
    if (res.statusCode != 200) {
      throw _badResponse(res);
    }
    return SessionDetails.fromJson(
        jsonDecode(res.body) as Map<String, dynamic>);
  }

  Future<SessionDetails> completeSession(int id) async {
    final res = await _client.post(_uri('/api/sessions/$id/complete'),
        headers: _headers());
    if (res.statusCode != 200) {
      throw _badResponse(res);
    }
    return SessionDetails.fromJson(
        jsonDecode(res.body) as Map<String, dynamic>);
  }

  Future<PagedList<SessionListItem>> organizedSessions({
    int page = 1,
    int pageSize = 30,
    int? sessionLifecycleStatusId,
  }) async {
    final query = <String, String>{
      'page': '$page',
      'pageSize': '$pageSize',
      if (sessionLifecycleStatusId != null)
        'sessionLifecycleStatusId': '$sessionLifecycleStatusId',
    };
    final res = await _client.get(_uri('/api/me/organized-sessions', query),
        headers: _headers());
    _ensureOk(res, 'Moji organizirani termini');
    return PagedList.fromJson(
      jsonDecode(res.body) as Map<String, dynamic>,
      SessionListItem.fromJson,
    );
  }

  Future<void> markAllNotificationsRead() async {
    final res = await _client.post(_uri('/api/me/notifications/read-all'),
        headers: _headers());
    if (res.statusCode != 204) {
      throw _badResponse(res);
    }
  }

  Future<PagedList<SessionListItem>> sessions({
    int page = 1,
    int pageSize = 30,
    int? hallId,
    int? sessionKindId,
    int? sessionLifecycleStatusId,
    DateTime? dateFromUtc,
    DateTime? dateToUtc,
  }) async {
    final query = <String, String>{
      'page': '$page',
      'pageSize': '$pageSize',
      if (hallId != null) 'hallId': '$hallId',
      if (sessionKindId != null) 'sessionKindId': '$sessionKindId',
      if (sessionLifecycleStatusId != null)
        'sessionLifecycleStatusId': '$sessionLifecycleStatusId',
      if (dateFromUtc != null)
        'dateFromUtc': dateFromUtc.toUtc().toIso8601String(),
      if (dateToUtc != null) 'dateToUtc': dateToUtc.toUtc().toIso8601String(),
    };
    final res =
        await _client.get(_uri('/api/sessions', query), headers: _headers());
    _ensureOk(res, 'Termini');
    return PagedList.fromJson(
      jsonDecode(res.body) as Map<String, dynamic>,
      SessionListItem.fromJson,
    );
  }

  Future<PagedList<SessionListItem>> mySessions({
    int page = 1,
    int pageSize = 30,
    int? hallId,
    DateTime? dateFromUtc,
    DateTime? dateToUtc,
  }) async {
    final query = <String, String>{
      'page': '$page',
      'pageSize': '$pageSize',
      if (hallId != null) 'hallId': '$hallId',
      if (dateFromUtc != null)
        'dateFromUtc': dateFromUtc.toUtc().toIso8601String(),
      if (dateToUtc != null) 'dateToUtc': dateToUtc.toUtc().toIso8601String(),
    };
    final res =
        await _client.get(_uri('/api/me/sessions', query), headers: _headers());
    _ensureOk(res, 'Moji termini');
    return PagedList.fromJson(
      jsonDecode(res.body) as Map<String, dynamic>,
      SessionListItem.fromJson,
    );
  }

  Future<SessionJoinQuote> joinQuote(int sessionId) async {
    final res = await _client.get(_uri('/api/sessions/$sessionId/join-coins'),
        headers: _headers());
    _ensureOk(res, 'Cijena');
    return SessionJoinQuote.fromJson(
        jsonDecode(res.body) as Map<String, dynamic>);
  }

  Future<void> joinSession(int sessionId, {String? inviteCode}) async {
    final res = await _client.post(
      _uri('/api/sessions/$sessionId/join'),
      headers: _headers(jsonBody: true),
      body: jsonEncode({'inviteCode': inviteCode}),
    );
    if (res.statusCode != 204) {
      throw _badResponse(res);
    }
  }

  Future<CoinWallet> wallet() async {
    final res =
        await _client.get(_uri('/api/me/coins/wallet'), headers: _headers());
    _ensureOk(res, 'Novčanik');
    return CoinWallet.fromJson(jsonDecode(res.body) as Map<String, dynamic>);
  }

  Future<PagedList<CoinLedgerEntry>> ledger({int page = 1}) async {
    final res = await _client.get(
      _uri('/api/me/coins/ledger', {'page': '$page', 'pageSize': '30'}),
      headers: _headers(),
    );
    _ensureOk(res, 'Transakcije');
    return PagedList.fromJson(
      jsonDecode(res.body) as Map<String, dynamic>,
      CoinLedgerEntry.fromJson,
    );
  }

  Future<StripeIntentResult> stripeCreateIntent(double coins) async {
    final res = await _client.post(
      _uri('/api/me/payments/stripe/create-intent'),
      headers: _headers(jsonBody: true),
      body: jsonEncode({'coinsToPurchase': coins}),
    );
    if (res.statusCode != 200) {
      throw _badResponse(res);
    }
    return StripeIntentResult.fromJson(
      jsonDecode(res.body) as Map<String, dynamic>,
    );
  }

  Future<CoinPurchaseResult> stripeCompletePurchase(String paymentIntentId) async {
    final res = await _client.post(
      _uri('/api/me/payments/stripe/complete'),
      headers: _headers(jsonBody: true),
      body: jsonEncode({'paymentIntentId': paymentIntentId}),
    );
    if (res.statusCode != 200) {
      throw _badResponse(res);
    }
    return CoinPurchaseResult.fromJson(
      jsonDecode(res.body) as Map<String, dynamic>,
    );
  }

  Future<PayPalOrderResult> paypalCreateOrder(
    double coins, {
    required String returnUrl,
    required String cancelUrl,
  }) async {
    final res = await _client.post(
      _uri('/api/me/payments/paypal/create-order'),
      headers: _headers(jsonBody: true),
      body: jsonEncode({
        'coinsToPurchase': coins,
        'returnUrl': returnUrl,
        'cancelUrl': cancelUrl,
      }),
    );
    if (res.statusCode != 200) {
      throw _badResponse(res);
    }
    return PayPalOrderResult.fromJson(
        jsonDecode(res.body) as Map<String, dynamic>);
  }

  Future<CoinPurchaseResult> paypalCapture(String payPalOrderId) async {
    final res = await _client.post(
      _uri('/api/me/payments/paypal/capture'),
      headers: _headers(jsonBody: true),
      body: jsonEncode({'payPalOrderId': payPalOrderId}),
    );
    if (res.statusCode != 200) {
      throw _badResponse(res);
    }
    return CoinPurchaseResult.fromJson(
      jsonDecode(res.body) as Map<String, dynamic>,
    );
  }

  Future<PagedList<UserNotification>> notifications({int page = 1}) async {
    final res = await _client.get(
      _uri('/api/me/notifications', {'page': '$page', 'pageSize': '30'}),
      headers: _headers(),
    );
    _ensureOk(res, 'Notifikacije');
    return PagedList.fromJson(
      jsonDecode(res.body) as Map<String, dynamic>,
      UserNotification.fromJson,
    );
  }

  Future<void> markNotificationRead(int id) async {
    final res = await _client.post(_uri('/api/me/notifications/$id/read'),
        headers: _headers());
    if (res.statusCode != 204) {
      throw _badResponse(res);
    }
  }

  Future<PagedList<CityItem>> cities({int pageSize = 200}) async {
    final res = await _client.get(
      _uri('/api/reference/cities', {'page': '1', 'pageSize': '$pageSize'}),
      headers: _headers(),
    );
    _ensureOk(res, 'Gradovi');
    return PagedList.fromJson(
      jsonDecode(res.body) as Map<String, dynamic>,
      CityItem.fromJson,
    );
  }

  void _ensureOk(http.Response res, String area) {
    if (res.statusCode >= 200 && res.statusCode < 300) {
      return;
    }
    if (res.statusCode == 401) {
      onUnauthorized?.call();
    }
    throw _badResponse(res, area);
  }

  ApiError _badResponse(http.Response res, [String? area]) {
    String message = area != null
        ? '$area: greška ${res.statusCode}'
        : 'Greška ${res.statusCode}';
    try {
      final body = jsonDecode(res.body);
      if (body is Map<String, dynamic>) {
        final errors = body['errors'];
        if (errors is Map<String, dynamic>) {
          final parts = <String>[];
          errors.forEach((key, value) {
            if (value is List) {
              parts.addAll(value.map((e) => e.toString()));
            } else {
              parts.add(value.toString());
            }
          });
          if (parts.isNotEmpty) {
            message = parts.join('\n');
          }
        } else if (body['title'] != null) {
          message = body['title'].toString();
        }
      }
    } catch (_) {}
    return ApiError(res.statusCode, message);
  }
}

