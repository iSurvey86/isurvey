namespace iSurvey.Models;

/// <summary>Điểm địa lý WGS84 (kinh/vĩ độ độ).</summary>
public readonly record struct GeoPoint(double Longitude, double Latitude);

/// <summary>Điểm mặt phẳng VN2000 (Easting, Northing mét).</summary>
public readonly record struct PlanePoint(double Easting, double Northing);

/// <summary>Biên độ địa lý WGS84.</summary>
public readonly record struct GeoBounds(
    double MinLongitude,
    double MinLatitude,
    double MaxLongitude,
    double MaxLatitude)
{
    public double WidthDegrees => MaxLongitude - MinLongitude;
    public double HeightDegrees => MaxLatitude - MinLatitude;

    public GeoPoint SouthWest => new(MinLongitude, MinLatitude);
    public GeoPoint NorthEast => new(MaxLongitude, MaxLatitude);
}

/// <summary>Bốn góc khung nhìn trong không gian mô hình (VN2000).</summary>
public readonly record struct ViewportCorners(
    PlanePoint BottomLeft,
    PlanePoint BottomRight,
    PlanePoint TopRight,
    PlanePoint TopLeft)
{
    public PlanePoint Center => new(
        (BottomLeft.Easting + TopRight.Easting) / 2.0,
        (BottomLeft.Northing + TopRight.Northing) / 2.0);
}
