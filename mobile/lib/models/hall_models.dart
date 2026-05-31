class HallListItem {
  HallListItem({
    required this.id,
    required this.name,
    required this.cityName,
    required this.countryName,
    required this.streetAddress,
    required this.capacityPeople,
    required this.pricePerHourCoins,
    required this.isActive,
    this.primaryImageUrl,
  });

  final int id;
  final String name;
  final String cityName;
  final String countryName;
  final String streetAddress;
  final int capacityPeople;
  final double pricePerHourCoins;
  final bool isActive;
  final String? primaryImageUrl;

  factory HallListItem.fromJson(Map<String, dynamic> json) {
    return HallListItem(
      id: json['id'] as int,
      name: json['name'] as String? ?? '',
      cityName: json['cityName'] as String? ?? '',
      countryName: json['countryName'] as String? ?? '',
      streetAddress: json['streetAddress'] as String? ?? '',
      capacityPeople: json['capacityPeople'] as int? ?? 0,
      pricePerHourCoins: (json['pricePerHourCoins'] as num?)?.toDouble() ?? 0,
      isActive: json['isActive'] as bool? ?? false,
      primaryImageUrl: json['primaryImageUrl'] as String?,
    );
  }
}

class HallDetails {
  HallDetails({
    required this.id,
    required this.name,
    required this.cityName,
    required this.countryName,
    required this.streetAddress,
    required this.capacityPeople,
    required this.pricePerHourCoins,
    required this.contactPhone,
    required this.isActive,
  });

  final int id;
  final String name;
  final String cityName;
  final String countryName;
  final String streetAddress;
  final int capacityPeople;
  final double pricePerHourCoins;
  final String contactPhone;
  final bool isActive;

  factory HallDetails.fromJson(Map<String, dynamic> json) {
    return HallDetails(
      id: json['id'] as int,
      name: json['name'] as String? ?? '',
      cityName: json['cityName'] as String? ?? '',
      countryName: json['countryName'] as String? ?? '',
      streetAddress: json['streetAddress'] as String? ?? '',
      capacityPeople: json['capacityPeople'] as int? ?? 0,
      pricePerHourCoins: (json['pricePerHourCoins'] as num?)?.toDouble() ?? 0,
      contactPhone: json['contactPhone'] as String? ?? '',
      isActive: json['isActive'] as bool? ?? false,
    );
  }
}

class HallPhoto {
  HallPhoto({
    required this.id,
    required this.imageUrl,
    required this.sortOrder,
    required this.isPrimary,
  });

  final int id;
  final String imageUrl;
  final int sortOrder;
  final bool isPrimary;

  factory HallPhoto.fromJson(Map<String, dynamic> json) {
    return HallPhoto(
      id: json['id'] as int? ?? 0,
      imageUrl: json['imageUrl'] as String? ?? '',
      sortOrder: json['sortOrder'] as int? ?? 0,
      isPrimary: json['isPrimary'] as bool? ?? false,
    );
  }
}

class HallReview {
  HallReview({
    required this.id,
    required this.hallId,
    required this.userId,
    required this.userDisplayName,
    required this.ratingStars,
    this.comment,
    required this.createdUtc,
  });

  final int id;
  final int hallId;
  final String userId;
  final String userDisplayName;
  final int ratingStars;
  final String? comment;
  final DateTime createdUtc;

  factory HallReview.fromJson(Map<String, dynamic> json) {
    return HallReview(
      id: json['id'] as int? ?? 0,
      hallId: json['hallId'] as int? ?? 0,
      userId: json['userId'] as String? ?? '',
      userDisplayName: json['userDisplayName'] as String? ?? '',
      ratingStars: json['ratingStars'] as int? ?? 0,
      comment: json['comment'] as String?,
      createdUtc: json['createdUtc'] != null
          ? DateTime.parse(json['createdUtc'] as String)
          : DateTime.now().toUtc(),
    );
  }
}

class HallReactionSummary {
  HallReactionSummary({
    required this.likeCount,
    required this.dislikeCount,
    this.userReaction,
  });

  final int likeCount;
  final int dislikeCount;
  final String? userReaction;

  factory HallReactionSummary.fromJson(Map<String, dynamic> json) {
    return HallReactionSummary(
      likeCount: json['likeCount'] as int? ?? 0,
      dislikeCount: json['dislikeCount'] as int? ?? 0,
      userReaction: json['userReaction'] as String?,
    );
  }
}

class RecommendedHall {
  RecommendedHall({
    required this.hallId,
    required this.name,
    required this.cityName,
    required this.averageRating,
    required this.reviewCount,
    required this.pricePerHourCoins,
    required this.explanation,
    required this.score,
  });

  final int hallId;
  final String name;
  final String cityName;
  final double averageRating;
  final int reviewCount;
  final double pricePerHourCoins;
  final String explanation;
  final double score;

  factory RecommendedHall.fromJson(Map<String, dynamic> json) {
    return RecommendedHall(
      hallId: json['hallId'] as int,
      name: json['name'] as String? ?? '',
      cityName: json['cityName'] as String? ?? '',
      averageRating: (json['averageRating'] as num?)?.toDouble() ?? 0,
      reviewCount: json['reviewCount'] as int? ?? 0,
      pricePerHourCoins: (json['pricePerHourCoins'] as num?)?.toDouble() ?? 0,
      explanation: json['explanation'] as String? ?? '',
      score: (json['score'] as num?)?.toDouble() ?? 0,
    );
  }
}

class HallEquipmentItem {
  HallEquipmentItem({
    required this.id,
    required this.equipmentTypeName,
    required this.quantity,
  });

  final int id;
  final String equipmentTypeName;
  final int quantity;

  factory HallEquipmentItem.fromJson(Map<String, dynamic> json) {
    return HallEquipmentItem(
      id: json['id'] as int,
      equipmentTypeName: json['equipmentTypeName'] as String? ?? '',
      quantity: json['quantity'] as int? ?? 0,
    );
  }
}

class CityItem {
  CityItem({required this.id, required this.name, required this.countryId});

  final int id;
  final String name;
  final int countryId;

  factory CityItem.fromJson(Map<String, dynamic> json) {
    return CityItem(
      id: json['id'] as int,
      name: json['name'] as String? ?? '',
      countryId: json['countryId'] as int? ?? 0,
    );
  }
}

