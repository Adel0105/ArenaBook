import 'dart:async';

import 'package:arena_book_mobile/models/notification_models.dart';
import 'package:arena_book_mobile/services/arena_book_api.dart';
import 'package:flutter/material.dart';

class NotificationPollingController extends ChangeNotifier with WidgetsBindingObserver {
  NotificationPollingController(this._api);

  static const pollInterval = Duration(seconds: 30);

  final ArenaBookApi _api;
  Timer? _timer;
  bool _active = false;

  List<UserNotification> items = [];
  int unreadCount = 0;
  bool polling = false;

  void start() {
    if (_active) {
      return;
    }
    _active = true;
    WidgetsBinding.instance.addObserver(this);
    _schedulePolling();
    refresh(silent: true);
  }

  void stop() {
    _active = false;
    _timer?.cancel();
    _timer = null;
    WidgetsBinding.instance.removeObserver(this);
  }

  @override
  void dispose() {
    stop();
    super.dispose();
  }

  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    if (!_active) {
      return;
    }
    if (state == AppLifecycleState.resumed) {
      _schedulePolling();
      refresh(silent: true);
    } else if (state == AppLifecycleState.paused ||
        state == AppLifecycleState.inactive) {
      _timer?.cancel();
      _timer = null;
    }
  }

  void _schedulePolling() {
    _timer?.cancel();
    _timer = Timer.periodic(pollInterval, (_) => refresh(silent: true));
  }

  Future<void> refresh({bool silent = false}) async {
    if (!silent) {
      polling = true;
      notifyListeners();
    }
    try {
      final page = await _api.notifications();
      items = page.items;
      unreadCount = page.items.where((n) => !n.isRead).length;
    } catch (_) {}
    polling = false;
    notifyListeners();
  }

  Future<void> markRead(int id) async {
    await _api.markNotificationRead(id);
    await refresh(silent: true);
  }

  Future<void> markAllRead() async {
    await _api.markAllNotificationsRead();
    await refresh(silent: true);
  }
}
