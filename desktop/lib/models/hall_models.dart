class HallListItem {
  HallListItem({
    required this.id,
    required this.name,
    required this.cityId,
    required this.cityName,
    required this.countryName,
    required this.streetAddress,
    required this.capacityPeople,
    required this.pricePerHourCoins,
    required this.contactPhone,
    required this.isActive,
    this.primaryImageUrl,
  });

  final int id;
  final String name;
  final int cityId;
  final String cityName;
  final String countryName;
  final String streetAddress;
  final int capacityPeople;
  final double pricePerHourCoins;
  final String contactPhone;
  final bool isActive;
  final String? primaryImageUrl;

  factory HallListItem.fromJson(Map<String, dynamic> json) {
    return HallListItem(
      id: (json['id'] as num).toInt(),
      name: json['name'] as String? ?? '',
      cityId: (json['cityId'] as num).toInt(),
      cityName: json['cityName'] as String? ?? '',
      countryName: json['countryName'] as String? ?? '',
      streetAddress: json['streetAddress'] as String? ?? '',
      capacityPeople: (json['capacityPeople'] as num).toInt(),
      pricePerHourCoins: (json['pricePerHourCoins'] as num).toDouble(),
      contactPhone: json['contactPhone'] as String? ?? '',
      isActive: json['isActive'] as bool? ?? true,
      primaryImageUrl: json['primaryImageUrl'] as String?,
    );
  }
}

class HallDetails {
  HallDetails({
    required this.id,
    required this.name,
    required this.cityId,
    required this.cityName,
    required this.countryId,
    required this.countryName,
    required this.streetAddress,
    required this.latitude,
    required this.longitude,
    required this.capacityPeople,
    required this.pricePerHourCoins,
    required this.contactPhone,
    required this.isActive,
    required this.createdUtc,
  });

  final int id;
  final String name;
  final int cityId;
  final String cityName;
  final int countryId;
  final String countryName;
  final String streetAddress;
  final double? latitude;
  final double? longitude;
  final int capacityPeople;
  final double pricePerHourCoins;
  final String contactPhone;
  final bool isActive;
  final DateTime createdUtc;

  factory HallDetails.fromJson(Map<String, dynamic> json) {
    return HallDetails(
      id: (json['id'] as num).toInt(),
      name: json['name'] as String? ?? '',
      cityId: (json['cityId'] as num).toInt(),
      cityName: json['cityName'] as String? ?? '',
      countryId: (json['countryId'] as num).toInt(),
      countryName: json['countryName'] as String? ?? '',
      streetAddress: json['streetAddress'] as String? ?? '',
      latitude: (json['latitude'] as num?)?.toDouble(),
      longitude: (json['longitude'] as num?)?.toDouble(),
      capacityPeople: (json['capacityPeople'] as num).toInt(),
      pricePerHourCoins: (json['pricePerHourCoins'] as num).toDouble(),
      contactPhone: json['contactPhone'] as String? ?? '',
      isActive: json['isActive'] as bool? ?? true,
      createdUtc: DateTime.parse(json['createdUtc'] as String),
    );
  }
}

class HallPhotoItem {
  HallPhotoItem({
    required this.id,
    required this.hallId,
    required this.sortOrder,
    required this.imageUrl,
  });

  final int id;
  final int hallId;
  final int sortOrder;
  final String imageUrl;

  factory HallPhotoItem.fromJson(Map<String, dynamic> json) {
    return HallPhotoItem(
      id: (json['id'] as num).toInt(),
      hallId: (json['hallId'] as num).toInt(),
      sortOrder: (json['sortOrder'] as num).toInt(),
      imageUrl: json['imageUrl'] as String? ?? '',
    );
  }
}

class HallEquipmentLink {
  HallEquipmentLink({
    required this.id,
    required this.hallId,
    required this.equipmentTypeId,
    required this.equipmentTypeName,
    required this.quantity,
  });

  final int id;
  final int hallId;
  final int equipmentTypeId;
  final String equipmentTypeName;
  final int quantity;

  factory HallEquipmentLink.fromJson(Map<String, dynamic> json) {
    return HallEquipmentLink(
      id: (json['id'] as num).toInt(),
      hallId: (json['hallId'] as num).toInt(),
      equipmentTypeId: (json['equipmentTypeId'] as num).toInt(),
      equipmentTypeName: json['equipmentTypeName'] as String? ?? '',
      quantity: (json['quantity'] as num).toInt(),
    );
  }
}

