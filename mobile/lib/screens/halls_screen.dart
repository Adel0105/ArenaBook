import 'package:arena_book_mobile/models/current_user.dart';
import 'package:arena_book_mobile/models/hall_models.dart';

import 'package:arena_book_mobile/screens/hall_detail_screen.dart';

import 'package:arena_book_mobile/services/arena_book_api.dart';

import 'package:arena_book_mobile/widgets/app_section.dart';

import 'package:flutter/material.dart';

class HallsScreen extends StatefulWidget {
  const HallsScreen({super.key, required this.api, required this.user});

  final ArenaBookApi api;
  final CurrentUser user;

  @override
  State<HallsScreen> createState() => _HallsScreenState();
}

class _HallsScreenState extends State<HallsScreen> {
  final _searchCtrl = TextEditingController();

  List<HallListItem> _halls = [];

  List<CityItem> _cities = [];

  int? _cityId;

  bool _loading = true;

  @override
  void initState() {
    super.initState();

    _bootstrap();
  }

  @override
  void dispose() {
    _searchCtrl.dispose();

    super.dispose();
  }

  Future<void> _bootstrap() async {
    try {
      final cities = await widget.api.cities();

      if (mounted) {
        setState(() => _cities = cities.items);
      }
    } catch (_) {}

    await _load();
  }

  Future<void> _load() async {
    setState(() => _loading = true);

    try {
      final page = await widget.api.halls(
        q: _searchCtrl.text.trim().isEmpty ? null : _searchCtrl.text.trim(),
        cityId: _cityId,
        isActive: true,
        pageSize: 50,
      );

      if (mounted) {
        setState(() {
          _halls = page.items;

          _loading = false;
        });
      }
    } catch (_) {
      if (mounted) {
        setState(() => _loading = false);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Row(
          children: [
            Icon(Icons.stadium_outlined, size: 22),
            SizedBox(width: 8),
            Text('Dvorane'),
          ],
        ),
      ),
      body: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          AppSection(
            title: 'Pretraga i filter',
            subtitle: 'Pronađite dvoranu po nazivu ili gradu',
            icon: Icons.search,
            tone: AppSectionTone.slate,
            padding: const EdgeInsets.fromLTRB(16, 8, 16, 8),
            children: [
              Row(
                children: [
                  Expanded(
                    child: TextField(
                      controller: _searchCtrl,
                      decoration: const InputDecoration(
                        hintText: 'Naziv dvorane…',
                        prefixIcon: Icon(Icons.search),
                        isDense: true,
                      ),
                      onSubmitted: (_) => _load(),
                    ),
                  ),
                  IconButton(icon: const Icon(Icons.search), onPressed: _load),
                ],
              ),
              if (_cities.isNotEmpty) ...[
                const SizedBox(height: 8),
                SizedBox(
                  height: 40,
                  child: ListView(
                    scrollDirection: Axis.horizontal,
                    children: [
                      FilterChip(
                        avatar: const Icon(Icons.public, size: 16),
                        label: const Text('Svi gradovi'),
                        selected: _cityId == null,
                        onSelected: (_) {
                          setState(() => _cityId = null);

                          _load();
                        },
                      ),
                      ..._cities.map(
                        (c) => Padding(
                          padding: const EdgeInsets.only(left: 6),
                          child: FilterChip(
                            label: Text(c.name),
                            selected: _cityId == c.id,
                            onSelected: (_) {
                              setState(() => _cityId = c.id);

                              _load();
                            },
                          ),
                        ),
                      ),
                    ],
                  ),
                ),
              ],
            ],
          ),
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 4, 16, 8),
            child: AppSectionHeader(
              title: 'Lista dvorana',
              subtitle: '${_halls.length} rezultata',
              icon: Icons.list_alt_outlined,
            ),
          ),
          Expanded(
            child: _loading
                ? const Center(child: CircularProgressIndicator())
                : _halls.isEmpty
                    ? const Center(
                        child: Text('Nema dvorana za odabrane kriterije.'))
                    : ListView.builder(
                        padding: const EdgeInsets.symmetric(horizontal: 16),
                        itemCount: _halls.length,
                        itemBuilder: (context, i) {
                          final h = _halls[i];

                          return AppListTile(
                            icon: Icons.stadium_outlined,
                            imageUrl: h.primaryImageUrl,
                            title: h.name,
                            subtitle: h.cityName,
                            trailing: NovcicChip(
                                amount: h.pricePerHourCoins, perHour: true),
                            onTap: () {
                              Navigator.of(context).push(
                                MaterialPageRoute(
                                  builder: (_) => HallDetailScreen(
                                    api: widget.api,
                                    hallId: h.id,
                                    user: widget.user,
                                  ),
                                ),
                              );
                            },
                          );
                        },
                      ),
          ),
        ],
      ),
    );
  }
}

