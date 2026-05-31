import 'package:arena_book_desktop/models/current_user.dart';
import 'package:arena_book_desktop/screens/dashboard_screen.dart';
import 'package:arena_book_desktop/screens/halls_screen.dart';
import 'package:arena_book_desktop/screens/payments_reports_screen.dart';
import 'package:arena_book_desktop/screens/platform_settings_screen.dart';
import 'package:arena_book_desktop/screens/reference_data_screen.dart';
import 'package:arena_book_desktop/screens/sessions_screen.dart';
import 'package:arena_book_desktop/screens/users_screen.dart';
import 'package:arena_book_desktop/services/arena_book_api.dart';
import 'package:arena_book_desktop/widgets/admin_sidebar_profile.dart';
import 'package:flutter/material.dart';

class AdminShell extends StatefulWidget {
  const AdminShell({
    super.key,
    required this.api,
    required this.user,
    required this.onLogout,
  });

  final ArenaBookApi api;
  final CurrentUser user;
  final Future<void> Function() onLogout;

  @override
  State<AdminShell> createState() => _AdminShellState();
}

class _AdminShellState extends State<AdminShell> {
  int _index = 0;
  bool _sidebarInteractive = false;

  static const _railBg = Color(0xFF0F172A);
  static const _railFg = Color(0xFFE2E8F0);
  static const _railMuted = Color(0xFF94A3B8);
  static const _railAccent = Color(0xFF4ADE80);
  static const _railIndicator = Color(0xFF1E293B);

  static const _navItems = [
    _NavItemData(0, Icons.dashboard_outlined, Icons.dashboard, 'Nadzorna ploča'),
    _NavItemData(1, Icons.apartment_outlined, Icons.apartment, 'Dvorane'),
    _NavItemData(2, Icons.event_note_outlined, Icons.event_note, 'Termini'),
    _NavItemData(3, Icons.group_outlined, Icons.group, 'Korisnici'),
    _NavItemData(4, Icons.payments_outlined, Icons.payments, 'Uplate'),
    _NavItemData(5, Icons.list_alt_outlined, Icons.list_alt, 'Šifarnici'),
    _NavItemData(6, Icons.settings_outlined, Icons.settings, 'Postavke'),
  ];

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (mounted) {
        setState(() => _sidebarInteractive = true);
      }
    });
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final railExtended = MediaQuery.sizeOf(context).width >= 1100;
    final railWidth = railExtended ? 220.0 : 72.0;

    Widget body;
    switch (_index) {
      case 0:
        body = DashboardScreen(api: widget.api);
        break;
      case 1:
        body = HallsScreen(api: widget.api);
        break;
      case 2:
        body = SessionsScreen(api: widget.api);
        break;
      case 3:
        body = UsersScreen(api: widget.api);
        break;
      case 4:
        body = PaymentsReportsScreen(api: widget.api);
        break;
      case 5:
        body = ReferenceDataScreen(api: widget.api);
        break;
      case 6:
        body = PlatformSettingsScreen(api: widget.api);
        break;
      default:
        body = DashboardScreen(api: widget.api);
    }

    return Scaffold(
      body: Row(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          SizedBox(
            width: railWidth,
            child: IgnorePointer(
              ignoring: !_sidebarInteractive,
              child: ColoredBox(
                color: _railBg,
                child: Column(
                  children: [
                    Padding(
                      padding: EdgeInsets.fromLTRB(railExtended ? 16 : 12, 20, 12, 16),
                      child: railExtended
                          ? const Row(
                              children: [
                                Icon(Icons.stadium, color: _railAccent, size: 28),
                                SizedBox(width: 10),
                                Text(
                                  'ArenaBook',
                                  style: TextStyle(
                                    color: _railFg,
                                    fontWeight: FontWeight.w700,
                                    fontSize: 16,
                                  ),
                                ),
                              ],
                            )
                          : const Center(
                              child: Icon(Icons.stadium, color: _railAccent, size: 28),
                            ),
                    ),
                    Expanded(
                      child: SingleChildScrollView(
                        padding: const EdgeInsets.symmetric(horizontal: 8),
                        child: Column(
                          children: [
                            for (final item in _navItems)
                              _SidebarNavTile(
                                item: item,
                                selected: _index == item.index,
                                extended: railExtended,
                                onTap: () => setState(() => _index = item.index),
                              ),
                          ],
                        ),
                      ),
                    ),
                    AdminSidebarProfile(
                      user: widget.user,
                      onLogout: widget.onLogout,
                      extended: railExtended,
                    ),
                  ],
                ),
              ),
            ),
          ),
          const VerticalDivider(width: 1, thickness: 1),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Material(
                  color: theme.colorScheme.surface,
                  child: SizedBox(
                    height: 64,
                    child: Align(
                      alignment: Alignment.centerLeft,
                      child: Padding(
                        padding: const EdgeInsets.only(left: 24),
                        child: Text(
                          _titleForIndex(_index),
                          style: theme.textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w600),
                        ),
                      ),
                    ),
                  ),
                ),
                Expanded(
                  child: ColoredBox(
                    color: const Color(0xFFF8FAFC),
                    child: body,
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  String _titleForIndex(int i) {
    switch (i) {
      case 0:
        return 'Nadzorna ploča';
      case 1:
        return 'Dvorane';
      case 2:
        return 'Termini';
      case 3:
        return 'Korisnici';
      case 4:
        return 'Uplate i izvještaji';
      case 5:
        return 'Referentni podaci';
      case 6:
        return 'Postavke platforme';
      default:
        return 'ArenaBook';
    }
  }
}

class _NavItemData {
  const _NavItemData(this.index, this.icon, this.selectedIcon, this.label);

  final int index;
  final IconData icon;
  final IconData selectedIcon;
  final String label;
}

class _SidebarNavTile extends StatelessWidget {
  const _SidebarNavTile({
    required this.item,
    required this.selected,
    required this.extended,
    required this.onTap,
  });

  final _NavItemData item;
  final bool selected;
  final bool extended;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final icon = selected ? item.selectedIcon : item.icon;
    final color = selected ? _AdminShellState._railAccent : _AdminShellState._railMuted;
    final labelColor = selected ? _AdminShellState._railFg : _AdminShellState._railMuted;

    return Padding(
      padding: const EdgeInsets.only(bottom: 4),
      child: Material(
        color: selected ? _AdminShellState._railIndicator : Colors.transparent,
        borderRadius: BorderRadius.circular(10),
        child: InkWell(
          onTap: onTap,
          borderRadius: BorderRadius.circular(10),
          child: Padding(
            padding: EdgeInsets.symmetric(horizontal: extended ? 14 : 0, vertical: 12),
            child: extended
                ? Row(
                    children: [
                      Icon(icon, color: color, size: 22),
                      const SizedBox(width: 12),
                      Expanded(
                        child: Text(
                          item.label,
                          style: TextStyle(
                            color: labelColor,
                            fontWeight: selected ? FontWeight.w600 : FontWeight.w500,
                            fontSize: 14,
                          ),
                        ),
                      ),
                    ],
                  )
                : Center(
                    child: Icon(icon, color: color, size: 24),
                  ),
          ),
        ),
      ),
    );
  }
}

