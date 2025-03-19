namespace Atc.Kusto.Api.Sample.Contracts;

public record NycTaxiTrip(
    DateTime DropoffDatetime,
    double DropoffLatitude,
    double DropoffLongitude,
    double FareAmount,
    double MtaTax,
    int PassengerCount,
    string PaymentType,
    DateTime PickupDatetime,
    double PickupLatitude,
    double PickupLongitude,
    string RateCode,
    string StoreAndFwdFlag,
    double Surcharge,
    double TipAmount,
    double TollsAmount,
    double TotalAmount,
    double TripDistance,
    string VendorId);