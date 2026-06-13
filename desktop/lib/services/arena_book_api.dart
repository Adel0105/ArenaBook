import 'dart:convert';
import 'dart:typed_data';

import 'package:arena_book_desktop/core/api_config.dart';
import 'package:arena_book_desktop/models/admin_dashboard_activity.dart';
import 'package:arena_book_desktop/models/admin_dashboard_summary.dart';
import 'package:arena_book_desktop/models/admin_user_models.dart';
import 'package:arena_book_desktop/models/auth_tokens.dart';
import 'package:arena_book_desktop/models/current_user.dart';
import 'package:arena_book_desktop/models/finance_models.dart';
import 'package:arena_book_desktop/models/hall_models.dart';
import 'package:arena_book_desktop/models/paged_list.dart';
import 'package:arena_book_desktop/models/platform_setting_models.dart';
import 'package:arena_book_desktop/models/reference_models.dart';
import 'package:arena_book_desktop/models/session_models.dart';
import 'package:arena_book_desktop/services/api_error.dart';
import 'package:http/http.dart' as http;

class ArenaBookApi {
  ArenaBookApi({http.Client? httpClient}) : _client = httpClient ?? http.Client();

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

  Future<AuthTokens> login({required String email, required String password}) async {
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
      throw ApiError(401, 'Sesija je istekla. Prijavite se ponovo.');
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

  Future<AdminDashboardSummary> adminDashboardSummary() async {
    final res = await _client.get(_uri('/api/admin/dashboard/summary'), headers: _headers());
    _ensureOk(res, 'Nadzorna ploča');
    return AdminDashboardSummary.fromJson(jsonDecode(res.body) as Map<String, dynamic>);
  }

  Future<AdminDashboardActivity> adminDashboardActivity({int months = 6}) async {
    final res = await _client.get(
      _uri('/api/admin/dashboard/activity', {'months': '$months'}),
      headers: _headers(),
    );
    _ensureOk(res, 'Aktivnost');
    return AdminDashboardActivity.fromJson(jsonDecode(res.body) as Map<String, dynamic>);
  }

  Future<PagedList<AdminUserListItem>> adminUsers({
    int page = 1,
    int pageSize = 20,
    String? q,
    String? email,
    DateTime? registeredFrom,
    DateTime? registeredTo,
    bool? isLockedOut,
  }) async {
    final query = <String, String>{
      'page': '$page',
      'pageSize': '$pageSize',
      if (q != null && q.isNotEmpty) 'q': q,
      if (email != null && email.isNotEmpty) 'email': email,
      if (registeredFrom != null) 'registeredFromUtc': registeredFrom.toUtc().toIso8601String(),
      if (registeredTo != null) 'registeredToUtc': registeredTo.toUtc().toIso8601String(),
      if (isLockedOut != null) 'isLockedOut': '$isLockedOut',
    };
    final res = await _client.get(_uri('/api/admin/users', query), headers: _headers());
    _ensureOk(res, 'Korisnici');
    return PagedList.fromJson(
      jsonDecode(res.body) as Map<String, dynamic>,
      AdminUserListItem.fromJson,
    );
  }

  Future<AdminUserDetails> adminUserById(String userId) async {
    final res = await _client.get(_uri('/api/admin/users/$userId'), headers: _headers());
    _ensureOk(res, 'Korisnik');
    return AdminUserDetails.fromJson(jsonDecode(res.body) as Map<String, dynamic>);
  }

  Future<AdminUserDetails> createAdminUser(Map<String, dynamic> body) async {
    final res = await _client.post(
      _uri('/api/admin/users'),
      headers: _headers(jsonBody: true),
      body: jsonEncode(body),
    );
    if (res.statusCode != 200) {
      throw _badResponse(res);
    }
    return AdminUserDetails.fromJson(jsonDecode(res.body) as Map<String, dynamic>);
  }

  Future<AdminUserDetails> updateAdminUser(String userId, Map<String, dynamic> body) async {
    final res = await _client.put(
      _uri('/api/admin/users/$userId'),
      headers: _headers(jsonBody: true),
      body: jsonEncode(body),
    );
    if (res.statusCode != 200) {
      throw _badResponse(res);
    }
    return AdminUserDetails.fromJson(jsonDecode(res.body) as Map<String, dynamic>);
  }

  Future<void> lockAdminUser(String userId) async {
    final res = await _client.post(_uri('/api/admin/users/$userId/lock'), headers: _headers());
    if (res.statusCode != 204) {
      throw _badResponse(res);
    }
  }

  Future<void> unlockAdminUser(String userId) async {
    final res = await _client.post(_uri('/api/admin/users/$userId/unlock'), headers: _headers());
    if (res.statusCode != 204) {
      throw _badResponse(res);
    }
  }

  Future<PagedList<HallListItem>> halls({
    int page = 1,
    int pageSize = 20,
    String? q,
    int? cityId,
    bool? isActive,
  }) async {
    final query = <String, String>{
      'page': '$page',
      'pageSize': '$pageSize',
      if (q != null && q.isNotEmpty) 'q': q,
      if (cityId != null) 'cityId': '$cityId',
      if (isActive != null) 'isActive': '$isActive',
    };
    final res = await _client.get(_uri('/api/halls', query), headers: _headers());
    _ensureOk(res, 'Dvorane');
    return PagedList.fromJson(jsonDecode(res.body) as Map<String, dynamic>, HallListItem.fromJson);
  }

  Future<HallDetails> hallById(int id) async {
    final res = await _client.get(_uri('/api/halls/$id'), headers: _headers());
    _ensureOk(res, 'Dvorana');
    return HallDetails.fromJson(jsonDecode(res.body) as Map<String, dynamic>);
  }

  Future<HallDetails> createHall(Map<String, dynamic> body) async {
    final res = await _client.post(
      _uri('/api/halls'),
      headers: _headers(jsonBody: true),
      body: jsonEncode(body),
    );
    if (res.statusCode != 200) {
      throw _badResponse(res);
    }
    return HallDetails.fromJson(jsonDecode(res.body) as Map<String, dynamic>);
  }

  Future<HallDetails> updateHall(int id, Map<String, dynamic> body) async {
    final res = await _client.put(
      _uri('/api/halls/$id'),
      headers: _headers(jsonBody: true),
      body: jsonEncode(body),
    );
    if (res.statusCode != 200) {
      throw _badResponse(res);
    }
    return HallDetails.fromJson(jsonDecode(res.body) as Map<String, dynamic>);
  }

  Future<void> deleteHall(int id) async {
    final res = await _client.delete(_uri('/api/halls/$id'), headers: _headers());
    if (res.statusCode != 204) {
      throw _badResponse(res);
    }
  }

  Future<PagedList<HallPhotoItem>> hallPhotos(int hallId, {int page = 1, int pageSize = 50}) async {
    final res = await _client.get(
      _uri('/api/halls/$hallId/photos', {'page': '$page', 'pageSize': '$pageSize'}),
      headers: _headers(),
    );
    _ensureOk(res, 'Fotografije dvorane');
    return PagedList.fromJson(jsonDecode(res.body) as Map<String, dynamic>, HallPhotoItem.fromJson);
  }

  Future<HallPhotoItem> createHallPhoto(int hallId, {required String imageUrl, required int sortOrder}) async {
    final res = await _client.post(
      _uri('/api/halls/$hallId/photos'),
      headers: _headers(jsonBody: true),
      body: jsonEncode({'imageUrl': imageUrl, 'sortOrder': sortOrder}),
    );
    if (res.statusCode != 200) {
      throw _badResponse(res);
    }
    return HallPhotoItem.fromJson(jsonDecode(res.body) as Map<String, dynamic>);
  }

  Future<HallPhotoItem> updateHallPhoto(
    int hallId,
    int photoId, {
    required String imageUrl,
    required int sortOrder,
  }) async {
    final res = await _client.put(
      _uri('/api/halls/$hallId/photos/$photoId'),
      headers: _headers(jsonBody: true),
      body: jsonEncode({'imageUrl': imageUrl, 'sortOrder': sortOrder}),
    );
    if (res.statusCode != 200) {
      throw _badResponse(res);
    }
    return HallPhotoItem.fromJson(jsonDecode(res.body) as Map<String, dynamic>);
  }

  Future<void> deleteHallPhoto(int hallId, int photoId) async {
    final res = await _client.delete(_uri('/api/halls/$hallId/photos/$photoId'), headers: _headers());
    if (res.statusCode != 204) {
      throw _badResponse(res);
    }
  }

  Future<PagedList<HallEquipmentLink>> hallEquipment(int hallId, {int page = 1, int pageSize = 50}) async {
    final res = await _client.get(
      _uri('/api/halls/$hallId/equipment', {'page': '$page', 'pageSize': '$pageSize'}),
      headers: _headers(),
    );
    _ensureOk(res, 'Oprema dvorane');
    return PagedList.fromJson(jsonDecode(res.body) as Map<String, dynamic>, HallEquipmentLink.fromJson);
  }

  Future<HallEquipmentLink> createHallEquipment(
    int hallId, {
    required int equipmentTypeId,
    required int quantity,
  }) async {
    final res = await _client.post(
      _uri('/api/halls/$hallId/equipment'),
      headers: _headers(jsonBody: true),
      body: jsonEncode({'equipmentTypeId': equipmentTypeId, 'quantity': quantity}),
    );
    if (res.statusCode != 200) {
      throw _badResponse(res);
    }
    return HallEquipmentLink.fromJson(jsonDecode(res.body) as Map<String, dynamic>);
  }

  Future<HallEquipmentLink> updateHallEquipment(
    int hallId,
    int linkId, {
    required int quantity,
  }) async {
    final res = await _client.put(
      _uri('/api/halls/$hallId/equipment/$linkId'),
      headers: _headers(jsonBody: true),
      body: jsonEncode({'quantity': quantity}),
    );
    if (res.statusCode != 200) {
      throw _badResponse(res);
    }
    return HallEquipmentLink.fromJson(jsonDecode(res.body) as Map<String, dynamic>);
  }

  Future<void> deleteHallEquipment(int hallId, int linkId) async {
    final res = await _client.delete(_uri('/api/halls/$hallId/equipment/$linkId'), headers: _headers());
    if (res.statusCode != 204) {
      throw _badResponse(res);
    }
  }

  Future<PagedList<SessionListItem>> sessions({
    int page = 1,
    int pageSize = 20,
    String? q,
    int? hallId,
    int? sessionLifecycleStatusId,
    String? organizerUserId,
    DateTime? dateFrom,
    DateTime? dateTo,
  }) async {
    final query = <String, String>{
      'page': '$page',
      'pageSize': '$pageSize',
      if (q != null && q.isNotEmpty) 'q': q,
      if (hallId != null) 'hallId': '$hallId',
      if (sessionLifecycleStatusId != null) 'sessionLifecycleStatusId': '$sessionLifecycleStatusId',
      if (organizerUserId != null && organizerUserId.isNotEmpty) 'organizerUserId': organizerUserId,
      if (dateFrom != null) 'dateFromUtc': dateFrom.toUtc().toIso8601String(),
      if (dateTo != null) 'dateToUtc': dateTo.toUtc().toIso8601String(),
    };
    final res = await _client.get(_uri('/api/sessions', query), headers: _headers());
    _ensureOk(res, 'Termini');
    return PagedList.fromJson(jsonDecode(res.body) as Map<String, dynamic>, SessionListItem.fromJson);
  }

  Future<SessionDetails> sessionById(int id) async {
    final res = await _client.get(_uri('/api/sessions/$id'), headers: _headers());
    _ensureOk(res, 'Termin');
    return SessionDetails.fromJson(jsonDecode(res.body) as Map<String, dynamic>);
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
    return SessionDetails.fromJson(jsonDecode(res.body) as Map<String, dynamic>);
  }

  Future<SessionDetails> updateSession(int id, Map<String, dynamic> body) async {
    final res = await _client.put(
      _uri('/api/sessions/$id'),
      headers: _headers(jsonBody: true),
      body: jsonEncode(body),
    );
    if (res.statusCode != 200) {
      throw _badResponse(res);
    }
    return SessionDetails.fromJson(jsonDecode(res.body) as Map<String, dynamic>);
  }

  Future<void> deleteSession(int id) async {
    final res = await _client.delete(_uri('/api/sessions/$id'), headers: _headers());
    if (res.statusCode != 204) {
      throw _badResponse(res);
    }
  }

  Future<void> confirmSession(int id) async {
    final res = await _client.post(_uri('/api/sessions/$id/confirm'), headers: _headers());
    if (res.statusCode != 200) {
      throw _badResponse(res);
    }
  }

  Future<void> cancelSession(int id, {required String reason}) async {
    final res = await _client.post(
      _uri('/api/sessions/$id/cancel'),
      headers: _headers(jsonBody: true),
      body: jsonEncode({'reason': reason}),
    );
    if (res.statusCode != 200) {
      throw _badResponse(res);
    }
  }

  Future<PagedList<CityItem>> cities({
    int page = 1,
    int pageSize = 100,
    String? q,
    int? countryId,
  }) async {
    final query = <String, String>{
      'page': '$page',
      'pageSize': '$pageSize',
      if (q != null && q.isNotEmpty) 'q': q,
      if (countryId != null) 'countryId': '$countryId',
    };
    final res = await _client.get(_uri('/api/reference/cities', query), headers: _headers());
    _ensureOk(res, 'Gradovi');
    return PagedList.fromJson(jsonDecode(res.body) as Map<String, dynamic>, CityItem.fromJson);
  }

  Future<PagedList<NamedReferenceItem>> countries({
    int page = 1,
    int pageSize = 100,
    String? q,
  }) async {
    final query = <String, String>{
      'page': '$page',
      'pageSize': '$pageSize',
      if (q != null && q.isNotEmpty) 'q': q,
    };
    final res = await _client.get(_uri('/api/reference/countries', query), headers: _headers());
    _ensureOk(res, 'Države');
    return PagedList.fromJson(jsonDecode(res.body) as Map<String, dynamic>, NamedReferenceItem.fromJson);
  }

  Future<NamedReferenceItem> createCountry(String name) async {
    final res = await _client.post(
      _uri('/api/reference/countries'),
      headers: _headers(jsonBody: true),
      body: jsonEncode({'name': name}),
    );
    if (res.statusCode != 200) {
      throw _badResponse(res);
    }
    return NamedReferenceItem.fromJson(jsonDecode(res.body) as Map<String, dynamic>);
  }

  Future<NamedReferenceItem> updateCountry(int id, String name) async {
    final res = await _client.put(
      _uri('/api/reference/countries/$id'),
      headers: _headers(jsonBody: true),
      body: jsonEncode({'name': name}),
    );
    if (res.statusCode != 200) {
      throw _badResponse(res);
    }
    return NamedReferenceItem.fromJson(jsonDecode(res.body) as Map<String, dynamic>);
  }

  Future<void> deleteCountry(int id) async {
    final res = await _client.delete(_uri('/api/reference/countries/$id'), headers: _headers());
    if (res.statusCode != 204) {
      throw _badResponse(res);
    }
  }

  Future<CityItem> createCity({required int countryId, required String name}) async {
    final res = await _client.post(
      _uri('/api/reference/cities'),
      headers: _headers(jsonBody: true),
      body: jsonEncode({'countryId': countryId, 'name': name}),
    );
    if (res.statusCode != 200) {
      throw _badResponse(res);
    }
    return CityItem.fromJson(jsonDecode(res.body) as Map<String, dynamic>);
  }

  Future<CityItem> updateCity(int id, {required int countryId, required String name}) async {
    final res = await _client.put(
      _uri('/api/reference/cities/$id'),
      headers: _headers(jsonBody: true),
      body: jsonEncode({'countryId': countryId, 'name': name}),
    );
    if (res.statusCode != 200) {
      throw _badResponse(res);
    }
    return CityItem.fromJson(jsonDecode(res.body) as Map<String, dynamic>);
  }

  Future<void> deleteCity(int id) async {
    final res = await _client.delete(_uri('/api/reference/cities/$id'), headers: _headers());
    if (res.statusCode != 204) {
      throw _badResponse(res);
    }
  }

  Future<PagedList<NamedReferenceItem>> equipmentTypes({
    int page = 1,
    int pageSize = 100,
    String? q,
  }) async {
    final query = <String, String>{
      'page': '$page',
      'pageSize': '$pageSize',
      if (q != null && q.isNotEmpty) 'q': q,
    };
    final res = await _client.get(_uri('/api/reference/equipment-types', query), headers: _headers());
    _ensureOk(res, 'Tipovi opreme');
    return PagedList.fromJson(jsonDecode(res.body) as Map<String, dynamic>, NamedReferenceItem.fromJson);
  }

  Future<NamedReferenceItem> createEquipmentType(String name) async {
    final res = await _client.post(
      _uri('/api/reference/equipment-types'),
      headers: _headers(jsonBody: true),
      body: jsonEncode({'name': name}),
    );
    if (res.statusCode != 200) {
      throw _badResponse(res);
    }
    return NamedReferenceItem.fromJson(jsonDecode(res.body) as Map<String, dynamic>);
  }

  Future<NamedReferenceItem> updateEquipmentType(int id, String name) async {
    final res = await _client.put(
      _uri('/api/reference/equipment-types/$id'),
      headers: _headers(jsonBody: true),
      body: jsonEncode({'name': name}),
    );
    if (res.statusCode != 200) {
      throw _badResponse(res);
    }
    return NamedReferenceItem.fromJson(jsonDecode(res.body) as Map<String, dynamic>);
  }

  Future<void> deleteEquipmentType(int id) async {
    final res = await _client.delete(_uri('/api/reference/equipment-types/$id'), headers: _headers());
    if (res.statusCode != 204) {
      throw _badResponse(res);
    }
  }

  Future<PagedList<ReferenceItem>> referenceSessionKinds({
    int page = 1,
    int pageSize = 100,
    String? q,
  }) async {
    final query = <String, String>{
      'page': '$page',
      'pageSize': '$pageSize',
      if (q != null && q.isNotEmpty) 'q': q,
    };
    final res = await _client.get(_uri('/api/reference/session-kinds', query), headers: _headers());
    _ensureOk(res, 'Vrste termina');
    return PagedList.fromJson(jsonDecode(res.body) as Map<String, dynamic>, ReferenceItem.fromJson);
  }

  Future<ReferenceItem> createSessionKind({required String code, required String displayName}) async {
    final res = await _client.post(
      _uri('/api/reference/session-kinds'),
      headers: _headers(jsonBody: true),
      body: jsonEncode({'code': code, 'displayName': displayName}),
    );
    if (res.statusCode != 200) {
      throw _badResponse(res);
    }
    return ReferenceItem.fromJson(jsonDecode(res.body) as Map<String, dynamic>);
  }

  Future<ReferenceItem> updateSessionKind(
    int id, {
    required String code,
    required String displayName,
  }) async {
    final res = await _client.put(
      _uri('/api/reference/session-kinds/$id'),
      headers: _headers(jsonBody: true),
      body: jsonEncode({'code': code, 'displayName': displayName}),
    );
    if (res.statusCode != 200) {
      throw _badResponse(res);
    }
    return ReferenceItem.fromJson(jsonDecode(res.body) as Map<String, dynamic>);
  }

  Future<void> deleteSessionKind(int id) async {
    final res = await _client.delete(_uri('/api/reference/session-kinds/$id'), headers: _headers());
    if (res.statusCode != 204) {
      throw _badResponse(res);
    }
  }

  Future<PagedList<ReferenceItem>> referenceSessionLifecycleStatuses({
    int page = 1,
    int pageSize = 100,
    String? q,
  }) async {
    final query = <String, String>{
      'page': '$page',
      'pageSize': '$pageSize',
      if (q != null && q.isNotEmpty) 'q': q,
    };
    final res = await _client.get(
      _uri('/api/reference/session-lifecycle-statuses', query),
      headers: _headers(),
    );
    _ensureOk(res, 'Statusi termina');
    return PagedList.fromJson(jsonDecode(res.body) as Map<String, dynamic>, ReferenceItem.fromJson);
  }

  Future<ReferenceItem> createSessionLifecycleStatus({
    required String code,
    required String displayName,
  }) async {
    final res = await _client.post(
      _uri('/api/reference/session-lifecycle-statuses'),
      headers: _headers(jsonBody: true),
      body: jsonEncode({'code': code, 'displayName': displayName}),
    );
    if (res.statusCode != 200) {
      throw _badResponse(res);
    }
    return ReferenceItem.fromJson(jsonDecode(res.body) as Map<String, dynamic>);
  }

  Future<ReferenceItem> updateSessionLifecycleStatus(
    int id, {
    required String code,
    required String displayName,
  }) async {
    final res = await _client.put(
      _uri('/api/reference/session-lifecycle-statuses/$id'),
      headers: _headers(jsonBody: true),
      body: jsonEncode({'code': code, 'displayName': displayName}),
    );
    if (res.statusCode != 200) {
      throw _badResponse(res);
    }
    return ReferenceItem.fromJson(jsonDecode(res.body) as Map<String, dynamic>);
  }

  Future<void> deleteSessionLifecycleStatus(int id) async {
    final res = await _client.delete(
      _uri('/api/reference/session-lifecycle-statuses/$id'),
      headers: _headers(),
    );
    if (res.statusCode != 204) {
      throw _badResponse(res);
    }
  }

  Future<PagedList<ReferenceItem>> referencePaymentProcessingStatuses({
    int page = 1,
    int pageSize = 100,
    String? q,
  }) async {
    final query = <String, String>{
      'page': '$page',
      'pageSize': '$pageSize',
      if (q != null && q.isNotEmpty) 'q': q,
    };
    final res = await _client.get(
      _uri('/api/reference/payment-processing-statuses', query),
      headers: _headers(),
    );
    _ensureOk(res, 'Statusi plaćanja');
    return PagedList.fromJson(jsonDecode(res.body) as Map<String, dynamic>, ReferenceItem.fromJson);
  }

  Future<ReferenceItem> createPaymentProcessingStatus({
    required String code,
    required String displayName,
  }) async {
    final res = await _client.post(
      _uri('/api/reference/payment-processing-statuses'),
      headers: _headers(jsonBody: true),
      body: jsonEncode({'code': code, 'displayName': displayName}),
    );
    if (res.statusCode != 200) {
      throw _badResponse(res);
    }
    return ReferenceItem.fromJson(jsonDecode(res.body) as Map<String, dynamic>);
  }

  Future<ReferenceItem> updatePaymentProcessingStatus(
    int id, {
    required String code,
    required String displayName,
  }) async {
    final res = await _client.put(
      _uri('/api/reference/payment-processing-statuses/$id'),
      headers: _headers(jsonBody: true),
      body: jsonEncode({'code': code, 'displayName': displayName}),
    );
    if (res.statusCode != 200) {
      throw _badResponse(res);
    }
    return ReferenceItem.fromJson(jsonDecode(res.body) as Map<String, dynamic>);
  }

  Future<void> deletePaymentProcessingStatus(int id) async {
    final res = await _client.delete(
      _uri('/api/reference/payment-processing-statuses/$id'),
      headers: _headers(),
    );
    if (res.statusCode != 204) {
      throw _badResponse(res);
    }
  }

  Future<PagedList<ReferenceItem>> sessionKinds({int pageSize = 50}) async {
    final res = await _client.get(
      _uri('/api/reference/session-kinds', {'page': '1', 'pageSize': '$pageSize'}),
      headers: _headers(),
    );
    _ensureOk(res, 'Vrste termina');
    return PagedList.fromJson(jsonDecode(res.body) as Map<String, dynamic>, ReferenceItem.fromJson);
  }

  Future<PagedList<ReferenceItem>> sessionLifecycleStatuses({int pageSize = 50}) async {
    final res = await _client.get(
      _uri('/api/reference/session-lifecycle-statuses', {'page': '1', 'pageSize': '$pageSize'}),
      headers: _headers(),
    );
    _ensureOk(res, 'Statusi termina');
    return PagedList.fromJson(jsonDecode(res.body) as Map<String, dynamic>, ReferenceItem.fromJson);
  }

  Future<PagedList<ReferenceItem>> paymentProcessingStatuses({int pageSize = 50}) async {
    final res = await _client.get(
      _uri('/api/reference/payment-processing-statuses', {'page': '1', 'pageSize': '$pageSize'}),
      headers: _headers(),
    );
    _ensureOk(res, 'Statusi plaćanja');
    return PagedList.fromJson(jsonDecode(res.body) as Map<String, dynamic>, ReferenceItem.fromJson);
  }

  Future<PagedList<ExternalPaymentListItem>> adminExternalPayments({
    int page = 1,
    int pageSize = 20,
    String? q,
    int? paymentProcessingStatusId,
    String? purposeCode,
    String? provider,
    DateTime? dateFrom,
    DateTime? dateTo,
    bool excludeDemoSeed = false,
  }) async {
    final query = <String, String>{
      'page': '$page',
      'pageSize': '$pageSize',
      if (q != null && q.isNotEmpty) 'q': q,
      if (paymentProcessingStatusId != null) 'paymentProcessingStatusId': '$paymentProcessingStatusId',
      if (purposeCode != null && purposeCode.isNotEmpty) 'purposeCode': purposeCode,
      if (provider != null && provider.isNotEmpty) 'provider': provider,
      if (dateFrom != null) 'dateFromUtc': dateFrom.toUtc().toIso8601String(),
      if (dateTo != null) 'dateToUtc': dateTo.toUtc().toIso8601String(),
      if (excludeDemoSeed) 'excludeDemoSeed': 'true',
    };
    final res = await _client.get(_uri('/api/admin/external-payments', query), headers: _headers());
    _ensureOk(res, 'Uplate');
    return PagedList.fromJson(
      jsonDecode(res.body) as Map<String, dynamic>,
      ExternalPaymentListItem.fromJson,
    );
  }

  Future<PagedList<CoinLedgerListItem>> adminCoinLedger({
    int page = 1,
    int pageSize = 20,
    String? q,
    String? reasonCode,
    DateTime? dateFrom,
    DateTime? dateTo,
    bool excludeDemoSeed = false,
  }) async {
    final query = <String, String>{
      'page': '$page',
      'pageSize': '$pageSize',
      if (q != null && q.isNotEmpty) 'q': q,
      if (reasonCode != null && reasonCode.isNotEmpty) 'reasonCode': reasonCode,
      if (dateFrom != null) 'dateFromUtc': dateFrom.toUtc().toIso8601String(),
      if (dateTo != null) 'dateToUtc': dateTo.toUtc().toIso8601String(),
      if (excludeDemoSeed) 'excludeDemoSeed': 'true',
    };
    final res = await _client.get(_uri('/api/admin/coins/ledger', query), headers: _headers());
    _ensureOk(res, 'Knjiga koina');
    return PagedList.fromJson(
      jsonDecode(res.body) as Map<String, dynamic>,
      CoinLedgerListItem.fromJson,
    );
  }

  Future<PagedList<CoinWalletListItem>> adminCoinWallets({
    int page = 1,
    int pageSize = 20,
    String? q,
  }) async {
    final query = <String, String>{
      'page': '$page',
      'pageSize': '$pageSize',
      if (q != null && q.isNotEmpty) 'q': q,
    };
    final res = await _client.get(_uri('/api/admin/coins/wallets', query), headers: _headers());
    _ensureOk(res, 'Novčanici');
    return PagedList.fromJson(
      jsonDecode(res.body) as Map<String, dynamic>,
      CoinWalletListItem.fromJson,
    );
  }

  Future<PagedList<PlatformSettingItem>> platformSettings({
    int page = 1,
    int pageSize = 50,
    String? q,
  }) async {
    final query = <String, String>{
      'page': '$page',
      'pageSize': '$pageSize',
      if (q != null && q.isNotEmpty) 'q': q,
    };
    final res = await _client.get(_uri('/api/reference/platform-settings', query), headers: _headers());
    _ensureOk(res, 'Postavke');
    return PagedList.fromJson(
      jsonDecode(res.body) as Map<String, dynamic>,
      PlatformSettingItem.fromJson,
    );
  }

  Future<PlatformSettingItem> createPlatformSetting({
    required String settingKey,
    required String settingValue,
  }) async {
    final res = await _client.post(
      _uri('/api/reference/platform-settings'),
      headers: _headers(jsonBody: true),
      body: jsonEncode({'settingKey': settingKey, 'settingValue': settingValue}),
    );
    if (res.statusCode != 200) {
      throw _badResponse(res);
    }
    return PlatformSettingItem.fromJson(jsonDecode(res.body) as Map<String, dynamic>);
  }

  Future<PlatformSettingItem> updatePlatformSetting(
    int id, {
    required String settingKey,
    required String settingValue,
  }) async {
    final res = await _client.put(
      _uri('/api/reference/platform-settings/$id'),
      headers: _headers(jsonBody: true),
      body: jsonEncode({'settingKey': settingKey, 'settingValue': settingValue}),
    );
    if (res.statusCode != 200) {
      throw _badResponse(res);
    }
    return PlatformSettingItem.fromJson(jsonDecode(res.body) as Map<String, dynamic>);
  }

  Future<void> deletePlatformSetting(int id) async {
    final res = await _client.delete(_uri('/api/reference/platform-settings/$id'), headers: _headers());
    if (res.statusCode != 204) {
      throw _badResponse(res);
    }
  }

  Future<List<HallEarningsListItem>> adminHallEarnings({
    DateTime? dateFrom,
    DateTime? dateTo,
  }) async {
    final query = <String, String>{
      if (dateFrom != null) 'dateFromUtc': dateFrom.toUtc().toIso8601String(),
      if (dateTo != null) 'dateToUtc': dateTo.toUtc().toIso8601String(),
    };
    final res = await _client.get(_uri('/api/admin/coins/hall-earnings', query), headers: _headers());
    _ensureOk(res, 'Zarada po dvoranama');
    final list = jsonDecode(res.body) as List<dynamic>;
    return list.map((e) => HallEarningsListItem.fromJson(e as Map<String, dynamic>)).toList();
  }

  Future<Uint8List> downloadAdminReportPdf(
    String reportPath, {
    DateTime? dateFrom,
    DateTime? dateTo,
  }) async {
    final query = <String, String>{
      if (dateFrom != null) 'dateFromUtc': dateFrom.toUtc().toIso8601String(),
      if (dateTo != null) 'dateToUtc': dateTo.toUtc().toIso8601String(),
    };
    final res = await _client.get(_uri(reportPath, query), headers: _headers());
    if (res.statusCode == 401) {
      throw ApiError(401, 'Nemate pristup izvještaju.');
    }
    if (res.statusCode != 200) {
      throw _badResponse(res);
    }
    return res.bodyBytes;
  }

  void _ensureOk(http.Response res, String area) {
    if (res.statusCode == 401) {
      onUnauthorized?.call();
      throw ApiError(401, 'Nemate pristup: $area.');
    }
    if (res.statusCode != 200) {
      throw _badResponse(res);
    }
  }

  Exception _badResponse(http.Response res) {
    try {
      final decoded = jsonDecode(res.body);
      if (decoded is Map<String, dynamic>) {
        final errors = decoded['errors'];
        if (errors is Map<String, dynamic>) {
          for (final entry in errors.entries) {
            final val = entry.value;
            if (val is List && val.isNotEmpty && val.first is String) {
              return ApiError(res.statusCode, val.first as String);
            }
          }
        }
        final detail = decoded['detail'] ?? decoded['title'];
        if (detail is String && detail.isNotEmpty) {
          return ApiError(res.statusCode, detail);
        }
      }
    } catch (_) {}
    return ApiError(res.statusCode, 'Greška servera (${res.statusCode}).');
  }
}

