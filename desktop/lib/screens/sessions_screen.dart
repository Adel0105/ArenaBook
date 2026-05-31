import 'package:arena_book_desktop/models/hall_models.dart';
import 'package:arena_book_desktop/models/paged_list.dart';
import 'package:arena_book_desktop/models/session_models.dart';
import 'package:arena_book_desktop/services/api_error.dart';
import 'package:arena_book_desktop/services/arena_book_api.dart';
import 'package:arena_book_desktop/widgets/paged_footer.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:intl/intl.dart';

class SessionsScreen extends StatefulWidget {
  const SessionsScreen({super.key, required this.api});

  final ArenaBookApi api;

  @override
  State<SessionsScreen> createState() => _SessionsScreenState();
}

class _SessionsScreenState extends State<SessionsScreen> {
  static final _participantFmt = DateFormat('dd.MM.yyyy HH:mm');

  final _searchCtrl = TextEditingController();
  final _organizerCtrl = TextEditingController();
  PagedList<SessionListItem>? _page;
  List<HallListItem> _halls = [];
  List<ReferenceItem> _kinds = [];
  List<ReferenceItem> _statuses = [];
  int _listPage = 1;
  int? _filterHallId;
  int? _filterStatusId;
  DateTime? _filterFrom;
  DateTime? _filterTo;
  bool _loadingList = true;
  String? _listError;

  SessionDetails? _selected;
  bool _isNew = false;
  bool _loadingDetail = false;
  bool _saving = false;
  String? _detailError;

  final _maxParticipantsCtrl = TextEditingController();
  final _maxAgeCtrl = TextEditingController();
  final _inviteCtrl = TextEditingController();
  final _organizerIdCtrl = TextEditingController();
  int? _formHallId;
  int? _formKindId;
  DateTime? _formStart;
  DateTime? _formEnd;

  static final _dateFmt = DateFormat('dd.MM.yyyy HH:mm');

  @override
  void initState() {
    super.initState();
    _bootstrap();
  }

  @override
  void dispose() {
    _searchCtrl.dispose();
    _organizerCtrl.dispose();
    _maxParticipantsCtrl.dispose();
    _maxAgeCtrl.dispose();
    _inviteCtrl.dispose();
    _organizerIdCtrl.dispose();
    super.dispose();
  }

  Future<void> _bootstrap() async {
    await Future.wait([
      _loadReference(),
      _loadList(),
    ]);
  }

  Future<void> _loadReference() async {
    try {
      final halls = await widget.api.halls(page: 1, pageSize: 100, isActive: true);
      final kinds = await widget.api.sessionKinds();
      final statuses = await widget.api.sessionLifecycleStatuses();
      if (!mounted) {
        return;
      }
      setState(() {
        _halls = halls.items;
        _kinds = kinds.items;
        _statuses = statuses.items;
      });
    } catch (_) {}
  }

  Future<void> _loadList({int? page}) async {
    final p = page ?? _listPage;
    setState(() {
      _loadingList = true;
      _listError = null;
    });
    try {
      final res = await widget.api.sessions(
        page: p,
        pageSize: 15,
        q: _searchCtrl.text.trim().isEmpty ? null : _searchCtrl.text.trim(),
        hallId: _filterHallId,
        sessionLifecycleStatusId: _filterStatusId,
        organizerUserId: _organizerCtrl.text.trim().isEmpty ? null : _organizerCtrl.text.trim(),
        dateFrom: _filterFrom,
        dateTo: _filterTo,
      );
      if (!mounted) {
        return;
      }
      setState(() {
        _page = res;
        _listPage = p;
        _loadingList = false;
      });
    } on ApiError catch (e) {
      if (!mounted) {
        return;
      }
      setState(() {
        _listError = e.message;
        _loadingList = false;
      });
    } catch (_) {
      if (!mounted) {
        return;
      }
      setState(() {
        _listError = 'Greška pri učitavanju termina.';
        _loadingList = false;
      });
    }
  }

  void _startNew() {
    final now = DateTime.now().toUtc();
    setState(() {
      _isNew = true;
      _selected = null;
      _detailError = null;
      _formHallId = _halls.isNotEmpty ? _halls.first.id : null;
      _formKindId = _kinds.isNotEmpty ? _kinds.first.id : null;
      _formStart = DateTime.utc(now.year, now.month, now.day + 1, 18);
      _formEnd = DateTime.utc(now.year, now.month, now.day + 1, 20);
      _maxParticipantsCtrl.text = '10';
      _maxAgeCtrl.clear();
      _inviteCtrl.clear();
      _organizerIdCtrl.clear();
    });
  }

  Future<void> _selectSession(int id) async {
    setState(() {
      _isNew = false;
      _loadingDetail = true;
      _detailError = null;
    });
    try {
      final d = await widget.api.sessionById(id);
      if (!mounted) {
        return;
      }
      _fillForm(d);
      setState(() {
        _selected = d;
        _loadingDetail = false;
      });
    } on ApiError catch (e) {
      if (!mounted) {
        return;
      }
      setState(() {
        _detailError = e.message;
        _loadingDetail = false;
      });
    } catch (_) {
      if (!mounted) {
        return;
      }
      setState(() {
        _detailError = 'Nije moguće učitati termin.';
        _loadingDetail = false;
      });
    }
  }

  void _fillForm(SessionDetails d) {
    _formHallId = d.hallId;
    _formKindId = d.sessionKindId;
    _formStart = d.startUtc;
    _formEnd = d.endUtc;
    _maxParticipantsCtrl.text = '${d.maxParticipants}';
    _maxAgeCtrl.text = d.maxAgeYears?.toString() ?? '';
    _inviteCtrl.text = d.inviteCode ?? '';
    _organizerIdCtrl.text = d.organizerUserId;
  }

  Future<void> _pickDateTime({required bool isStart}) async {
    final initial = (isStart ? _formStart : _formEnd)?.toLocal() ?? DateTime.now();
    final date = await showDatePicker(
      context: context,
      initialDate: initial,
      firstDate: DateTime(2020),
      lastDate: DateTime(2035),
    );
    if (date == null || !mounted) {
      return;
    }
    if (!context.mounted) {
      return;
    }
    final time = await showTimePicker(context: context, initialTime: TimeOfDay.fromDateTime(initial));
    if (time == null) {
      return;
    }
    final local = DateTime(date.year, date.month, date.day, time.hour, time.minute);
    setState(() {
      if (isStart) {
        _formStart = local.toUtc();
      } else {
        _formEnd = local.toUtc();
      }
    });
  }

  Future<void> _save() async {
    if (_formStart == null || _formEnd == null) {
      setState(() => _detailError = 'Odaberite početak i kraj termina.');
      return;
    }
    setState(() {
      _saving = true;
      _detailError = null;
    });
    try {
      if (_isNew) {
        if (_formHallId == null || _formKindId == null) {
          throw ApiError(400, 'Odaberite dvoranu i vrstu termina.');
        }
        final body = <String, dynamic>{
          'hallId': _formHallId,
          'sessionKindId': _formKindId,
          'startUtc': _formStart!.toIso8601String(),
          'endUtc': _formEnd!.toIso8601String(),
          'maxParticipants': int.tryParse(_maxParticipantsCtrl.text.trim()) ?? 1,
          if (_maxAgeCtrl.text.trim().isNotEmpty) 'maxAgeYears': int.parse(_maxAgeCtrl.text.trim()),
          if (_inviteCtrl.text.trim().isNotEmpty) 'inviteCode': _inviteCtrl.text.trim(),
          if (_organizerIdCtrl.text.trim().isNotEmpty) 'organizerUserId': _organizerIdCtrl.text.trim(),
        };
        final created = await widget.api.createSession(body);
        if (!mounted) {
          return;
        }
        setState(() {
          _isNew = false;
          _selected = created;
          _saving = false;
        });
        _fillForm(created);
      } else if (_selected != null) {
        final body = {
          'startUtc': _formStart!.toIso8601String(),
          'endUtc': _formEnd!.toIso8601String(),
          'maxParticipants': int.tryParse(_maxParticipantsCtrl.text.trim()) ?? _selected!.maxParticipants,
          if (_maxAgeCtrl.text.trim().isNotEmpty) 'maxAgeYears': int.parse(_maxAgeCtrl.text.trim()),
        };
        final updated = await widget.api.updateSession(_selected!.id, body);
        if (!mounted) {
          return;
        }
        setState(() {
          _selected = updated;
          _saving = false;
        });
        _fillForm(updated);
      }
      await _loadList(page: _listPage);
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Termin spremljen.')));
      }
    } on ApiError catch (e) {
      if (!mounted) {
        return;
      }
      setState(() {
        _detailError = e.message;
        _saving = false;
      });
    } catch (_) {
      if (!mounted) {
        return;
      }
      setState(() {
        _detailError = 'Spremanje nije uspjelo.';
        _saving = false;
      });
    }
  }

  Future<void> _confirm() async {
    final id = _selected?.id;
    if (id == null) {
      return;
    }
    setState(() => _saving = true);
    try {
      await widget.api.confirmSession(id);
      await _selectSession(id);
      await _loadList(page: _listPage);
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Termin potvrđen.')));
      }
    } on ApiError catch (e) {
      if (!mounted) {
        return;
      }
      setState(() => _detailError = e.message);
    } finally {
      if (mounted) {
        setState(() => _saving = false);
      }
    }
  }

  Future<void> _cancel() async {
    final id = _selected?.id;
    if (id == null) {
      return;
    }
    final reasonCtrl = TextEditingController();
    final ok = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Moderacija termina'),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text('Otkazati ovaj termin? Učesnici više neće moći koristiti rezervaciju.'),
            const SizedBox(height: 12),
            TextField(
              controller: reasonCtrl,
              decoration: const InputDecoration(
                labelText: 'Razlog otkazivanja',
                border: OutlineInputBorder(),
              ),
              maxLines: 2,
            ),
          ],
        ),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('Odustani')),
          FilledButton(onPressed: () => Navigator.pop(ctx, true), child: const Text('Otkaži termin')),
        ],
      ),
    );
    final reason = reasonCtrl.text.trim();
    reasonCtrl.dispose();
    if (ok != true) {
      return;
    }
    if (reason.isEmpty) {
      setState(() => _detailError = 'Razlog otkazivanja je obavezan.');
      return;
    }
    setState(() => _saving = true);
    try {
      await widget.api.cancelSession(id, reason: reason);
      await _selectSession(id);
      await _loadList(page: _listPage);
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Termin otkazan.')));
      }
    } on ApiError catch (e) {
      if (!mounted) {
        return;
      }
      setState(() => _detailError = e.message);
    } finally {
      if (mounted) {
        setState(() => _saving = false);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.all(16),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          SizedBox(width: 400, child: _buildListPanel(context)),
          const SizedBox(width: 16),
          Expanded(child: _buildDetailPanel(context)),
        ],
      ),
    );
  }

  Widget _buildListPanel(BuildContext context) {
    final theme = Theme.of(context);
    return Card(
      elevation: 0,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(12),
        side: BorderSide(color: theme.colorScheme.outlineVariant),
      ),
      child: Column(
        children: [
          Padding(
            padding: const EdgeInsets.all(12),
            child: Column(
              children: [
                TextField(
                  controller: _searchCtrl,
                  decoration: const InputDecoration(
                    hintText: 'Pretraži termine…',
                    prefixIcon: Icon(Icons.search),
                    isDense: true,
                    border: OutlineInputBorder(),
                  ),
                  onSubmitted: (_) => _loadList(page: 1),
                ),
                const SizedBox(height: 8),
                TextField(
                  controller: _organizerCtrl,
                  decoration: const InputDecoration(
                    labelText: 'Organizator (ID)',
                    isDense: true,
                    border: OutlineInputBorder(),
                  ),
                  onSubmitted: (_) => _loadList(page: 1),
                ),
                const SizedBox(height: 8),
                Row(
                  children: [
                    Expanded(
                      child: DropdownButtonFormField<int?>(
                        isExpanded: true,
                        value: _filterHallId,
                        decoration: const InputDecoration(labelText: 'Dvorana', isDense: true, border: OutlineInputBorder()),
                        items: [
                          const DropdownMenuItem(value: null, child: Text('Sve')),
                          for (final h in _halls)
                            DropdownMenuItem(
                              value: h.id,
                              child: Text(h.name, overflow: TextOverflow.ellipsis),
                            ),
                        ],
                        onChanged: (v) {
                          setState(() => _filterHallId = v);
                          _loadList(page: 1);
                        },
                      ),
                    ),
                    const SizedBox(width: 8),
                    Expanded(
                      child: DropdownButtonFormField<int?>(
                        isExpanded: true,
                        value: _filterStatusId,
                        decoration: const InputDecoration(labelText: 'Status', isDense: true, border: OutlineInputBorder()),
                        items: [
                          const DropdownMenuItem(value: null, child: Text('Svi')),
                          for (final s in _statuses)
                            DropdownMenuItem(
                              value: s.id,
                              child: Text(s.displayName, overflow: TextOverflow.ellipsis),
                            ),
                        ],
                        onChanged: (v) {
                          setState(() => _filterStatusId = v);
                          _loadList(page: 1);
                        },
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 8),
                Align(
                  alignment: Alignment.centerRight,
                  child: FilledButton.icon(
                    onPressed: _startNew,
                    icon: const Icon(Icons.add, size: 18),
                    label: const Text('Novi termin'),
                  ),
                ),
              ],
            ),
          ),
          if (_listError != null)
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 12),
              child: Text(_listError!, style: TextStyle(color: theme.colorScheme.error)),
            ),
          Expanded(
            child: _loadingList
                ? const Center(child: CircularProgressIndicator())
                : ListView.builder(
                    itemCount: _page?.items.length ?? 0,
                    itemBuilder: (context, i) {
                      final item = _page!.items[i];
                      final selected = !_isNew && _selected?.id == item.id;
                      return ListTile(
                        selected: selected,
                        title: Text(item.hallName, maxLines: 1, overflow: TextOverflow.ellipsis),
                        subtitle: Text(
                          '${_dateFmt.format(item.startUtc.toLocal())}\n${item.sessionLifecycleCode}',
                          style: theme.textTheme.bodySmall,
                        ),
                        isThreeLine: true,
                        onTap: () => _selectSession(item.id),
                      );
                    },
                  ),
          ),
          if (_page != null)
            PagedFooter(
              page: _page!.page,
              totalPages: _page!.totalPages,
              totalCount: _page!.totalCount,
              isLoading: _loadingList,
              onPageChanged: (p) => _loadList(page: p),
            ),
        ],
      ),
    );
  }

  Widget _buildDetailPanel(BuildContext context) {
    final theme = Theme.of(context);
    if (!_isNew && _selected == null) {
      return Card(
        elevation: 0,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(12),
          side: BorderSide(color: theme.colorScheme.outlineVariant),
        ),
        child: Center(
          child: Text(
            'Odaberite termin s popisa ili kreirajte novi.',
            style: theme.textTheme.bodyLarge?.copyWith(color: theme.colorScheme.onSurfaceVariant),
          ),
        ),
      );
    }

    final d = _selected;
    final canConfirm = d != null && d.sessionLifecycleCode.toLowerCase() == 'pending';
    final canCancel = d != null && !d.sessionLifecycleCode.toLowerCase().contains('cancel');

    return Card(
      elevation: 0,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(12),
        side: BorderSide(color: theme.colorScheme.outlineVariant),
      ),
      child: _loadingDetail
          ? const Center(child: CircularProgressIndicator())
          : SingleChildScrollView(
              padding: const EdgeInsets.all(20),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    _isNew ? 'Novi termin' : '${d!.hallName} · ${d.sessionLifecycleCode}',
                    style: theme.textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w600),
                  ),
                  if (!_isNew && d != null) ...[
                    const SizedBox(height: 4),
                    Text(
                      '${d.hallName} · ${d.organizerEmail ?? d.organizerUserId}',
                      style: theme.textTheme.bodyMedium?.copyWith(color: theme.colorScheme.onSurfaceVariant),
                    ),
                  ],
                  if (_detailError != null) ...[
                    const SizedBox(height: 12),
                    Text(_detailError!, style: TextStyle(color: theme.colorScheme.error)),
                  ],
                  const SizedBox(height: 16),
                  if (_isNew) ...[
                    DropdownButtonFormField<int>(
                      isExpanded: true,
                      value: _formHallId,
                      decoration: const InputDecoration(labelText: 'Dvorana', border: OutlineInputBorder()),
                      items: [
                        for (final h in _halls)
                          DropdownMenuItem(
                            value: h.id,
                            child: Text(h.name, overflow: TextOverflow.ellipsis),
                          ),
                      ],
                      onChanged: (v) => setState(() => _formHallId = v),
                    ),
                    const SizedBox(height: 12),
                    DropdownButtonFormField<int>(
                      isExpanded: true,
                      value: _formKindId,
                      decoration: const InputDecoration(labelText: 'Vrsta', border: OutlineInputBorder()),
                      items: [
                        for (final k in _kinds)
                          DropdownMenuItem(
                            value: k.id,
                            child: Text(k.displayName, overflow: TextOverflow.ellipsis),
                          ),
                      ],
                      onChanged: (v) => setState(() => _formKindId = v),
                    ),
                    const SizedBox(height: 12),
                    TextField(
                      controller: _organizerIdCtrl,
                      decoration: const InputDecoration(
                        labelText: 'Organizator (korisnički ID, opcionalno)',
                        border: OutlineInputBorder(),
                      ),
                    ),
                    const SizedBox(height: 12),
                  ],
                  Row(
                    children: [
                      Expanded(
                        child: OutlinedButton(
                          onPressed: () => _pickDateTime(isStart: true),
                          child: Text(
                            _formStart == null ? 'Početak' : _dateFmt.format(_formStart!.toLocal()),
                          ),
                        ),
                      ),
                      const SizedBox(width: 12),
                      Expanded(
                        child: OutlinedButton(
                          onPressed: () => _pickDateTime(isStart: false),
                          child: Text(
                            _formEnd == null ? 'Kraj' : _dateFmt.format(_formEnd!.toLocal()),
                          ),
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 12),
                  Row(
                    children: [
                      Expanded(
                        child: TextField(
                          controller: _maxParticipantsCtrl,
                          decoration: const InputDecoration(labelText: 'Maks. sudionika', border: OutlineInputBorder()),
                          keyboardType: TextInputType.number,
                          inputFormatters: [FilteringTextInputFormatter.digitsOnly],
                        ),
                      ),
                      const SizedBox(width: 12),
                      Expanded(
                        child: TextField(
                          controller: _maxAgeCtrl,
                          decoration: const InputDecoration(labelText: 'Maks. dob (god)', border: OutlineInputBorder()),
                          keyboardType: TextInputType.number,
                          inputFormatters: [FilteringTextInputFormatter.digitsOnly],
                        ),
                      ),
                    ],
                  ),
                  if (_isNew) ...[
                    const SizedBox(height: 12),
                    TextField(
                      controller: _inviteCtrl,
                      decoration: const InputDecoration(labelText: 'Pozivni kod (opcionalno)', border: OutlineInputBorder()),
                    ),
                  ],
                  if (d != null && d.participants.isNotEmpty) ...[
                    const SizedBox(height: 20),
                    Text('Sudionici', style: theme.textTheme.titleMedium),
                    const SizedBox(height: 8),
                    ...d.participants.map(
                      (p) => ListTile(
                        dense: true,
                        title: Text(p.userEmail ?? 'Sudionik'),
                        subtitle: Text('Pridružio se: ${_participantFmt.format(p.joinedUtc.toLocal())}'),
                        trailing: p.isOrganizer ? const Chip(label: Text('Organizator')) : null,
                      ),
                    ),
                  ],
                  const SizedBox(height: 16),
                  Wrap(
                    spacing: 12,
                    runSpacing: 8,
                    children: [
                      FilledButton.icon(
                        onPressed: _saving ? null : _save,
                        icon: const Icon(Icons.save_outlined),
                        label: const Text('Spremi'),
                      ),
                      if (!_isNew && d != null) ...[
                        if (canConfirm)
                          OutlinedButton.icon(
                            onPressed: _saving ? null : _confirm,
                            icon: const Icon(Icons.check_circle_outline),
                            label: const Text('Potvrdi'),
                          ),
                        if (canCancel)
                          OutlinedButton.icon(
                            onPressed: _saving ? null : _cancel,
                            icon: const Icon(Icons.cancel_outlined),
                            label: const Text('Otkaži'),
                          ),
                      ],
                    ],
                  ),
                ],
              ),
            ),
    );
  }
}

