import 'package:arena_book_desktop/models/platform_setting_models.dart';
import 'package:arena_book_desktop/services/api_error.dart';
import 'package:arena_book_desktop/services/arena_book_api.dart';
import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

class PlatformSettingsScreen extends StatefulWidget {
  const PlatformSettingsScreen({super.key, required this.api});

  final ArenaBookApi api;

  @override
  State<PlatformSettingsScreen> createState() => _PlatformSettingsScreenState();
}

class _PlatformSettingsScreenState extends State<PlatformSettingsScreen> {
  final _searchCtrl = TextEditingController();
  final _keyCtrl = TextEditingController();
  final _valueCtrl = TextEditingController();
  List<PlatformSettingItem> _items = [];
  PlatformSettingItem? _selected;
  bool _isNew = false;
  bool _loading = true;
  bool _saving = false;
  String? _error;

  static final _dateFmt = DateFormat('dd.MM.yyyy HH:mm');

  @override
  void initState() {
    super.initState();
    _load();
  }

  @override
  void dispose() {
    _searchCtrl.dispose();
    _keyCtrl.dispose();
    _valueCtrl.dispose();
    super.dispose();
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final res = await widget.api.platformSettings(
        pageSize: 100,
        q: _searchCtrl.text.trim().isEmpty ? null : _searchCtrl.text.trim(),
      );
      if (!mounted) {
        return;
      }
      setState(() {
        _items = res.items;
        _loading = false;
      });
    } on ApiError catch (e) {
      if (!mounted) {
        return;
      }
      setState(() {
        _error = e.message;
        _loading = false;
      });
    } catch (_) {
      if (!mounted) {
        return;
      }
      setState(() {
        _error = 'Nije moguće učitati postavke.';
        _loading = false;
      });
    }
  }

  void _startNew() {
    setState(() {
      _isNew = true;
      _selected = null;
      _keyCtrl.clear();
      _valueCtrl.clear();
    });
  }

  void _select(PlatformSettingItem item) {
    setState(() {
      _isNew = false;
      _selected = item;
      _keyCtrl.text = item.settingKey;
      _valueCtrl.text = item.settingValue;
    });
  }

  Future<void> _save() async {
    final key = _keyCtrl.text.trim();
    final value = _valueCtrl.text.trim();
    if (key.isEmpty || value.isEmpty) {
      setState(() => _error = 'Ključ i vrijednost su obavezni.');
      return;
    }
    setState(() {
      _saving = true;
      _error = null;
    });
    try {
      if (_isNew) {
        await widget.api.createPlatformSetting(settingKey: key, settingValue: value);
      } else if (_selected != null) {
        await widget.api.updatePlatformSetting(
          _selected!.id,
          settingKey: key,
          settingValue: value,
        );
      }
      if (!mounted) {
        return;
      }
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Postavka sačuvana.')),
      );
      await _load();
      if (!mounted) {
        return;
      }
      setState(() {
        _saving = false;
        _isNew = false;
        _selected = null;
        _keyCtrl.clear();
        _valueCtrl.clear();
      });
    } on ApiError catch (e) {
      if (!mounted) {
        return;
      }
      setState(() {
        _error = e.message;
        _saving = false;
      });
    } catch (_) {
      if (!mounted) {
        return;
      }
      setState(() {
        _error = 'Spremanje nije uspjelo.';
        _saving = false;
      });
    }
  }

  Future<void> _delete() async {
    final item = _selected;
    if (item == null) {
      return;
    }
    final ok = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Brisanje postavke'),
        content: Text('Obrisati postavku "${item.settingKey}"?'),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('Odustani')),
          FilledButton(onPressed: () => Navigator.pop(ctx, true), child: const Text('Obriši')),
        ],
      ),
    );
    if (ok != true) {
      return;
    }
    setState(() => _saving = true);
    try {
      await widget.api.deletePlatformSetting(item.id);
      if (!mounted) {
        return;
      }
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Postavka obrisana.')),
      );
      setState(() {
        _selected = null;
        _isNew = false;
        _keyCtrl.clear();
        _valueCtrl.clear();
      });
      await _load();
    } on ApiError catch (e) {
      if (!mounted) {
        return;
      }
      setState(() {
        _error = e.message;
        _saving = false;
      });
    } catch (_) {
      if (!mounted) {
        return;
      }
      setState(() {
        _error = 'Brisanje nije uspjelo.';
        _saving = false;
      });
    }
    if (mounted) {
      setState(() => _saving = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final editing = _isNew || _selected != null;

    return Padding(
      padding: const EdgeInsets.all(24),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          SizedBox(
            width: 360,
            child: Card(
              elevation: 0,
              shape: RoundedRectangleBorder(
                borderRadius: BorderRadius.circular(12),
                side: BorderSide(color: theme.colorScheme.outlineVariant),
              ),
              child: Column(
                children: [
                  Padding(
                    padding: const EdgeInsets.all(12),
                    child: Row(
                      children: [
                        Expanded(
                          child: TextField(
                            controller: _searchCtrl,
                            decoration: const InputDecoration(
                              labelText: 'Pretraga',
                              prefixIcon: Icon(Icons.search),
                              isDense: true,
                            ),
                            onSubmitted: (_) => _load(),
                          ),
                        ),
                        IconButton(onPressed: _load, icon: const Icon(Icons.refresh)),
                      ],
                    ),
                  ),
                  Expanded(
                    child: _loading
                        ? const Center(child: CircularProgressIndicator())
                        : _error != null && _items.isEmpty
                            ? Center(child: Text(_error!))
                            : ListView.builder(
                                itemCount: _items.length,
                                itemBuilder: (context, i) {
                                  final item = _items[i];
                                  final selected = _selected?.id == item.id;
                                  return ListTile(
                                    selected: selected,
                                    title: Text(
                                      item.settingKey,
                                      maxLines: 1,
                                      overflow: TextOverflow.ellipsis,
                                    ),
                                    subtitle: Text(
                                      item.settingValue,
                                      maxLines: 1,
                                      overflow: TextOverflow.ellipsis,
                                    ),
                                    onTap: () => _select(item),
                                  );
                                },
                              ),
                  ),
                  Padding(
                    padding: const EdgeInsets.all(12),
                    child: FilledButton.icon(
                      onPressed: _startNew,
                      icon: const Icon(Icons.add),
                      label: const Text('Nova postavka'),
                    ),
                  ),
                ],
              ),
            ),
          ),
          const SizedBox(width: 20),
          Expanded(
            child: Card(
              elevation: 0,
              shape: RoundedRectangleBorder(
                borderRadius: BorderRadius.circular(12),
                side: BorderSide(color: theme.colorScheme.outlineVariant),
              ),
              child: Padding(
                padding: const EdgeInsets.all(24),
                child: editing
                    ? Column(
                        crossAxisAlignment: CrossAxisAlignment.stretch,
                        children: [
                          Text(
                            _isNew ? 'Nova postavka platforme' : 'Uredi postavku',
                            style: theme.textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w600),
                          ),
                          const SizedBox(height: 8),
                          Text(
                            'Maksimalan broj učesnika, minimalna cijena termina, tečaj koina i sl.',
                            style: theme.textTheme.bodyMedium?.copyWith(
                              color: theme.colorScheme.onSurfaceVariant,
                            ),
                          ),
                          const SizedBox(height: 24),
                          if (_error != null)
                            Padding(
                              padding: const EdgeInsets.only(bottom: 12),
                              child: Text(_error!, style: TextStyle(color: theme.colorScheme.error)),
                            ),
                          TextField(
                            controller: _keyCtrl,
                            decoration: const InputDecoration(
                              labelText: 'Ključ postavke *',
                              hintText: 'Platform.Session.MaxParticipantsPerSession',
                            ),
                          ),
                          const SizedBox(height: 16),
                          TextField(
                            controller: _valueCtrl,
                            decoration: const InputDecoration(labelText: 'Vrijednost *'),
                          ),
                          if (!_isNew && _selected != null) ...[
                            const SizedBox(height: 12),
                            Text(
                              'Zadnja izmjena: ${_dateFmt.format(_selected!.updatedUtc)}',
                              style: theme.textTheme.bodySmall?.copyWith(
                                color: theme.colorScheme.onSurfaceVariant,
                              ),
                            ),
                          ],
                          const Spacer(),
                          Row(
                            children: [
                              if (!_isNew && _selected != null)
                                OutlinedButton.icon(
                                  onPressed: _saving ? null : _delete,
                                  icon: const Icon(Icons.delete_outline),
                                  label: const Text('Obriši'),
                                ),
                              const Spacer(),
                              FilledButton(
                                onPressed: _saving ? null : _save,
                                child: _saving
                                    ? const SizedBox(
                                        width: 22,
                                        height: 22,
                                        child: CircularProgressIndicator(strokeWidth: 2),
                                      )
                                    : const Text('Sačuvaj'),
                              ),
                            ],
                          ),
                        ],
                      )
                    : Center(
                        child: Text(
                          'Odaberite postavku s liste ili dodajte novu.',
                          style: theme.textTheme.bodyLarge?.copyWith(
                            color: theme.colorScheme.onSurfaceVariant,
                          ),
                        ),
                      ),
              ),
            ),
          ),
        ],
      ),
    );
  }
}

