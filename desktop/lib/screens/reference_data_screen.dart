import 'package:arena_book_desktop/models/paged_list.dart';
import 'package:arena_book_desktop/models/reference_models.dart';
import 'package:arena_book_desktop/models/session_models.dart';
import 'package:arena_book_desktop/services/api_error.dart';
import 'package:arena_book_desktop/services/arena_book_api.dart';
import 'package:flutter/material.dart';

class ReferenceDataScreen extends StatefulWidget {
  const ReferenceDataScreen({super.key, required this.api});

  final ArenaBookApi api;

  @override
  State<ReferenceDataScreen> createState() => _ReferenceDataScreenState();
}

class _ReferenceDataScreenState extends State<ReferenceDataScreen>
    with SingleTickerProviderStateMixin {
  late final TabController _tabs;

  @override
  void initState() {
    super.initState();
    _tabs = TabController(length: 6, vsync: this);
  }

  @override
  void dispose() {
    _tabs.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(24, 16, 24, 0),
          child: Text(
            'Upravljanje referentnim podacima (države, gradovi, tipovi, statusi).',
            style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                  color: Theme.of(context).colorScheme.onSurfaceVariant,
                ),
          ),
        ),
        TabBar(
          controller: _tabs,
          isScrollable: true,
          tabs: const [
            Tab(text: 'Države'),
            Tab(text: 'Gradovi'),
            Tab(text: 'Tipovi opreme'),
            Tab(text: 'Vrste termina'),
            Tab(text: 'Statusi termina'),
            Tab(text: 'Statusi plaćanja'),
          ],
        ),
        Expanded(
          child: TabBarView(
            controller: _tabs,
            children: [
              _NameCrudTab(
                api: widget.api,
                entityLabel: 'državu',
                newTitle: 'Nova država',
                editTitle: 'Uredi državu',
                load: (api, q) => api.countries(pageSize: 200, q: q),
                create: (api, name) => api.createCountry(name),
                update: (api, id, name) => api.updateCountry(id, name),
                delete: (api, id) => api.deleteCountry(id),
              ),
              _CityCrudTab(api: widget.api),
              _NameCrudTab(
                api: widget.api,
                entityLabel: 'tip opreme',
                newTitle: 'Novi tip opreme',
                editTitle: 'Uredi tip opreme',
                load: (api, q) => api.equipmentTypes(pageSize: 200, q: q),
                create: (api, name) => api.createEquipmentType(name),
                update: (api, id, name) => api.updateEquipmentType(id, name),
                delete: (api, id) => api.deleteEquipmentType(id),
              ),
              _CodeCrudTab(
                api: widget.api,
                entityLabel: 'vrstu termina',
                newTitle: 'Nova vrsta termina',
                editTitle: 'Uredi vrstu termina',
                load: (api, q) => api.referenceSessionKinds(pageSize: 200, q: q),
                create: (api, code, display) =>
                    api.createSessionKind(code: code, displayName: display),
                update: (api, id, code, display) =>
                    api.updateSessionKind(id, code: code, displayName: display),
                delete: (api, id) => api.deleteSessionKind(id),
              ),
              _CodeCrudTab(
                api: widget.api,
                entityLabel: 'status termina',
                newTitle: 'Novi status termina',
                editTitle: 'Uredi status termina',
                load: (api, q) => api.referenceSessionLifecycleStatuses(pageSize: 200, q: q),
                create: (api, code, display) =>
                    api.createSessionLifecycleStatus(code: code, displayName: display),
                update: (api, id, code, display) =>
                    api.updateSessionLifecycleStatus(id, code: code, displayName: display),
                delete: (api, id) => api.deleteSessionLifecycleStatus(id),
              ),
              _CodeCrudTab(
                api: widget.api,
                entityLabel: 'status plaćanja',
                newTitle: 'Novi status plaćanja',
                editTitle: 'Uredi status plaćanja',
                load: (api, q) => api.referencePaymentProcessingStatuses(pageSize: 200, q: q),
                create: (api, code, display) =>
                    api.createPaymentProcessingStatus(code: code, displayName: display),
                update: (api, id, code, display) =>
                    api.updatePaymentProcessingStatus(id, code: code, displayName: display),
                delete: (api, id) => api.deletePaymentProcessingStatus(id),
              ),
            ],
          ),
        ),
      ],
    );
  }
}

typedef _NameLoad = Future<PagedList<NamedReferenceItem>> Function(ArenaBookApi api, String? q);
typedef _NameCreate = Future<NamedReferenceItem> Function(ArenaBookApi api, String name);
typedef _NameUpdate = Future<NamedReferenceItem> Function(ArenaBookApi api, int id, String name);
typedef _NameDelete = Future<void> Function(ArenaBookApi api, int id);

class _NameCrudTab extends StatefulWidget {
  const _NameCrudTab({
    required this.api,
    required this.entityLabel,
    required this.newTitle,
    required this.editTitle,
    required this.load,
    required this.create,
    required this.update,
    required this.delete,
  });

  final ArenaBookApi api;
  final String entityLabel;
  final String newTitle;
  final String editTitle;
  final _NameLoad load;
  final _NameCreate create;
  final _NameUpdate update;
  final _NameDelete delete;

  @override
  State<_NameCrudTab> createState() => _NameCrudTabState();
}

class _NameCrudTabState extends State<_NameCrudTab> {
  final _searchCtrl = TextEditingController();
  final _nameCtrl = TextEditingController();
  List<NamedReferenceItem> _items = [];
  NamedReferenceItem? _selected;
  bool _isNew = false;
  bool _loading = true;
  bool _saving = false;
  String? _error;

  @override
  void initState() {
    super.initState();
    _load();
  }

  @override
  void dispose() {
    _searchCtrl.dispose();
    _nameCtrl.dispose();
    super.dispose();
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final res = await widget.load(
        widget.api,
        _searchCtrl.text.trim().isEmpty ? null : _searchCtrl.text.trim(),
      );
      if (!mounted) {
        return;
      }
      setState(() {
        _items = res.items;
        _loading = false;
      });
    } on ApiError catch (e) {
      if (mounted) {
        setState(() {
          _error = e.message;
          _loading = false;
        });
      }
    } catch (_) {
      if (mounted) {
        setState(() {
          _error = 'Učitavanje nije uspjelo.';
          _loading = false;
        });
      }
    }
  }

  void _startNew() {
    setState(() {
      _isNew = true;
      _selected = null;
      _nameCtrl.clear();
      _error = null;
    });
  }

  void _select(NamedReferenceItem item) {
    setState(() {
      _isNew = false;
      _selected = item;
      _nameCtrl.text = item.name;
      _error = null;
    });
  }

  Future<void> _save() async {
    final name = _nameCtrl.text.trim();
    if (name.isEmpty) {
      setState(() => _error = 'Naziv je obavezan.');
      return;
    }
    setState(() {
      _saving = true;
      _error = null;
    });
    try {
      if (_isNew) {
        await widget.create(widget.api, name);
      } else if (_selected != null) {
        await widget.update(widget.api, _selected!.id, name);
      }
      if (!mounted) {
        return;
      }
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Zapis sačuvan.')),
      );
      setState(() {
        _saving = false;
        _isNew = false;
        _selected = null;
        _nameCtrl.clear();
      });
      await _load();
    } on ApiError catch (e) {
      if (mounted) {
        setState(() {
          _error = e.message;
          _saving = false;
        });
      }
    } catch (_) {
      if (mounted) {
        setState(() {
          _error = 'Spremanje nije uspjelo.';
          _saving = false;
        });
      }
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
        title: const Text('Potvrda brisanja'),
        content: Text('Obrisati "${item.name}"?'),
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
      await widget.delete(widget.api, item.id);
      if (!mounted) {
        return;
      }
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Zapis obrisan.')),
      );
      setState(() {
        _selected = null;
        _isNew = false;
        _nameCtrl.clear();
      });
      await _load();
    } on ApiError catch (e) {
      if (mounted) {
        setState(() => _error = e.message);
      }
    } catch (_) {
      if (mounted) {
        setState(() => _error = 'Brisanje nije uspjelo.');
      }
    }
    if (mounted) {
      setState(() => _saving = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return _CrudShell(
      searchCtrl: _searchCtrl,
      onSearch: _load,
      onRefresh: _load,
      onNew: _startNew,
      newLabel: 'Novi zapis',
      loading: _loading,
      error: _error,
      listEmpty: _items.isEmpty,
      list: ListView.builder(
        itemCount: _items.length,
        itemBuilder: (context, i) {
          final item = _items[i];
          return ListTile(
            selected: _selected?.id == item.id,
            title: Text(item.name),
            onTap: () => _select(item),
          );
        },
      ),
      editing: _isNew || _selected != null,
      formTitle: _isNew ? widget.newTitle : widget.editTitle,
      formError: _error,
      formFields: [
        TextField(
          controller: _nameCtrl,
          decoration: const InputDecoration(
            labelText: 'Naziv *',
            border: OutlineInputBorder(),
          ),
        ),
      ],
      saving: _saving,
      onSave: _save,
      onDelete: _selected != null && !_isNew ? _delete : null,
      emptyHint: 'Odaberite ${widget.entityLabel} s liste ili dodajte novu.',
    );
  }
}

class _CityCrudTab extends StatefulWidget {
  const _CityCrudTab({required this.api});

  final ArenaBookApi api;

  @override
  State<_CityCrudTab> createState() => _CityCrudTabState();
}

class _CityCrudTabState extends State<_CityCrudTab> {
  final _searchCtrl = TextEditingController();
  final _nameCtrl = TextEditingController();
  List<CityItem> _items = [];
  List<NamedReferenceItem> _countries = [];
  CityItem? _selected;
  int? _countryId;
  bool _isNew = false;
  bool _loading = true;
  bool _saving = false;
  String? _error;

  @override
  void initState() {
    super.initState();
    _bootstrap();
  }

  @override
  void dispose() {
    _searchCtrl.dispose();
    _nameCtrl.dispose();
    super.dispose();
  }

  Future<void> _bootstrap() async {
    try {
      final countries = await widget.api.countries(pageSize: 200);
      if (!mounted) {
        return;
      }
      setState(() => _countries = countries.items);
      await _load();
    } catch (_) {
      if (mounted) {
        setState(() {
          _error = 'Nije moguće učitati države.';
          _loading = false;
        });
      }
    }
  }

  String _countryName(int id) {
    for (final c in _countries) {
      if (c.id == id) {
        return c.name;
      }
    }
    return '—';
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final res = await widget.api.cities(
        pageSize: 200,
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
      if (mounted) {
        setState(() {
          _error = e.message;
          _loading = false;
        });
      }
    } catch (_) {
      if (mounted) {
        setState(() {
          _error = 'Učitavanje nije uspjelo.';
          _loading = false;
        });
      }
    }
  }

  void _startNew() {
    if (_countries.isEmpty) {
      setState(() => _error = 'Prvo dodajte barem jednu državu.');
      return;
    }
    setState(() {
      _isNew = true;
      _selected = null;
      _nameCtrl.clear();
      _countryId = _countries.first.id;
      _error = null;
    });
  }

  void _select(CityItem item) {
    setState(() {
      _isNew = false;
      _selected = item;
      _nameCtrl.text = item.name;
      _countryId = item.countryId;
      _error = null;
    });
  }

  Future<void> _save() async {
    final name = _nameCtrl.text.trim();
    if (name.isEmpty || _countryId == null) {
      setState(() => _error = 'Naziv i država su obavezni.');
      return;
    }
    setState(() {
      _saving = true;
      _error = null;
    });
    try {
      if (_isNew) {
        await widget.api.createCity(countryId: _countryId!, name: name);
      } else if (_selected != null) {
        await widget.api.updateCity(
          _selected!.id,
          countryId: _countryId!,
          name: name,
        );
      }
      if (!mounted) {
        return;
      }
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Grad sačuvan.')),
      );
      setState(() {
        _saving = false;
        _isNew = false;
        _selected = null;
        _nameCtrl.clear();
      });
      await _load();
    } on ApiError catch (e) {
      if (mounted) {
        setState(() {
          _error = e.message;
          _saving = false;
        });
      }
    } catch (_) {
      if (mounted) {
        setState(() {
          _error = 'Spremanje nije uspjelo.';
          _saving = false;
        });
      }
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
        title: const Text('Potvrda brisanja'),
        content: Text('Obrisati grad "${item.name}"?'),
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
      await widget.api.deleteCity(item.id);
      if (!mounted) {
        return;
      }
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Grad obrisan.')),
      );
      setState(() {
        _selected = null;
        _isNew = false;
        _nameCtrl.clear();
      });
      await _load();
    } on ApiError catch (e) {
      if (mounted) {
        setState(() => _error = e.message);
      }
    } catch (_) {
      if (mounted) {
        setState(() => _error = 'Brisanje nije uspjelo.');
      }
    }
    if (mounted) {
      setState(() => _saving = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return _CrudShell(
      searchCtrl: _searchCtrl,
      onSearch: _load,
      onRefresh: _load,
      onNew: _startNew,
      newLabel: 'Novi grad',
      loading: _loading,
      error: _error,
      listEmpty: _items.isEmpty,
      list: ListView.builder(
        itemCount: _items.length,
        itemBuilder: (context, i) {
          final item = _items[i];
          return ListTile(
            selected: _selected?.id == item.id,
            title: Text(item.name),
            subtitle: Text(_countryName(item.countryId)),
            onTap: () => _select(item),
          );
        },
      ),
      editing: _isNew || _selected != null,
      formTitle: _isNew ? 'Novi grad' : 'Uredi grad',
      formError: _error,
      formFields: [
        TextField(
          controller: _nameCtrl,
          decoration: const InputDecoration(
            labelText: 'Naziv grada *',
            border: OutlineInputBorder(),
          ),
        ),
        const SizedBox(height: 12),
        DropdownButtonFormField<int>(
          isExpanded: true,
          value: _countryId,
          decoration: const InputDecoration(
            labelText: 'Država *',
            border: OutlineInputBorder(),
          ),
          items: [
            for (final c in _countries)
              DropdownMenuItem(value: c.id, child: Text(c.name)),
          ],
          onChanged: _countries.isEmpty
              ? null
              : (v) => setState(() => _countryId = v),
        ),
      ],
      saving: _saving,
      onSave: _save,
      onDelete: _selected != null && !_isNew ? _delete : null,
      emptyHint: 'Odaberite grad s liste ili dodajte novi.',
    );
  }
}

typedef _CodeLoad = Future<PagedList<ReferenceItem>> Function(ArenaBookApi api, String? q);
typedef _CodeCreate = Future<ReferenceItem> Function(
  ArenaBookApi api,
  String code,
  String displayName,
);
typedef _CodeUpdate = Future<ReferenceItem> Function(
  ArenaBookApi api,
  int id,
  String code,
  String displayName,
);
typedef _CodeDelete = Future<void> Function(ArenaBookApi api, int id);

class _CodeCrudTab extends StatefulWidget {
  const _CodeCrudTab({
    required this.api,
    required this.entityLabel,
    required this.newTitle,
    required this.editTitle,
    required this.load,
    required this.create,
    required this.update,
    required this.delete,
  });

  final ArenaBookApi api;
  final String entityLabel;
  final String newTitle;
  final String editTitle;
  final _CodeLoad load;
  final _CodeCreate create;
  final _CodeUpdate update;
  final _CodeDelete delete;

  @override
  State<_CodeCrudTab> createState() => _CodeCrudTabState();
}

class _CodeCrudTabState extends State<_CodeCrudTab> {
  final _searchCtrl = TextEditingController();
  final _codeCtrl = TextEditingController();
  final _displayCtrl = TextEditingController();
  List<ReferenceItem> _items = [];
  ReferenceItem? _selected;
  bool _isNew = false;
  bool _loading = true;
  bool _saving = false;
  String? _error;

  @override
  void initState() {
    super.initState();
    _load();
  }

  @override
  void dispose() {
    _searchCtrl.dispose();
    _codeCtrl.dispose();
    _displayCtrl.dispose();
    super.dispose();
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final res = await widget.load(
        widget.api,
        _searchCtrl.text.trim().isEmpty ? null : _searchCtrl.text.trim(),
      );
      if (!mounted) {
        return;
      }
      setState(() {
        _items = res.items;
        _loading = false;
      });
    } on ApiError catch (e) {
      if (mounted) {
        setState(() {
          _error = e.message;
          _loading = false;
        });
      }
    } catch (_) {
      if (mounted) {
        setState(() {
          _error = 'Učitavanje nije uspjelo.';
          _loading = false;
        });
      }
    }
  }

  void _startNew() {
    setState(() {
      _isNew = true;
      _selected = null;
      _codeCtrl.clear();
      _displayCtrl.clear();
      _error = null;
    });
  }

  void _select(ReferenceItem item) {
    setState(() {
      _isNew = false;
      _selected = item;
      _codeCtrl.text = item.code;
      _displayCtrl.text = item.displayName;
      _error = null;
    });
  }

  Future<void> _save() async {
    final code = _codeCtrl.text.trim();
    final display = _displayCtrl.text.trim();
    if (code.isEmpty || display.isEmpty) {
      setState(() => _error = 'Šifra i naziv su obavezni.');
      return;
    }
    setState(() {
      _saving = true;
      _error = null;
    });
    try {
      if (_isNew) {
        await widget.create(widget.api, code, display);
      } else if (_selected != null) {
        await widget.update(widget.api, _selected!.id, code, display);
      }
      if (!mounted) {
        return;
      }
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Zapis sačuvan.')),
      );
      setState(() {
        _saving = false;
        _isNew = false;
        _selected = null;
        _codeCtrl.clear();
        _displayCtrl.clear();
      });
      await _load();
    } on ApiError catch (e) {
      if (mounted) {
        setState(() {
          _error = e.message;
          _saving = false;
        });
      }
    } catch (_) {
      if (mounted) {
        setState(() {
          _error = 'Spremanje nije uspjelo.';
          _saving = false;
        });
      }
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
        title: const Text('Potvrda brisanja'),
        content: Text('Obrisati "${item.displayName}"?'),
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
      await widget.delete(widget.api, item.id);
      if (!mounted) {
        return;
      }
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Zapis obrisan.')),
      );
      setState(() {
        _selected = null;
        _isNew = false;
        _codeCtrl.clear();
        _displayCtrl.clear();
      });
      await _load();
    } on ApiError catch (e) {
      if (mounted) {
        setState(() => _error = e.message);
      }
    } catch (_) {
      if (mounted) {
        setState(() => _error = 'Brisanje nije uspjelo.');
      }
    }
    if (mounted) {
      setState(() => _saving = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return _CrudShell(
      searchCtrl: _searchCtrl,
      onSearch: _load,
      onRefresh: _load,
      onNew: _startNew,
      newLabel: 'Novi zapis',
      loading: _loading,
      error: _error,
      listEmpty: _items.isEmpty,
      list: ListView.builder(
        itemCount: _items.length,
        itemBuilder: (context, i) {
          final item = _items[i];
          return ListTile(
            selected: _selected?.id == item.id,
            title: Text(item.displayName),
            subtitle: Text(item.code),
            onTap: () => _select(item),
          );
        },
      ),
      editing: _isNew || _selected != null,
      formTitle: _isNew ? widget.newTitle : widget.editTitle,
      formError: _error,
      formFields: [
        TextField(
          controller: _codeCtrl,
          decoration: const InputDecoration(
            labelText: 'Šifra *',
            border: OutlineInputBorder(),
          ),
        ),
        const SizedBox(height: 12),
        TextField(
          controller: _displayCtrl,
          decoration: const InputDecoration(
            labelText: 'Naziv za prikaz *',
            border: OutlineInputBorder(),
          ),
        ),
      ],
      saving: _saving,
      onSave: _save,
      onDelete: _selected != null && !_isNew ? _delete : null,
      emptyHint: 'Odaberite ${widget.entityLabel} s liste ili dodajte novi.',
    );
  }
}

class _CrudShell extends StatelessWidget {
  const _CrudShell({
    required this.searchCtrl,
    required this.onSearch,
    required this.onRefresh,
    required this.onNew,
    required this.newLabel,
    required this.loading,
    required this.error,
    required this.listEmpty,
    required this.list,
    required this.editing,
    required this.formTitle,
    required this.formError,
    required this.formFields,
    required this.saving,
    required this.onSave,
    required this.emptyHint,
    this.onDelete,
  });

  final TextEditingController searchCtrl;
  final VoidCallback onSearch;
  final VoidCallback onRefresh;
  final VoidCallback onNew;
  final String newLabel;
  final bool loading;
  final String? error;
  final bool listEmpty;
  final Widget list;
  final bool editing;
  final String formTitle;
  final String? formError;
  final List<Widget> formFields;
  final bool saving;
  final VoidCallback onSave;
  final VoidCallback? onDelete;
  final String emptyHint;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
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
                            controller: searchCtrl,
                            decoration: const InputDecoration(
                              labelText: 'Pretraga',
                              prefixIcon: Icon(Icons.search),
                              isDense: true,
                            ),
                            onSubmitted: (_) => onSearch(),
                          ),
                        ),
                        IconButton(onPressed: onRefresh, icon: const Icon(Icons.refresh)),
                      ],
                    ),
                  ),
                  Expanded(
                    child: loading
                        ? const Center(child: CircularProgressIndicator())
                        : error != null && listEmpty
                            ? Center(child: Text(error!))
                            : list,
                  ),
                  Padding(
                    padding: const EdgeInsets.all(12),
                    child: FilledButton.icon(
                      onPressed: onNew,
                      icon: const Icon(Icons.add),
                      label: Text(newLabel),
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
                            formTitle,
                            style: theme.textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w600),
                          ),
                          const SizedBox(height: 16),
                          if (formError != null)
                            Padding(
                              padding: const EdgeInsets.only(bottom: 12),
                              child: Text(formError!, style: TextStyle(color: theme.colorScheme.error)),
                            ),
                          ...formFields,
                          const Spacer(),
                          Row(
                            children: [
                              if (onDelete != null)
                                OutlinedButton.icon(
                                  onPressed: saving ? null : onDelete,
                                  icon: const Icon(Icons.delete_outline),
                                  label: const Text('Obriši'),
                                ),
                              const Spacer(),
                              FilledButton(
                                onPressed: saving ? null : onSave,
                                child: saving
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
                          emptyHint,
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
