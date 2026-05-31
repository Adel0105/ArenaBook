import 'package:arena_book_mobile/models/session_models.dart';
import 'package:arena_book_mobile/screens/create_edit_session_screen.dart';
import 'package:arena_book_mobile/services/api_error.dart';
import 'package:arena_book_mobile/services/arena_book_api.dart';
import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

class MyOrganizedSessionsScreen extends StatefulWidget {
  const MyOrganizedSessionsScreen({super.key, required this.api});

  final ArenaBookApi api;

  @override
  State<MyOrganizedSessionsScreen> createState() =>
      _MyOrganizedSessionsScreenState();
}

class _MyOrganizedSessionsScreenState extends State<MyOrganizedSessionsScreen> {
  List<SessionListItem> _items = [];
  bool _loading = true;
  static final _fmt = DateFormat('dd.MM.yyyy HH:mm');

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() => _loading = true);
    try {
      final page = await widget.api.organizedSessions(pageSize: 100);
      if (mounted) {
        setState(() {
          _items = page.items;
          _loading = false;
        });
      }
    } catch (_) {
      if (mounted) {
        setState(() => _loading = false);
      }
    }
  }

  Future<void> _confirm(SessionListItem s) async {
    try {
      await widget.api.confirmSession(s.id);
      _load();
    } on ApiError catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text(e.message)));
      }
    }
  }

  Future<void> _cancel(SessionListItem s) async {
    final reasonCtrl = TextEditingController();
    final ok = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Otkazi termin?'),
        content: TextField(
          controller: reasonCtrl,
          decoration: const InputDecoration(
            labelText: 'Razlog otkazivanja',
            border: OutlineInputBorder(),
          ),
          maxLines: 2,
        ),
        actions: [
          TextButton(
              onPressed: () => Navigator.pop(ctx, false),
              child: const Text('Ne')),
          FilledButton(
              onPressed: () => Navigator.pop(ctx, true),
              child: const Text('Da')),
        ],
      ),
    );
    final reason = reasonCtrl.text.trim();
    reasonCtrl.dispose();
    if (ok != true) {
      return;
    }
    if (reason.isEmpty) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Razlog otkazivanja je obavezan.')),
        );
      }
      return;
    }
    try {
      await widget.api.cancelSession(s.id, reason: reason);
      _load();
    } on ApiError catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text(e.message)));
      }
    }
  }

  Future<void> _complete(SessionListItem s) async {
    try {
      await widget.api.completeSession(s.id);
      _load();
    } on ApiError catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text(e.message)));
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Moji kreirani termini'),
        actions: [
          IconButton(
              icon: const Icon(Icons.add),
              onPressed: () async {
                final ok = await Navigator.of(context).push<bool>(
                  MaterialPageRoute(
                      builder: (_) => CreateEditSessionScreen(api: widget.api)),
                );
                if (ok == true) {
                  _load();
                }
              }),
        ],
      ),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : RefreshIndicator(
              onRefresh: _load,
              child: _items.isEmpty
                  ? ListView(children: const [
                      Center(child: Text('Nemate kreiranih termina.'))
                    ])
                  : ListView.builder(
                      itemCount: _items.length,
                      itemBuilder: (context, i) {
                        final s = _items[i];
                        return Card(
                          child: ExpansionTile(
                            title: Text(s.hallName),
                            subtitle: Text(
                              '${_fmt.format(s.startUtc.toLocal())} · ${s.sessionLifecycleCode} · ${s.sessionKindCode}',
                            ),
                            children: [
                              if (s.inviteCode != null)
                                ListTile(title: Text('Kod: ${s.inviteCode}')),
                              Row(
                                mainAxisAlignment: MainAxisAlignment.end,
                                children: [
                                  if (s.sessionLifecycleCode == 'PENDING')
                                    TextButton(
                                      onPressed: () => _confirm(s),
                                      child: const Text('Potvrdi'),
                                    ),
                                  if (s.sessionLifecycleCode == 'CONFIRMED')
                                    TextButton(
                                      onPressed: () => _complete(s),
                                      child: const Text('Završi'),
                                    ),
                                  TextButton(
                                    onPressed: () async {
                                      final ok = await Navigator.of(context)
                                          .push<bool>(
                                        MaterialPageRoute(
                                          builder: (_) =>
                                              CreateEditSessionScreen(
                                            api: widget.api,
                                            sessionId: s.id,
                                          ),
                                        ),
                                      );
                                      if (ok == true) {
                                        _load();
                                      }
                                    },
                                    child: const Text('Uredi'),
                                  ),
                                  TextButton(
                                    onPressed: () => _cancel(s),
                                    child: const Text('Otkaži'),
                                  ),
                                ],
                              ),
                            ],
                          ),
                        );
                      },
                    ),
            ),
    );
  }
}

