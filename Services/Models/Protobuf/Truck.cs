// <auto-generated>
//     Generated by the protocol buffer compiler.  DO NOT EDIT!
//     source: Truck.proto
// </auto-generated>
#pragma warning disable 1591, 0612, 3021
#region Designer generated code

using pb = global::Google.Protobuf;
using pbc = global::Google.Protobuf.Collections;
using pbr = global::Google.Protobuf.Reflection;
using scg = global::System.Collections.Generic;
namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Protobuf.Models {

  /// <summary>Holder for reflection information generated from Truck.proto</summary>
  public static partial class TruckReflection {

    #region Descriptor
    /// <summary>File descriptor for Truck.proto</summary>
    public static pbr::FileDescriptor Descriptor {
      get { return descriptor; }
    }
    private static pbr::FileDescriptor descriptor;

    static TruckReflection() {
      byte[] descriptorData = global::System.Convert.FromBase64String(
          string.Concat(
            "Cgt0bXA2QjI4LnRtcCJ6CgVUcnVjaxIQCghsYXRpdHVkZRgBIAEoARIRCgls",
            "b25naXR1ZGUYAiABKAESDQoFc3BlZWQYAyABKAESEgoKc3BlZWRfdW5pdBgE",
            "IAEoCRIPCgdoZWFkaW5nGAUgASgBEhgKEHRlbXBlcmF0dXJlX3VuaXQYBiAB",
            "KAlCSaoCRk1pY3Jvc29mdC5BenVyZS5Jb1RTb2x1dGlvbnMuRGV2aWNlU2lt",
            "dWxhdGlvbi5TZXJ2aWNlcy5Qcm90b2J1Zi5Nb2RlbHNiBnByb3RvMw=="));
      descriptor = pbr::FileDescriptor.FromGeneratedCode(descriptorData,
          new pbr::FileDescriptor[] { },
          new pbr::GeneratedClrTypeInfo(null, new pbr::GeneratedClrTypeInfo[] {
            new pbr::GeneratedClrTypeInfo(typeof(global::Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Protobuf.Models.Truck), global::Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Protobuf.Models.Truck.Parser, new[]{ "Latitude", "Longitude", "Speed", "SpeedUnit", "Heading", "TemperatureUnit" }, null, null, null)
          }));
    }
    #endregion

  }
  #region Messages
  public sealed partial class Truck : pb::IMessage<Truck> {
    private static readonly pb::MessageParser<Truck> _parser = new pb::MessageParser<Truck>(() => new Truck());
    private pb::UnknownFieldSet _unknownFields;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pb::MessageParser<Truck> Parser { get { return _parser; } }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pbr::MessageDescriptor Descriptor {
      get { return global::Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Protobuf.Models.TruckReflection.Descriptor.MessageTypes[0]; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public Truck() {
      OnConstruction();
    }

    partial void OnConstruction();

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public Truck(Truck other) : this() {
      latitude_ = other.latitude_;
      longitude_ = other.longitude_;
      speed_ = other.speed_;
      speedUnit_ = other.speedUnit_;
      heading_ = other.heading_;
      temperatureUnit_ = other.temperatureUnit_;
      _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public Truck Clone() {
      return new Truck(this);
    }

    /// <summary>Field number for the "latitude" field.</summary>
    public const int LatitudeFieldNumber = 1;
    private double latitude_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public double Latitude {
      get { return latitude_; }
      set {
        latitude_ = value;
      }
    }

    /// <summary>Field number for the "longitude" field.</summary>
    public const int LongitudeFieldNumber = 2;
    private double longitude_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public double Longitude {
      get { return longitude_; }
      set {
        longitude_ = value;
      }
    }

    /// <summary>Field number for the "speed" field.</summary>
    public const int SpeedFieldNumber = 3;
    private double speed_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public double Speed {
      get { return speed_; }
      set {
        speed_ = value;
      }
    }

    /// <summary>Field number for the "speed_unit" field.</summary>
    public const int SpeedUnitFieldNumber = 4;
    private string speedUnit_ = "";
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public string SpeedUnit {
      get { return speedUnit_; }
      set {
        speedUnit_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
      }
    }

    /// <summary>Field number for the "heading" field.</summary>
    public const int HeadingFieldNumber = 5;
    private double heading_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public double Heading {
      get { return heading_; }
      set {
        heading_ = value;
      }
    }

    /// <summary>Field number for the "temperature_unit" field.</summary>
    public const int TemperatureUnitFieldNumber = 6;
    private string temperatureUnit_ = "";
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public string TemperatureUnit {
      get { return temperatureUnit_; }
      set {
        temperatureUnit_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override bool Equals(object other) {
      return Equals(other as Truck);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public bool Equals(Truck other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if (!pbc::ProtobufEqualityComparers.BitwiseDoubleEqualityComparer.Equals(Latitude, other.Latitude)) return false;
      if (!pbc::ProtobufEqualityComparers.BitwiseDoubleEqualityComparer.Equals(Longitude, other.Longitude)) return false;
      if (!pbc::ProtobufEqualityComparers.BitwiseDoubleEqualityComparer.Equals(Speed, other.Speed)) return false;
      if (SpeedUnit != other.SpeedUnit) return false;
      if (!pbc::ProtobufEqualityComparers.BitwiseDoubleEqualityComparer.Equals(Heading, other.Heading)) return false;
      if (TemperatureUnit != other.TemperatureUnit) return false;
      return Equals(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override int GetHashCode() {
      int hash = 1;
      if (Latitude != 0D) hash ^= pbc::ProtobufEqualityComparers.BitwiseDoubleEqualityComparer.GetHashCode(Latitude);
      if (Longitude != 0D) hash ^= pbc::ProtobufEqualityComparers.BitwiseDoubleEqualityComparer.GetHashCode(Longitude);
      if (Speed != 0D) hash ^= pbc::ProtobufEqualityComparers.BitwiseDoubleEqualityComparer.GetHashCode(Speed);
      if (SpeedUnit.Length != 0) hash ^= SpeedUnit.GetHashCode();
      if (Heading != 0D) hash ^= pbc::ProtobufEqualityComparers.BitwiseDoubleEqualityComparer.GetHashCode(Heading);
      if (TemperatureUnit.Length != 0) hash ^= TemperatureUnit.GetHashCode();
      if (_unknownFields != null) {
        hash ^= _unknownFields.GetHashCode();
      }
      return hash;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override string ToString() {
      return pb::JsonFormatter.ToDiagnosticString(this);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void WriteTo(pb::CodedOutputStream output) {
      if (Latitude != 0D) {
        output.WriteRawTag(9);
        output.WriteDouble(Latitude);
      }
      if (Longitude != 0D) {
        output.WriteRawTag(17);
        output.WriteDouble(Longitude);
      }
      if (Speed != 0D) {
        output.WriteRawTag(25);
        output.WriteDouble(Speed);
      }
      if (SpeedUnit.Length != 0) {
        output.WriteRawTag(34);
        output.WriteString(SpeedUnit);
      }
      if (Heading != 0D) {
        output.WriteRawTag(41);
        output.WriteDouble(Heading);
      }
      if (TemperatureUnit.Length != 0) {
        output.WriteRawTag(50);
        output.WriteString(TemperatureUnit);
      }
      if (_unknownFields != null) {
        _unknownFields.WriteTo(output);
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public int CalculateSize() {
      int size = 0;
      if (Latitude != 0D) {
        size += 1 + 8;
      }
      if (Longitude != 0D) {
        size += 1 + 8;
      }
      if (Speed != 0D) {
        size += 1 + 8;
      }
      if (SpeedUnit.Length != 0) {
        size += 1 + pb::CodedOutputStream.ComputeStringSize(SpeedUnit);
      }
      if (Heading != 0D) {
        size += 1 + 8;
      }
      if (TemperatureUnit.Length != 0) {
        size += 1 + pb::CodedOutputStream.ComputeStringSize(TemperatureUnit);
      }
      if (_unknownFields != null) {
        size += _unknownFields.CalculateSize();
      }
      return size;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(Truck other) {
      if (other == null) {
        return;
      }
      if (other.Latitude != 0D) {
        Latitude = other.Latitude;
      }
      if (other.Longitude != 0D) {
        Longitude = other.Longitude;
      }
      if (other.Speed != 0D) {
        Speed = other.Speed;
      }
      if (other.SpeedUnit.Length != 0) {
        SpeedUnit = other.SpeedUnit;
      }
      if (other.Heading != 0D) {
        Heading = other.Heading;
      }
      if (other.TemperatureUnit.Length != 0) {
        TemperatureUnit = other.TemperatureUnit;
      }
      _unknownFields = pb::UnknownFieldSet.MergeFrom(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(pb::CodedInputStream input) {
      uint tag;
      while ((tag = input.ReadTag()) != 0) {
        switch(tag) {
          default:
            _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, input);
            break;
          case 9: {
            Latitude = input.ReadDouble();
            break;
          }
          case 17: {
            Longitude = input.ReadDouble();
            break;
          }
          case 25: {
            Speed = input.ReadDouble();
            break;
          }
          case 34: {
            SpeedUnit = input.ReadString();
            break;
          }
          case 41: {
            Heading = input.ReadDouble();
            break;
          }
          case 50: {
            TemperatureUnit = input.ReadString();
            break;
          }
        }
      }
    }

  }

  #endregion

}

#endregion Designer generated code
