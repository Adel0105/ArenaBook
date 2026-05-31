import 'package:arena_book_desktop/models/current_user.dart';
import 'package:flutter/material.dart';

class AdminSidebarProfile extends StatelessWidget {
  const AdminSidebarProfile({
    super.key,
    required this.user,
    required this.onLogout,
    required this.extended,
  });

  final CurrentUser user;
  final Future<void> Function() onLogout;
  final bool extended;

  @override
  Widget build(BuildContext context) {
    const railFg = Color(0xFFE2E8F0);
    const railMuted = Color(0xFF94A3B8);
    final initial = user.firstName.isNotEmpty
        ? user.firstName[0].toUpperCase()
        : user.email[0].toUpperCase();

    final avatar = CircleAvatar(
      radius: 22,
      backgroundColor: const Color(0xFF16A34A),
      child: Text(
        initial,
        style: const TextStyle(color: Colors.white, fontWeight: FontWeight.w700, fontSize: 18),
      ),
    );

    if (!extended) {
      return SafeArea(
        top: false,
        child: Padding(
          padding: const EdgeInsets.only(bottom: 16),
          child: Center(
            child: Material(
              color: Colors.transparent,
              child: InkWell(
                onTap: () => _confirmLogout(context),
                customBorder: const CircleBorder(),
                child: avatar,
              ),
            ),
          ),
        ),
      );
    }

    return SafeArea(
      top: false,
      child: Padding(
        padding: const EdgeInsets.fromLTRB(8, 8, 8, 16),
        child: Material(
          color: const Color(0xFF1E293B),
          borderRadius: BorderRadius.circular(12),
          child: Padding(
            padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 10),
            child: Row(
              children: [
                avatar,
                const SizedBox(width: 10),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Text(
                        user.displayName,
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: const TextStyle(
                          color: railFg,
                          fontWeight: FontWeight.w600,
                          fontSize: 14,
                        ),
                      ),
                      const SizedBox(height: 2),
                      const Text(
                        'Administrator',
                        style: TextStyle(color: railMuted, fontSize: 12),
                      ),
                    ],
                  ),
                ),
                IconButton(
                  tooltip: 'Odjava',
                  onPressed: () => _confirmLogout(context),
                  icon: const Icon(Icons.logout, color: railMuted, size: 20),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }

  Future<void> _confirmLogout(BuildContext context) async {
    final ok = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Odjava'),
        content: const Text('Želite li se odjaviti?'),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('Odustani')),
          FilledButton(onPressed: () => Navigator.pop(ctx, true), child: const Text('Odjavi se')),
        ],
      ),
    );
    if (ok == true) {
      await onLogout();
    }
  }
}

