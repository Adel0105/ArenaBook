import 'package:arena_book_mobile/services/arena_book_api.dart';
import 'package:arena_book_mobile/services/notification_polling_controller.dart';
import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

class NotificationsScreen extends StatefulWidget {
  const NotificationsScreen({
    super.key,
    required this.api,
    required this.notifications,
  });

  final ArenaBookApi api;
  final NotificationPollingController notifications;

  @override
  State<NotificationsScreen> createState() => _NotificationsScreenState();
}

class _NotificationsScreenState extends State<NotificationsScreen> {
  static final _fmt = DateFormat('dd.MM.yyyy HH:mm');

  @override
  void initState() {
    super.initState();
    widget.notifications.addListener(_onPollUpdate);
    if (widget.notifications.items.isEmpty) {
      widget.notifications.refresh();
    }
  }

  @override
  void dispose() {
    widget.notifications.removeListener(_onPollUpdate);
    super.dispose();
  }

  void _onPollUpdate() {
    if (mounted) {
      setState(() {});
    }
  }

  Future<void> _markAllRead() async {
    await widget.notifications.markAllRead();
  }

  @override
  Widget build(BuildContext context) {
    final items = widget.notifications.items;
    final loading = widget.notifications.polling && items.isEmpty;

    return Scaffold(
      appBar: AppBar(
        title: const Text('Notifikacije'),
        actions: [
          if (widget.notifications.polling && items.isNotEmpty)
            const Padding(
              padding: EdgeInsets.only(right: 8),
              child: SizedBox(
                width: 18,
                height: 18,
                child: CircularProgressIndicator(strokeWidth: 2),
              ),
            ),
          TextButton(
            onPressed: items.isEmpty ? null : _markAllRead,
            child: const Text('Pročitaj sve'),
          ),
        ],
      ),
      body: loading
          ? const Center(child: CircularProgressIndicator())
          : RefreshIndicator(
              onRefresh: () => widget.notifications.refresh(),
              child: items.isEmpty
                  ? ListView(
                      physics: const AlwaysScrollableScrollPhysics(),
                      children: const [
                        SizedBox(height: 48),
                        Center(child: Text('Nema notifikacija.')),
                      ],
                    )
                  : ListView.builder(
                      physics: const AlwaysScrollableScrollPhysics(),
                      itemCount: items.length,
                      itemBuilder: (context, i) {
                        final n = items[i];
                        return ListTile(
                          leading: Icon(
                            n.isRead
                                ? Icons.notifications_none
                                : Icons.notifications_active,
                            color: n.isRead
                                ? null
                                : Theme.of(context).colorScheme.primary,
                          ),
                          title: Text(
                            n.title,
                            style: TextStyle(
                              fontWeight:
                                  n.isRead ? FontWeight.normal : FontWeight.bold,
                            ),
                          ),
                          subtitle: Text(
                            '${n.body}\n${_fmt.format(n.createdUtc.toLocal())}',
                          ),
                          isThreeLine: true,
                          onTap: () async {
                            if (!n.isRead) {
                              await widget.notifications.markRead(n.id);
                            }
                          },
                        );
                      },
                    ),
            ),
    );
  }
}
