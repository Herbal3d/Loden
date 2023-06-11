// Copyright 2019 Robert Adams
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
//     Unless required by applicable law or agreed to in writing, software
//     distributed under the License is distributed on an "AS IS" BASIS,
//     WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//     See the License for the specific language governing permissions and
//     limitations under the License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

// Definition of classes for Cesium 3d-Tiles.
// https://github.com/AnalyticalGraphicsInc/3d-tiles/tree/master/specification
// Notated for correct JSON serialization by Json.NET
namespace org.herbal3d.Tiles {
    public class TileSet {
        public TileAsset asset;
        public TileProperties properties;
        public float geometricError;
        public Tile root;
        public string[] extensionsUsed;
        public string[] extensionsRequired;
        public TileExtensions extensions;
        public TileExtensions extras;

        public TileSet() {
            asset = new TileAsset();
            geometricError = 1.0f;
            root = new Tile();
        }

        public void ToJSON(StreamWriter outt) {
            string json = JsonSerializer.Serialize(outt, new JsonSerializerOptions {
                WriteIndented = true,
                IncludeFields = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                Converters = { new JustAnArrayIO() }
            });
            outt.Flush();
        }

        public static TileSet FromStream(Stream pIn) {
            TileSet ret = JsonSerializer.Deserialize<TileSet>(pIn, new JsonSerializerOptions {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                IncludeFields = true,
                Converters = { new JustAnArrayIO() }
            });
            return ret;
        }

        public static TileSet FromString(string pIn) {
            TileSet ret = JsonSerializer.Deserialize<TileSet>(pIn, new JsonSerializerOptions {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                IncludeFields = true,
                Converters = { new JustAnArrayIO() }
            });
            return ret;
        }
    }

    public class TileAsset {
        public string version = "1.0";
        public string tilesetVersion;
        public TileExtensions extensions;
        public TileExtensions extras;

        public TileAsset() {
        }
    }

    public class Tile {
        public TileBoundingVolume boundingVolume = new TileBoundingVolume();
        public TileBoundingVolume viewerRequestVolume;
        public float geometricError = 1.0f;
        public string refine;
        public TileTransform transform;
        public TileContent content;
        public TileContent[] children;
        public TileExtensions extensions;
        public TileExtensions extras;

        public Tile() {
        }
    }

    public class TileContent {
        public TileBoundingVolume boundingVolume;
        public string uri;
        // Basil extension so a tile's content can be multiple GLTF files.
        public string[] uris;
        public TileExtensions extensions;
        public TileExtensions extras;

        public TileContent() {
        }
        public TileContent(string pURI) {
            uri = pURI;
        }
    }

    public class TileBoundingVolume {
        public TileBox box;
        public TileRegion region;
        public TileSphere sphere;
        public TileExtensions extensions;
        public TileExtensions extras;
    }

    // Several fields are just an array of numbers. This class is used as
    //    the base class and the JSON converter creates the correct underlying
    //    class.
    public abstract class JustAnArray {
        // public static int Size;
        abstract public double[] GetArray();
        abstract public string Name();

        static public double[] ReadArray(ref Utf8JsonReader reader, int pArraySize)
        {
            if (reader.TokenType != JsonTokenType.StartArray) {
                throw new JsonException();
            }
            double[] values = new double[pArraySize];
            int i = 0;
            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray) {
                if (reader.TokenType != JsonTokenType.Number) {
                    throw new JsonException();
                }
                values[i++] = reader.GetDouble();
            }
            return values;
        }
        static public void WriteArray(Utf8JsonWriter writer, double[] pArray) {
            writer.WriteStartArray();
            for (int ii=0; ii < pArray.Length; ii++) {
                writer.WriteNumberValue(pArray[ii]);
            }
            writer.WriteEndArray();
        }
    }

    // double[16] specifying node transform
    // [1,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1]
    public class TileTransform : JustAnArray {
        public double[] transform;

        public static int ArraySize = 16;
        public override double[] GetArray() { return transform; }
        public override string Name() { return "transform"; }

        public TileTransform() {
            transform = new double[16] { 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1 };
        }
        public TileTransform(double[] pValues) {
            transform = pValues;
        }
    }

    // double[12] defining oriented bounding box.
    // The first three elements define the x, y, and z values for the center of the box.
    //    The next three elements(with indices 3, 4, and 5) define the x axis direction
    //    and half-length.The next three elements(indices 6, 7, and 8) define the y axis
    //    direction and half-length.The last three elements(indices 9, 10, and 11)
    //    define the z axis direction and half-length.
    public class TileBox : JustAnArray {
        public double[] box;

        public static int ArraySize = 12;
        public override double[] GetArray() { return box; }
        public override string Name() { return "box"; }

        public TileBox() {
            box = new double[ArraySize];
        }
        public TileBox(double[] pValues) {
            box = pValues;
        }
    }
    // double[6] defining a region
    // An array of six numbers that define a bounding geographic region in EPSG:4979
    //    coordinates with the order[west, south, east, north, minimum height, maximum height].
    //    Longitudes and latitudes are in radians, and heights are in meters above
    //    (or below) the WGS84 ellipsoid.
    public class TileRegion : JustAnArray {
        public double[] region;
        public static int ArraySize = 6;
        public override double[] GetArray() { return region; }
        public override string Name() { return "region"; }
        public TileRegion() {
            region = new double[ArraySize];
        }
        public TileRegion(double[] pValues) {
            region = pValues;
        }
    }
    // double[6] defining a sphere
    // The first three elements define the x, y, and z values for the center of the sphere.
    //  The last element (with index 3) defines the radius in meters.
    public class TileSphere : JustAnArray {
        public double[] sphere;
        public static int ArraySize = 4;
        public override double[] GetArray() { return sphere; }
        public override string Name() { return "sphere"; }
        public TileSphere() {
            sphere = new double[4] { 0, 0, 0, 1 };
        }
        public TileSphere(double[] pValues) {
            sphere = pValues;
        }
    }

    public class JustAnArrayIO : JsonConverterFactory {
        public override bool CanConvert(Type typeToConvert) {
            return typeof(JustAnArray).IsAssignableFrom(typeToConvert);
        }
        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) {
            switch (typeToConvert.Name) {
                case nameof(TileTransform):
                    return new TileTransformConverter();
                case nameof(TileBox):
                    return new TileBoxConverter();
                case nameof(TileRegion):
                    return new TileRegionConverter();
                case nameof(TileSphere):
                    return new TileSphereConverter();
            }
            throw new JsonException();
        }
    }
    public class TileTransformConverter: JsonConverter<TileTransform> {
        public override bool CanConvert(Type objectType) {
            return objectType == typeof(TileTransform);
        }
        public override TileTransform Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            var values = JustAnArray.ReadArray(ref reader, TileTransform.ArraySize);
            return new TileTransform(values);
        }
        public override void Write(Utf8JsonWriter writer, TileTransform value, JsonSerializerOptions options) {
            JustAnArray.WriteArray(writer, value.GetArray());
        }
    }
    public class TileBoxConverter: JsonConverter<TileBox> {
        public override bool CanConvert(Type objectType) {
            return objectType == typeof(TileBox);
        }
        public override TileBox Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            var values = JustAnArray.ReadArray(ref reader, TileBox.ArraySize);
            return new TileBox(values);
        }
        public override void Write(Utf8JsonWriter writer, TileBox value, JsonSerializerOptions options) {
            JustAnArray.WriteArray(writer, value.GetArray());
        }
    }
    public class TileRegionConverter: JsonConverter<TileRegion> {
        public override bool CanConvert(Type objectType) {
            return objectType == typeof(TileRegion);
        }
        public override TileRegion Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            var values = JustAnArray.ReadArray(ref reader, TileRegion.ArraySize);
            return new TileRegion(values);
        }
        public override void Write(Utf8JsonWriter writer, TileRegion value, JsonSerializerOptions options) {
            JustAnArray.WriteArray(writer, value.GetArray());
        }
    }
    // TileBox
    // TileRegion
    public class TileSphereConverter: JsonConverter<TileSphere> {
        public override bool CanConvert(Type objectType) {
            return objectType == typeof(TileSphere);
        }
        public override TileSphere Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            var values = JustAnArray.ReadArray(ref reader, TileSphere.ArraySize);
            return new TileSphere(values);
        }
        public override void Write(Utf8JsonWriter writer, TileSphere value, JsonSerializerOptions options) {
            JustAnArray.WriteArray(writer, value.GetArray());
        }
    }

    public class TileExtensions : Dictionary<string, object> {
    }

    public class TileProperty {
        public double minimum;
        public double maximum;
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public TileExtensions extensions;
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public TileExtensions extras;
    }
    public class TileProperties : Dictionary<string, TileProperty> {
    }
}
