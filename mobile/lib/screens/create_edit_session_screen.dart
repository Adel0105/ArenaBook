import 'package:arena_book_mobile/models/hall_models.dart';
import 'package:arena_book_mobile/models/session_models.dart';
import 'package:arena_book_mobile/services/api_error.dart';
import 'package:arena_book_mobile/services/arena_book_api.dart';
import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

class CreateEditSessionScreen extends StatefulWidget {
  const CreateEditSessionScreen({
    super.key,
    required this.api,
    this.sessionId,
    this.initialHallId,
  });

  final ArenaBookApi api;
  final int? sessionId;
  final int? initialHallId;

  @override
  State<CreateEditSessionScreen> createState() =>
      _CreateEditSessionScreenState();
}

class _CreateEditSessionScreenState extends State<CreateEditSessionScreen> {
  List<HallListItem> _halls = [];
  List<SessionKindItem> _kinds = [];
  int? _hallId;
  int? _kindId;
  bool _isInvite = false;
  DateTime? _start;
  DateTime? _end;
  final _maxParticipantsCtrl = TextEditingController(text: '10');
  final _maxAgeCtrl = TextEditingController();
  final _inviteCtrl = TextEditingController();
  bool _loading = true;
  bool _saving = false;
  String? _error;
  SessionDetails? _existing;
  static final _fmt = DateFormat('dd.MM.yyyy HH:mm');

  bool get _isEdit => widget.sessionId != null;

  int? _kindIdForCode(String code) {
    for (final k in _kinds) {
      if (k.code == code) {
        return k.id;
      }
    }
    return null;
  }

  @override
  void initState() {
    super.initState();
    _hallId = widget.initialHallId;
    _bootstrap();
  }

  @override
  void dispose() {
    _maxParticipantsCtrl.dispose();
    _maxAgeCtrl.dispose();
    _inviteCtrl.dispose();
    super.dispose();
  }

  Future<void> _bootstrap() async {
    try {
      final halls = await widget.api.halls(isActive: true, pageSize: 100);
      final kinds = await widget.api.sessionKinds();
      if (_isEdit) {
        _existing = await widget.api.sessionById(widget.sessionId!);
      }
      if (!mounted) {
        return;
      }
      setState(() {
        _halls = halls.items;
        _kinds = kinds.items;
        if (_existing != null) {
          _hallId = _existing!.hallId;
          _kindId = _kindIdForCode(_existing!.sessionKindCode);
          _isInvite = _existing!.sessionKindCode == 'INVITE';
          _start = _existing!.startUtc.toLocal();
          _end = _existing!.endUtc.toLocal();
          _maxParticipantsCtrl.text = '${_existing!.maxParticipants}';
          if (_existing!.maxAgeYears != null) {
            _maxAgeCtrl.text = '${_existing!.maxAgeYears}';
          }
          _inviteCtrl.text = _existing!.inviteCode ?? '';
        } else {
          _kindId = _kindIdForCode('PUBLIC') ?? kinds.items.first.id;
          _isInvite = false;
        }
        _loading = false;
      });
    } catch (e) {
      if (mounted) {
        setState(() {
          _error = e.toString();
          _loading = false;
        });
      }
    }
  }

  Future<void> _pickStart() async {
    final date = await showDatePicker(
      context: context,
      initialDate: _start ?? DateTime.now().add(const Duration(days: 1)),
      firstDate: DateTime.now(),
      lastDate: DateTime.now().add(const Duration(days: 365)),
    );
    if (date == null || !mounted) {
      return;
    }
    final time = await showTimePicker(
      context: context,
      initialTime: TimeOfDay.fromDateTime(_start ?? DateTime.now()),
    );
    if (time != null) {
      setState(() {
        _start =
            DateTime(date.year, date.month, date.day, time.hour, time.minute);
      });
    }
  }

  Future<void> _pickEnd() async {
    final date = await showDatePicker(
      context: context,
      initialDate:
          _end ?? (_start ?? DateTime.now()).add(const Duration(hours: 1)),
      firstDate: DateTime.now(),
      lastDate: DateTime.now().add(const Duration(days: 365)),
    );
    if (date == null || !mounted) {
      return;
    }
    final time = await showTimePicker(
      context: context,
      initialTime: TimeOfDay.fromDateTime(
          _end ?? DateTime.now().add(const Duration(hours: 1))),
    );
    if (time != null) {
      setState(() {
        _end =
            DateTime(date.year, date.month, date.day, time.hour, time.minute);
      });
    }
  }

  Future<void> _save() async {
    if (_hallId == null || _kindId == null || _start == null || _end == null) {
      setState(() => _error = 'Popunite dvoranu, vrstu i vrijeme termina.');
      return;
    }
    setState(() {
      _saving = true;
      _error = null;
    });
    try {
      if (_isEdit) {
        await widget.api.updateSession(widget.sessionId!, {
          'startUtc': _start!.toUtc().toIso8601String(),
          'endUtc': _end!.toUtc().toIso8601String(),
          'maxParticipants': int.parse(_maxParticipantsCtrl.text),
          'maxAgeYears': _isInvite ? null : int.tryParse(_maxAgeCtrl.text),
        });
      } else {
        await widget.api.createSession({
          'hallId': _hallId,
          'sessionKindId': _kindId,
          'startUtc': _start!.toUtc().toIso8601String(),
          'endUtc': _end!.toUtc().toIso8601String(),
          'maxParticipants': int.parse(_maxParticipantsCtrl.text),
          if (!_isInvite && _maxAgeCtrl.text.isNotEmpty)
            'maxAgeYears': int.parse(_maxAgeCtrl.text),
          if (_isInvite && _inviteCtrl.text.isNotEmpty)
            'inviteCode': _inviteCtrl.text.trim(),
        });
      }
      if (!mounted) {
        return;
      }
      Navigator.of(context).pop(true);
    } on ApiError catch (e) {
      setState(() => _error = e.message);
    } catch (e) {
      setState(() => _error = e.toString());
    } finally {
      if (mounted) {
        setState(() => _saving = false);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text(_isEdit ? 'Uredi termin' : 'Kreiraj termin')),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : ListView(
              padding: const EdgeInsets.all(16),
              children: [
                if (!_isEdit) ...[
                  DropdownButtonFormField<int>(
                    initialValue: _hallId,
                    decoration: const InputDecoration(labelText: 'Dvorana *'),
                    items: _halls
                        .map((h) =>
                            DropdownMenuItem(value: h.id, child: Text(h.name)))
                        .toList(),
                    onChanged: (v) => setState(() => _hallId = v),
                  ),
                  const SizedBox(height: 12),
                  SegmentedButton<bool>(
                    segments: const [
                      ButtonSegment(value: false, label: Text('Javni')),
                      ButtonSegment(value: true, label: Text('Privatni (kod)')),
                    ],
                    selected: {_isInvite},
                    onSelectionChanged: (s) {
                      setState(() {
                        _isInvite = s.first;
                        final code = _isInvite ? 'INVITE' : 'PUBLIC';
                        _kindId = _kindIdForCode(code);
                      });
                    },
                  ),
                  if (_isInvite)
                    TextField(
                      controller: _inviteCtrl,
                      decoration: const InputDecoration(
                        labelText: 'Kod poziva (prazno = auto)',
                      ),
                    )
                  else
                    TextField(
                      controller: _maxAgeCtrl,
                      decoration:
                          const InputDecoration(labelText: 'Max dob (godine)'),
                      keyboardType: TextInputType.number,
                    ),
                ],
                ListTile(
                  title:
                      Text(_start == null ? 'Početak *' : _fmt.format(_start!)),
                  trailing: const Icon(Icons.schedule),
                  onTap: _pickStart,
                ),
                ListTile(
                  title: Text(_end == null ? 'Kraj *' : _fmt.format(_end!)),
                  trailing: const Icon(Icons.schedule),
                  onTap: _pickEnd,
                ),
                TextField(
                  controller: _maxParticipantsCtrl,
                  decoration: const InputDecoration(labelText: 'Max učesnika'),
                  keyboardType: TextInputType.number,
                ),
                if (_isEdit && !_isInvite)
                  TextField(
                    controller: _maxAgeCtrl,
                    decoration:
                        const InputDecoration(labelText: 'Max dob (godine)'),
                    keyboardType: TextInputType.number,
                  ),
                if (_error != null)
                  Padding(
                    padding: const EdgeInsets.only(top: 8),
                    child: Text(_error!,
                        style: TextStyle(
                            color: Theme.of(context).colorScheme.error)),
                  ),
                const SizedBox(height: 16),
                FilledButton(
                  onPressed: _saving ? null : _save,
                  child: _saving
                      ? const CircularProgressIndicator()
                      : Text(_isEdit ? 'Spremi' : 'Kreiraj termin'),
                ),
              ],
            ),
    );
  }
}

