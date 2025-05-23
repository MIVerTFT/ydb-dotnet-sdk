﻿using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace Ydb.Sdk.Value;

public partial class YdbValue
{
    public static YdbValue MakeBool(bool value) => new(MakePrimitiveType(Type.Types.PrimitiveTypeId.Bool),
        new Ydb.Value { BoolValue = value });

    public static YdbValue MakeInt8(sbyte value) => new(MakePrimitiveType(Type.Types.PrimitiveTypeId.Int8),
        new Ydb.Value { Int32Value = value });

    public static YdbValue MakeUint8(byte value) =>
        new(MakePrimitiveType(Type.Types.PrimitiveTypeId.Uint8),
            new Ydb.Value { Uint32Value = value });

    public static YdbValue MakeInt16(short value) => new(MakePrimitiveType(Type.Types.PrimitiveTypeId.Int16),
        new Ydb.Value { Int32Value = value });

    public static YdbValue MakeUint16(ushort value) =>
        new(MakePrimitiveType(Type.Types.PrimitiveTypeId.Uint16), new Ydb.Value { Uint32Value = value });

    public static YdbValue MakeInt32(int value) => new(MakePrimitiveType(Type.Types.PrimitiveTypeId.Int32),
        new Ydb.Value { Int32Value = value });

    public static YdbValue MakeUint32(uint value) =>
        new(MakePrimitiveType(Type.Types.PrimitiveTypeId.Uint32), new Ydb.Value { Uint32Value = value });

    public static YdbValue MakeInt64(long value) =>
        new(MakePrimitiveType(Type.Types.PrimitiveTypeId.Int64), new Ydb.Value { Int64Value = value });

    public static YdbValue MakeUint64(ulong value) =>
        new(MakePrimitiveType(Type.Types.PrimitiveTypeId.Uint64), new Ydb.Value { Uint64Value = value });

    public static YdbValue MakeFloat(float value) =>
        new(MakePrimitiveType(Type.Types.PrimitiveTypeId.Float), new Ydb.Value { FloatValue = value });

    public static YdbValue MakeDouble(double value) =>
        new(MakePrimitiveType(Type.Types.PrimitiveTypeId.Double), new Ydb.Value { DoubleValue = value });

    public static YdbValue MakeDate(DateTime value) =>
        new(MakePrimitiveType(Type.Types.PrimitiveTypeId.Date), new Ydb.Value
            { Uint32Value = (uint)value.Subtract(DateTime.UnixEpoch).TotalDays });

    public static YdbValue MakeDatetime(DateTime value) =>
        new(MakePrimitiveType(Type.Types.PrimitiveTypeId.Datetime), new Ydb.Value
        {
            Uint32Value = (uint)((value.Ticks - DateTime.UnixEpoch.Ticks) *
                Duration.NanosecondsPerTick / Duration.NanosecondsPerSecond)
        });

    public static YdbValue MakeTimestamp(DateTime value) =>
        new(MakePrimitiveType(Type.Types.PrimitiveTypeId.Timestamp), new Ydb.Value
            { Uint64Value = (ulong)(value.Ticks - DateTime.UnixEpoch.Ticks) * Duration.NanosecondsPerTick / 1000 });

    public static YdbValue MakeInterval(TimeSpan value) =>
        new(MakePrimitiveType(Type.Types.PrimitiveTypeId.Interval), new Ydb.Value
            { Int64Value = value.Ticks * Duration.NanosecondsPerTick / 1000 });

    public static YdbValue MakeString(byte[] value) =>
        new(MakePrimitiveType(Type.Types.PrimitiveTypeId.String), new Ydb.Value
            { BytesValue = ByteString.CopyFrom(value) });

    public static YdbValue MakeUtf8(string value) => new(MakePrimitiveType(Type.Types.PrimitiveTypeId.Utf8),
        new Ydb.Value { TextValue = value });

    public static YdbValue MakeYson(byte[] value) =>
        new(MakePrimitiveType(Type.Types.PrimitiveTypeId.Yson),
            new Ydb.Value { BytesValue = ByteString.CopyFrom(value) });

    public static YdbValue MakeJson(string value) => new(MakePrimitiveType(Type.Types.PrimitiveTypeId.Json),
        new Ydb.Value { TextValue = value });

    public static YdbValue MakeJsonDocument(string value) =>
        new(MakePrimitiveType(Type.Types.PrimitiveTypeId.JsonDocument), new Ydb.Value { TextValue = value });

    public static YdbValue MakeUuid(Guid guid)
    {
        var bytes = guid.ToByteArray();

        var low = BitConverter.ToUInt64(bytes, 0);
        var high = BitConverter.ToUInt64(bytes, 8);

        return new YdbValue(MakePrimitiveType(Type.Types.PrimitiveTypeId.Uuid),
            new Ydb.Value { Low128 = low, High128 = high });
    }

    private static byte GetDecimalScale(decimal value)
    {
        var bits = decimal.GetBits(value);
        var flags = bits[3];
        var scale = (byte)((flags >> 16) & 0x7F);
        return scale;
    }

    private static uint GetDecimalPrecision(decimal value)
    {
        var bits = decimal.GetBits(value);
        value = new decimal(lo: bits[0], mid: bits[1], hi: bits[2], isNegative: false, scale: 0);

        var precision = 0u;
        while (value != decimal.Zero)
        {
            value = Math.Round(value / 10);
            precision++;
        }

        return precision;
    }

    private static Ydb.Value MakeDecimalValue(decimal value)
    {
        var bits = decimal.GetBits(value);

        var low64 = ((ulong)(uint)bits[1] << 32) + (uint)bits[0];
        var high64 = (ulong)(uint)bits[2];

        unchecked
        {
            // make value negative
            if (value < 0)
            {
                low64 = ~low64;
                high64 = ~high64;

                if (low64 == (ulong)-1L)
                {
                    high64 += 1;
                }

                low64 += 1;
            }
        }

        return new Ydb.Value
        {
            Low128 = low64,
            High128 = high64
        };
    }

    public static YdbValue MakeDecimalWithPrecision(decimal value, uint? precision = null, uint? scale = null)
    {
        var valueScale = GetDecimalScale(value);
        var valuePrecision = GetDecimalPrecision(value);
        scale ??= GetDecimalScale(value);
        precision ??= valuePrecision;

        if ((int)valuePrecision - valueScale > (int)precision - scale)
        {
            throw new InvalidCastException(
                $"Decimal with precision ({valuePrecision}, {valueScale}) can't fit into ({precision}, {scale})");
        }

        // multiply for fill value with trailing zeros
        // ex: 123.45 -> 123.4500...00
        value *= 1.00000000000000000000000000000m; // 29 zeros, max supported by c# decimal
        value = Math.Round(value, (int)scale);

        var type = new Type
        {
            DecimalType = new DecimalType { Scale = (uint)scale, Precision = (uint)precision }
        };

        var ydbValue = MakeDecimalValue(value);

        return new YdbValue(type, ydbValue);
    }

    public static YdbValue MakeDecimal(decimal value) => MakeDecimalWithPrecision(value, 22, 9);

    private static YdbValue MakeOptional(YdbValue value) =>
        new(
            new Type { OptionalType = new OptionalType { Item = value._protoType } },
            value.TypeId != YdbTypeId.OptionalType
                ? value._protoValue
                : new Ydb.Value { NestedValue = value._protoValue });

    // TODO: MakeEmptyList with complex types
    public static YdbValue MakeEmptyList(YdbTypeId typeId) =>
        new(
            new Type { ListType = new ListType { Item = MakePrimitiveType(typeId) } },
            new Ydb.Value());

    // TODO: Check items type
    public static YdbValue MakeList(IReadOnlyList<YdbValue> values)
    {
        if (values.Count == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(values));
        }

        var value = new Ydb.Value();
        value.Items.Add(values.Select(v => v._protoValue));

        return new YdbValue(new Type { ListType = new ListType { Item = values[0]._protoType } }, value);
    }

    public static YdbValue MakeTuple(IReadOnlyList<YdbValue> values)
    {
        var type = new Type
        {
            TupleType = new TupleType()
        };

        type.TupleType.Elements.Add(values.Select(v => v._protoType));

        var value = new Ydb.Value();
        value.Items.Add(values.Select(v => v._protoValue));

        return new YdbValue(type, value);
    }

    public static YdbValue MakeStruct(IReadOnlyDictionary<string, YdbValue> members)
    {
        var type = new Type
        {
            StructType = new StructType()
        };

        type.StructType.Members.Add(
            members.Select(m => new StructMember { Name = m.Key, Type = m.Value._protoType }));

        var value = new Ydb.Value();
        value.Items.Add(members.Select(m => m.Value._protoValue));

        return new YdbValue(type, value);
    }

    private static Type MakePrimitiveType(Type.Types.PrimitiveTypeId primitiveTypeId) =>
        new() { TypeId = primitiveTypeId };

    private static Type MakePrimitiveType(YdbTypeId typeId)
    {
        EnsurePrimitiveTypeId(typeId);
        return new Type { TypeId = (Type.Types.PrimitiveTypeId)typeId };
    }

    private static bool IsPrimitiveTypeId(YdbTypeId typeId) => (uint)typeId < YdbTypeIdRanges.ComplexTypesFirst;

    private static void EnsurePrimitiveTypeId(YdbTypeId typeId)
    {
        if (!IsPrimitiveTypeId(typeId))
        {
            throw new ArgumentException($"Complex types aren't supported in current method: {typeId}", "typeId");
        }
    }

    public static YdbValue MakeOptionalBool(bool? value = null) => MakeOptionalOf(value, YdbTypeId.Bool, MakeBool);

    public static YdbValue MakeOptionalInt8(sbyte? value = null) => MakeOptionalOf(value, YdbTypeId.Int8, MakeInt8);

    public static YdbValue MakeOptionalUint8(byte? value = null) => MakeOptionalOf(value, YdbTypeId.Uint8, MakeUint8);

    public static YdbValue MakeOptionalInt16(short? value = null) => MakeOptionalOf(value, YdbTypeId.Int16, MakeInt16);

    public static YdbValue MakeOptionalUint16(ushort? value = null) =>
        MakeOptionalOf(value, YdbTypeId.Uint16, MakeUint16);

    public static YdbValue MakeOptionalInt32(int? value = null) => MakeOptionalOf(value, YdbTypeId.Int32, MakeInt32);

    public static YdbValue MakeOptionalUint32(uint? value = null) =>
        MakeOptionalOf(value, YdbTypeId.Uint32, MakeUint32);

    public static YdbValue MakeOptionalInt64(long? value = null) => MakeOptionalOf(value, YdbTypeId.Int64, MakeInt64);

    public static YdbValue MakeOptionalUint64(ulong? value = null) =>
        MakeOptionalOf(value, YdbTypeId.Uint64, MakeUint64);

    public static YdbValue MakeOptionalFloat(float? value = null) => MakeOptionalOf(value, YdbTypeId.Float, MakeFloat);

    public static YdbValue MakeOptionalDouble(double? value = null) =>
        MakeOptionalOf(value, YdbTypeId.Double, MakeDouble);

    public static YdbValue MakeOptionalDate(DateTime? value = null) => MakeOptionalOf(value, YdbTypeId.Date, MakeDate);

    public static YdbValue MakeOptionalDatetime(DateTime? value = null) =>
        MakeOptionalOf(value, YdbTypeId.Datetime, MakeDatetime);

    public static YdbValue MakeOptionalTimestamp(DateTime? value = null) =>
        MakeOptionalOf(value, YdbTypeId.Timestamp, MakeTimestamp);

    public static YdbValue MakeOptionalInterval(TimeSpan? value = null) =>
        MakeOptionalOf(value, YdbTypeId.Interval, MakeInterval);

    public static YdbValue MakeOptionalString(byte[]? value = null) =>
        MakeOptionalOf(value, YdbTypeId.String, MakeString);

    public static YdbValue MakeOptionalUtf8(string? value = null) => MakeOptionalOf(value, YdbTypeId.Utf8, MakeUtf8);

    public static YdbValue MakeOptionalYson(byte[]? value = null) => MakeOptionalOf(value, YdbTypeId.Yson, MakeYson);

    public static YdbValue MakeOptionalJson(string? value = null) => MakeOptionalOf(value, YdbTypeId.Json, MakeJson);

    public static YdbValue MakeOptionalJsonDocument(string? value = null) =>
        MakeOptionalOf(value, YdbTypeId.JsonDocument, MakeJsonDocument);

    public static YdbValue MakeOptionalUuid(Guid? value = null) => MakeOptionalOf(value, YdbTypeId.Uuid, MakeUuid);

    public static YdbValue MakeOptionalDecimal(decimal? value = null) =>
        MakeOptionalOf(value, YdbTypeId.DecimalType, MakeDecimal);

    private static YdbValue MakeOptionalOf<T>(T? value, YdbTypeId type, Func<T, YdbValue> func) where T : struct =>
        value is null ? MakeEmptyOptional(type) : MakeOptional(func((T)value));

    private static YdbValue MakeOptionalOf<T>(T? value, YdbTypeId type, Func<T, YdbValue> func) where T : class =>
        value is null ? MakeEmptyOptional(type) : MakeOptional(func(value));

    private static YdbValue MakeEmptyOptional(YdbTypeId typeId)
    {
        if (IsPrimitiveTypeId(typeId))
        {
            return new YdbValue(
                new Type { OptionalType = new OptionalType { Item = MakePrimitiveType(typeId) } },
                new Ydb.Value { NullFlagValue = new NullValue() });
        }

        if (typeId == YdbTypeId.DecimalType)
        {
            return new YdbValue(
                new Type
                {
                    OptionalType = new OptionalType
                        { Item = new Type { DecimalType = new DecimalType { Scale = 9, Precision = 22 } } }
                },
                new Ydb.Value { NullFlagValue = new NullValue() }
            );
        }

        throw new ArgumentException($"This type is not supported: {typeId}", nameof(typeId));
    }
}
