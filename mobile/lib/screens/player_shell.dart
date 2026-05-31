import 'package:arena_book_mobile/core/app_currency.dart';

import 'package:arena_book_mobile/models/current_user.dart';

import 'package:arena_book_mobile/screens/coins_screen.dart';

import 'package:arena_book_mobile/screens/halls_screen.dart';

import 'package:arena_book_mobile/screens/home_screen.dart';

import 'package:arena_book_mobile/screens/profile_screen.dart';

import 'package:arena_book_mobile/screens/sessions_screen.dart';

import 'package:arena_book_mobile/services/arena_book_api.dart';

import 'package:arena_book_mobile/services/notification_polling_controller.dart';

import 'package:flutter/material.dart';

class PlayerShell extends StatefulWidget {
  const PlayerShell({
    super.key,
    required this.api,
    required this.user,
    required this.onLogout,
    required this.onUserUpdated,
  });

  final ArenaBookApi api;

  final CurrentUser user;

  final VoidCallback onLogout;

  final void Function(CurrentUser user) onUserUpdated;

  @override
  State<PlayerShell> createState() => _PlayerShellState();
}

class _PlayerShellState extends State<PlayerShell> {
  int _index = 0;
  late final NotificationPollingController _notifications;

  @override
  void initState() {
    super.initState();
    _notifications = NotificationPollingController(widget.api);
    _notifications.start();
  }

  @override
  void dispose() {
    _notifications.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final pages = [
      HomeScreen(
        api: widget.api,
        user: widget.user,
        notifications: _notifications,
      ),
      HallsScreen(api: widget.api, user: widget.user),
      SessionsScreen(api: widget.api),
      CoinsScreen(api: widget.api, isActive: _index == 3),
      ProfileScreen(
        api: widget.api,
        user: widget.user,
        onLogout: widget.onLogout,
        onUserUpdated: widget.onUserUpdated,
      ),
    ];

    return Scaffold(
      body: pages[_index],
      bottomNavigationBar: NavigationBar(
        selectedIndex: _index,
        onDestinationSelected: (i) => setState(() => _index = i),
        destinations: [
          const NavigationDestination(
            icon: Icon(Icons.home_outlined),
            selectedIcon: Icon(Icons.home),
            label: 'Početna',
          ),
          const NavigationDestination(
            icon: Icon(Icons.stadium_outlined),
            selectedIcon: Icon(Icons.stadium),
            label: 'Dvorane',
          ),
          const NavigationDestination(
            icon: Icon(Icons.event_outlined),
            selectedIcon: Icon(Icons.event),
            label: 'Termini',
          ),
          NavigationDestination(
            icon: const Icon(Icons.toll_outlined),
            selectedIcon: const Icon(Icons.toll),
            label: AppCurrency.tabLabel,
          ),
          const NavigationDestination(
            icon: Icon(Icons.person_outline),
            selectedIcon: Icon(Icons.person),
            label: 'Profil',
          ),
        ],
      ),
    );
  }
}

