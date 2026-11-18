using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace HueGates.Logic
{
    public static class LevelCatalog
    {
        public static LevelSet Parse(string json)
        {
            JsonValue root = JsonValue.Parse(json);
            var set = new LevelSet();
            set.version = root.GetInt("version");
            set.game = root.GetString("game");
            List<JsonValue> rows = root.GetArray("levels");
            set.levels = new Level[rows.Count];
            for (int i = 0; i < rows.Count; i++) set.levels[i] = ReadLevel(rows[i]);
            return set;
        }

        public static Picture ParsePicture(string json)
        {
            JsonValue root = JsonValue.Parse(json);
            var picture = new Picture();
            picture.name = root.GetString("name");
            picture.order = root.Has("order") ? root.GetInt("order") : 0;
            picture.size = root.GetInt("size");
            List<JsonValue> rows = root.GetArray("pixels");
            picture.pixels = new string[rows.Count][];
            for (int y = 0; y < rows.Count; y++)
            {
                List<JsonValue> row = rows[y].Array();
                picture.pixels[y] = new string[row.Count];
                for (int x = 0; x < row.Count; x++) picture.pixels[y][x] = row[x].String();
            }
            List<JsonValue> pieces = root.GetArray("pieces");
            picture.pieces = new int[pieces.Count][];
            for (int p = 0; p < pieces.Count; p++)
            {
                List<JsonValue> piece = pieces[p].Array();
                picture.pieces[p] = new int[piece.Count];
                for (int i = 0; i < piece.Count; i++) picture.pieces[p][i] = piece[i].Int();
            }
            return picture;
        }

        static Level ReadLevel(JsonValue value)
        {
            var level = new Level();
            level.id = value.GetInt("id");
            level.size = value.GetInt("size");
            level.colors = value.GetInt("colors");
            level.lives = value.GetInt("lives");
            level.note = value.Has("note") ? value.GetString("note") : "";
            List<JsonValue> palette = value.GetArray("palette");
            level.palette = new string[palette.Count];
            for (int i = 0; i < palette.Count; i++) level.palette[i] = palette[i].String();
            JsonValue gates = value.GetObject("gates");
            level.gates = new Gates();
            level.gates.T = ReadInts(gates, "T");
            level.gates.B = ReadInts(gates, "B");
            level.gates.L = ReadInts(gates, "L");
            level.gates.R = ReadInts(gates, "R");
            List<JsonValue> arrows = value.GetArray("arrows");
            level.arrows = new Arrow[arrows.Count];
            for (int i = 0; i < arrows.Count; i++)
            {
                JsonValue arrow = arrows[i];
                level.arrows[i] = new Arrow();
                level.arrows[i].r = arrow.GetInt("r");
                level.arrows[i].c = arrow.GetInt("c");
                level.arrows[i].color = arrow.GetInt("color");
                level.arrows[i].dir = ParseDir(arrow.GetString("dir"));
            }
            if (value.Has("solution"))
            {
                level.solution = ReadInts(value, "solution");
            }
            return level;
        }

        static int[] ReadInts(JsonValue obj, string key)
        {
            List<JsonValue> values = obj.GetArray(key);
            var result = new int[values.Count];
            for (int i = 0; i < values.Count; i++) result[i] = values[i].Int();
            return result;
        }

        static Dir ParseDir(string dir)
        {
            switch (dir)
            {
                case "U": return Dir.U;
                case "D": return Dir.D;
                case "L": return Dir.L;
                case "R": return Dir.R;
                default: throw new FormatException("Unknown direction " + dir);
            }
        }
    }

    sealed class JsonValue
    {
        enum Kind { Null, Bool, Number, String, Array, Object }

        Kind kind;
        bool boolValue;
        double numberValue;
        string stringValue;
        List<JsonValue> arrayValue;
        Dictionary<string, JsonValue> objectValue;

        public static JsonValue Parse(string json)
        {
            var parser = new JsonParser(json);
            JsonValue value = parser.ParseValue();
            parser.Skip();
            if (!parser.End) throw new FormatException("Trailing data in JSON at " + parser.Index);
            return value;
        }

        public bool Has(string key)
        {
            return Object().ContainsKey(key);
        }

        public int GetInt(string key)
        {
            return Object()[key].Int();
        }

        public string GetString(string key)
        {
            return Object()[key].String();
        }

        public List<JsonValue> GetArray(string key)
        {
            return Object()[key].Array();
        }

        public JsonValue GetObject(string key)
        {
            JsonValue value = Object()[key];
            value.Object();
            return value;
        }

        public int Int()
        {
            if (kind != Kind.Number) throw new FormatException("Expected number");
            return (int)numberValue;
        }

        public string String()
        {
            if (kind != Kind.String) throw new FormatException("Expected string");
            return stringValue;
        }

        public List<JsonValue> Array()
        {
            if (kind != Kind.Array) throw new FormatException("Expected array");
            return arrayValue;
        }

        public Dictionary<string, JsonValue> Object()
        {
            if (kind != Kind.Object) throw new FormatException("Expected object");
            return objectValue;
        }

        public static JsonValue Number(double value)
        {
            return new JsonValue { kind = Kind.Number, numberValue = value };
        }

        public static JsonValue Text(string value)
        {
            return new JsonValue { kind = Kind.String, stringValue = value };
        }

        public static JsonValue Bool(bool value)
        {
            return new JsonValue { kind = Kind.Bool, boolValue = value };
        }

        public static JsonValue Null()
        {
            return new JsonValue { kind = Kind.Null };
        }

        public static JsonValue Array(List<JsonValue> value)
        {
            return new JsonValue { kind = Kind.Array, arrayValue = value };
        }

        public static JsonValue Object(Dictionary<string, JsonValue> value)
        {
            return new JsonValue { kind = Kind.Object, objectValue = value };
        }
    }

    sealed class JsonParser
    {
        readonly string text;
        int index;

        public JsonParser(string text)
        {
            this.text = text;
        }

        public int Index { get { return index; } }
        public bool End { get { return index >= text.Length; } }

        public void Skip()
        {
            while (index < text.Length && char.IsWhiteSpace(text[index])) index++;
        }

        public JsonValue ParseValue()
        {
            Skip();
            if (index >= text.Length) throw new FormatException("Unexpected end of JSON");
            char c = text[index];
            if (c == '{') return ParseObject();
            if (c == '[') return ParseArray();
            if (c == '"') return JsonValue.Text(ParseString());
            if (c == 't') { Expect("true"); return JsonValue.Bool(true); }
            if (c == 'f') { Expect("false"); return JsonValue.Bool(false); }
            if (c == 'n') { Expect("null"); return JsonValue.Null(); }
            return JsonValue.Number(ParseNumber());
        }

        JsonValue ParseObject()
        {
            index++;
            var map = new Dictionary<string, JsonValue>();
            Skip();
            if (Peek('}')) { index++; return JsonValue.Object(map); }
            while (true)
            {
                Skip();
                string key = ParseString();
                Skip();
                if (index >= text.Length || text[index] != ':') throw new FormatException("Expected ':'");
                index++;
                map[key] = ParseValue();
                Skip();
                if (Peek('}')) { index++; break; }
                if (!Peek(',')) throw new FormatException("Expected ',' in object");
                index++;
            }
            return JsonValue.Object(map);
        }

        JsonValue ParseArray()
        {
            index++;
            var list = new List<JsonValue>();
            Skip();
            if (Peek(']')) { index++; return JsonValue.Array(list); }
            while (true)
            {
                list.Add(ParseValue());
                Skip();
                if (Peek(']')) { index++; break; }
                if (!Peek(',')) throw new FormatException("Expected ',' in array");
                index++;
            }
            return JsonValue.Array(list);
        }

        string ParseString()
        {
            if (index >= text.Length || text[index] != '"') throw new FormatException("Expected string");
            index++;
            var builder = new StringBuilder();
            while (index < text.Length)
            {
                char c = text[index++];
                if (c == '"') return builder.ToString();
                if (c != '\\') { builder.Append(c); continue; }
                if (index >= text.Length) throw new FormatException("Bad escape");
                char e = text[index++];
                switch (e)
                {
                    case '"':
                    case '\\':
                    case '/': builder.Append(e); break;
                    case 'b': builder.Append('\b'); break;
                    case 'f': builder.Append('\f'); break;
                    case 'n': builder.Append('\n'); break;
                    case 'r': builder.Append('\r'); break;
                    case 't': builder.Append('\t'); break;
                    case 'u':
                        if (index + 4 > text.Length) throw new FormatException("Bad unicode escape");
                        int code = int.Parse(text.Substring(index, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                        builder.Append((char)code);
                        index += 4;
                        break;
                    default: throw new FormatException("Bad escape");
                }
            }
            throw new FormatException("Unterminated string");
        }

        double ParseNumber()
        {
            int start = index;
            if (Peek('-')) index++;
            while (index < text.Length && char.IsDigit(text[index])) index++;
            if (Peek('.'))
            {
                index++;
                while (index < text.Length && char.IsDigit(text[index])) index++;
            }
            if (Peek('e') || Peek('E'))
            {
                index++;
                if (Peek('+') || Peek('-')) index++;
                while (index < text.Length && char.IsDigit(text[index])) index++;
            }
            string slice = text.Substring(start, index - start);
            return double.Parse(slice, CultureInfo.InvariantCulture);
        }

        void Expect(string token)
        {
            if (index + token.Length > text.Length || text.Substring(index, token.Length) != token)
                throw new FormatException("Expected " + token);
            index += token.Length;
        }

        bool Peek(char c)
        {
            return index < text.Length && text[index] == c;
        }
    }
}
